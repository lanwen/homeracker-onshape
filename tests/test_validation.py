from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "scripts"))
from validation import build_validation, definition_expression


def test_fixture_converts_enums_and_lengths_to_featurescript():
    expression = definition_expression({"kind": "PANEL", "panelFit": "FULL_COVER",
                                        "panelClearance": 0.2, "cornerMounts": True})
    assert '"kind" : HomerackerKind.PANEL' in expression
    assert '"panelFit" : PanelFit.FULL_COVER' in expression
    assert '"panelClearance" : 0.2 * millimeter' in expression
    assert '"cornerMounts" : true' in expression


def test_fixture_preserves_feature_source_and_names_independent_cases():
    cases = [{"name": "support_3u", "onshape": {"kind": "SUPPORT", "supportUnits": 3, "xHoles": False}}]
    result = build_validation("FeatureScript 2500;\n// sentinel\n", cases)
    assert result.startswith("FeatureScript 2500;\n// sentinel\n")
    assert 'const caseId = id + "support_3u"' in result
    assert "homeracker(context, caseId" in result
    assert '"value" : "support_3u"' in result
    assert "Expected one solid" in result
    assert "Unexpected mate count" in result
    assert "qMateConnectorsOfParts(bodies)" in result
    assert "evMateConnector" in result
    assert "part_name" in result
