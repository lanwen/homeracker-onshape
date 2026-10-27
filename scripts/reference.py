"""Render pinned upstream source directly, recording inputs and mesh measurements."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import tarfile

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "scripts"))
from compare import load_mesh, mesh_stats
from setup import DOWNLOADS


def verify_bosl(bosl, archive):
    """Require geometry sources to match the checksum-verified pinned archive."""
    expected_hash = DOWNLOADS[archive.name][1]
    if not archive.exists() or hashlib.sha256(archive.read_bytes()).hexdigest() != expected_hash:
        raise ValueError("Pinned BOSL2 archive missing or changed; run mise run setup")
    hashes = []
    expected_paths = set()
    with tarfile.open(archive) as tar:
        for member in tar.getmembers():
            if not member.isfile() or not member.name.endswith(".scad"):
                continue
            relative = Path(*Path(member.name).parts[1:])
            expected_paths.add(relative)
            expected = hashlib.sha256(tar.extractfile(member).read()).hexdigest()
            local = bosl / relative
            if not local.is_file() or hashlib.sha256(local.read_bytes()).hexdigest() != expected:
                raise ValueError(f"BOSL2 source differs from v2.0.774: {relative}")
            hashes.append(f"{relative.as_posix()} {expected}")
    actual_paths = {p.relative_to(bosl) for p in bosl.rglob("*.scad") if p.is_file()}
    if actual_paths != expected_paths:
        raise ValueError("BOSL2 SCAD file set differs from v2.0.774")
    return {"version": "v2.0.774", "archive_sha256": expected_hash,
            "scad_tree_sha256": hashlib.sha256("\n".join(sorted(hashes)).encode()).hexdigest()}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--upstream", type=Path, default=Path(os.environ.get("HOMERACKER_SOURCE", ROOT.parent / "homeracker")))
    parser.add_argument("--case", action="append", help="Render only named cases (repeatable)")
    parser.add_argument("--backend", choices=["manifest", "CGAL", "Manifold"], default="manifest")
    args = parser.parse_args()
    upstream = args.upstream.resolve()
    manifest = json.loads((ROOT / "comparisons/cases.json").read_text())
    commit = subprocess.check_output(["git", "-C", str(upstream), "rev-parse", "HEAD"], text=True).strip()
    if commit != manifest["upstream_commit"]:
        raise SystemExit(f"Upstream revision mismatch: expected {manifest['upstream_commit']}, got {commit}")
    changed = subprocess.check_output(["git", "-C", str(upstream), "status", "--porcelain", "--", "models/core/lib", "models/panel/lib"], text=True)
    if changed:
        raise SystemExit("Upstream geometry has uncommitted changes; use a clean pinned checkout")
    executable = os.environ.get("OPENSCAD", str(ROOT / ".tools/OpenSCAD.app/Contents/MacOS/OpenSCAD"))
    bosl = Path(os.environ.get("BOSL2_PATH", ROOT / ".tools/libraries/BOSL2")).resolve()
    bosl_provenance = verify_bosl(bosl, ROOT / ".tools/downloads/BOSL2-v2.0.774.tar.gz")
    env = dict(os.environ, OPENSCADPATH=os.pathsep.join([str(bosl.parent), str(upstream)]))
    version = subprocess.run([executable, "--version"], capture_output=True, text=True, check=True)
    version_text = (version.stdout + version.stderr).strip()
    if "2026.10.23" not in version_text:
        raise SystemExit(f"Reference renderer version mismatch: {version_text}")
    cases = [c for c in manifest["cases"] if not args.case or c["name"] in args.case]
    if args.case and set(args.case) != {c["name"] for c in cases}:
        raise SystemExit("Unknown case name")
    out = ROOT / ("artifacts/reference" if args.backend == "manifest" else f"artifacts/reference-{args.backend.lower()}")
    out.mkdir(parents=True, exist_ok=True)
    invalid = []
    for case in cases:
        name = case["name"]
        backend = case.get("backend", "Manifold") if args.backend == "manifest" else args.backend
        source = out / f"{name}.scad"
        source.write_text(f'include <{upstream / case["module"]}>\n$fn=64;\n{case["call"]}\n')
        stl = out / f"{name}.stl"
        result = subprocess.run([executable, f"--backend={backend}", "--export-format", "binstl", "-o", str(stl), str(source)], env=env, capture_output=True, text=True)
        (out / f"{name}.log").write_text(result.stdout + result.stderr)
        if result.returncode or "ERROR:" in result.stderr or "WARNING:" in result.stderr:
            raise SystemExit(f"SCAD reference {name} failed; see {out / (name + '.log')}")
        mesh = load_mesh(stl)
        stats = mesh_stats(mesh)
        record = {"case": case, "upstream_commit": commit, "openscad": version_text,
                  "bosl2": bosl_provenance, "backend": backend,
                  "source_sha256": hashlib.sha256((upstream / case["module"]).read_bytes()).hexdigest(),
                  "sha256": hashlib.sha256(stl.read_bytes()).hexdigest(), **stats}
        record["status"] = "valid" if stats["watertight"] and stats["winding_consistent"] and stats["volume_mm3"] > 0 else "invalid_reference"
        if record["status"] != "valid":
            invalid.append(name)
        (out / f"{name}.json").write_text(json.dumps(record, indent=2) + "\n")
        print(f"{name}: {record['status']}, {stats['volume_mm3']:.3f} mm³, {stats['components']} surface component(s)", flush=True)
    if invalid:
        raise SystemExit("Invalid reference meshes: " + ", ".join(invalid))


if __name__ == "__main__":
    main()
