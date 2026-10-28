// Assemble a support against a pin opening using the same frame match as a Fastened mate.
function checkPinMateFit(context is Context, id is Id, shape is ConnectorShape, pull is PullAxis,
        pinSuffix is string, station is number)
{
    var connectorId = id + "connector";
    var supportId = id + "support";
    homeracker(context, connectorId, { "kind" : HomerackerKind.CONNECTOR,
            "connectorShape" : shape, "pullAxis" : pull,
            "connectorCorePositiveX" : true, "connectorCoreNegativeX" : true,
            "connectorCorePositiveY" : true, "connectorCoreNegativeY" : true,
            "connectorCoreNegativeZ" : true });
    homeracker(context, supportId, { "kind" : HomerackerKind.SUPPORT, "supportUnits" : 3,
            "supportPinMates" : SupportPinMates.ALL });
    var supportBody = qBodyType(qCreatedBy(supportId, EntityType.BODY), BodyType.SOLID);
    var connectorBody = qBodyType(qCreatedBy(connectorId, EntityType.BODY), BodyType.SOLID);
    var sourceMate = qCreatedBy(supportId + ("pinMate" ~ station), EntityType.BODY);
    var source = evMateConnector(context, { "mateConnector" : sourceMate });
    var target = evMateConnector(context, { "mateConnector" : qCreatedBy(connectorId + pinSuffix, EntityType.BODY) });
    var destination = coordSystem(target.origin, target.xAxis, -target.zAxis);
    opTransform(context, id + "alignSupport", {
            "bodies" : supportBody, "transform" : toWorld(destination) * fromWorld(source) });
    var placed = evMateConnector(context, { "mateConnector" : sourceMate });
    if (norm(placed.origin - target.origin) > 0.000001 * millimeter ||
            norm(placed.xAxis - target.xAxis) > 0.000001 || norm(placed.zAxis + target.zAxis) > 0.000001)
        throw regenError("Single pin mate did not align the support frame");
    for (var clash in evCollision(context, { "tools" : supportBody, "targets" : connectorBody }))
        if (clash["type"] == ClashType.INTERFERE || clash["type"] == ClashType.TOOL_IN_TARGET ||
                clash["type"] == ClashType.TARGET_IN_TOOL)
            throw regenError("Single pin mate caused a support collision");
    println({ "fit_case" : id, "single_pin_mate_aligned" : true, "no_solid_interference" : true });
}

function checkPanelPinMateFit(context is Context, id is Id, panelDefinition is map, matePath is array)
{
    const panelId = id + "panel";
    const supportId = id + "support";
    homeracker(context, panelId, panelDefinition);
    homeracker(context, supportId, { "kind" : HomerackerKind.SUPPORT, "supportUnits" : 1,
            "supportPinMates" : SupportPinMates.SELECTED,
            "supportMateRows" : [{ "mateRow" : 1, "positiveZ" : true, "negativeZ" : false }] });
    const supportBody = qBodyType(qCreatedBy(supportId, EntityType.BODY), BodyType.SOLID);
    const panelBody = qBodyType(qCreatedBy(panelId, EntityType.BODY), BodyType.SOLID);
    const source = evMateConnector(context, { "mateConnector" : qCreatedBy(supportId + "pinMate0", EntityType.BODY) });
    var mateId = panelId;
    for (var segment in matePath)
        mateId = mateId + segment;
    const target = evMateConnector(context, { "mateConnector" : qCreatedBy(mateId, EntityType.BODY) });
    opTransform(context, id + "alignSupport", { "bodies" : supportBody,
            "transform" : toWorld(coordSystem(target.origin, target.xAxis, -target.zAxis)) * fromWorld(source) });
    const placed = evMateConnector(context, { "mateConnector" : qCreatedBy(supportId + "pinMate0", EntityType.BODY) });
    if (!tolerantEquals(placed.origin, target.origin) || !tolerantEquals(placed.zAxis, -target.zAxis))
        throw regenError("Panel pin mate did not align its contact frame");
    if (evDistance(context, { "side0" : supportBody, "side1" : panelBody }).distance > 0.000001 * millimeter)
        throw regenError("Panel pin mate left a gap");
    for (var clash in evCollision(context, { "tools" : supportBody, "targets" : panelBody }))
        if (clash["type"] == ClashType.INTERFERE || clash["type"] == ClashType.TOOL_IN_TARGET ||
                clash["type"] == ClashType.TARGET_IN_TOOL)
            throw regenError("Panel pin mate caused a support collision");
    println({ "panel_fit_case" : id, "surface_contact" : true, "no_solid_interference" : true });
}

annotation { "Feature Type Name" : "Pin mate fit checks" }
export const pinMateFitChecks = defineFeature(function(context is Context, id is Id, definition is map)
precondition {} {
    checkPinMateFit(context, id + "positiveZArm", ConnectorShape.ELBOW, PullAxis.Z, "armPinMate0axis0", 0);
    checkPinMateFit(context, id + "negativeZArm", ConnectorShape.STRAIGHT, PullAxis.Z, "armPinMate1axis1", 0);
    checkPinMateFit(context, id + "positiveXArm", ConnectorShape.ELBOW, PullAxis.X, "armPinMate2axis0", 0);
    checkPinMateFit(context, id + "negativeYArm", ConnectorShape.SIX, PullAxis.Y, "armPinMate5axis1", 0);
    checkPinMateFit(context, id + "negativeXCore", ConnectorShape.END, PullAxis.Z, "corePinMate3", 1);
    checkPinMateFit(context, id + "negativeZCore", ConnectorShape.END, PullAxis.X, "corePinMate1", 1);
    for (var thickness in [1, 2, 6])
        checkPanelPinMateFit(context, id + ("flat" ~ thickness),
                { "kind" : HomerackerKind.FLAT_PANEL, "flatThickness" : thickness * millimeter }, ["hole0", "mate"]);
    for (var fit in [PanelFit.INTERFIT, PanelFit.FULL_COVER])
        for (var path in [["corner00", "pinX", "mate"], ["corner11", "pinY", "mate"],
                          ["horizontal0", "pin0", "mate"], ["horizontal1", "pin1", "mate"],
                          ["vertical0", "pin0", "mate"], ["vertical1", "pin0", "mate"]])
            checkPanelPinMateFit(context, id + ("panel" ~ fit ~ path[0]),
                    { "kind" : HomerackerKind.PANEL, "panelFit" : fit }, path);
});
