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

export enum SupportPinMates
{
    annotation { "Name" : "None" } NONE,
    annotation { "Name" : "Selected rows" } SELECTED,
    annotation { "Name" : "All rows" } ALL
}

// Each row has independent +Z, -Z, +X, and -X opening selections.
// Repeated rows combine their sides without creating duplicate connectors.
function supportMateRows(mode is SupportPinMates, rows is array, units is number, xHoles is boolean) returns array
{
    var selected = [];
    for (var i = 0; i < units; i += 1)
        selected = append(selected, [mode == SupportPinMates.ALL, mode == SupportPinMates.ALL,
                mode == SupportPinMates.ALL && xHoles, mode == SupportPinMates.ALL && xHoles]);
    if (mode == SupportPinMates.SELECTED)
        for (var row in rows)
        {
            if (!(row.mateRow is number) || row.mateRow < 1 || row.mateRow > units || row.mateRow != round(row.mateRow))
                throw regenError("Choose a pin mate row from 1 to the support length in grid units.", ["supportMateRows"]);
            const index = row.mateRow - 1;
            var sides = selected[index];
            sides[0] = sides[0] || row.positiveZ != false;
            sides[1] = sides[1] || row.negativeZ == true;
            sides[2] = sides[2] || (xHoles && row.positiveX == true);
            sides[3] = sides[3] || (xHoles && row.negativeX == true);
            selected[index] = sides;
        }
    return selected;
}

function supportPinMatesAt(context is Context, id is Id, owner is Query, center is Vector,
        station is number, sides is array)
{
    const suffixes = ["pinMate", "oppositePinMate", "xPinMate", "oppositeXPinMate"];
    const normals = [vector(0, 0, 1), vector(0, 0, -1), vector(1, 0, 0), vector(-1, 0, 0)];
    const labels = ["Z pin ", "Z pin ", "X pin ", "X pin "];
    for (var side = 0; side < 4; side += 1)
        if (sides[side])
            assemblyMate(context, id + (suffixes[side] ~ station),
                    "Support " ~ labels[side] ~ (station + 1) ~ (side % 2 == 0 ? " + opening" : " - opening"),
                    owner, center + normals[side] * U / 2, normals[side], vector(0, 1, 0));
}

function support(context is Context, id is Id, units is number, xHoles is boolean,
        mates is boolean, pinMode is SupportPinMates, pinRows is array)
{
    const selected = supportMateRows(mates ? pinMode : SupportPinMates.NONE, pinRows, units, xHoles);
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
            supportPinMatesAt(context, id, body(bar), c, i, selected[i]);
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
