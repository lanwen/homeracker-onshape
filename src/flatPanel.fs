// Domain values are separate from the plain parameter maps stored by Onshape.
export type PinCell typecheck canBePinCell;

export predicate canBePinCell(value)
{
    value is map;
    value.column is number;
    value.row is number;
    value.column == round(value.column) && value.column >= 1;
    value.row == round(value.row) && value.row >= 1;
}

function pinCell(column is number, row is number) returns PinCell
{
    var cell = { "column" : column, "row" : row };
    if (!canBePinCell(cell))
        throw regenError("A pin-hole cell needs a positive whole column and row.");
    return cell as PinCell;
}

// Preserve the saved dialog schema, including its array-update stamp.
function pinCellFromHole(hole is map) returns PinCell
{
    var cell = { "column" : hole.holeColumn, "row" : hole.holeRow };
    if (!canBePinCell(cell))
        throw regenError("A pin-hole cell needs a positive whole column and row.", ["flatHoles"]);
    return cell as PinCell;
}

function pinHoleDefinition(cell is PinCell) returns map
{
    return { "holeColumn" : cell.column, "holeRow" : cell.row, "holeRevision" : 0 };
}

export type NotchRectangle typecheck canBeNotchRectangle;

export predicate canBeNotchRectangle(value)
{
    value is map;
    value.center is Vector;
    size(value.center) == 2;
    isLength(value.center[0]);
    isLength(value.center[1]);
    value.size is Vector;
    size(value.size) == 2;
    isLength(value.size[0]);
    isLength(value.size[1]);
    value.size[0] > 0 * millimeter && value.size[1] > 0 * millimeter;
}

function notchRectangle(center is Vector, dimensions is Vector) returns NotchRectangle
{
    var rectangle = { "center" : center, "size" : dimensions };
    if (!canBeNotchRectangle(rectangle))
        throw regenError("A connector notch needs a 2D centre and positive length dimensions.");
    return rectangle as NotchRectangle;
}

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
        notchRectangle(core + vector(sx * offset, 0 * millimeter),
                vector(length + 2 * clearance, ARM + 2 * clearance)),
        notchRectangle(core + vector(0 * millimeter, sy * offset),
                vector(ARM + 2 * clearance, length + 2 * clearance))
    ];
}

function samePinCell(a is map, b is map) returns boolean
{
    return pinCellFromHole(a) == pinCellFromHole(b);
}

function hasPinCell(cells is array, cell is map) returns boolean
{
    for (var other in cells)
        if (samePinCell(other, cell)) return true;
    return false;
}

function flatHoleFits(width is ValueWithUnits, height is ValueWithUnits,
        thickness is ValueWithUnits, faceChamfers is boolean, cutCorners is array,
        clearance is ValueWithUnits, cell is PinCell) returns boolean
{
    var columns = round(width / U);
    var rows = round(height / U);
    if (cell.column < 1 || cell.column > columns || cell.row < 1 || cell.row > rows)
        return false;
    if (cell.column != 1 && cell.column != columns && cell.row != 1 && cell.row != rows)
        return false;
    var point = vector((cell.column - 0.5) * U - width / 2, height / 2 - (cell.row - 0.5) * U);
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
    var cells = [];
    for (var hole in holes) cells = append(cells, pinCellFromHole(hole));
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
        var cell = cells[i];
        if (cell.column > columns || cell.row > rows)
            throw regenError("Pin hole " ~ (i + 1) ~ " is outside the panel grid. Choose a column and row within its width and height.", ["flatHoles"]);
        if (cell.column != 1 && cell.column != columns &&
            cell.row != 1 && cell.row != rows)
            throw regenError("Pin hole " ~ (i + 1) ~ " must be on an edge. Choose the first or last column, or the first or last row.", ["flatHoles"]);
        var x = (cell.column - 0.5) * U - width / 2;
        var y = height / 2 - (cell.row - 0.5) * U;
        var radius = PIN / 2 + lead;
        var margin = faceChamfers ? edgeBevel : 0 * millimeter;
        if (abs(x) + radius + margin >= width / 2 ||
            abs(y) + radius + margin >= height / 2)
            throw regenError("Pin hole " ~ (i + 1) ~ " is too close to the panel edge. Move it inward or increase the panel size.", ["flatHoles"]);
        if (!flatHoleFits(width, height, thickness, faceChamfers, cutCorners, cornerClearance, cell))
            throw regenError("Pin hole " ~ (i + 1) ~ " overlaps a connector notch. Choose another edge cell.", ["flatHoles"]);
        for (var j = 0; j < i; j += 1)
        {
            var other = cells[j];
            var spacing = (PIN + PIN) / 2 + 2 * lead;
            if (abs(cell.column - other.column) * U <= spacing &&
                abs(cell.row - other.row) * U <= spacing)
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
            assemblyMate(context, tag + "mate", "Face pin " ~ (i + 1), body(plate),
                    center + vector(0 * millimeter, 0 * millimeter, thickness / 2), vector(0, 0, 1));
    }
}
