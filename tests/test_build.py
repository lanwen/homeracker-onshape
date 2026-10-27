from pathlib import Path
import json
import sys

import pytest

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "scripts"))
from build import MODULES, bundle_sources, write_bundle


@pytest.fixture
def project(tmp_path):
    source = tmp_path / "src"
    source.mkdir()
    for name in reversed(MODULES):
        text = (f'FeatureScript 3095;\nimport(path : "onshape/std/geometry.fs", version : "3095.0");\n// License\n'
                if name == "preamble.fs" else f"// {name}\nconst {name[:-3].upper()}_MARKER = 1;\n")
        (source / name).write_text(text)
    return tmp_path


def test_bundle_uses_dependency_order_and_maps_source_lines(project):
    code, mapping = bundle_sources(project / "src")
    assert [m["source"] for m in mapping["modules"]] == [f"src/{name}" for name in MODULES]
    lines = code.splitlines()
    for module in mapping["modules"]:
        actual = "\n".join(lines[module["start_line"] - 1:module["end_line"]]) + "\n"
        expected = (project / module["source"]).read_text()
        assert actual == expected
    assert code.count("FeatureScript 3095;") == 1
    assert code.count("import(") == 1


def test_repeated_build_is_identical_and_check_does_not_overwrite_stale_output(project):
    first = write_bundle(project)
    files = {p.name: p.read_bytes() for p in (project / "dist").iterdir()}
    assert write_bundle(project) == first
    assert files == {p.name: p.read_bytes() for p in (project / "dist").iterdir()}
    write_bundle(project, check=True)
    (project / "src/support.fs").write_text("// changed source\n")
    with pytest.raises(ValueError, match="stale"):
        write_bundle(project, check=True)
    assert (project / "dist/homeracker.fs").read_text() == first


def test_check_rejects_missing_bundle_and_stale_line_map(project):
    with pytest.raises(ValueError, match="missing or stale"):
        write_bundle(project, check=True)
    write_bundle(project)
    mapping = project / "dist/homeracker.sources.json"
    mapping.write_text("{}\n")
    with pytest.raises(ValueError, match="homeracker.sources.json"):
        write_bundle(project, check=True)


@pytest.mark.parametrize("text", ["FeatureScript 3095;", 'import(path : "other", version : "");'])
def test_modules_cannot_add_another_header_or_import(project, text):
    (project / "src/support.fs").write_text(text + "\n")
    with pytest.raises(ValueError, match="preamble.fs"):
        bundle_sources(project / "src")


def test_mismatched_versions_fail(project):
    preamble = project / "src/preamble.fs"
    preamble.write_text(preamble.read_text().replace('"3095.0"', '"3094.0"'))
    with pytest.raises(ValueError, match="matching"):
        bundle_sources(project / "src")


def test_project_version_is_rendered_without_changing_source_or_line_mapping(project):
    preamble = project / "src/preamble.fs"
    preamble.write_text(preamble.read_text() + "// HomeRacker v{{PROJECT_VERSION}}\n")
    (project / "VERSION").write_text("0.1.0\n")
    original = preamble.read_text()
    code, mapping = bundle_sources(project / "src")
    assert "// HomeRacker v0.1.0" in code
    assert mapping["version"] == "0.1.0"
    released, released_mapping = bundle_sources(project / "src", version="0.2.3")
    assert "// HomeRacker v0.2.3" in released
    assert released_mapping["version"] == "0.2.3"
    assert mapping["modules"] == released_mapping["modules"]
    assert mapping["sha256"] != released_mapping["sha256"]
    assert preamble.read_text() == original


def test_validation_rebuilds_bundle_from_current_sources(project, monkeypatch):
    import validation

    (project / "comparisons").mkdir()
    (project / "comparisons/cases.json").write_text(json.dumps({
        "cases": [{"name": "support_3u", "onshape": {"kind": "SUPPORT", "supportUnits": 3, "xHoles": False}}]
    }))
    write_bundle(project)
    (project / "src/support.fs").write_text("// fresh support implementation\n")
    monkeypatch.setattr(validation, "ROOT", project)
    monkeypatch.setattr(sys, "argv", ["validation.py", "--case", "support_3u"])
    validation.main()
    fixture = (project / "artifacts/homeracker-validation.fs").read_text()
    assert "// fresh support implementation" in fixture
    assert fixture.startswith((project / "dist/homeracker.fs").read_text())
    write_bundle(project, check=True)
