"""Install workspace-local, pinned tools; never modify the upstream checkout."""
from pathlib import Path
import hashlib
import platform
import shutil
import subprocess
import sys
import tarfile
import urllib.request

ROOT = Path(__file__).resolve().parents[1]
TOOLS = ROOT / ".tools"
OPENSCAD_VERSION = "2026.10.23"
BOSL_VERSION = "2.0.774"
DOWNLOADS = {
    f"OpenSCAD-{OPENSCAD_VERSION}.dmg": (
        f"https://files.openscad.org/snapshots/OpenSCAD-{OPENSCAD_VERSION}.dmg",
        "fe9fc63010bc2a0f420eec2dbbd250760b1de9b3ec40a47b7fcda2b278d482cc",
    ),
    f"BOSL2-v{BOSL_VERSION}.tar.gz": (
        f"https://codeload.github.com/BelfrySCAD/BOSL2/tar.gz/refs/tags/v{BOSL_VERSION}",
        "0250251c0653fbe723e78a2cfd5e8d0b7eff0eb66a1bfdfae4fce21cf9a22ea1",
    ),
}


def download(name):
    url, expected = DOWNLOADS[name]
    path = TOOLS / "downloads" / name
    path.parent.mkdir(parents=True, exist_ok=True)
    if not path.exists():
        print(f"Downloading {url}", flush=True)
        urllib.request.urlretrieve(url, path)
    actual = hashlib.sha256(path.read_bytes()).hexdigest()
    if actual != expected:
        raise RuntimeError(f"Checksum mismatch: {path}; remove it and retry")
    return path


def main():
    if platform.system() != "Darwin":
        raise SystemExit("Automatic OpenSCAD setup currently supports macOS only. Set OPENSCAD and BOSL2_PATH for other systems.")
    python = ROOT / ".venv/bin/python"
    if not python.exists():
        subprocess.run([sys.executable, "-m", "venv", str(ROOT / ".venv")], check=True)
    subprocess.run([str(python), "-m", "pip", "install", "-r", str(ROOT / "requirements.lock")], check=True)
    bosl = TOOLS / "libraries/BOSL2"
    if not bosl.exists():
        archive = download(f"BOSL2-v{BOSL_VERSION}.tar.gz")
        bosl.parent.mkdir(parents=True, exist_ok=True)
        with tarfile.open(archive) as tar:
            tar.extractall(bosl.parent, filter="data")
        (bosl.parent / f"BOSL2-{BOSL_VERSION}").rename(bosl)
    app = TOOLS / "OpenSCAD.app"
    if not app.exists():
        image = download(f"OpenSCAD-{OPENSCAD_VERSION}.dmg")
        mount = TOOLS / "mount"
        subprocess.run(["hdiutil", "attach", "-readonly", "-nobrowse", "-mountpoint", str(mount), str(image)], check=True)
        try:
            shutil.copytree(mount / "OpenSCAD.app", app, symlinks=True)
        finally:
            subprocess.run(["hdiutil", "detach", str(mount)], check=True)
    subprocess.run([str(app / "Contents/MacOS/OpenSCAD"), "--version"], check=True)


if __name__ == "__main__":
    main()
