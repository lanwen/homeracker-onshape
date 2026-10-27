// Canonical hole along Z. At the centre the square is 4.8 mm, narrowing to
// 4.0 mm at +/-6.7 mm, then flaring to 5.6 mm at +/-7.5 mm.
function supportHole(context is Context, id is Id, center is Vector, axis is PullAxis)
{
    var stations = [-U / 2, -U / 2 + PIN_BEVEL, 0 * millimeter,
                    U / 2 - PIN_BEVEL, U / 2];
    var sides = [PIN + 2 * PIN_BEVEL, PIN, PIN + 0.8 * millimeter,
                 PIN, PIN + 2 * PIN_BEVEL];
    for (var k in [0, 1, 2, 3])
    {
        var seg = id + ("segment" ~ k);
        squareFrustum(context, seg, stations[k], sides[k], stations[k + 1], sides[k + 1]);
        var rot = rotationAround(line(vector(0, 0, 0) * millimeter, vector(0, 1, 0)), 0 * degree);
        if (axis == PullAxis.X)
            rot = rotationAround(line(vector(0, 0, 0) * millimeter, vector(0, 1, 0)), 90 * degree);
        if (axis == PullAxis.Y)
            rot = rotationAround(line(vector(0, 0, 0) * millimeter, vector(1, 0, 0)), -90 * degree);
        opTransform(context, seg + "place", {
                "bodies" : body(seg + "loft"),
                "transform" : transform(center) * rot });
    }
}

function support(context is Context, id is Id, units is number, xHoles is boolean, mates is boolean)
{
    var bar = id + "bar";
    makeBox(context, bar, vector(0, 0, 0) * millimeter, vector(U, units * U, U));
    bevel(context, id + "edgeChamfer", bar, EDGE);
    if (mates)
    {
        assemblyMate(context, id + "startMate", "Support -Y end", body(bar),
                vector(0 * millimeter, -units * U / 2, 0 * millimeter), vector(0, -1, 0));
        assemblyMate(context, id + "endMate", "Support +Y end", body(bar),
                vector(0 * millimeter, units * U / 2, 0 * millimeter), vector(0, 1, 0));
    }
    for (var i = 0; i < units; i += 1)
    {
        var c = vector(0 * millimeter, (i - (units - 1) / 2) * U, 0 * millimeter);
        if (mates)
        {
            assemblyMate(context, id + ("pinMate" ~ i), "Support Z pin " ~ (i + 1),
                    body(bar), c, vector(0, 0, 1));
            if (xHoles)
                assemblyMate(context, id + ("xPinMate" ~ i), "Support X pin " ~ (i + 1),
                        body(bar), c, vector(1, 0, 0));
        }
        var h = id + ("hole" ~ i);
        supportHole(context, h, c, PullAxis.Z);
        for (var k in [0, 1, 2, 3])
            cut(context, h + ("cut" ~ k), body(bar), body(h + ("segment" ~ k) + "loft"));
        if (xHoles)
        {
            var xh = id + ("xhole" ~ i);
            supportHole(context, xh, c, PullAxis.X);
            for (var k in [0, 1, 2, 3])
                cut(context, xh + ("cut" ~ k), body(bar), body(xh + ("segment" ~ k) + "loft"));
        }
    }
}
