// Run with scripts/validation.py --dialogs; not included in the released feature.
annotation { "Feature Type Name" : "Dialog behavior checks" }
export const dialogBehaviorChecks = defineFeature(function(context is Context, id is Id, definition is map)
precondition {} {
    if (supportMateRows(SupportPinMates.NONE, [], 2, true) !=
            [[false, false, false, false], [false, false, false, false]])
        throw regenError("Support default must keep only end points");
    if (supportMateRows(SupportPinMates.ALL, [], 1, false) != [[true, true, false, false]] ||
            supportMateRows(SupportPinMates.ALL, [], 1, true) != [[true, true, true, true]])
        throw regenError("Support all-row selection failed");
    const selectedRows = [{ "mateRow" : 2 }, { "mateRow" : 2, "negativeZ" : true, "negativeX" : true }];
    if (supportMateRows(SupportPinMates.SELECTED, selectedRows, 2, true) !=
            [[false, false, false, false], [true, true, false, true]])
        throw regenError("Support row sides must combine without duplicates");
    if (try silent(supportMateRows(SupportPinMates.SELECTED, [{ "mateRow" : 3 }], 2, true)) != undefined ||
            try silent(supportMateRows(SupportPinMates.SELECTED, [{ "mateRow" : 0 }], 2, true)) != undefined ||
            try silent(supportMateRows(SupportPinMates.SELECTED, [{ "mateRow" : 1.5 }], 2, true)) != undefined)
        throw regenError("Out-of-range support mate row accepted");
    if (!(pinCell(2, 3) is PinCell) || canBePinCell({ "column" : 0, "row" : 1 }))
        throw regenError("PinCell type contract failed");
    if (!(notchRectangle(vector(0, 0) * millimeter, vector(1, 2) * millimeter) is NotchRectangle) ||
            canBeNotchRectangle({ "center" : vector(0, 0) * millimeter, "size" : vector(-1, 2) * millimeter }))
        throw regenError("NotchRectangle type contract failed");
    if (!samePinCell({ "holeColumn" : 2, "holeRow" : 3, "holeRevision" : 7 },
                    { "holeColumn" : 2, "holeRow" : 3, "holeRevision" : 0 }))
        throw regenError("Dialog metadata changed pin-cell identity");
    if (canBeConnectorSpec({ "dimensions" : 2, "ways" : 6, "name" : "Invalid" }))
        throw regenError("Invalid connector layout accepted");
    var panel = { "kind" : HomerackerKind.FLAT_PANEL, "flatWidthUnits" : 3, "flatHeightUnits" : 3,
        "flatThickness" : 2 * millimeter, "flatChamfers" : true, "flatCornerClearance" : 0.2 * millimeter,
        "flatCutNW" : false, "flatCutNE" : false, "flatCutSW" : false, "flatCutSE" : false, "flatHoles" : [] };
    panel = homerackerEditLogic(context, id, {}, panel, true, {});
    if (size(panel.flatHoles) != 4) throw regenError("Missing default corners");
    var next = panel;
    next.flatHoles = append(panel.flatHoles, { "holeColumn" : 1, "holeRow" : 1 });
    next = homerackerEditLogic(context, id, panel, next, false, {});
    if (!samePinCell(next.flatHoles[4], { "holeColumn" : 2, "holeRow" : 1 })) throw regenError("Next free cell not selected");
    for (var i = 0; i < 8; i += 1) {
        var added = next;
        added.flatHoles = append(next.flatHoles, { "holeColumn" : 1, "holeRow" : 1 });
        next = homerackerEditLogic(context, id, next, added, false, {});
    }
    if (size(next.flatHoles) != 8) throw regenError("Edge capacity not enforced");
    if (next.flatHoles[0].holeRevision < 1) throw regenError("Rejected append must change the array stamp");
    if (!next.flatAtHoleCapacity || size(next.flatFullHoles) != 8)
        throw regenError("Capacity view missing");
    var removed = next;
    removed.flatFullHoles = [];
    removed = homerackerEditLogic(context, id, next, removed, false, { "flatFullHoles" : true });
    if (removed.flatHoles != [] || removed.flatAtHoleCapacity) throw regenError("Removal undone");
    panel.flatCutNW = true;
    var available = flatPanelHoleCells(panel);
    if (size(available) != 5 || hasPinCell(available, { "holeColumn" : 1, "holeRow" : 1 }) ||
            hasPinCell(available, { "holeColumn" : 2, "holeRow" : 1 }) ||
            hasPinCell(available, { "holeColumn" : 1, "holeRow" : 2 }))
        throw regenError("Top-left notch not aligned with C1 R1");
    for (var shape in [ConnectorShape.END,ConnectorShape.STRAIGHT,ConnectorShape.ELBOW,ConnectorShape.TEE,
            ConnectorShape.CROSS,ConnectorShape.CORNER3,ConnectorShape.CORNER4,ConnectorShape.FIVE,ConnectorShape.SIX]) {
        var spec = connectorShapeSpec(shape);
        if (spec.ways < spec.dimensions || spec.ways > 2 * spec.dimensions) throw regenError("Invalid named shape");
    }
    for (var dimensions in [1, 2, 3])
        for (var ways = dimensions; ways <= 2 * dimensions; ways += 1)
            for (var pull in [PullAxis.NONE, PullAxis.X, PullAxis.Y, PullAxis.Z])
            {
                var legacy = { "kind" : HomerackerKind.CONNECTOR, "connectorShape" : ConnectorShape.LEGACY,
                        "connectorDimensions" : dimensions, "connectorWays" : ways, "pullAxis" : pull };
                var edited = homerackerEditLogic(context, id, {}, legacy, true, {});
                var keys = ["connectorWallNegativeZ", "connectorWallPositiveX", "connectorWallNegativeX",
                            "connectorWallPositiveY", "connectorWallNegativeY"];
                var indices = [1, 2, 3, 4, 5];
                var spec = connectorSpec(dimensions, ways, "Test");
                for (var side = 0; side < size(keys); side += 1)
                    if (edited[keys[side]] != corePinSideAvailable(spec, indices[side], pull))
                        throw regenError("Legacy connector wall visibility mismatch");
                edited.connectorShape = [ConnectorShape.END, ConnectorShape.STRAIGHT,
                        ConnectorShape.ELBOW, ConnectorShape.TEE, ConnectorShape.CROSS,
                        ConnectorShape.CORNER3, ConnectorShape.CORNER4, ConnectorShape.FIVE, ConnectorShape.SIX]
                        [dimensions == 1 ? ways - 1 : (dimensions == 2 ? ways : ways + 2)];
                edited = homerackerEditLogic(context, id, legacy, edited, false, {});
                for (var side = 0; side < size(keys); side += 1)
                    if (edited[keys[side]] != corePinSideAvailable(spec, indices[side], pull))
                        throw regenError("Named connector wall visibility mismatch");
            }
    homeracker(context, id + "flat", { "kind" : HomerackerKind.FLAT_PANEL });
    var solid = qBodyType(qCreatedBy(id + "flat", EntityType.BODY), BodyType.SOLID);
    var back = qCoincidesWithPlane(qOwnedByBody(solid,EntityType.FACE),plane(vector(0,0,1)*millimeter,vector(0,0,1)));
    if (abs(evArea(context,{ "entities" : back })/(millimeter^2) - (60*45-4*16)) > 0.000001)
        throw regenError("Back perimeter or hole entrances were chamfered");
    var mates = evaluateQuery(context,qBodyType(qCreatedBy(id + "flat",EntityType.BODY),BodyType.MATE_CONNECTOR));
    if (size(mates) != 4) throw regenError("Unexpected center mate");
    var first = evMateConnector(context,{ "mateConnector" : mates[0] });
    if (norm(first.origin/millimeter-vector(-22.5,15,1)) > 0.000001) throw regenError("C1 R1 not on back contact face");
    println("Next cell, edge capacity, removal, notch labels, named shapes, square back, hole-only mates and top-left origin passed");
},{});
