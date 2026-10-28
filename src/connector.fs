// Connector arm order follows the upstream lookup: +Z, -Z, +X, -X, +Y, -Y.
function armOn(dimensions is number, ways is number, index is number) returns boolean
{
    if (dimensions == 1)
        return index == 0 || (ways == 2 && index == 1);
    if (dimensions == 2)
        return index == 0 || index == 2 ||
                (ways >= 3 && index == 1) || (ways == 4 && index == 3);
    return index == 0 || index == 2 || index == 4 ||
            (ways >= 4 && index == 1) ||
            (ways >= 5 && index == 3) ||
            (ways == 6 && index == 5);
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

function connector(context is Context, id is Id, dimensions is number,
        ways is number, pull is PullAxis, mates is boolean)
{
    if (dimensions < 1 || dimensions > 3 || ways < dimensions || ways > 2 * dimensions)
        throw regenError("Choose 1–2 directions for 1 dimension, 2–4 for 2 dimensions, or 3–6 for 3 dimensions.");
    var core = id + "core";
    var cavities = [];
    makeBox(context, core, vector(0, 0, 0) * millimeter, vector(ARM, ARM, ARM));
    bevel(context, id + "coreChamfer", core, EDGE);
    for (var i in [0, 1, 2, 3, 4, 5])
    {
        if (armOn(dimensions, ways, i))
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
                var axis = i < 2 ? (j == 0 ? PullAxis.X : PullAxis.Y) :
                        (i < 4 ? (j == 0 ? PullAxis.Y : PullAxis.Z) :
                        (j == 0 ? PullAxis.X : PullAxis.Z));
                var hole = a + ("pin" ~ j);
                passage(context, hole, c, axis, body(outer));
            }
            add(context, a + "join", body(core), body(outer));
        }
    }
    // The original differences ALL inner arm cavities from the assembled
    // core-plus-arms. This also clears the 2.1 mm socket overlap in the core.
    cut(context, id + "hollowAllArms", body(core), qUnion(cavities));
    if (ways > 4)
    {
        var tetra = id + "printInterface";
        printTetrahedron(context, tetra);
        add(context, id + "joinInterface", body(core), body(tetra + "loft"));
    }
    else if (!(ways == 4 && dimensions == 2))
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
    if (mates)
    {
        var names = ["+Z", "-Z", "+X", "-X", "+Y", "-Y"];
        for (var i in [0, 1, 2, 3, 4, 5])
        {
            if (armOn(dimensions, ways, i))
            {
                var normal = armCenter(i) / U;
                // A support end seats at the socket floor, half a grid unit
                // from the core centre, rather than at the open mouth.
                assemblyMate(context, id + ("socketMate" ~ i), "Socket " ~ names[i],
                        qBodyType(qCreatedBy(id, EntityType.BODY), BodyType.SOLID), armCenter(i) / 2, normal);
            }
        }
    }
}
