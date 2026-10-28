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


SHAPE_LAYOUTS = {"END": (1, 1), "STRAIGHT": (1, 2), "ELBOW": (2, 2), "TEE": (2, 3),
                 "CROSS": (2, 4), "CORNER3": (3, 3), "CORNER4": (3, 4), "FIVE": (3, 5), "SIX": (3, 6)}


def connector_layout(definition):
    dimensions, ways = SHAPE_LAYOUTS.get(definition.get("connectorShape"),
        (definition.get("connectorDimensions", 3), definition.get("connectorWays", 6)))
    return ways, {
        "X": (dimensions >= 2, dimensions == 2 and ways == 4 or dimensions == 3 and ways >= 5),
        "Y": (dimensions == 3, dimensions == 3 and ways == 6),
        "Z": (True, ways > dimensions),
    }


CORE_SIDES = [("NegativeZ", "Z", False), ("PositiveX", "X", True), ("NegativeX", "X", False),
              ("PositiveY", "Y", True), ("NegativeY", "Y", False)]


def core_pin_available(definition, axis, positive):
    _, arms = connector_layout(definition)
    pull = definition.get("pullAxis", "NONE")
    return pull != "NONE" and pull != axis and not arms[axis][0 if positive else 1]


def core_pin_mate_count(definition):
    return sum(definition.get("connectorCore" + side, False) and core_pin_available(definition, axis, positive)
               for side, axis, positive in CORE_SIDES)


def core_hole_checks(case):
    params = case["onshape"]
    lines = []
    for side_name, axis, positive in CORE_SIDES:
        if not core_pin_available(params, axis, positive):
            continue
        enabled = params.get("connectorCore" + side_name, False)
        index = "XYZ".index(axis)
        sign = 1 if positive else -1
        probes = [("bore", sign * 8.6, 1.998, 3.998, False),
                  ("innerEntrance", sign * 7.7, 0.02, 5.0, False),
                  ("outerEntrance", sign * 9.5, 0.02, 5.0, False),
                  ("collar", sign * 8.6, 0.02, 4.4, True)]
        if not enabled:
            probes = [("closedWall", sign * 8.6, 0.02, 3.998, True)]
        if enabled and params.get("mateConnectors", True):
            side_index = {"NegativeZ": 1, "PositiveX": 2, "NegativeX": 3, "PositiveY": 4, "NegativeY": 5}[side_name]
            origin = [0, 0, 0]
            origin[index] = sign * 7.6
            normal = [0, 0, 0]
            normal[index] = -sign
            along = [int(a == params["pullAxis"]) for a in "XYZ"]
            lines.extend([
                '            {',
                f'                const frame = evMateConnector(context, {{ "mateConnector" : qCreatedBy(caseId + "corePinMate{side_index}", EntityType.BODY) }});',
                f'                if (norm(frame.origin / millimeter - vector({", ".join(map(str, origin))})) > 0.000001 ||',
                f'                        norm(frame.zAxis - vector({", ".join(map(str, normal))})) > 0.000001 ||',
                f'                        norm(frame.xAxis - vector({", ".join(map(str, along))})) > 0.000001)',
                f'                    throw regenError("Incorrect core mate frame for {case["name"]} {side_name}");',
                '            }',
            ])
        for tag, station, depth, side, material_expected in probes:
            center = [0, 0, 0]
            center[index] = station
            dims = [side, side, side]
            dims[index] = depth
            lines.extend([
                '            {',
                '                const checkedPart = qUnion(evaluateQuery(context, bodies));',
                f'                const probeId = caseId + "core{side_name}{tag}";',
                f'                makeBox(context, probeId, vector({", ".join(map(str, center))}) * millimeter,',
                f'                        vector({", ".join(map(str, dims))}) * millimeter);',
                '                var hitsMaterial = false;',
                '                for (var clash in evCollision(context, { "tools" : body(probeId), "targets" : checkedPart }))',
                '                    if (clash["type"] == ClashType.INTERFERE || clash["type"] == ClashType.TOOL_IN_TARGET ||',
                '                            clash["type"] == ClashType.TARGET_IN_TOOL || clash["type"] == ClashType.ABUT_TOOL_IN_TARGET)',
                '                        hitsMaterial = true;',
                f'                if (hitsMaterial != {str(material_expected).lower()})',
                f'                    throw regenError("Core {side_name} {tag} probe failed for {case["name"]}");',
                '                opDeleteBodies(context, probeId + "delete", { "entities" : body(probeId) });',
                '            }',
            ])
    baseline = {**params, **{"connectorCore" + side: False for side, _, _ in CORE_SIDES}, "mateConnectors": False}
    lines.extend([
        '            {',
        f'                const baselineId = id + "coreBaseline{case["name"]}";',
        f'                homeracker(context, baselineId, {definition_expression(baseline)});',
        '                const baselineBodies = qBodyType(qCreatedBy(baselineId, EntityType.BODY), BodyType.SOLID);',
        '                const removed = (evVolume(context, { "entities" : baselineBodies }) - evVolume(context, { "entities" : bodies })) / (millimeter ^ 3);',
        ('                if (removed < 0.000001)' if core_pin_mate_count(params) else '                if (abs(removed) > 0.000001)'),
        f'                    throw regenError("Unexpected core wall removal for {case["name"]}");',
        '                opDeleteBodies(context, baselineId + "delete", { "entities" : baselineBodies });',
        '            }',
    ])
    return lines


def connector_mate_checks(case):
    params = case["onshape"]
    if params["kind"] != "CONNECTOR" or not params.get("mateConnectors", True):
        return []
    _, arms = connector_layout(params)
    pull = params.get("pullAxis", "NONE")
    lines = []

    def frame_check(suffix, origin, normal, along=None):
        if along is None:
            along = [0, 1, 0] if abs(normal[0]) > 0.5 else [1, 0, 0]
        return [
            '            {',
            f'                const frame = evMateConnector(context, {{ "mateConnector" : qCreatedBy(caseId + "{suffix}", EntityType.BODY) }});',
            f'                if (norm(frame.origin / millimeter - vector({", ".join(map(str, origin))})) > 0.000001 ||',
            f'                        norm(frame.zAxis - vector({", ".join(map(str, normal))})) > 0.000001' + (f' || norm(frame.xAxis - vector({", ".join(map(str, along))})) > 0.000001)' if along is not None else ')'),
            f'                    throw regenError("Incorrect connector mate frame for {case["name"]} {suffix}");',
            '            }',
        ]

    directions = [("Z", True), ("Z", False), ("X", True), ("X", False), ("Y", True), ("Y", False)]
    for side, (axis, positive) in enumerate(directions):
        occupied = arms[axis][0 if positive else 1]
        socket_expected = occupied and axis != pull
        lines.extend([
            f'            if (size(evaluateQuery(context, qCreatedBy(caseId + "socketMate{side}", EntityType.BODY))) != {int(socket_expected)})',
            f'                throw regenError("Unexpected socket mate for {case["name"]} side {side}");',
        ])
        direction = [0, 0, 0]
        direction["XYZ".index(axis)] = 1 if positive else -1
        axial_opening = axis == pull
        lines.extend([
            f'            if (size(evaluateQuery(context, qCreatedBy(caseId + "pullThroughMate{side}", EntityType.BODY))) != {int(axial_opening)})',
            f'                throw regenError("Unexpected pull opening for {case["name"]} side {side}");',
        ])
        if axial_opening:
            distance = 22.5 if occupied else 9.6
            lines.extend(frame_check(f"pullThroughMate{side}", [v * distance for v in direction], direction))
        if not occupied:
            continue
        if socket_expected:
            lines.extend(frame_check(f"socketMate{side}", [v * 7.5 for v in direction], direction))
        transverse = {"Z": ["X", "Y"], "X": ["Y", "Z"], "Y": ["X", "Z"]}[axis]
        for passage, hole_axis in enumerate(transverse):
            hole_direction = [int(a == hole_axis) for a in "XYZ"]
            center = [v * 15 for v in direction]
            lines.extend(frame_check(f"armPinMate{side}axis{passage}",
                                     [c + n * 7.6 for c, n in zip(center, hole_direction)], [-n for n in hole_direction], direction))
            lines.extend(frame_check(f"oppositeArmPinMate{side}axis{passage}",
                                     [c - n * 7.6 for c, n in zip(center, hole_direction)], hole_direction, direction))
    return lines


def support_mate_checks(case):
    params = case["onshape"]
    if params["kind"] != "SUPPORT" or not params.get("mateConnectors", True):
        return []
    lines = []
    for suffix, sign in [("startMate", -1), ("endMate", 1)]:
        lines.extend([
            '            {',
            f'                const frame = evMateConnector(context, {{ "mateConnector" : qCreatedBy(caseId + "{suffix}", EntityType.BODY) }});',
            f'                if (norm(frame.origin / millimeter - vector(0, {sign * params["supportUnits"] * 7.5}, 0)) > 0.000001 ||',
            f'                        norm(frame.zAxis - vector(0, {sign}, 0)) > 0.000001 ||',
            '                        norm(frame.xAxis - vector(1, 0, 0)) > 0.000001)',
            f'                    throw regenError("Incorrect support end mate for {case["name"]} {suffix}");',
            '            }',
        ])
    for station in range(params["supportUnits"]):
        y = (station - (params["supportUnits"] - 1) / 2) * 15
        for axis in (["Z", "X"] if params.get("xHoles", False) else ["Z"]):
            for sign in [1, -1]:
                suffix = ("pinMate" if sign == 1 else "oppositePinMate") if axis == "Z" else ("xPinMate" if sign == 1 else "oppositeXPinMate")
                origin = [0, y, 0]
                origin["XYZ".index(axis)] = sign * 7.5
                normal = [sign if a == axis else 0 for a in "XYZ"]
                lines.extend([
                    '            {',
                    f'                const frame = evMateConnector(context, {{ "mateConnector" : qCreatedBy(caseId + "{suffix}{station}", EntityType.BODY) }});',
                    f'                if (norm(frame.origin / millimeter - vector({", ".join(map(str, origin))})) > 0.000001 ||',
                    f'                        norm(frame.zAxis - vector({", ".join(map(str, normal))})) > 0.000001 ||',
                    '                        norm(frame.xAxis - vector(0, 1, 0)) > 0.000001)',
                    f'                    throw regenError("Incorrect support opening mate for {case["name"]} {suffix}{station}");',
                    '            }',
                ])
    return lines


def expected_mate_count(definition):
    if not definition.get("mateConnectors", True):
        return 0
    kind = definition["kind"]
    if kind == "SUPPORT":
        return 2 + 2 * definition["supportUnits"] * (2 if definition.get("xHoles", False) else 1)
    if kind == "CONNECTOR":
        ways, arms = connector_layout(definition)
        pull = definition.get("pullAxis", "NONE")
        axial = sum(arms[pull]) if pull != "NONE" else 0
        return 5 * ways - axial + 2 * (pull != "NONE") + core_pin_mate_count(definition)
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
        bounds_check = []
        if case.get("expected_bounds_mm"):
            low, high = case["expected_bounds_mm"]
            bounds_check = [
                f'            if (norm(bounds.minCorner / millimeter - vector({", ".join(map(str, low))})) > 0.000001 ||',
                f'                    norm(bounds.maxCorner / millimeter - vector({", ".join(map(str, high))})) > 0.000001)',
                f'                throw regenError("Bounds changed from v0.1.5 for {name}");',
            ]
        lines.extend([
            "        {",
            f'            const caseId = id + "{name}";',
            f"            homeracker(context, caseId, {definition_expression(case['onshape'])});",
            "            const bodies = qBodyType(qCreatedBy(caseId, EntityType.BODY), BodyType.SOLID);",
            "            if (size(evaluateQuery(context, bodies)) != 1)",
            f'                throw regenError("Expected one solid for {name}");',
            *expected_volume_check,
            *connector_mate_checks(case),
            *support_mate_checks(case),
            *notch_checks,
            *(core_hole_checks(case) if case.get("check_core_holes") else []),
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
            *bounds_check,
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
    parser.add_argument("--mates", action="store_true", help="Build physical fit checks for pin-opening mates")
    args = parser.parse_args()
    if args.dialogs or args.mates:
        if args.case or args.dialogs and args.mates:
            parser.error("Use only one of --dialogs, --mates, or --case")
        source = write_bundle(ROOT).replace("export const homeracker =", "const homeracker =")
        fixture = "dialog-checks" if args.dialogs else "mate-fit-checks"
        out = ROOT / ("artifacts/homeracker-" + fixture + ".fs")
        out.parent.mkdir(parents=True, exist_ok=True)
        out.write_text(source + "\n" + (ROOT / ("comparisons/" + fixture + ".fs")).read_text())
        print(out)
        return
    manifest = json.loads((ROOT / "comparisons/cases.json").read_text())
    all_cases = manifest["cases"]
    for name in ["flat-panels.json", "core-pin-holes.json"]:
        native_cases = ROOT / "comparisons" / name
        if native_cases.exists():
            all_cases = [*all_cases, *json.loads(native_cases.read_text())["cases"]]
    baseline_path = ROOT / "comparisons/baseline-v0.1.5.json"
    if baseline_path.exists():
        baseline = json.loads(baseline_path.read_text())["cases"]
        for case in all_cases:
            if case["name"] in baseline:
                previous = baseline[case["name"]]
                case.setdefault("expected_volume_mm3", previous["volume"])
                case["expected_bounds_mm"] = [previous["min"], previous["max"]]
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
