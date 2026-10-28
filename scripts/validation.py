"""Build one temporary Onshape feature containing the comparison fixtures.

The generated Feature Studio contains the public Homeracker feature and a
separate comparison feature that creates named parts at their original origins.
It makes no network requests.
"""
import argparse
import hashlib
import json
from pathlib import Path

from build import write_bundle

ROOT = Path(__file__).resolve().parents[1]


def definition_expression(definition):
    enum_types = {"kind": "HomerackerKind", "pullAxis": "PullAxis", "panelFit": "PanelFit", "connectorShape": "ConnectorShape"}
    fields = []
    for key, value in definition.items():
        if key in enum_types:
            expression = f"{enum_types[key]}.{value}"
        elif key in ("panelClearance", "flatThickness", "flatCornerClearance"):
            expression = f"{value} * millimeter"
        elif key == "flatHoles":
            expression = "[" + ", ".join("{ " + ", ".join(f'"{k}" : {v}' for k, v in {**hole, 'holeRevision': hole.get('holeRevision', 0)}.items()) + " }" for hole in value) + "]"
        else:
            expression = json.dumps(value)
        fields.append(f'"{key}" : {expression}')
    return "{ " + ", ".join(fields) + " }"


def expected_mate_count(definition):
    if not definition.get("mateConnectors", True):
        return 0
    kind = definition["kind"]
    if kind == "SUPPORT":
        return 2 + definition["supportUnits"] * (2 if definition.get("xHoles", False) else 1)
    if kind == "CONNECTOR":
        shapes = {"END": 1, "STRAIGHT": 2, "ELBOW": 2, "TEE": 3, "CROSS": 4,
                  "CORNER3": 3, "CORNER4": 4, "FIVE": 5, "SIX": 6}
        if definition.get("connectorShape") in shapes:
            return shapes[definition["connectorShape"]]
        return definition["connectorWays"]
    if kind == "SLEEVE":
        return 2 + definition.get("sleeveUnits", 3)
    if kind == "FLAT_PANEL":
        return len(definition.get("flatHoles", [{}, {}, {}, {}]))
    count = (8 if definition.get("cornerMounts", True) else 0)
    for length, sides in [(definition["panelX"], ["northMounts", "southMounts"]),
                          (definition["panelY"], ["eastMounts", "westMounts"])]:
        count += (length - 2) * sum(definition.get(side, True) or definition["panelFit"] == "INTERFIT"
                                     for side in sides)
    return count


def build_validation(source, cases):
    lines = [source.rstrip(), "", "// Generated comparison fixtures; keep out of the distributed feature.",
             'annotation { "Feature Type Name" : "Homeracker comparisons" }',
             "export const homerackerComparisons = defineFeature(function(context is Context, id is Id, definition is map)",
             "    precondition {}", "    {"]
    for case in cases:
        name = case["name"]
        expected_volume_check = []
        if "expected_volume_mm3" in case:
            expected_volume_check = [
                f'            if (abs(evVolume(context, {{ "entities" : bodies }}) / (millimeter ^ 3) - {case["expected_volume_mm3"]}) > 0.000001)',
                f'                throw regenError("Unexpected volume for {name}");',
            ]
        notch_checks = []
        if case["onshape"]["kind"] == "FLAT_PANEL":
            params = case["onshape"]
            cx = (params["flatWidthUnits"] - 1) * 7.5
            cy = (params["flatHeightUnits"] - 1) * 7.5
            positions = [(-cx, cy), (cx, cy), (-cx, -cy), (cx, -cy)]
            for corner, key in enumerate(["flatCutNW", "flatCutNE", "flatCutSW", "flatCutSE"]):
                if not params.get(key, False):
                    continue
                gap = params.get("flatCornerClearance", 0.2)
                sx = 1 if corner in (0, 2) else -1
                sy = -1 if corner < 2 else 1
                x, y = positions[corner]
                length = 9.6 + 22.5
                offset = (22.5 - 9.6) / 2
                rectangles = [(x + sx * offset, y, length, 19.2),
                              (x, y + sy * offset, 19.2, length)]
                # For negative clearance, verify a slightly inset contracted
                # footprint is clear. Positive clearance uses the nominal elbow.
                inset = min(gap, 0) - 0.001
                for leg, (rx, ry, rw, rh) in enumerate(rectangles):
                    notch_checks.extend([
                        '            {',
                        '                const plateForCheck = qUnion(evaluateQuery(context, bodies));',
                        f'                const envelope = caseId + "notchEnvelope{corner}Leg{leg}";',
                        f'                makeBox(context, envelope, vector({rx}, {ry}, 0) * millimeter,',
                        f'                        vector({rw + 2 * inset}, {rh + 2 * inset}, {params["flatThickness"] + 2}) * millimeter);',
                        '                const clearance = evDistance(context, { "side0" : plateForCheck, "side1" : body(envelope) });',
                        f'                if (clearance.distance / millimeter < {gap - inset} - 0.00001)',
                        f'                    throw regenError("Connector elbow clearance failed for {name}");',
                        f'                opDeleteBodies(context, caseId + "deleteEnvelope{corner}Leg{leg}", {{ "entities" : body(envelope) }});',
                        '            }',
                    ])
        lines.extend([
            "        {",
            f'            const caseId = id + "{name}";',
            f"            homeracker(context, caseId, {definition_expression(case['onshape'])});",
            "            const bodies = qBodyType(qCreatedBy(caseId, EntityType.BODY), BodyType.SOLID);",
            "            if (size(evaluateQuery(context, bodies)) != 1)",
            f'                throw regenError("Expected one solid for {name}");',
            *expected_volume_check,
            *notch_checks,
            '            const mates = qBodyType(qCreatedBy(caseId, EntityType.BODY), BodyType.MATE_CONNECTOR);',
            f'            if (size(evaluateQuery(context, mates)) != {expected_mate_count(case["onshape"])})',
            f'                throw regenError("Unexpected mate count for {name}");',
            '            if (size(evaluateQuery(context, qMateConnectorsOfParts(bodies))) != size(evaluateQuery(context, mates)))',
            f'                throw regenError("Unowned mate connector for {name}");',
            '            println({ "case" : "' + name + '", "part_name" : getProperty(context, { "entity" : bodies, "propertyType" : PropertyType.NAME }),',
            '                    "mate_count" : size(evaluateQuery(context, mates)) });',
            '            for (var mate in evaluateQuery(context, mates))',
            '            {',
            '                const frame = evMateConnector(context, { "mateConnector" : mate });',
            '                println({ "case" : "' + name + '", "mate_name" : getProperty(context, { "entity" : mate, "propertyType" : PropertyType.NAME }),',
            '                        "origin_mm" : frame.origin / millimeter, "z_axis" : frame.zAxis, "x_axis" : frame.xAxis });',
            '            }',
            '            setProperty(context, { "entities" : bodies, "propertyType" : PropertyType.NAME,',
            f'                    "value" : "{name}" }});',
            "            const bounds = evBox3d(context, { \"topology\" : bodies, \"tight\" : true });",
            f'            println({{ "case" : "{name}",',
            '                    "volume_mm3" : evVolume(context, { "entities" : bodies }) / (millimeter ^ 3),',
            '                    "min_mm" : bounds.minCorner / millimeter,',
            '                    "max_mm" : bounds.maxCorner / millimeter });',
            "        }",
        ])
    # Compare the geometry above the plate for each face-chamfer on/off pair.
    # Removing the flat face bevel must leave every mount unchanged.
    names = {case["name"] for case in cases}
    for case in cases:
        name = case["name"]
        if not name.endswith("_square") or name.removesuffix("_square") not in names:
            continue
        normal = name.removesuffix("_square")
        full_cover = case["onshape"]["panelFit"] == "FULL_COVER"
        plate_top = -7.6 if full_cover else -6.6
        lines.extend([
            '        {',
            f'            const pairId = id + "{name}MountCheck";',
            '            var volumes = [];',
            f'            const sources = [id + "{normal}", id + "{name}"];',
            '            for (var i in [0, 1])',
            '            {',
            '                const copyId = pairId + ("copy" ~ i);',
            '                const maskId = pairId + ("mask" ~ i);',
            '                const clipId = pairId + ("clip" ~ i);',
            '                opPattern(context, copyId, {',
            '                        "entities" : qBodyType(qCreatedBy(sources[i], EntityType.BODY), BodyType.SOLID),',
            '                        "transforms" : [identityTransform()], "instanceNames" : ["check"],',
            '                        "copyPropertiesAndAttributes" : false });',
            f'                makeBox(context, maskId, vector(0, 0, {plate_top + 100.001}) * millimeter,',
            '                        vector(2000, 2000, 200) * millimeter);',
            '                opBoolean(context, clipId, {',
            '                        "tools" : qUnion([qBodyType(qCreatedBy(copyId, EntityType.BODY), BodyType.SOLID), body(maskId)]),',
            '                        "operationType" : BooleanOperationType.INTERSECTION });',
            '                const upper = qBodyType(qCreatedBy(clipId, EntityType.BODY), BodyType.SOLID);',
            '                volumes = append(volumes, evVolume(context, { "entities" : upper }) / (millimeter ^ 3));',
            '                opDeleteBodies(context, pairId + ("delete" ~ i), { "entities" : upper });',
            '            }',
            '            if (abs(volumes[0] - volumes[1]) > 0.000001)',
            f'                throw regenError("Face chamfer toggle changed mounts for {normal}");',
            f'            println({{ "case" : "{normal}", "mount_geometry_unchanged" : true }});',
            '        }',
        ])
    lines.extend(["    });", ""])
    return "\n".join(lines)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--case", action="append")
    parser.add_argument("--dialogs", action="store_true", help="Build the dialog behavior checks instead of geometry comparisons")
    args = parser.parse_args()
    if args.dialogs:
        if args.case:
            parser.error("Use --dialogs or --case, not both")
        source = write_bundle(ROOT).replace("export const homeracker =", "const homeracker =")
        out = ROOT / "artifacts/homeracker-dialog-checks.fs"
        out.parent.mkdir(parents=True, exist_ok=True)
        out.write_text(source + "\n" + (ROOT / "comparisons/dialog-checks.fs").read_text())
        print(out)
        return
    manifest = json.loads((ROOT / "comparisons/cases.json").read_text())
    all_cases = manifest["cases"]
    flat_cases = ROOT / "comparisons/flat-panels.json"
    if flat_cases.exists():
        all_cases = [*all_cases, *json.loads(flat_cases.read_text())["cases"]]
    cases = [c for c in all_cases if not args.case or c["name"] in args.case]
    if not cases or args.case and set(args.case) != {c["name"] for c in cases}:
        parser.error("Unknown case name")
    source = write_bundle(ROOT)
    out = ROOT / "artifacts/homeracker-validation.fs"
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(build_validation(source, cases))
    out.with_suffix(".json").write_text(json.dumps({
        "feature_source_sha256": hashlib.sha256(source.encode()).hexdigest(),
        "validation_source_sha256": hashlib.sha256(out.read_bytes()).hexdigest(),
        "cases": [c["name"] for c in cases],
    }, indent=2) + "\n")
    print(out)


if __name__ == "__main__":
    main()
