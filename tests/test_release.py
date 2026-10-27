import hashlib
from pathlib import Path
import sys

import pytest

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "scripts"))
from build import MODULES, write_bundle
from release import prepare_release, release_tag, select_version, release_pages


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


def test_release_tag_uses_semver():
    assert release_tag("0.1.0") == "v0.1.0"
    with pytest.raises(ValueError, match="MAJOR.MINOR.PATCH"):
        release_tag("a" * 40)


def test_automatic_patch_manual_minor_and_rerun_versions():
    first_sha, next_sha = "a" * 40, "b" * 40
    releases = [{"tag_name": "bundle-" + first_sha, "target_commitish": first_sha}]
    assert select_version("0.1.0", next_sha, releases) == "0.1.0"
    releases.append({"tag_name": "v0.1.0", "target_commitish": first_sha})
    assert select_version("0.1.0", next_sha, releases) == "0.1.1"
    assert select_version("0.2.0", next_sha, releases) == "0.2.0"
    assert select_version("1.0.0", next_sha, releases) == "1.0.0"
    assert select_version("0.1.0", first_sha, releases) == "0.1.0"
    releases.append({"tag_name": "v0.1.9", "target_commitish": "c" * 40})
    assert select_version("0.1.0", next_sha, releases) == "0.1.10"
    assert select_version("0.1.0", first_sha, releases) == "0.1.0"
    with pytest.raises(ValueError, match="full lowercase"):
        select_version("0.1.0", "abc", releases)


def test_release_selection_reads_all_paginated_results():
    assert release_pages('[{"tag_name":"v0.1.0"}]\n[{"tag_name":"v0.2.0"}]\n') == [
        {"tag_name": "v0.1.0"}, {"tag_name": "v0.2.0"}]


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
