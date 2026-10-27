"""Physical-interface checks against generated, unmodified upstream geometry."""
import hashlib
import io
import json
from pathlib import Path
import sys
import tarfile

import numpy as np
import pytest

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "scripts"))
from compare import load_mesh
from reference import verify_bosl
from setup import DOWNLOADS


def test_bosl_source_tampering_is_rejected(tmp_path, monkeypatch):
    archive = tmp_path / "BOSL2-v2.0.774.tar.gz"
    source = b"module example() {}\n"
    with tarfile.open(archive, "w:gz") as tar:
        member = tarfile.TarInfo("BOSL2-2.0.774/std.scad")
        member.size = len(source)
        tar.addfile(member, io.BytesIO(source))
    monkeypatch.setitem(DOWNLOADS, archive.name, ("unused", hashlib.sha256(archive.read_bytes()).hexdigest()))
    library = tmp_path / "BOSL2"
    library.mkdir()
    (library / "std.scad").write_bytes(source)
    assert verify_bosl(library, archive)["version"] == "v2.0.774"
    (library / "std.scad").write_text("// modified\n")
    with pytest.raises(ValueError, match="source differs"):
        verify_bosl(library, archive)


def reference(name):
    path = ROOT / "artifacts/reference" / f"{name}.stl"
    if not path.exists():
        pytest.skip("Run mise run reference for upstream geometry checks")
    metadata = json.loads(path.with_suffix(".json").read_text())
    assert metadata["status"] == "valid"
    assert metadata["sha256"] == hashlib.sha256(path.read_bytes()).hexdigest()
    return load_mesh(path)


@pytest.mark.parametrize("name", ["support_1u", "support_3u", "support_3u_cross"])
def test_support_pin_holes_are_open(name):
    mesh = reference(name)
    origins = [[0, y, 20] for y in ([0] if name == "support_1u" else [-15, 0, 15])]
    locations, _, _ = mesh.ray.intersects_location(origins, [[0, 0, -1]] * len(origins))
    assert len(locations) == 0, "Pin holes must be continuous; no caps or centre membrane"


@pytest.mark.parametrize("name", ["connector_5way", "connector_6way"])
def test_connector_socket_openings_reach_core_stop(name):
    mesh = reference(name)
    origins = np.eye(3) * 30
    directions = -np.eye(3)
    locations, index_ray, _ = mesh.ray.intersects_location(origins, directions)
    for axis in range(3):
        # A pin-free socket centerline should first touch its stop at +7.5 mm,
        # not a stray cap at the +22.5 mm arm entrance.
        assert max(locations[index_ray == axis, axis]) == pytest.approx(7.5, abs=0.001)


def test_reference_meshes_have_one_continuous_surface():
    manifest = json.loads((ROOT / "comparisons/cases.json").read_text())
    for case in manifest["cases"]:
        reference(case["name"])
        metadata = json.loads((ROOT / "artifacts/reference" / f"{case['name']}.json").read_text())
        assert metadata["components"] == 1, case["name"]
