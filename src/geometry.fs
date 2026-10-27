function makeBox(context is Context, id is Id, center is Vector, dimensions is Vector)
{
    fCuboid(context, id, {
        "corner1" : center - dimensions / 2,
        "corner2" : center + dimensions / 2
    });
}

function body(id is Id) returns Query
{
    return qCreatedBy(id, EntityType.BODY);
}

function add(context is Context, id is Id, targets is Query, tools is Query)
{
    opBoolean(context, id, { "tools" : qUnion([targets, tools]),
            "operationType" : BooleanOperationType.UNION });
}

function cut(context is Context, id is Id, targets is Query, tools is Query)
{
    opBoolean(context, id, { "targets" : targets, "tools" : tools,
            "operationType" : BooleanOperationType.SUBTRACTION });
}

function bevel(context is Context, id is Id, source is Id, amount is ValueWithUnits)
{
    opChamfer(context, id, { "entities" : qCreatedBy(source, EntityType.EDGE),
            "chamferType" : ChamferType.EQUAL_OFFSETS, "width" : amount });
}

function bevelExceptPlane(context is Context, id is Id, source is Id,
        amount is ValueWithUnits, excluded is Plane)
{
    var edges = qCreatedBy(source, EntityType.EDGE);
    opChamfer(context, id, {
            "entities" : qSubtraction(edges, qCoincidesWithPlane(edges, excluded)),
            "chamferType" : ChamferType.EQUAL_OFFSETS, "width" : amount });
}

function bevelOnPlane(context is Context, id is Id, source is Id,
        amount is ValueWithUnits, selected is Plane)
{
    var edges = qCreatedBy(source, EntityType.EDGE);
    opChamfer(context, id, {
            "entities" : qCoincidesWithPlane(edges, selected),
            "chamferType" : ChamferType.EQUAL_OFFSETS, "width" : amount });
}

function bevelCorner(context is Context, id is Id, source is Id,
        corner is Vector, xLength is ValueWithUnits, yLength is ValueWithUnits,
        height is ValueWithUnits, sx is number, sy is number)
{
    var edges = qCreatedBy(source, EntityType.EDGE);
    var xEdge = qClosestTo(edges, corner - vector(sx * xLength / 2,
                                                  0 * millimeter, 0 * millimeter));
    var yEdge = qClosestTo(edges, corner - vector(0 * millimeter,
                                                  sy * yLength / 2, 0 * millimeter));
    var zEdge = qClosestTo(edges, corner - vector(0 * millimeter,
                                                  0 * millimeter, height / 2));
    opChamfer(context, id, {
            "entities" : qUnion([xEdge, yEdge, zEdge]),
            "chamferType" : ChamferType.EQUAL_OFFSETS, "width" : EDGE });
}

function bevelAt(context is Context, id is Id, source is Id, point is Vector,
        amount is ValueWithUnits)
{
    opChamfer(context, id, {
            "entities" : qClosestTo(qCreatedBy(source, EntityType.EDGE), point),
            "chamferType" : ChamferType.EQUAL_OFFSETS, "width" : amount });
}

function bevelMountedWall(context is Context, id is Id, source is Id,
        alongX is boolean, outside is ValueWithUnits, middle is ValueWithUnits,
        length is ValueWithUnits, height is ValueWithUnits)
{
    var edges = qCreatedBy(source, EntityType.EDGE);
    var top = WALL + height;
    var midZ = WALL + height / 2;
    var selected = [];
    if (alongX)
    {
        selected = [
            qClosestTo(edges, vector(0 * millimeter, outside, top)),
            qClosestTo(edges, vector(-length / 2, middle, top)),
            qClosestTo(edges, vector(length / 2, middle, top)),
            qClosestTo(edges, vector(-length / 2, outside, midZ)),
            qClosestTo(edges, vector(length / 2, outside, midZ))
        ];
    }
    else
    {
        selected = [
            qClosestTo(edges, vector(outside, 0 * millimeter, top)),
            qClosestTo(edges, vector(middle, -length / 2, top)),
            qClosestTo(edges, vector(middle, length / 2, top)),
            qClosestTo(edges, vector(outside, -length / 2, midZ)),
            qClosestTo(edges, vector(outside, length / 2, midZ))
        ];
    }
    opChamfer(context, id, {
            "entities" : qUnion(selected),
            "chamferType" : ChamferType.EQUAL_OFFSETS, "width" : EDGE });
}

function bevelMountLedge(context is Context, id is Id, source is Id,
        alongX is boolean, outside is ValueWithUnits, length is ValueWithUnits)
{
    var edges = qCreatedBy(source, EntityType.EDGE);
    var bottom = qCoincidesWithPlane(edges,
            plane(vector(0, 0, 0) * millimeter, vector(0, 0, 1)));
    var endA = alongX ? vector(-length / 2, outside, WALL / 2) :
            vector(outside, -length / 2, WALL / 2);
    var endB = alongX ? vector(length / 2, outside, WALL / 2) :
            vector(outside, length / 2, WALL / 2);
    opChamfer(context, id, {
            "entities" : qUnion([bottom, qClosestTo(edges, endA), qClosestTo(edges, endB)]),
            "chamferType" : ChamferType.EQUAL_OFFSETS, "width" : EDGE });
}

function triangleRelief(context is Context, id is Id,
        p0 is Vector, p1 is Vector, p2 is Vector,
        bottomZ is ValueWithUnits, depth is ValueWithUnits, target is Query)
{
    var sk = newSketchOnPlane(context, id + "sketch", {
            "sketchPlane" : plane(vector(0 * millimeter, 0 * millimeter, bottomZ),
                                  vector(0, 0, 1)) });
    skLineSegment(sk, "a", { "start" : p0, "end" : p1 });
    skLineSegment(sk, "b", { "start" : p1, "end" : p2 });
    skLineSegment(sk, "c", { "start" : p2, "end" : p0 });
    skSolve(sk);
    opExtrude(context, id + "prism", {
            "entities" : qSketchRegion(id + "sketch"),
            "direction" : vector(0, 0, 1),
            "endBound" : BoundingType.BLIND,
            "endDepth" : depth });
    opDeleteBodies(context, id + "deleteSketch", {
            "entities" : body(id + "sketch") });
    cut(context, id + "cut", target, body(id + "prism"));
}

function bridgeCornerCut(context is Context, id is Id, corner is Vector,
        inwardX is number, inwardY is number, target is Query)
{
    // BOSL2 chamfer_corner_mask(chamfer=0.5) makes an octahedron with
    // 1 mm axis intercepts at the selected corner.
    var size = EDGE;
    var sk = newSketchOnPlane(context, id + "sketch", {
            "sketchPlane" : plane(vector(0 * millimeter, 0 * millimeter, corner[2]),
                                  vector(0, 0, 1)) });
    var p0 = vector(corner[0], corner[1]);
    var p1 = vector(corner[0] + inwardX * size, corner[1]);
    var p2 = vector(corner[0], corner[1] + inwardY * size);
    skLineSegment(sk, "a", { "start" : p0, "end" : p1 });
    skLineSegment(sk, "b", { "start" : p1, "end" : p2 });
    skLineSegment(sk, "c", { "start" : p2, "end" : p0 });
    skSolve(sk);
    opPoint(context, id + "apex", {
            "point" : corner - vector(0 * millimeter, 0 * millimeter, size) });
    opLoft(context, id + "tetra", {
            "profileSubqueries" : [qSketchRegion(id + "sketch"),
                                   qCreatedBy(id + "apex", EntityType.VERTEX)] });
    opDeleteBodies(context, id + "deleteProfiles", {
            "entities" : qUnion([body(id + "sketch"), body(id + "apex")]) });
    cut(context, id + "subtract", target, body(id + "tetra"));
}

// A square section in the XY plane. Lofting consecutive pairs rather than the
// full stack keeps the OpenSCAD prismoid's straight faces and sharp transitions.
function squareSection(context is Context, id is Id, z is ValueWithUnits,
        side is ValueWithUnits)
{
    var sk = newSketchOnPlane(context, id, {
            "sketchPlane" : plane(vector(0 * millimeter, 0 * millimeter, z), vector(0, 0, 1)) });
    skRectangle(sk, "square", {
            "firstCorner" : vector(-side / 2, -side / 2),
            "secondCorner" : vector(side / 2, side / 2) });
    skSolve(sk);
}

function squareFrustum(context is Context, id is Id,
        z0 is ValueWithUnits, side0 is ValueWithUnits,
        z1 is ValueWithUnits, side1 is ValueWithUnits)
{
    squareSection(context, id + "lower", z0, side0);
    squareSection(context, id + "upper", z1, side1);
    opLoft(context, id + "loft", {
            "profileSubqueries" : [qSketchRegion(id + "lower"),
                                   qSketchRegion(id + "upper")] });
    opDeleteBodies(context, id + "deleteProfiles", {
            "entities" : qUnion([body(id + "lower"), body(id + "upper")]) });
}

function passage(context is Context, id is Id, center is Vector,
        axis is PullAxis, target is Query)
{
    var straight = id + "straight";
    var dims = axis == PullAxis.X ? vector(ARM, PIN, PIN) :
            (axis == PullAxis.Y ? vector(PIN, ARM, PIN) : vector(PIN, PIN, ARM));
    makeBox(context, straight, center, dims);
    cut(context, id + "straightCut", target, body(straight));
    for (var side in [0, 1])
    {
        var z0 = side == 0 ? -ARM / 2 + LEAD_LIP : ARM / 2 - PIN_BEVEL;
        var z1 = side == 0 ? -ARM / 2 + PIN_BEVEL : ARM / 2 - LEAD_LIP;
        var s0 = side == 0 ? PIN + 2 * PIN_BEVEL : PIN;
        var s1 = side == 0 ? PIN : PIN + 2 * PIN_BEVEL;
        var taper = id + ("lead" ~ side);
        squareFrustum(context, taper, z0, s0, z1, s1);
        var rot = rotationAround(line(vector(0, 0, 0) * millimeter, vector(0, 1, 0)), 0 * degree);
        if (axis == PullAxis.X)
            rot = rotationAround(line(vector(0, 0, 0) * millimeter, vector(0, 1, 0)), 90 * degree);
        if (axis == PullAxis.Y)
            rot = rotationAround(line(vector(0, 0, 0) * millimeter, vector(1, 0, 0)), -90 * degree);
        opTransform(context, taper + "place", {
                "bodies" : body(taper + "loft"),
                "transform" : transform(center) * rot });
        // BOSL2's partial-edge cuboid chamfer reaches full width 0.01 mm
        // before the outside face, leaving a short square entry lip.
        var lipOffset = (side == 0 ? -1 : 1) * (ARM / 2 - LEAD_LIP / 2);
        var overlapShift = (side == 0 ? 1 : -1) * LEAD_JOIN_OVERLAP / 2;
        lipOffset = lipOffset + overlapShift;
        var lipCenter = center +
                (axis == PullAxis.X ? vector(lipOffset, 0 * millimeter, 0 * millimeter) :
                 (axis == PullAxis.Y ? vector(0 * millimeter, lipOffset, 0 * millimeter) :
                                       vector(0 * millimeter, 0 * millimeter, lipOffset)));
        var lipDims = axis == PullAxis.X ?
                vector(LEAD_LIP + LEAD_JOIN_OVERLAP, PIN + 2 * PIN_BEVEL, PIN + 2 * PIN_BEVEL) :
                (axis == PullAxis.Y ?
                 vector(PIN + 2 * PIN_BEVEL, LEAD_LIP + LEAD_JOIN_OVERLAP, PIN + 2 * PIN_BEVEL) :
                 vector(PIN + 2 * PIN_BEVEL, PIN + 2 * PIN_BEVEL, LEAD_LIP + LEAD_JOIN_OVERLAP));
        makeBox(context, taper + "lip", lipCenter, lipDims);
        add(context, taper + "joinLip", body(taper + "loft"), body(taper + "lip"));
        cut(context, taper + "cut", target, body(taper + "loft"));
    }
}

function panelPassage(context is Context, id is Id, center is Vector,
        axis is PullAxis, entrySign is number, target is Query)
{
    var straight = id + "straight";
    var dims = axis == PullAxis.X ? vector(WALL + 1 * millimeter, PIN, PIN) :
            vector(PIN, WALL + 1 * millimeter, PIN);
    makeBox(context, straight, center, dims);
    cut(context, id + "straightCut", target, body(straight));
    var z0 = entrySign < 0 ? -WALL / 2 + LEAD_LIP : WALL / 2 - PIN_BEVEL;
    var z1 = entrySign < 0 ? -WALL / 2 + PIN_BEVEL : WALL / 2 - LEAD_LIP;
    var s0 = entrySign < 0 ? PIN + 2 * PIN_BEVEL : PIN;
    var s1 = entrySign < 0 ? PIN : PIN + 2 * PIN_BEVEL;
    var taper = id + "lead";
    squareFrustum(context, taper, z0, s0, z1, s1);
    var rot = axis == PullAxis.X ?
            rotationAround(line(vector(0, 0, 0) * millimeter, vector(0, 1, 0)), 90 * degree) :
            rotationAround(line(vector(0, 0, 0) * millimeter, vector(1, 0, 0)), -90 * degree);
    opTransform(context, taper + "place", {
            "bodies" : body(taper + "loft"),
            "transform" : transform(center) * rot });
    var lipOffset = entrySign * (WALL / 2 - LEAD_LIP / 2 - LEAD_JOIN_OVERLAP / 2);
    var lipCenter = center + (axis == PullAxis.X ?
            vector(lipOffset, 0 * millimeter, 0 * millimeter) :
            vector(0 * millimeter, lipOffset, 0 * millimeter));
    var lipDims = axis == PullAxis.X ?
            vector(LEAD_LIP + LEAD_JOIN_OVERLAP, PIN + 2 * PIN_BEVEL, PIN + 2 * PIN_BEVEL) :
            vector(PIN + 2 * PIN_BEVEL, LEAD_LIP + LEAD_JOIN_OVERLAP, PIN + 2 * PIN_BEVEL);
    makeBox(context, id + "lip", lipCenter, lipDims);
    add(context, id + "joinLeadLip", body(taper + "loft"), body(id + "lip"));
    cut(context, id + "leadCut", target, body(taper + "loft"));
}
