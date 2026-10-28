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
    homeracker(context, supportId, { "kind" : HomerackerKind.SUPPORT, "supportUnits" : 3 });
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

annotation { "Feature Type Name" : "Pin mate fit checks" }
export const pinMateFitChecks = defineFeature(function(context is Context, id is Id, definition is map)
precondition {} {
    checkPinMateFit(context, id + "positiveZArm", ConnectorShape.ELBOW, PullAxis.Z, "armPinMate0axis0", 0);
    checkPinMateFit(context, id + "negativeZArm", ConnectorShape.STRAIGHT, PullAxis.Z, "armPinMate1axis1", 0);
    checkPinMateFit(context, id + "positiveXArm", ConnectorShape.ELBOW, PullAxis.X, "armPinMate2axis0", 0);
    checkPinMateFit(context, id + "negativeYArm", ConnectorShape.SIX, PullAxis.Y, "armPinMate5axis1", 0);
    checkPinMateFit(context, id + "negativeXCore", ConnectorShape.END, PullAxis.Z, "corePinMate3", 1);
    checkPinMateFit(context, id + "negativeZCore", ConnectorShape.END, PullAxis.X, "corePinMate1", 1);
});
