from pathlib import Path
import sys

import numpy as np
import trimesh

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "scripts"))
from compare import compare_meshes, load_mesh


def test_same_shape_passes_despite_triangle_subdivision():
    box = trimesh.creation.box([15, 45, 15])
    result = compare_meshes(box, box.subdivide(), samples=100)
    assert result["status"] == "pass"


def test_translation_is_not_hidden_by_registration():
    box = trimesh.creation.box([15, 45, 15])
    shifted = box.copy()
    shifted.apply_translation([0.2, 0, 0])
    result = compare_meshes(box, shifted, samples=100)
    assert result["status"] == "fail"
    assert not result["checks"]["bounds"]


def test_moved_hole_with_identical_bounds_and_volume_fails():
    # A through-hole square tube; translating only its cavity changes fit while
    # preserving both total volume and the external bounding box.
    from OCP.BRepAlgoAPI import BRepAlgoAPI_Cut
    from OCP.BRepPrimAPI import BRepPrimAPI_MakeBox
    from OCP.gp import gp_Pnt
    from OCP.STEPControl import STEPControl_AsIs, STEPControl_Writer
    import tempfile

    outer = BRepPrimAPI_MakeBox(gp_Pnt(-7.5, -7.5, -7.5), 15, 15, 15).Shape()
    with tempfile.TemporaryDirectory() as temp:
        meshes = []
        for offset in (0, 0.4):
            cavity = BRepPrimAPI_MakeBox(gp_Pnt(-2 + offset, -2, -8), 4, 4, 16).Shape()
            shape = BRepAlgoAPI_Cut(outer, cavity).Shape()
            path = Path(temp) / f"{offset}.step"
            writer = STEPControl_Writer()
            writer.Transfer(shape, STEPControl_AsIs)
            writer.Write(str(path))
            meshes.append(load_mesh(path))
        result = compare_meshes(*meshes, samples=300)
    assert result["checks"]["volume"]
    assert result["checks"]["bounds"]
    assert not result["checks"]["surface"]
    assert result["status"] == "fail"


def test_inverted_winding_fails_even_for_same_surface():
    box = trimesh.creation.box([15, 45, 15])
    inverted = box.copy()
    inverted.invert()
    result = compare_meshes(box, inverted, samples=100)
    assert not result["checks"]["valid_candidate"]


def test_units_error_is_detected():
    box = trimesh.creation.box([15, 45, 15])
    scaled = box.copy()
    scaled.apply_scale(0.001)
    result = compare_meshes(box, scaled, samples=100)
    assert not result["checks"]["bounds"]
    assert not result["checks"]["volume"]
