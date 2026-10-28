export enum ConnectorShape
{
    annotation { "Name" : "End (2U)" } END,
    annotation { "Name" : "Straight (3U)" } STRAIGHT,
    annotation { "Name" : "Elbow" } ELBOW,
    annotation { "Name" : "T-shape" } TEE,
    annotation { "Name" : "X-shape" } CROSS,
    annotation { "Name" : "Corner 3-way" } CORNER3,
    annotation { "Name" : "Corner 4-way" } CORNER4,
    annotation { "Name" : "5-way" } FIVE,
    annotation { "Name" : "6-way" } SIX,
    annotation { "Name" : "Custom dimensions", "Hidden" : true } LEGACY
}

// A validated socket layout; keep dimensions, arm count, and label together.
export type ConnectorSpec typecheck canBeConnectorSpec;

export predicate canBeConnectorSpec(value)
{
    value is map;
    value.dimensions is number;
    value.dimensions == round(value.dimensions);
    value.dimensions >= 1 && value.dimensions <= 3;
    value.ways is number;
    value.ways == round(value.ways);
    value.ways >= value.dimensions && value.ways <= 2 * value.dimensions;
    value.name is string;
    value.name != "";
}

function connectorSpec(dimensions is number, ways is number, name is string) returns ConnectorSpec
{
    var spec = { "dimensions" : dimensions, "ways" : ways, "name" : name };
    if (!canBeConnectorSpec(spec))
        throw regenError("Choose 1–2 directions for 1 dimension, 2–4 for 2 dimensions, or 3–6 for 3 dimensions.");
    return spec as ConnectorSpec;
}

function connectorShapeSpec(shape is ConnectorShape) returns ConnectorSpec
{
    if (shape == ConnectorShape.END) return connectorSpec(1, 1, "End");
    if (shape == ConnectorShape.STRAIGHT) return connectorSpec(1, 2, "Straight");
    if (shape == ConnectorShape.ELBOW) return connectorSpec(2, 2, "Elbow");
    if (shape == ConnectorShape.TEE) return connectorSpec(2, 3, "T-shape");
    if (shape == ConnectorShape.CROSS) return connectorSpec(2, 4, "X-shape");
    if (shape == ConnectorShape.CORNER3) return connectorSpec(3, 3, "Corner 3-way");
    if (shape == ConnectorShape.CORNER4) return connectorSpec(3, 4, "Corner 4-way");
    if (shape == ConnectorShape.FIVE) return connectorSpec(3, 5, "5-way");
    return connectorSpec(3, 6, "6-way");
}

// Side order matches the connector socket order.
export type CorePinHoles typecheck canBeCorePinHoles;

export predicate canBeCorePinHoles(value)
{
    value is map;
    value.sides is array;
    size(value.sides) == 6;
    for (var side in value.sides)
        side is boolean;
}

function corePinHoles(positiveX is boolean, negativeX is boolean,
        positiveY is boolean, negativeY is boolean, negativeZ is boolean) returns CorePinHoles
{
    return { "sides" : [false, negativeZ, positiveX, negativeX, positiveY, negativeY] } as CorePinHoles;
}

function corePinSideAxis(side is number) returns PullAxis
{
    return side < 2 ? PullAxis.Z : (side < 4 ? PullAxis.X : PullAxis.Y);
}

function corePinSideAvailable(spec is ConnectorSpec, side is number, pull is PullAxis) returns boolean
{
    return pull != PullAxis.NONE && corePinSideAxis(side) != pull && !armOn(spec, side);
}

function corePinPassage(side is number) returns PinPassageSpec
{
    var normal = armCenter(side) / U;
    return pinPassageSpec(normal * ((ARM + ARM_INNER) / 4), corePinSideAxis(side), WALL);
}

function applyCorePinHoles(context is Context, id is Id, spec is ConnectorSpec,
        holes is CorePinHoles, pull is PullAxis)
{
    for (var side = 0; side < 6; side += 1)
        if (holes.sides[side] && corePinSideAvailable(spec, side, pull))
        {
            var passageId = id + ("corePin" ~ side);
            var target = qSubtraction(
                    qBodyType(qCreatedBy(id, EntityType.BODY), BodyType.SOLID),
                    qCreatedBy(passageId, EntityType.BODY));
            applyPinPassage(context, passageId, target, corePinPassage(side));
        }
}

function addCorePinMates(context is Context, id is Id, spec is ConnectorSpec,
        holes is CorePinHoles, pull is PullAxis)
{
    var owner = qBodyType(qCreatedBy(id, EntityType.BODY), BodyType.SOLID);
    var names = ["+Z", "-Z", "+X", "-X", "+Y", "-Y"];
    for (var side = 0; side < 6; side += 1)
        if (holes.sides[side] && corePinSideAvailable(spec, side, pull))
            assemblyMate(context, id + ("corePinMate" ~ side), "Core " ~ names[side] ~ " pin", owner,
                    (armCenter(side) / U) * ARM_INNER / 2, -armCenter(side) / U,
                    connectorAxisDirection(pull));
}

// Connector arm order follows the upstream lookup: +Z, -Z, +X, -X, +Y, -Y.
function armOn(spec is ConnectorSpec, index is number) returns boolean
{
    if (spec.dimensions == 1)
        return index == 0 || (spec.ways == 2 && index == 1);
    if (spec.dimensions == 2)
        return index == 0 || index == 2 ||
                (spec.ways >= 3 && index == 1) || (spec.ways == 4 && index == 3);
    return index == 0 || index == 2 || index == 4 ||
            (spec.ways >= 4 && index == 1) ||
            (spec.ways >= 5 && index == 3) ||
            (spec.ways == 6 && index == 5);
}

function armCenter(index is number) returns Vector
{
    if (index == 0) return vector(0 * millimeter, 0 * millimeter, U);
    if (index == 1) return vector(0 * millimeter, 0 * millimeter, -U);
    if (index == 2) return vector(U, 0 * millimeter, 0 * millimeter);
    if (index == 3) return vector(-U, 0 * millimeter, 0 * millimeter);
    if (index == 4) return vector(0 * millimeter, U, 0 * millimeter);
    return vector(0 * millimeter, -U, 0 * millimeter);
}

function armBottom(index is number) returns Plane
{
    if (index == 0) return plane(vector(0 * millimeter, 0 * millimeter, U / 2), vector(0, 0, 1));
    if (index == 1) return plane(vector(0 * millimeter, 0 * millimeter, -U / 2), vector(0, 0, 1));
    if (index == 2) return plane(vector(U / 2, 0 * millimeter, 0 * millimeter), vector(1, 0, 0));
    if (index == 3) return plane(vector(-U / 2, 0 * millimeter, 0 * millimeter), vector(1, 0, 0));
    if (index == 4) return plane(vector(0 * millimeter, U / 2, 0 * millimeter), vector(0, 1, 0));
    return plane(vector(0 * millimeter, -U / 2, 0 * millimeter), vector(0, 1, 0));
}

function printTetrahedron(context is Context, id is Id)
{
    var offset = ARM / 2 - EDGE;
    var side = U - FIT / 2 - WALL / 2;
    var tri = id + "triangle";
    var sk = newSketchOnPlane(context, tri, {
            "sketchPlane" : plane(vector(0 * millimeter, 0 * millimeter, offset), vector(0, 0, 1)) });
    skLineSegment(sk, "a", { "start" : vector(offset, offset),
                            "end" : vector(offset + side, offset) });
    skLineSegment(sk, "b", { "start" : vector(offset + side, offset),
                            "end" : vector(offset, offset + side) });
    skLineSegment(sk, "c", { "start" : vector(offset, offset + side),
                            "end" : vector(offset, offset) });
    skSolve(sk);
    opPoint(context, id + "apex", {
            "point" : vector(offset, offset, offset + side) });
    opLoft(context, id + "loft", {
            "profileSubqueries" : [qSketchRegion(tri),
                                   qCreatedBy(id + "apex", EntityType.VERTEX)] });
    opDeleteBodies(context, id + "deleteProfiles", {
            "entities" : qUnion([body(tri), body(id + "apex")]) });
}

function connectorPinAxis(side is number, passage is number) returns PullAxis
{
    return side < 2 ? (passage == 0 ? PullAxis.X : PullAxis.Y) :
            (side < 4 ? (passage == 0 ? PullAxis.Y : PullAxis.Z) :
            (passage == 0 ? PullAxis.X : PullAxis.Z));
}

function connectorAxisDirection(axis is PullAxis) returns Vector
{
    return axis == PullAxis.X ? vector(1, 0, 0) :
            (axis == PullAxis.Y ? vector(0, 1, 0) : vector(0, 0, 1));
}

function addConnectorMates(context is Context, id is Id, spec is ConnectorSpec,
        coreHoles is CorePinHoles, pull is PullAxis)
{
    addCorePinMates(context, id, spec, coreHoles, pull);
    var owner = qBodyType(qCreatedBy(id, EntityType.BODY), BodyType.SOLID);
    var names = ["+Z", "-Z", "+X", "-X", "+Y", "-Y"];
    if (pull != PullAxis.NONE)
        for (var side = 0; side < 6; side += 1)
            if (corePinSideAxis(side) == pull)
            {
                var normal = armCenter(side) / U;
                var distance = armOn(spec, side) ? 1.5 * U : ARM / 2;
                assemblyMate(context, id + ("pullThroughMate" ~ side),
                        "Pull-through " ~ names[side] ~ " opening", owner, normal * distance, normal);
            }
    for (var side = 0; side < 6; side += 1)
        if (armOn(spec, side))
        {
            // Axial socket stops disappear when the support can pass through.
            if (corePinSideAxis(side) != pull)
                assemblyMate(context, id + ("socketMate" ~ side), "Socket " ~ names[side],
                        owner, armCenter(side) / 2, armCenter(side) / U);
            for (var passage in [0, 1])
            {
                var axis = connectorPinAxis(side, passage);
                addPinOpeningMates(context, id + ("armPinMate" ~ side ~ "axis" ~ passage),
                        id + ("oppositeArmPinMate" ~ side ~ "axis" ~ passage),
                        "Arm " ~ names[side] ~ " " ~ axis ~ " pin", owner,
                        { "center" : armCenter(side), "normal" : connectorAxisDirection(axis),
                          "span" : ARM_INNER, "along" : armCenter(side) / U, "inside" : true });
            }
        }
}

function connector(context is Context, id is Id, spec is ConnectorSpec, pull is PullAxis, coreHoles is CorePinHoles, mates is boolean)
{
    var core = id + "core";
    var cavities = [];
    makeBox(context, core, vector(0, 0, 0) * millimeter, vector(ARM, ARM, ARM));
    bevel(context, id + "coreChamfer", core, EDGE);
    for (var i in [0, 1, 2, 3, 4, 5])
    {
        if (armOn(spec, i))
        {
            var a = id + ("arm" ~ i);
            var outer = a + "outer";
            var c = armCenter(i);
            var dims = i < 2 ? vector(ARM, ARM, U) :
                    (i < 4 ? vector(U, ARM, ARM) : vector(ARM, U, ARM));
            var innerDims = i < 2 ? vector(ARM_INNER, ARM_INNER, U) :
                    (i < 4 ? vector(U, ARM_INNER, ARM_INNER) : vector(ARM_INNER, U, ARM_INNER));
            makeBox(context, outer, c, dims);
            bevelExceptPlane(context, a + "outerChamfer", outer, EDGE, armBottom(i));
            makeBox(context, a + "cavity", c, innerDims);
            bevelOnPlane(context, a + "innerChamfer", a + "cavity", EDGE, armBottom(i));
            cavities = append(cavities, body(a + "cavity"));
            // Both transverse directions have a square pin passage.
            for (var j in [0, 1])
            {
                var axis = connectorPinAxis(i, j);
                var hole = a + ("pin" ~ j);
                applyPinPassage(context, hole, body(outer), pinPassageSpec(c, axis, ARM));
            }
            add(context, a + "join", body(core), body(outer));
        }
    }
    // The original differences ALL inner arm cavities from the assembled
    // core-plus-arms. This also clears the 2.1 mm socket overlap in the core.
    cut(context, id + "hollowAllArms", body(core), qUnion(cavities));
    if (spec.ways > 4)
    {
        var tetra = id + "printInterface";
        printTetrahedron(context, tetra);
        add(context, id + "joinInterface", body(core), body(tetra + "loft"));
    }
    else if (!(spec.ways == 4 && spec.dimensions == 2))
    {
        // OpenSCAD intersects the raw connector with the printing base.
        var mask = id + "printBase";
        makeBox(context, mask, vector(ARM / 2, ARM / 2, 0 * millimeter),
                vector(2 * ARM, 2 * ARM, 3 * U));
        var cornerEdge = qClosestTo(qCreatedBy(mask, EntityType.EDGE),
                vector(-ARM / 2, -ARM / 2, 0 * millimeter));
        opChamfer(context, mask + "cornerChamfer", {
                "entities" : cornerEdge,
                "chamferType" : ChamferType.EQUAL_OFFSETS,
                "width" : 3 * EDGE });
        opBoolean(context, id + "clipToPrintBase", {
                "tools" : qUnion([body(core), body(mask)]),
                "operationType" : BooleanOperationType.INTERSECTION });
    }
    if (pull != PullAxis.NONE)
    {
        var pullTarget = qUnion(evaluateQuery(context,
                qBodyType(qCreatedBy(id, EntityType.BODY), BodyType.SOLID)));
        var pass = id + "pullThrough";
        var p = 4 * U;
        var dims = pull == PullAxis.X ? vector(p, ARM_INNER, ARM_INNER) :
                (pull == PullAxis.Y ? vector(ARM_INNER, p, ARM_INNER) :
                vector(ARM_INNER, ARM_INNER, p));
        makeBox(context, pass, vector(0, 0, 0) * millimeter, dims);
        // The printing-base intersection can replace the original core body.
        // Capture the finished connector before creating the cutter.
        cut(context, id + "pullCut", pullTarget, body(pass));
    }
    applyCorePinHoles(context, id, spec, coreHoles, pull);
    if (mates)
        addConnectorMates(context, id, spec, coreHoles, pull);
}

// Onshape dialog conditions accept booleans and enums, not numeric comparisons.
function connectorEditLogic(definition is map) returns map
{
    var spec = definition.connectorShape == ConnectorShape.LEGACY ?
            { "dimensions" : definition.connectorDimensions, "ways" : definition.connectorWays, "name" : "Custom" } :
            connectorShapeSpec(definition.connectorShape);
    var valid = canBeConnectorSpec(spec);
    definition.connectorWallPositiveX = valid && corePinSideAvailable(spec as ConnectorSpec, 2, definition.pullAxis);
    definition.connectorWallNegativeX = valid && corePinSideAvailable(spec as ConnectorSpec, 3, definition.pullAxis);
    definition.connectorWallPositiveY = valid && corePinSideAvailable(spec as ConnectorSpec, 4, definition.pullAxis);
    definition.connectorWallNegativeY = valid && corePinSideAvailable(spec as ConnectorSpec, 5, definition.pullAxis);
    definition.connectorWallNegativeZ = valid && corePinSideAvailable(spec as ConnectorSpec, 1, definition.pullAxis);
    return definition;
}
