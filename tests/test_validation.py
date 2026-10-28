from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "scripts"))
from validation import build_validation, definition_expression, connector_layout, core_pin_mate_count, expected_mate_count


def test_fixture_converts_enums_and_lengths_to_featurescript():
    expression = definition_expression({"kind": "PANEL", "panelFit": "FULL_COVER",
                                        "panelClearance": 0.2, "cornerMounts": True,
                                        "flatHoles": [{"holeColumn": 1, "holeRow": 3}]})
    assert '"kind" : HomerackerKind.PANEL' in expression
    assert '"panelFit" : PanelFit.FULL_COVER' in expression
    assert '"panelClearance" : 0.2 * millimeter' in expression
    assert '"cornerMounts" : true' in expression
    assert '"holeColumn" : 1' in expression
    assert '"holeRow" : 3' in expression


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


def test_three_way_corner_keeps_negative_z_wall():
    definition = {"connectorShape": "CORNER3", "pullAxis": "X", "connectorCoreNegativeZ": True}
    assert connector_layout(definition)[1]["Z"] == (True, False)
    assert core_pin_mate_count(definition) == 1


def test_core_pin_sides_are_independent_and_require_a_closed_transverse_wall():
    definition = {"connectorShape": "END", "pullAxis": "Z", "connectorCorePositiveX": True}
    assert core_pin_mate_count(definition) == 1
    assert core_pin_mate_count({**definition, "connectorCoreNegativeX": True}) == 2
    assert core_pin_mate_count({**definition, "pullAxis": "NONE"}) == 0
    assert core_pin_mate_count({**definition, "pullAxis": "X"}) == 0
    assert core_pin_mate_count({**definition, "connectorShape": "SIX"}) == 0


def test_pull_through_uses_two_opening_references_and_keeps_arm_pin_points():
    straight = {"kind": "CONNECTOR", "connectorShape": "STRAIGHT", "pullAxis": "NONE"}
    assert expected_mate_count(straight) == 10  # Two seats and eight pin-opening points.
    assert expected_mate_count({**straight, "pullAxis": "Z"}) == 10
    assert expected_mate_count({**straight, "pullAxis": "X"}) == 12
    assert expected_mate_count({**straight, "mateConnectors": False}) == 0
