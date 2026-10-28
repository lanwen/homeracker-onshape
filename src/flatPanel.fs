// A rectangular mounting blank in XY, with pin holes normal to its flat face.
// Hole positions are measured from the centre in millimetres, without snapping.
function flatPanel(context is Context, id is Id, width is ValueWithUnits,
        height is ValueWithUnits, thickness is ValueWithUnits, faceChamfers is boolean,
        holes is array, mates is boolean)
{
    var plate = id + "plate";
    makeBox(context, plate, vector(0, 0, 0) * millimeter, vector(width, height, thickness));
    var edgeBevel = min(EDGE, thickness / 2 - 0.01 * millimeter);
    if (faceChamfers)
        bevel(context, id + "faceChamfer", plate, edgeBevel);
    // Keep the entrances beveled regardless of the face perimeter option.
    var lead = min(PIN_BEVEL, thickness / 2 - 0.05 * millimeter);
    for (var i = 0; i < size(holes); i += 1)
    {
        var hole = holes[i];
        var radius = hole.holeSize / 2 + lead;
        var margin = faceChamfers ? edgeBevel : 0 * millimeter;
        if (abs(hole.holeX) + radius + margin >= width / 2 ||
            abs(hole.holeY) + radius + margin >= height / 2)
            throw regenError("Pin hole " ~ (i + 1) ~ " is too close to the panel edge. Move it inward or increase the panel size.", ["flatHoles"]);
        for (var j = 0; j < i; j += 1)
        {
            var other = holes[j];
            var spacing = (hole.holeSize + other.holeSize) / 2 + 2 * lead;
            if (abs(hole.holeX - other.holeX) <= spacing &&
                abs(hole.holeY - other.holeY) <= spacing)
                throw regenError("Pin holes " ~ (j + 1) ~ " and " ~ (i + 1) ~ " overlap. Move them apart.", ["flatHoles"]);
        }
        var tag = id + ("hole" ~ i);
        var center = vector(hole.holeX, hole.holeY, 0 * millimeter);
        makeBox(context, tag + "straight", center,
                vector(hole.holeSize, hole.holeSize, thickness + 0.1 * millimeter));
        for (var side in [0, 1])
        {
            var taper = tag + ("lead" ~ side);
            var z0 = side == 0 ? -thickness / 2 : thickness / 2 - lead;
            var z1 = side == 0 ? -thickness / 2 + lead : thickness / 2;
            var s0 = side == 0 ? hole.holeSize + 2 * lead : hole.holeSize;
            var s1 = side == 0 ? hole.holeSize : hole.holeSize + 2 * lead;
            squareFrustum(context, taper, z0, s0, z1, s1);
            opTransform(context, taper + "place", {
                    "bodies" : body(taper + "loft"), "transform" : transform(center) });
            add(context, tag + ("joinLead" ~ side), body(tag + "straight"), body(taper + "loft"));
        }
        cut(context, tag + "cut", body(plate), body(tag + "straight"));
        if (mates)
            assemblyMate(context, tag + "mate", "Face pin " ~ (i + 1), body(plate), center, vector(0, 0, 1));
    }
    if (mates)
        assemblyMate(context, id + "faceMate", "Flat panel face", body(plate),
                vector(0 * millimeter, 0 * millimeter, -thickness / 2), vector(0, 0, -1));
}
