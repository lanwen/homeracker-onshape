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
        // Keep the upstream sleeve cutter's extra 0.01 mm depth.
        applyPinPassage(context, id + ("hole" ~ i), body(shell),
                pinPassageSpec(center, PullAxis.X, ARM + LEAD_LIP));
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
