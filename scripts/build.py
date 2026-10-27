"""Bundle local FeatureScript fragments into one uploadable Feature Studio."""
import argparse
import hashlib
import json
from pathlib import Path
import re
from versioning import parse_version, read_version

ROOT = Path(__file__).resolve().parents[1]
# Explicit dependency order; do not rely on filesystem or alphabetical order.
MODULES = (
    "preamble.fs",
    "constants.fs",
    "geometry.fs",
    "support.fs",
    "connector.fs",
    "panel.fs",
    "feature.fs",
)


def bundle_sources(source_dir, version=None):
    chunks = []
    locations = []
    next_line = 1
    if version is not None:
        parse_version(version)
    for name in MODULES:
        text = (source_dir / name).read_text(encoding="utf-8").strip()
        source_hash = hashlib.sha256((text + "\n").encode()).hexdigest()
        if "{{PROJECT_VERSION}}" in text:
            version = version or read_version(source_dir.parent)
            text = text.replace("{{PROJECT_VERSION}}", version)
        if not text:
            raise ValueError(f"Empty source fragment: {name}")
        if name == "preamble.fs":
            header = re.match(r'FeatureScript (\d+);\nimport\(path : "onshape/std/geometry.fs", version : "(\d+)\.0"\);', text)
            if (not header or header[1] != header[2] or
                len(re.findall(r"(?m)^FeatureScript\s", text)) != 1 or
                len(re.findall(r"(?m)^import\s*\(", text)) != 1):
                raise ValueError("Preamble must start with matching FeatureScript and standard library versions")
        elif re.search(r"(?m)^\s*(FeatureScript\s|(?:export\s+)?(?:\w+::)?import\s*\()", text):
            raise ValueError(f"Headers and imports belong in preamble.fs, not {name}")
        chunks.append(text)
        line_count = len(text.splitlines())
        locations.append({
            "source": f"src/{name}",
            "start_line": next_line,
            "end_line": next_line + line_count - 1,
            "sha256": source_hash,
        })
        next_line += line_count + 1
    code = "\n\n".join(chunks) + "\n"
    mapping = {
        "bundle": "dist/homeracker.fs",
        "sha256": hashlib.sha256(code.encode()).hexdigest(),
        "modules": locations,
    }
    if version is not None:
        mapping["version"] = version
    return code, mapping


def write_bundle(root=ROOT, check=False, version=None):
    code, mapping = bundle_sources(root / "src", version=version)
    outputs = {
        root / "dist/homeracker.fs": code,
        root / "dist/homeracker.sources.json": json.dumps(mapping, indent=2) + "\n",
    }
    if check:
        stale = [str(path.relative_to(root)) for path, text in outputs.items()
                 if not path.exists() or path.read_bytes() != text.encode()]
        if stale:
            raise ValueError("Bundle is missing or stale: " + ", ".join(stale) + "; run mise run build")
    else:
        for path, text in outputs.items():
            path.parent.mkdir(parents=True, exist_ok=True)
            if not path.exists() or path.read_bytes() != text.encode():
                path.write_bytes(text.encode())
    return code


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="Fail if generated outputs are missing or stale; do not write files")
    parser.add_argument("--version", help="Release version; defaults to VERSION")
    args = parser.parse_args()
    try:
        write_bundle(check=args.check, version=args.version)
    except ValueError as error:
        parser.exit(1, f"{error}\n")
    print("Bundle is current" if args.check else ROOT / "dist/homeracker.fs")


if __name__ == "__main__":
    main()
