from pathlib import Path
import sys

from pypdf import PdfReader
import pytest

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "scripts"))
from guide import write_guide

ROOT = Path(__file__).resolve().parents[1]


def test_user_guide_has_version_settings_origin_and_working_links(tmp_path):
    pdf = write_guide(ROOT / "docs/guide.md", tmp_path / "guide.pdf", "0.1.2")
    reader = PdfReader(pdf)
    text = "\n".join(page.extract_text() for page in reader.pages)
    for expected in ["HomeRacker for Onshape", "v0.1.2", "Support", "Connector", "Panel", "Add the feature", "X pin holes", "Directions",
                     "Face chamfers", "Corner mounts", "Mate connectors", "Patrick", "Kirill Merkushev"]:
        assert expected in text
    assert "**" not in text
    links = [str(annotation.get_object()["/A"]["/URI"])
             for page in reader.pages for annotation in page.get("/Annots", [])]
    assert "https://github.com/kellerlabs/homeracker" in links
    assert not any("development.md" in link or "compatibility.md" in link for link in links)
    assert any(link.startswith("https://cad.onshape.com/documents/") for link in links)
    original = pdf.read_bytes()
    write_guide(ROOT / "docs/guide.md", pdf, "0.1.2")
    assert pdf.read_bytes() == original


def test_guide_fails_if_source_cannot_be_rendered(tmp_path):
    source = tmp_path / "guide.md"
    source.write_text("")
    with pytest.raises(ValueError, match="non-empty source document"):
        write_guide(source, tmp_path / "guide.pdf", "0.1.2")
    source.write_text("![unhandled image](image.png)")
    with pytest.raises(ValueError, match="Unsupported guide content"):
        write_guide(source, tmp_path / "guide.pdf", "0.1.2")
