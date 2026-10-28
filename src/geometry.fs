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

function bevelAt(context is Context, id is Id, source is Id, point is Vector,
        amount is ValueWithUnits)
{
    opChamfer(context, id, {
            "entities" : qClosestTo(qCreatedBy(source, EntityType.EDGE), point),
            "chamferType" : ChamferType.EQUAL_OFFSETS, "width" : amount });
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

// Owned connectors travel with their part when inserted into an assembly.
// A face attachment also follows later Part Studio transformations. Attaching
// directly to a body fails to resolve its frame in older Part Studio contexts.
function assemblyMate(context is Context, id is Id, label is string, owner is Query,
        origin is Vector, normal is Vector)
{
    var x = abs(normal[0]) > 0.5 ? vector(0, 1, 0) : vector(1, 0, 0);
    assemblyMate(context, id, label, owner, origin, normal, x);
}

function assemblyMate(context is Context, id is Id, label is string, owner is Query,
        origin is Vector, normal is Vector, along is Vector)
{
    opMateConnector(context, id, {
            "coordSystem" : coordSystem(origin, along, normal),
            "owner" : owner,
            "attachTo" : qNthElement(qClosestTo(qOwnedByBody(owner, EntityType.FACE), origin), 0) });
    setProperty(context, { "entities" : qCreatedBy(id, EntityType.BODY),
            "propertyType" : PropertyType.NAME, "value" : label });
}
