// Two overlapping rectangles cover a connector core and its two inward arms.
// Corner indices: top-left, top-right, bottom-left, bottom-right.
function flatNotchRectangles(width is ValueWithUnits, height is ValueWithUnits,
        corner is number, clearance is ValueWithUnits) returns array
{
    var sx = (corner == 0 || corner == 2) ? 1 : -1;
    var sy = corner < 2 ? -1 : 1;
    var core = vector(-sx * (width / 2 - U / 2), -sy * (height / 2 - U / 2));
    var length = ARM / 2 + 1.5 * U;
    var offset = (1.5 * U - ARM / 2) / 2;
    return [
        { "center" : core + vector(sx * offset, 0 * millimeter),
          "size" : vector(length + 2 * clearance, ARM + 2 * clearance) },
        { "center" : core + vector(0 * millimeter, sy * offset),
          "size" : vector(ARM + 2 * clearance, length + 2 * clearance) }
    ];
}

function samePinCell(a is map, b is map) returns boolean
{
    return a.holeColumn == b.holeColumn && a.holeRow == b.holeRow;
}

function hasPinCell(cells is array, cell is map) returns boolean
{
    for (var other in cells)
        if (samePinCell(other, cell)) return true;
    return false;
}

function flatHoleFits(width is ValueWithUnits, height is ValueWithUnits,
        thickness is ValueWithUnits, faceChamfers is boolean, cutCorners is array,
        clearance is ValueWithUnits, hole is map) returns boolean
{
    var columns = round(width / U);
    var rows = round(height / U);
    if (hole.holeColumn < 1 || hole.holeColumn > columns || hole.holeRow < 1 || hole.holeRow > rows)
        return false;
    if (hole.holeColumn != 1 && hole.holeColumn != columns && hole.holeRow != 1 && hole.holeRow != rows)
        return false;
    var point = vector((hole.holeColumn - 0.5) * U - width / 2, height / 2 - (hole.holeRow - 0.5) * U);
    var lead = min(PIN_BEVEL, thickness / 2 - 0.05 * millimeter);
    var margin = faceChamfers ? min(EDGE, thickness / 2 - 0.01 * millimeter) : 0 * millimeter;
    var radius = PIN / 2 + lead + margin;
    for (var corner in [0, 1, 2, 3])
        if (cutCorners[corner])
            for (var rectangle in flatNotchRectangles(width, height, corner, clearance))
                if (abs(point[0] - rectangle.center[0]) <= rectangle.size[0] / 2 + radius &&
                        abs(point[1] - rectangle.center[1]) <= rectangle.size[1] / 2 + radius)
                    return false;
    return true;
}

// A rectangular mounting blank in XY, with pin holes normal to its flat face.
// Hole centres follow the 15 mm grid; columns and rows start at the top-left cell.
function flatPanel(context is Context, id is Id, width is ValueWithUnits,
        height is ValueWithUnits, thickness is ValueWithUnits, faceChamfers is boolean,
        holes is array, cutCorners is array, cornerClearance is ValueWithUnits, mates is boolean)
{
    var columns = round(width / U);
    var rows = round(height / U);
    var plate = id + "plate";
    makeBox(context, plate, vector(0, 0, 0) * millimeter, vector(width, height, thickness));
    for (var i in [0, 1, 2, 3])
    {
        if (cutCorners[i])
        {
            if (width < 2 * U || height < 2 * U)
                throw regenError("Corner cutouts need a panel at least 2 by 2 grid units.");
            var rectangles = flatNotchRectangles(width, height, i, cornerClearance);
            for (var r in [0, 1])
            {
                var rectangle = rectangles[r];
                var cutter = id + ("cornerCut" ~ i ~ "leg" ~ r);
                makeBox(context, cutter, vector(rectangle.center[0], rectangle.center[1], 0 * millimeter),
                        vector(rectangle.size[0], rectangle.size[1], thickness + 1 * millimeter));
                cut(context, cutter + "subtract", body(plate), body(cutter));
            }
        }
    }
    if (size(evaluateQuery(context, qBodyType(qCreatedBy(plate, EntityType.BODY), BodyType.SOLID))) != 1)
        throw regenError("The corner cutouts must leave one connected panel. Increase the panel size or disable a cutout.");
    var edgeBevel = min(EDGE, thickness / 2 - 0.01 * millimeter);
    if (faceChamfers)
        bevelOnPlane(context, id + "faceChamfer", plate, edgeBevel,
                plane(vector(0 * millimeter, 0 * millimeter, -thickness / 2), vector(0, 0, -1)));
    // Keep the entrances beveled regardless of the face perimeter option.
    var lead = min(PIN_BEVEL, thickness / 2 - 0.05 * millimeter);
    for (var i = 0; i < size(holes); i += 1)
    {
        var hole = holes[i];
        if (hole.holeColumn > columns || hole.holeRow > rows)
            throw regenError("Pin hole " ~ (i + 1) ~ " is outside the panel grid. Choose a column and row within its width and height.", ["flatHoles"]);
        if (hole.holeColumn != 1 && hole.holeColumn != columns &&
            hole.holeRow != 1 && hole.holeRow != rows)
            throw regenError("Pin hole " ~ (i + 1) ~ " must be on an edge. Choose the first or last column, or the first or last row.", ["flatHoles"]);
        var x = (hole.holeColumn - 0.5) * U - width / 2;
        var y = height / 2 - (hole.holeRow - 0.5) * U;
        var radius = PIN / 2 + lead;
        var margin = faceChamfers ? edgeBevel : 0 * millimeter;
        if (abs(x) + radius + margin >= width / 2 ||
            abs(y) + radius + margin >= height / 2)
            throw regenError("Pin hole " ~ (i + 1) ~ " is too close to the panel edge. Move it inward or increase the panel size.", ["flatHoles"]);
        if (!flatHoleFits(width, height, thickness, faceChamfers, cutCorners, cornerClearance, hole))
            throw regenError("Pin hole " ~ (i + 1) ~ " overlaps a connector notch. Choose another edge cell.", ["flatHoles"]);
        for (var j = 0; j < i; j += 1)
        {
            var other = holes[j];
            var spacing = (PIN + PIN) / 2 + 2 * lead;
            if (abs(hole.holeColumn - other.holeColumn) * U <= spacing &&
                abs(hole.holeRow - other.holeRow) * U <= spacing)
                throw regenError("Pin holes " ~ (j + 1) ~ " and " ~ (i + 1) ~ " overlap. Move them apart.", ["flatHoles"]);
        }
        var tag = id + ("hole" ~ i);
        var center = vector(x, y, 0 * millimeter);
        makeBox(context, tag + "straight", center,
                vector(PIN, PIN, thickness + 0.1 * millimeter));
        for (var side in [0])
        {
            var taper = tag + ("lead" ~ side);
            var z0 = side == 0 ? -thickness / 2 : thickness / 2 - lead;
            var z1 = side == 0 ? -thickness / 2 + lead : thickness / 2;
            var s0 = side == 0 ? PIN + 2 * lead : PIN;
            var s1 = side == 0 ? PIN : PIN + 2 * lead;
            squareFrustum(context, taper, z0, s0, z1, s1);
            opTransform(context, taper + "place", {
                    "bodies" : body(taper + "loft"), "transform" : transform(center) });
            add(context, tag + ("joinLead" ~ side), body(tag + "straight"), body(taper + "loft"));
        }
        cut(context, tag + "cut", body(plate), body(tag + "straight"));
        if (mates)
            assemblyMate(context, tag + "mate", "Face pin " ~ (i + 1), body(plate), center, vector(0, 0, 1));
    }
}
