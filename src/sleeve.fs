// The upstream sleeve hole cutter is 0.01 mm wider than the sleeve. Keeping
// that extra depth preserves its entrance taper positions after clipping.
function sleevePassage(context is Context, id is Id, center is Vector,
        target is Query, chamfers is boolean)
{
    var depth = ARM + LEAD_LIP;
    makeBox(context, id + "straight", center, vector(depth, PIN, PIN));
    cut(context, id + "straightCut", target, body(id + "straight"));
    if (!chamfers)
        return;
    for (var side in [0, 1])
    {
        var taper = id + ("lead" ~ side);
        var z0 = side == 0 ? -depth / 2 + LEAD_LIP : depth / 2 - PIN_BEVEL;
        var z1 = side == 0 ? -depth / 2 + PIN_BEVEL : depth / 2 - LEAD_LIP;
        var s0 = side == 0 ? PIN + 2 * PIN_BEVEL : PIN;
        var s1 = side == 0 ? PIN : PIN + 2 * PIN_BEVEL;
        squareFrustum(context, taper, z0, s0, z1, s1);
        opTransform(context, taper + "place", {
                "bodies" : body(taper + "loft"),
                "transform" : transform(center) * rotationAround(
                        line(vector(0, 0, 0) * millimeter, vector(0, 1, 0)), 90 * degree) });
        var sign = side == 0 ? -1 : 1;
        var offset = sign * (depth / 2 - LEAD_LIP / 2 - LEAD_JOIN_OVERLAP / 2);
        makeBox(context, taper + "lip", center + vector(offset, 0 * millimeter, 0 * millimeter),
                vector(LEAD_LIP + LEAD_JOIN_OVERLAP, PIN + 2 * PIN_BEVEL, PIN + 2 * PIN_BEVEL));
        add(context, taper + "joinLip", body(taper + "loft"), body(taper + "lip"));
        cut(context, taper + "cut", target, body(taper + "loft"));
    }
}

// Upstream coordinates: sleeve length along Z, its open side toward +Y.
function sleeve(context is Context, id is Id, units is number,
        chamfers is boolean, mates is boolean)
{
    var depth = U + WALL + FIT / 2;
    var height = units * U - FIT;
    var innerBack = -depth / 2 + WALL;
    var pinY = (WALL + FIT / 2) / 2;
    var shell = id + "shell";
    makeBox(context, shell, vector(0, 0, 0) * millimeter, vector(ARM, depth, height));
    if (chamfers)
        bevel(context, id + "outerChamfer", shell, EDGE);
    // BOSL2 align(BACK, inside=true) makes the cavity flush with +Y.
    makeBox(context, id + "cavity", vector(0 * millimeter, WALL / 2, 0 * millimeter),
            vector(U + FIT, U + FIT / 2, height + 2 * millimeter));
    cut(context, id + "openBack", body(shell), body(id + "cavity"));
    for (var i = 0; i < units; i += 1)
    {
        var center = vector(0 * millimeter, pinY, (i - (units - 1) / 2) * U);
        sleevePassage(context, id + ("hole" ~ i), center, body(shell), chamfers);
    }
    if (mates)
    {
        var owner = qBodyType(qCreatedBy(id, EntityType.BODY), BodyType.SOLID);
        assemblyMate(context, id + "supportMate", "Sleeve support axis", owner,
                vector(0 * millimeter, pinY, 0 * millimeter), vector(0, 0, 1));
        // Reproduce upstream's overridden BACK attachment on the inner wall.
        assemblyMate(context, id + "innerBackMate", "Sleeve inner back", owner,
                vector(0 * millimeter, innerBack, 0 * millimeter), vector(0, 1, 0));
        for (var i = 0; i < units; i += 1)
            assemblyMate(context, id + ("pinMate" ~ i), "Sleeve X pin " ~ (i + 1), owner,
                    vector(0 * millimeter, pinY, (i - (units - 1) / 2) * U), vector(1, 0, 0));
    }
}
