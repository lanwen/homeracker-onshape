"""Compare STEP (or STL) solids with SCAD meshes in the same millimeter frame.

No registration, centering, or scaling is performed. A sampled surface-distance
check complements volume/bounds checks; it is not a proof of exact equivalence.
"""
import argparse
import hashlib
import json
from pathlib import Path
import tempfile

import numpy as np
import trimesh

ROOT = Path(__file__).resolve().parents[1]


def step_to_mesh(path):
    from OCP.BRepCheck import BRepCheck_Analyzer
    from OCP.BRepMesh import BRepMesh_IncrementalMesh
    from OCP.IFSelect import IFSelect_RetDone
    from OCP.STEPControl import STEPControl_Reader
    from OCP.StlAPI import StlAPI_Writer

    reader = STEPControl_Reader()
    if reader.ReadFile(str(path)) != IFSelect_RetDone or reader.TransferRoots() == 0:
        raise ValueError(f"Could not read STEP geometry: {path}")
    shape = reader.OneShape()
    if shape.IsNull() or not BRepCheck_Analyzer(shape).IsValid():
        raise ValueError(f"Invalid STEP B-rep: {path}")
    mesher = BRepMesh_IncrementalMesh(shape, 0.005, False, 0.05, True)
    if not mesher.IsDone():
        raise ValueError(f"STEP tessellation failed: {path}")
    with tempfile.TemporaryDirectory(prefix="homeracker-step-") as temp:
        stl = Path(temp) / "part.stl"
        writer = StlAPI_Writer()
        writer.ASCIIMode = False
        if not writer.Write(shape, str(stl)):
            raise ValueError(f"STEP mesh export failed: {path}")
        return load_mesh(stl)


def load_mesh(path):
    path = Path(path)
    if path.suffix.lower() in (".step", ".stp"):
        return step_to_mesh(path)
    mesh = trimesh.load(path, force="mesh", process=True)
    if not isinstance(mesh, trimesh.Trimesh) or len(mesh.faces) == 0:
        raise ValueError(f"Empty or invalid mesh: {path}")
    if not np.isfinite(mesh.vertices).all():
        raise ValueError(f"Non-finite mesh coordinates: {path}")
    return mesh


def mesh_stats(mesh):
    return {
        "bounds_mm": mesh.bounds.tolist(),
        "extents_mm": mesh.extents.tolist(),
        "volume_mm3": float(mesh.volume),
        "surface_area_mm2": float(mesh.area),
        "watertight": bool(mesh.is_watertight),
        "winding_consistent": bool(mesh.is_winding_consistent),
        "components": int(trimesh.graph.connected_component_labels(
            mesh.face_adjacency, node_count=len(mesh.faces)).max() + 1),
        "vertices": len(mesh.vertices),
        "triangles": len(mesh.faces),
    }


def surface_distances(source, target, samples):
    points, _ = trimesh.sample.sample_surface(source, count=samples, seed=1947)
    points = np.vstack([source.vertices, source.triangles_center, points])
    distances = []
    for start in range(0, len(points), 512):
        _, values, _ = trimesh.proximity.closest_point(target, points[start:start + 512])
        distances.append(values)
    values = np.concatenate(distances)
    return {"max_mm": float(values.max()), "p99_mm": float(np.percentile(values, 99)),
            "rms_mm": float(np.sqrt(np.mean(values ** 2))), "points": len(values)}


def compare_meshes(reference, candidate, distance_tol=0.03, volume_tol=0.001, samples=8000):
    ref_stats, cand_stats = mesh_stats(reference), mesh_stats(candidate)
    ref_volume = ref_stats["volume_mm3"]
    volume_error = abs(cand_stats["volume_mm3"] - ref_volume) / max(abs(ref_volume), 1e-12)
    bounds_error = float(np.abs(reference.bounds - candidate.bounds).max())
    forward = surface_distances(reference, candidate, samples)
    backward = surface_distances(candidate, reference, samples)
    checks = {
        "valid_reference": ref_stats["watertight"] and ref_stats["winding_consistent"] and ref_volume > 0,
        "valid_candidate": cand_stats["watertight"] and cand_stats["winding_consistent"] and cand_stats["volume_mm3"] > 0,
        "component_count": ref_stats["components"] == cand_stats["components"],
        "bounds": bounds_error <= distance_tol,
        "volume": volume_error <= volume_tol,
        "surface": max(forward["max_mm"], backward["max_mm"]) <= distance_tol,
    }
    return {"status": "pass" if all(checks.values()) else "fail", "checks": checks,
            "thresholds": {"distance_mm": distance_tol, "relative_volume": volume_tol},
            "bounds_error_mm": bounds_error, "relative_volume_error": volume_error,
            "reference_to_candidate": forward, "candidate_to_reference": backward,
            "reference": ref_stats, "candidate": cand_stats}


def compare_files(reference, candidate, **kwargs):
    result = compare_meshes(load_mesh(reference), load_mesh(candidate), **kwargs)
    return {"reference_file": str(reference), "candidate_file": str(candidate),
            "reference_sha256": hashlib.sha256(Path(reference).read_bytes()).hexdigest(),
            "candidate_sha256": hashlib.sha256(Path(candidate).read_bytes()).hexdigest(), **result}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("reference", type=Path, nargs="?")
    parser.add_argument("candidate", type=Path, nargs="?")
    parser.add_argument("--batch", action="store_true")
    parser.add_argument("--case", action="append")
    parser.add_argument("--distance-tol", type=float, default=0.03)
    parser.add_argument("--volume-tol", type=float, default=0.001)
    parser.add_argument("--samples", type=int, default=8000)
    parser.add_argument("--output", type=Path, default=ROOT / "artifacts/comparison.json")
    args = parser.parse_args()
    if args.distance_tol <= 0 or args.volume_tol <= 0 or args.samples < 1:
        parser.error("Tolerances and sample count must be positive")
    options = dict(distance_tol=args.distance_tol, volume_tol=args.volume_tol, samples=args.samples)
    if args.batch:
        manifest = json.loads((ROOT / "comparisons/cases.json").read_text())
        cases = [c for c in manifest["cases"] if not args.case or c["name"] in args.case]
        if not cases or args.case and set(args.case) != {c["name"] for c in cases}:
            parser.error("Unknown case name")
        results = {}
        for case in cases:
            name = case["name"]
            ref = ROOT / "artifacts/reference" / f"{name}.stl"
            ref_metadata = ref.with_suffix(".json")
            candidate = ROOT / "artifacts/onshape" / f"{name}.step"
            if not ref.exists() or not candidate.exists() or not ref_metadata.exists():
                results[name] = {"status": "pending", "missing": [str(p) for p in (ref, ref_metadata, candidate) if not p.exists()]}
            else:
                provenance = json.loads(ref_metadata.read_text())
                if (provenance["status"] != "valid" or
                    provenance["upstream_commit"] != manifest["upstream_commit"] or
                    provenance["case"] != case or
                    provenance["sha256"] != hashlib.sha256(ref.read_bytes()).hexdigest()):
                    results[name] = {"status": "invalid_reference", "metadata": str(ref_metadata)}
                else:
                    results[name] = {"provenance": provenance, **compare_files(ref, candidate, **options)}
            print(f"{name}: {results[name]['status']}", flush=True)
        result = {"cases": results, "status": "pass" if all(r["status"] == "pass" for r in results.values()) else "incomplete_or_failed"}
    else:
        if not args.reference or not args.candidate:
            parser.error("Supply reference and candidate, or --batch")
        result = compare_files(args.reference, args.candidate, **options)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2) + "\n")
    print(f"Report: {args.output}")
    raise SystemExit(0 if result["status"] == "pass" else 1)


if __name__ == "__main__":
    main()
