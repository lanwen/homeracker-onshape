from pathlib import Path
import re

import yaml

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github/workflows/release.yml"


def workflow():
    # BaseLoader keeps the YAML key "on" as text rather than YAML 1.1 boolean True.
    return yaml.load(WORKFLOW.read_text(), Loader=yaml.BaseLoader)


def test_bundle_inputs_trigger_releases_but_other_docs_and_tests_do_not():
    data = workflow()
    pushes = data["on"]["push"]
    assert pushes["branches"] == ["main"]
    paths = pushes["paths"]
    assert "src/**" in paths and "assets/**" in paths
    assert "scripts/build.py" in paths and "scripts/release.py" in paths
    assert "docs/guide.md" in paths
    assert "README.md" not in paths
    assert "scripts/guide.py" in paths
    assert "docs/development.md" not in paths
    assert "docs/compatibility.md" not in paths
    assert "requirements-ci.txt" in paths
    assert not any(path.startswith("tests/")
                   for path in paths)
    assert "tests/**" in data["on"]["pull_request"]["paths"]
    assert "workflow_dispatch" in data["on"]


def test_release_is_gated_and_serialized_without_canceling_pending_pushes():
    data = workflow()
    assert data["permissions"] == {"contents": "read"}
    release = data["jobs"]["release"]
    assert release["needs"] == "validate"
    assert "refs/heads/main" in release["if"]
    assert release["permissions"] == {"contents": "write"}
    assert release["concurrency"]["queue"] == "max"
    assert release["concurrency"]["cancel-in-progress"] == "false"
    publish = release["steps"][-1]["run"]
    assert "--clobber" in publish and "--target \"$GITHUB_SHA\"" in publish


def test_every_external_action_is_commit_pinned_and_mise_cache_is_enabled():
    data = workflow()
    for job in data["jobs"].values():
        for step in job["steps"]:
            if "uses" not in step:
                continue
            assert re.fullmatch(r"[\w-]+/[\w-]+@[0-9a-f]{40}", step["uses"])
            if step["uses"].startswith("jdx/mise-action@"):
                assert step["with"]["cache"] == "true"
