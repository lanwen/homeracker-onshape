"""Prepare deterministic GitHub Release assets from a checked local bundle."""

import argparse
import hashlib
from pathlib import Path
import re
import shutil

from build import write_bundle

ROOT = Path(__file__).resolve().parents[1]
ASSET_NAMES = ("homeracker.fs", "homeracker.sources.json")
LOGO_SOURCE = Path("assets/homeracker-logo.svg")


def release_tag(sha: str) -> str:
    if not re.fullmatch(r"[0-9a-f]{40}", sha):
        raise ValueError("Release commit must be a full lowercase 40-character SHA")
    return f"bundle-{sha}"


def prepare_release(root: Path = ROOT) -> list[Path]:
    """Check the bundle, copy the optional logo, and write checksums."""
    write_bundle(root, check=True)
    dist = root / "dist"
    assets = [dist / name for name in ASSET_NAMES]
    logo = root / LOGO_SOURCE
    staged_logo = dist / "logo.svg"
    if logo.is_file():
        if not logo.read_bytes().strip():
            raise ValueError(f"Empty logo asset: {logo}")
        shutil.copyfile(logo, staged_logo)
        assets.append(staged_logo)
    elif staged_logo.exists():
        staged_logo.unlink()
    checksums = dist / "SHA256SUMS"
    lines = [f"{hashlib.sha256(path.read_bytes()).hexdigest()}  {path.name}"
             for path in assets]
    checksums.write_text("\n".join(lines) + "\n", encoding="ascii")
    return [*assets, checksums]


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    group = parser.add_mutually_exclusive_group(required=True)
    group.add_argument("--prepare", action="store_true")
    group.add_argument("--tag", metavar="SHA")
    args = parser.parse_args()
    try:
        if args.prepare:
            for path in prepare_release():
                print(path.relative_to(ROOT))
        else:
            print(release_tag(args.tag))
    except ValueError as error:
        parser.exit(1, f"{error}\n")


if __name__ == "__main__":
    main()
