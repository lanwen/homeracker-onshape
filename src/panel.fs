function bevelCorner(context is Context, id is Id, source is Id,
        corner is Vector, xLength is ValueWithUnits, yLength is ValueWithUnits,
        height is ValueWithUnits, sx is number, sy is number)
{
    var edges = qCreatedBy(source, EntityType.EDGE);
    var xEdge = qClosestTo(edges, corner - vector(sx * xLength / 2,
                                                  0 * millimeter, 0 * millimeter));
    var yEdge = qClosestTo(edges, corner - vector(0 * millimeter,
                                                  sy * yLength / 2, 0 * millimeter));
    var zEdge = qClosestTo(edges, corner - vector(0 * millimeter,
                                                  0 * millimeter, height / 2));
    opChamfer(context, id, {
            "entities" : qUnion([xEdge, yEdge, zEdge]),
            "chamferType" : ChamferType.EQUAL_OFFSETS, "width" : EDGE });
}

function bevelMountedWall(context is Context, id is Id, source is Id,
        alongX is boolean, outside is ValueWithUnits, middle is ValueWithUnits,
        length is ValueWithUnits, height is ValueWithUnits)
{
    var edges = qCreatedBy(source, EntityType.EDGE);
    var top = WALL + height;
    var midZ = WALL + height / 2;
    var selected = [];
    if (alongX)
    {
        selected = [
            qClosestTo(edges, vector(0 * millimeter, outside, top)),
            qClosestTo(edges, vector(-length / 2, middle, top)),
            qClosestTo(edges, vector(length / 2, middle, top)),
            qClosestTo(edges, vector(-length / 2, outside, midZ)),
            qClosestTo(edges, vector(length / 2, outside, midZ))
        ];
    }
    else
    {
        selected = [
            qClosestTo(edges, vector(outside, 0 * millimeter, top)),
            qClosestTo(edges, vector(middle, -length / 2, top)),
            qClosestTo(edges, vector(middle, length / 2, top)),
            qClosestTo(edges, vector(outside, -length / 2, midZ)),
            qClosestTo(edges, vector(outside, length / 2, midZ))
        ];
    }
    opChamfer(context, id, {
            "entities" : qUnion(selected),
            "chamferType" : ChamferType.EQUAL_OFFSETS, "width" : EDGE });
}

function bevelMountLedge(context is Context, id is Id, source is Id,
        alongX is boolean, outside is ValueWithUnits, length is ValueWithUnits)
{
    var edges = qCreatedBy(source, EntityType.EDGE);
    var bottom = qCoincidesWithPlane(edges,
            plane(vector(0, 0, 0) * millimeter, vector(0, 0, 1)));
    var endA = alongX ? vector(-length / 2, outside, WALL / 2) :
            vector(outside, -length / 2, WALL / 2);
    var endB = alongX ? vector(length / 2, outside, WALL / 2) :
            vector(outside, length / 2, WALL / 2);
    opChamfer(context, id, {
            "entities" : qUnion([bottom, qClosestTo(edges, endA), qClosestTo(edges, endB)]),
            "chamferType" : ChamferType.EQUAL_OFFSETS, "width" : EDGE });
}

function triangleRelief(context is Context, id is Id,
        p0 is Vector, p1 is Vector, p2 is Vector,
        bottomZ is ValueWithUnits, depth is ValueWithUnits, target is Query)
{
    var sk = newSketchOnPlane(context, id + "sketch", {
            "sketchPlane" : plane(vector(0 * millimeter, 0 * millimeter, bottomZ),
                                  vector(0, 0, 1)) });
    skLineSegment(sk, "a", { "start" : p0, "end" : p1 });
    skLineSegment(sk, "b", { "start" : p1, "end" : p2 });
    skLineSegment(sk, "c", { "start" : p2, "end" : p0 });
    skSolve(sk);
    opExtrude(context, id + "prism", {
            "entities" : qSketchRegion(id + "sketch"),
            "direction" : vector(0, 0, 1),
            "endBound" : BoundingType.BLIND,
            "endDepth" : depth });
    opDeleteBodies(context, id + "deleteSketch", {
            "entities" : body(id + "sketch") });
    cut(context, id + "cut", target, body(id + "prism"));
}

function bridgeCornerCut(context is Context, id is Id, corner is Vector,
        inwardX is number, inwardY is number, target is Query)
{
    // BOSL2 chamfer_corner_mask(chamfer=0.5) makes an octahedron with
    // 1 mm axis intercepts at the selected corner.
    var size = EDGE;
    var sk = newSketchOnPlane(context, id + "sketch", {
            "sketchPlane" : plane(vector(0 * millimeter, 0 * millimeter, corner[2]),
                                  vector(0, 0, 1)) });
    var p0 = vector(corner[0], corner[1]);
    var p1 = vector(corner[0] + inwardX * size, corner[1]);
    var p2 = vector(corner[0], corner[1] + inwardY * size);
    skLineSegment(sk, "a", { "start" : p0, "end" : p1 });
    skLineSegment(sk, "b", { "start" : p1, "end" : p2 });
    skLineSegment(sk, "c", { "start" : p2, "end" : p0 });
    skSolve(sk);
    opPoint(context, id + "apex", {
            "point" : corner - vector(0 * millimeter, 0 * millimeter, size) });
    opLoft(context, id + "tetra", {
            "profileSubqueries" : [qSketchRegion(id + "sketch"),
                                   qCreatedBy(id + "apex", EntityType.VERTEX)] });
    opDeleteBodies(context, id + "deleteProfiles", {
            "entities" : qUnion([body(id + "sketch"), body(id + "apex")]) });
    cut(context, id + "subtract", target, body(id + "tetra"));
}

function panelPassage(context is Context, id is Id, center is Vector,
        axis is PullAxis, entrySign is number, target is Query, mates is boolean)
{
    var straight = id + "straight";
    var dims = axis == PullAxis.X ? vector(WALL + 1 * millimeter, PIN, PIN) :
            vector(PIN, WALL + 1 * millimeter, PIN);
    makeBox(context, straight, center, dims);
    cut(context, id + "straightCut", target, body(straight));
    if (mates)
    {
        // Mate at the outer contact face; the pin lead-in faces the other way.
        var normal = axis == PullAxis.X ? vector(-entrySign, 0, 0) : vector(0, -entrySign, 0);
        assemblyMate(context, id + "mate", "Panel " ~ (axis == PullAxis.X ? "X" : "Y") ~ " pin (" ~
                        (round(center[0] / (0.1 * millimeter)) / 10) ~ ", " ~ (round(center[1] / (0.1 * millimeter)) / 10) ~ ")", target,
                center + normal * WALL / 2, normal);
    }
    var z0 = entrySign < 0 ? -WALL / 2 + LEAD_LIP : WALL / 2 - PIN_BEVEL;
    var z1 = entrySign < 0 ? -WALL / 2 + PIN_BEVEL : WALL / 2 - LEAD_LIP;
    var s0 = entrySign < 0 ? PIN + 2 * PIN_BEVEL : PIN;
    var s1 = entrySign < 0 ? PIN : PIN + 2 * PIN_BEVEL;
    var taper = id + "lead";
    squareFrustum(context, taper, z0, s0, z1, s1);
    var rot = axis == PullAxis.X ?
            rotationAround(line(vector(0, 0, 0) * millimeter, vector(0, 1, 0)), 90 * degree) :
            rotationAround(line(vector(0, 0, 0) * millimeter, vector(1, 0, 0)), -90 * degree);
    opTransform(context, taper + "place", {
            "bodies" : body(taper + "loft"),
            "transform" : transform(center) * rot });
    var lipOffset = entrySign * (WALL / 2 - LEAD_LIP / 2 - LEAD_JOIN_OVERLAP / 2);
    var lipCenter = center + (axis == PullAxis.X ?
            vector(lipOffset, 0 * millimeter, 0 * millimeter) :
            vector(0 * millimeter, lipOffset, 0 * millimeter));
    var lipDims = axis == PullAxis.X ?
            vector(LEAD_LIP + LEAD_JOIN_OVERLAP, PIN + 2 * PIN_BEVEL, PIN + 2 * PIN_BEVEL) :
            vector(PIN + 2 * PIN_BEVEL, LEAD_LIP + LEAD_JOIN_OVERLAP, PIN + 2 * PIN_BEVEL);
    makeBox(context, id + "lip", lipCenter, lipDims);
    add(context, id + "joinLeadLip", body(taper + "loft"), body(id + "lip"));
    cut(context, id + "leadCut", target, body(taper + "loft"));
}

export enum PanelFit
{
    annotation { "Name" : "InterFit" } INTERFIT,
    annotation { "Name" : "Full cover" } FULL_COVER
}

// A panel is drawn with its face in XY. The 2 mm plate lies at Z = 0..2 mm;
// positive Z points into the rack. The panel footprint and mount heights are
// the formulae in models/panel/lib/panel.scad.
function panel(context is Context, id is Id, nx is number, ny is number,
        fit is PanelFit, clearance is ValueWithUnits, corners is boolean,
        north is boolean, south is boolean, east is boolean, west is boolean,
        faceChamfers is boolean, mates is boolean)
{
    // Only the flat plate perimeter is optional; mounting geometry keeps its bevels.
    var w = nx * U - 2 * WALL - FIT;
    var d = ny * U - 2 * WALL - FIT;
    var base = id + "base";
    makeBox(context, base, vector(0 * millimeter, 0 * millimeter, WALL / 2),
            vector(w, d, WALL));
    if (faceChamfers)
        bevelExceptPlane(context, id + "baseChamfer", base, EDGE,
                plane(vector(0 * millimeter, 0 * millimeter, WALL), vector(0, 0, 1)));

    if (fit == PanelFit.FULL_COVER)
    {
        var cover = id + "cover";
        // Upstream cover extends half a unit on each side beyond the grid.
        makeBox(context, cover, vector(0 * millimeter, 0 * millimeter, WALL / 2),
                vector((nx + 1) * U - clearance,
                       (ny + 1) * U - clearance, WALL));
        if (faceChamfers)
            bevelOnPlane(context, id + "coverChamfer", cover, EDGE,
                    plane(vector(0 * millimeter, 0 * millimeter, 0 * millimeter), vector(0, 0, 1)));
        add(context, id + "coverJoin", body(base), body(cover));
    }

    var mh = U + FIT + (fit == PanelFit.FULL_COVER ? WALL : 0 * millimeter);
    var ch = corners ? mh : WALL;
    var strip = U - WALL;
    // The four corner brackets each comprise two 13 x 2 mm legs. The holes
    // pass through each leg parallel to the panel face.
    for (var ix in [0, 1])
    {
        for (var iy in [0, 1])
        {
            var sx = ix == 0 ? -1 : 1;
            var sy = iy == 0 ? -1 : 1;
            var tag = id + ("corner" ~ ix ~ iy);
            var x = sx * (w / 2 - strip / 2);
            var y = sy * (d / 2 - strip / 2);
            var legX = tag + "x";
            var legY = tag + "y";
            var reliefX = fit == PanelFit.FULL_COVER && nx > 2 &&
                    !(iy == 0 ? south : north);
            var reliefY = fit == PanelFit.FULL_COVER && ny > 2 &&
                    !(ix == 0 ? west : east);
            makeBox(context, legX, vector(x, sy * (d / 2 - WALL / 2), WALL + ch / 2),
                    vector(strip, WALL, ch));
            var outerCorner = vector(sx * w / 2, sy * d / 2, WALL + ch);
            bevelCorner(context, tag + "xChamfer", legX,
                    outerCorner, strip, WALL, ch, sx, sy);
            if (reliefX)
                bevelAt(context, tag + "xInnerTop", legX,
                        vector(sx * (w / 2 - strip),
                               sy * (d / 2 - WALL / 2), WALL + ch), EDGE);
            add(context, tag + "joinX", body(base), body(legX));
            makeBox(context, legY, vector(sx * (w / 2 - WALL / 2), y, WALL + ch / 2),
                    vector(WALL, strip, ch));
            bevelCorner(context, tag + "yChamfer", legY,
                    outerCorner, WALL, strip, ch, sx, sy);
            if (reliefY)
                bevelAt(context, tag + "yInnerTop", legY,
                        vector(sx * (w / 2 - WALL / 2),
                               sy * (d / 2 - strip), WALL + ch), EDGE);
            add(context, tag + "joinY", body(base), body(legY));
            // Upstream retains these full-cover clearance cuts even with chamfers off.
            if (reliefX && corners)
            {
                var innerX = sx * (w / 2 - strip);
                var outerY = sy * d / 2;
                triangleRelief(context, tag + "xRelief",
                        vector(innerX, outerY),
                        vector(innerX + sx * EDGE, outerY),
                        vector(innerX, outerY - sy * EDGE),
                        WALL + EDGE - 0.05 * millimeter,
                        ch - EDGE + 0.1 * millimeter, body(base));
            }
            if (reliefY && corners)
            {
                var outerX = sx * w / 2;
                var innerY = sy * (d / 2 - strip);
                triangleRelief(context, tag + "yRelief",
                        vector(outerX, innerY),
                        vector(outerX - sx * EDGE, innerY),
                        vector(outerX, innerY + sy * EDGE),
                        WALL + EDGE - 0.05 * millimeter,
                        ch - EDGE + 0.1 * millimeter, body(base));
            }
            if (corners)
            {
                var z = WALL + ch / 2 +
                        (fit == PanelFit.FULL_COVER ? WALL / 2 + FIT / 2 : FIT / 2);
                var hx = tag + "pinX";
                var hy = tag + "pinY";
                panelPassage(context, hx,
                        vector(sx * (w / 2 - WALL / 2),
                               sy * (d / 2 - strip / 2 + (WALL + FIT) / 2), z),
                        PullAxis.X, -sx, body(base), mates);
                panelPassage(context, hy,
                        vector(sx * (w / 2 - strip / 2 + (WALL + FIT) / 2),
                               sy * (d / 2 - WALL / 2), z),
                        PullAxis.Y, -sy, body(base), mates);
            }
        }
    }

    // Intermediate edge mount walls and bottom ledges occupy each grid cell
    // between the corner mounts. The 2 mm bridges tie their ends to the wall.
    if (nx > 2)
    {
        for (var iy in [0, 1])
        {
            var sy = iy == 0 ? -1 : 1;
            var mounted = iy == 0 ? south : north;
            var side = id + ("horizontal" ~ iy);
            var long = (nx - 2) * U;
            var wallLong = !mounted && fit == PanelFit.FULL_COVER ? long + 2 * EDGE : long;
            var h = mounted ? mh : (fit == PanelFit.INTERFIT ? mh : WALL);
            var wallY = mounted ? sy * (d / 2 + WALL / 2 + FIT / 2) :
                    sy * (d / 2 - WALL / 2);
            makeBox(context, side + "wall",
                    vector(0 * millimeter, wallY, WALL + h / 2),
                    vector(wallLong, WALL, h));
            if (mounted)
                bevelMountedWall(context, side + "wallChamfer", side + "wall", true,
                        sy * (d / 2 + WALL + FIT / 2), wallY, long, h);
            if (!mounted)
                bevelAt(context, side + "wallChamfer", side + "wall",
                        vector(0 * millimeter, sy * d / 2, WALL + h), EDGE);
            if (mounted)
            {
                makeBox(context, side + "ledge",
                        vector(0 * millimeter, sy * (d / 2 + FIT / 4), WALL / 2),
                        vector(long, 2 * WALL + FIT / 2, WALL));
                bevelMountLedge(context, side + "ledgeChamfer", side + "ledge", true,
                        sy * (d / 2 + WALL + FIT / 2), long);
                add(context, side + "joinLedge", body(base), body(side + "ledge"));
                for (var end in [0, 1])
                {
                    var s = end == 0 ? -1 : 1;
                    var bridge = side + ("bridge" ~ end);
                    var bridgeSolid = bridge + "solid";
                    makeBox(context, bridgeSolid,
                            vector(s * (long / 2 - WALL / 2),
                                   sy * (d / 2 - WALL / 2), WALL + h / 2),
                            vector(WALL, WALL, h));
                    bridgeCornerCut(context, bridge + "cornerCut",
                            vector(s * long / 2, sy * d / 2, WALL + h),
                            -s, -sy, body(bridgeSolid));
                    add(context, bridge + "join", body(base), body(bridgeSolid));
                    var fill = side + ("filler" ~ end);
                    makeBox(context, fill,
                            vector(s * (long / 2 - WALL / 2),
                                   sy * (d / 2 + FIT / 4), WALL + h / 2),
                            vector(WALL, FIT / 2, h));
                    bevelAt(context, side + ("fillerChamfer" ~ end), fill,
                            vector(s * long / 2,
                                   sy * (d / 2 + FIT / 4), WALL + h), EDGE);
                    add(context, side + ("joinFiller" ~ end), body(base), body(fill));
                    var endFill = side + ("endFiller" ~ end);
                    makeBox(context, endFill,
                            vector(s * (long / 2 + FIT / 4),
                                   sy * (d / 2 - WALL / 2), WALL + h / 2),
                            vector(FIT / 2, WALL, h));
                    bevelAt(context, side + ("endFillerChamfer" ~ end), endFill,
                            vector(s * (long / 2 + FIT / 4),
                                   sy * d / 2, WALL + h), EDGE);
                    add(context, side + ("joinEndFiller" ~ end), body(base), body(endFill));
                }
            }
            add(context, side + "join", body(base), body(side + "wall"));
            for (var i = 0; i < nx - 2; i += 1)
            {
                if (mounted || fit == PanelFit.INTERFIT)
                {
                    var hole = side + ("pin" ~ i);
                    panelPassage(context, hole,
                            vector((i - (nx - 3) / 2) * U,
                                   wallY, WALL + h / 2 +
                                   (fit == PanelFit.FULL_COVER ? WALL / 2 + FIT / 2 : FIT / 2)),
                            PullAxis.Y, -sy, body(base), mates);
                }
            }
        }
    }
    if (ny > 2)
    {
        for (var ix in [0, 1])
        {
            var sx = ix == 0 ? -1 : 1;
            var mounted = ix == 0 ? west : east;
            var side = id + ("vertical" ~ ix);
            var long = (ny - 2) * U;
            var wallLong = !mounted && fit == PanelFit.FULL_COVER ? long + 2 * EDGE : long;
            var h = mounted ? mh : (fit == PanelFit.INTERFIT ? mh : WALL);
            var wallX = mounted ? sx * (w / 2 + WALL / 2 + FIT / 2) :
                    sx * (w / 2 - WALL / 2);
            makeBox(context, side + "wall",
                    vector(wallX, 0 * millimeter, WALL + h / 2),
                    vector(WALL, wallLong, h));
            if (mounted)
                bevelMountedWall(context, side + "wallChamfer", side + "wall", false,
                        sx * (w / 2 + WALL + FIT / 2), wallX, long, h);
            if (!mounted)
                bevelAt(context, side + "wallChamfer", side + "wall",
                        vector(sx * w / 2, 0 * millimeter, WALL + h), EDGE);
            if (mounted)
            {
                makeBox(context, side + "ledge",
                        vector(sx * (w / 2 + FIT / 4), 0 * millimeter, WALL / 2),
                        vector(2 * WALL + FIT / 2, long, WALL));
                bevelMountLedge(context, side + "ledgeChamfer", side + "ledge", false,
                        sx * (w / 2 + WALL + FIT / 2), long);
                add(context, side + "joinLedge", body(base), body(side + "ledge"));
                for (var end in [0, 1])
                {
                    var s = end == 0 ? -1 : 1;
                    var bridge = side + ("bridge" ~ end);
                    var bridgeSolid = bridge + "solid";
                    makeBox(context, bridgeSolid,
                        vector(sx * (w / 2 - WALL / 2),
                                   s * (long / 2 - WALL / 2), WALL + h / 2),
                            vector(WALL, WALL, h));
                    bridgeCornerCut(context, bridge + "cornerCut",
                            vector(sx * w / 2, s * long / 2, WALL + h),
                            -sx, -s, body(bridgeSolid));
                    add(context, bridge + "join", body(base), body(bridgeSolid));
                    var fill = side + ("filler" ~ end);
                    makeBox(context, fill,
                            vector(sx * (w / 2 + FIT / 4),
                                   s * (long / 2 - WALL / 2), WALL + h / 2),
                            vector(FIT / 2, WALL, h));
                    bevelAt(context, side + ("fillerChamfer" ~ end), fill,
                            vector(sx * (w / 2 + FIT / 4),
                                   s * long / 2, WALL + h), EDGE);
                    add(context, side + ("joinFiller" ~ end), body(base), body(fill));
                    var endFill = side + ("endFiller" ~ end);
                    makeBox(context, endFill,
                        vector(sx * (w / 2 - WALL / 2),
                                   s * (long / 2 + FIT / 4), WALL + h / 2),
                            vector(WALL, FIT / 2, h));
                    bevelAt(context, side + ("endFillerChamfer" ~ end), endFill,
                            vector(sx * w / 2,
                                   s * (long / 2 + FIT / 4), WALL + h), EDGE);
                    add(context, side + ("joinEndFiller" ~ end), body(base), body(endFill));
                }
            }
            add(context, side + "join", body(base), body(side + "wall"));
            for (var i = 0; i < ny - 2; i += 1)
            {
                if (mounted || fit == PanelFit.INTERFIT)
                {
                    var hole = side + ("pin" ~ i);
                    panelPassage(context, hole,
                            vector(wallX,
                                   (i - (ny - 3) / 2) * U, WALL + h / 2 +
                                   (fit == PanelFit.FULL_COVER ? WALL / 2 + FIT / 2 : FIT / 2)),
                            PullAxis.X, -sx, body(base), mates);
                }
            }
        }
    }
    // OpenSCAD's attachable(CENTER) places the centre of the complete
    // plate-and-mount envelope at the origin.
    opTransform(context, id + "centerPanel", {
            "bodies" : body(base),
            "transform" : transform(vector(0 * millimeter, 0 * millimeter,
                                           -mh / 2 - WALL / 2)) });
}
