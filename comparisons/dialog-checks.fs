// Run with scripts/validation.py --dialogs; not included in the released feature.
annotation { "Feature Type Name" : "Dialog behavior checks" }
export const dialogBehaviorChecks = defineFeature(function(context is Context, id is Id, definition is map)
precondition {} {
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
        if (spec[1] < spec[0] || spec[1] > 2 * spec[0]) throw regenError("Invalid named shape");
    }
    homeracker(context, id + "flat", { "kind" : HomerackerKind.FLAT_PANEL });
    var solid = qBodyType(qCreatedBy(id + "flat", EntityType.BODY), BodyType.SOLID);
    var back = qCoincidesWithPlane(qOwnedByBody(solid,EntityType.FACE),plane(vector(0,0,1)*millimeter,vector(0,0,1)));
    if (abs(evArea(context,{ "entities" : back })/(millimeter^2) - (60*45-4*16)) > 0.000001)
        throw regenError("Back perimeter or hole entrances were chamfered");
    var mates = evaluateQuery(context,qBodyType(qCreatedBy(id + "flat",EntityType.BODY),BodyType.MATE_CONNECTOR));
    if (size(mates) != 4) throw regenError("Unexpected center mate");
    var first = evMateConnector(context,{ "mateConnector" : mates[0] });
    if (norm(first.origin/millimeter-vector(-22.5,15,0)) > 0.000001) throw regenError("C1 R1 not top left");
    println("Next cell, edge capacity, removal, notch labels, named shapes, square back, hole-only mates and top-left origin passed");
},{});
