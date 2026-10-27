import pytest
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "scripts"))

from versioning import parse_version


@pytest.mark.parametrize("value", ["0.1.0", "1.0.0", "10.20.300"])
def test_stable_semver(value):
    assert ".".join(map(str, parse_version(value))) == value


@pytest.mark.parametrize("value", ["v1.0.0", "01.0.0", "1.0", "1.0.0-rc.1", "1.0.0\n", "1.0.-1"])
def test_rejects_ambiguous_or_non_release_versions(value):
    with pytest.raises(ValueError, match="MAJOR.MINOR.PATCH"):
        parse_version(value)
