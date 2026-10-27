"""Stable semantic versions shared by bundles, GitHub, and Onshape."""
from pathlib import Path
import re


def parse_version(value: str) -> tuple[int, int, int]:
    if not re.fullmatch(r"(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)", value):
        raise ValueError("Version must be MAJOR.MINOR.PATCH without leading zeros")
    return tuple(map(int, value.split(".")))


def read_version(root: Path) -> str:
    value = (root / "VERSION").read_text(encoding="ascii").strip()
    parse_version(value)
    return value
