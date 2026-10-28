"""Prepare deterministic GitHub Release assets from a checked local bundle."""

import argparse
import hashlib
import json
from pathlib import Path
import re
import shutil
import subprocess

from build import write_bundle
from guide import write_guide
from versioning import parse_version, read_version

ROOT = Path(__file__).resolve().parents[1]
ASSET_NAMES = ("homeracker.fs", "homeracker.sources.json")
LOGO_SOURCE = Path("assets/homeracker-logo.svg")


def release_tag(version: str) -> str:
    parse_version(version)
    return f"v{version}"


def select_version(base: str, sha: str, releases: list[dict]) -> str:
    """Reuse a release for this commit; otherwise honor the base or bump patch."""
    base_parts = parse_version(base)
    if not re.fullmatch(r"[0-9a-f]{40}", sha):
        raise ValueError("Release commit must be a full lowercase 40-character SHA")
    versions = []
    for release in releases:
        tag = release.get("tag_name", "")
        if not tag.startswith("v"):
            continue
        try:
            parts = parse_version(tag[1:])
        except ValueError:
            continue
        if release.get("target_commitish") == sha:
            return tag[1:]
        versions.append(parts)
    latest = max(versions, default=None)
    if latest is None or base_parts > latest:
        return base
    major, minor, patch = latest
    return f"{major}.{minor}.{patch + 1}"


def release_pages(output: str) -> list[dict]:
    """gh api --paginate prints one JSON array per page, even on older gh."""
    decoder = json.JSONDecoder()
    releases = []
    while output.strip():
        output = output.lstrip()
        page, end = decoder.raw_decode(output)
        releases.extend(page)
        output = output[end:]
    return releases


def prepare_release(root: Path = ROOT, version: str | None = None) -> list[Path]:
    """Check the bundle, render its guide, copy the optional logo, and write checksums."""
    write_bundle(root, check=True, version=version)
    dist = root / "dist"
    assets = [dist / name for name in ASSET_NAMES]
    bundle_version = json.loads((dist / "homeracker.sources.json").read_text()).get("version")
    assets.append(write_guide(root / "docs/guide.md", dist / "homeracker-guide.pdf",
                              version or bundle_version or read_version(root)))
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
    group.add_argument("--tag", metavar="VERSION")
    group.add_argument("--resolve", metavar="SHA", help="Select the next version from GitHub releases")
    parser.add_argument("--version", help="Version used when building release assets")
    args = parser.parse_args()
    try:
        if args.prepare:
            for path in prepare_release(version=args.version):
                print(path.relative_to(ROOT))
        elif args.resolve:
            result = subprocess.run([
                "gh", "api", "repos/{owner}/{repo}/releases?per_page=100", "--paginate"
            ], check=True, capture_output=True, text=True)
            releases = release_pages(result.stdout)
            print(release_tag(select_version(read_version(ROOT), args.resolve, releases)))
        else:
            print(release_tag(args.tag))
    except ValueError as error:
        parser.exit(1, f"{error}\n")


if __name__ == "__main__":
    main()
