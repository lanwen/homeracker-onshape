import hashlib
from pathlib import Path
import sys

import pytest

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "scripts"))
from build import MODULES, write_bundle
from release import prepare_release, release_tag


@pytest.fixture
def project(tmp_path):
    source = tmp_path / "src"
    source.mkdir()
    for name in MODULES:
        text = ("FeatureScript 3095;\n"
                'import(path : "onshape/std/geometry.fs", version : "3095.0");\n'
                if name == "preamble.fs" else f"// {name}\n")
        (source / name).write_text(text, encoding="utf-8")
    return tmp_path


def test_release_tag_requires_exact_full_commit():
    sha = "a" * 40
    assert release_tag(sha) == "bundle-" + sha
    for invalid in ("a" * 12, "A" * 40, "g" * 40, "a" * 41, ""):
        with pytest.raises(ValueError, match="full lowercase"):
            release_tag(invalid)


def test_prepare_release_checksums_built_assets_and_logo(project):
    write_bundle(project)
    logo = project / "assets/homeracker-logo.svg"
    logo.parent.mkdir()
    logo.write_text("<svg xmlns='http://www.w3.org/2000/svg'/>\n")
    assets = prepare_release(project)
    assert [p.name for p in assets] == [
        "homeracker.fs", "homeracker.sources.json", "logo.svg", "SHA256SUMS"]
    assert (project / "dist/logo.svg").read_bytes() == logo.read_bytes()
    lines = assets[-1].read_text().splitlines()
    assert lines == [f"{hashlib.sha256(path.read_bytes()).hexdigest()}  {path.name}"
                     for path in assets[:-1]]
    assert prepare_release(project) == assets
    assert assets[-1].read_text().splitlines() == lines


def test_prepare_release_rejects_missing_or_stale_bundle(project):
    with pytest.raises(ValueError, match="missing or stale"):
        prepare_release(project)
    write_bundle(project)
    (project / "src/support.fs").write_text("// changed\n")
    with pytest.raises(ValueError, match="stale"):
        prepare_release(project)


def test_prepare_release_drops_old_logo_when_source_removed(project):
    write_bundle(project)
    staged = project / "dist/logo.svg"
    staged.write_text("obsolete")
    assets = prepare_release(project)
    assert [p.name for p in assets] == ["homeracker.fs", "homeracker.sources.json", "SHA256SUMS"]
    assert not staged.exists()
