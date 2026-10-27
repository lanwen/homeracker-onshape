"""Check the SVG used by the Onshape toolbar and release assets."""
from pathlib import Path
import xml.etree.ElementTree as ET


def test_feature_icon_is_square_self_contained_vector_svg():
    path = Path(__file__).resolve().parents[1] / "assets/homeracker-logo.svg"
    root = ET.parse(path).getroot()
    svg = "{http://www.w3.org/2000/svg}"
    assert root.tag == svg + "svg"
    _, _, width, height = map(float, root.attrib["viewBox"].split())
    assert width == height and width > 0
    assert root.findall(".//" + svg + "path")
    for element in root.iter():
        assert element.tag not in {svg + "image", svg + "script", svg + "foreignObject"}
        for name, value in element.attrib.items():
            if name.rsplit("}", 1)[-1] == "href":
                assert value.startswith("#")
