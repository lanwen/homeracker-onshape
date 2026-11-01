export enum PanelCornerClearance
{
    annotation { "Name" : "Both directions" } BOTH,
    annotation { "Name" : "Horizontal only" } HORIZONTAL,
    annotation { "Name" : "Vertical only" } VERTICAL,
    annotation { "Name" : "No inset" } NONE
}

export enum PanelEdge
{
    annotation { "Name" : "Top" } NORTH,
    annotation { "Name" : "Bottom" } SOUTH,
    annotation { "Name" : "Left" } WEST,
    annotation { "Name" : "Right" } EAST
}

export type PanelClearanceSpec typecheck canBePanelClearanceSpec;

export predicate canBePanelClearanceSpec(value)
{
    value is map;
    value.custom is boolean;
    value.edges is array;
    size(value.edges) == 4;
    value.depths is array;
    size(value.depths) == 4;
    for (var depth in value.depths)
    {
        isLength(depth);
        depth >= 0 * millimeter;
        depth <= 5 * millimeter;
    }
    for (var edge in value.edges)
    {
        edge is array;
        for (var cell in edge)
            cell is boolean;
    }
}

function panelEdgeIndex(side is PanelEdge) returns number
{
    if (side == PanelEdge.NORTH) return 0;
    if (side == PanelEdge.SOUTH) return 1;
    if (side == PanelEdge.WEST) return 2;
    return 3;
}

function panelCornerInset(mode is PanelCornerClearance, horizontal is boolean) returns boolean
{
    return mode == PanelCornerClearance.BOTH ||
            mode == (horizontal ? PanelCornerClearance.HORIZONTAL : PanelCornerClearance.VERTICAL);
}

// Grid positions count left to right on horizontal edges and top to bottom on vertical edges.
function panelClearanceSpec(nx is number, ny is number, corners is array, ranges is array, depths is array) returns PanelClearanceSpec
{
    const adjacentCorners = [[0, 1], [2, 3], [0, 2], [1, 3]];
    var edges = [];
    var custom = size(ranges) > 0;
    for (var depth in depths)
        custom = custom || abs(depth - (WALL + FIT / 2)) > 0.000001 * millimeter;
    for (var mode in corners)
        custom = custom || mode != PanelCornerClearance.BOTH;
    for (var side = 0; side < 4; side += 1)
    {
        const count = side < 2 ? nx : ny;
        var cells = [];
        for (var cell = 0; cell < count; cell += 1)
            cells = append(cells,
                    (cell == 0 && panelCornerInset(corners[adjacentCorners[side][0]], side < 2)) ||
                    (cell == count - 1 && panelCornerInset(corners[adjacentCorners[side][1]], side < 2)));
        edges = append(edges, cells);
    }
    for (var range in ranges)
    {
        const side = panelEdgeIndex(range.clearanceSide);
        const count = side < 2 ? nx : ny;
        if (range.clearanceStart < 1 || range.clearanceLength < 1 ||
                range.clearanceStart != round(range.clearanceStart) ||
                range.clearanceLength != round(range.clearanceLength) ||
                range.clearanceStart + range.clearanceLength - 1 > count)
            throw regenError("Keep each clearance within its edge. Top and bottom use the grid width. Left and right use the grid height.",
                    ["panelEdgeClearances"]);
        var cells = edges[side];
        for (var cell = range.clearanceStart - 1; cell < range.clearanceStart + range.clearanceLength - 1; cell += 1)
            cells[cell] = true;
        edges[side] = cells;
    }
    // A zero-depth side uses the normal mount profile even when selected by a corner or range.
    for (var side = 0; side < 4; side += 1)
        if (depths[side] == 0 * millimeter)
        {
            var cells = edges[side];
            for (var cell = 0; cell < size(cells); cell += 1)
                cells[cell] = false;
            edges[side] = cells;
        }
    return { "custom" : custom, "edges" : edges, "depths" : depths } as PanelClearanceSpec;
}

function panelCellDepth(spec is PanelClearanceSpec, mountedSides is array, side is number, cell is number) returns ValueWithUnits
{
    return spec.edges[side][cell] ? spec.depths[side] :
            (mountedSides[side] ? 0 * millimeter : WALL + FIT / 2);
}

// Build an edge from continuous runs of flush mounts or inset connector sections.
// All runs join the same flat plate. Depths are measured from the flush outer mount face.
function panelClearanceMounts(context is Context, id is Id, target is Query,
        nx is number, ny is number, fit is PanelFit, corners is boolean, mountedSides is array,
        edgeChamfers is boolean, mates is boolean, spec is PanelClearanceSpec)
{
    const w = nx * U - 2 * WALL - FIT;
    const d = ny * U - 2 * WALL - FIT;
    const height = U + FIT + (fit == PanelFit.FULL_COVER ? WALL : 0 * millimeter);
    var outerFaces = [];
    var endFaces = [];
    var pins = [];
    var pieces = [];
    var chamferWidth = EDGE;
    for (var side = 0; side < 4; side += 1)
    {
        const horizontal = side < 2;
        const sign = side == 0 || side == 3 ? 1 : -1;
        const count = horizontal ? nx : ny;
        const alongSize = horizontal ? nx * U : ny * U;
        const edge = (horizontal ? d : w) / 2;
        // The adjacent edge decides whether each end stops at the plate or extends to a flush corner.
        const nearSide = horizontal ? 2 : 1;
        const farSide = horizontal ? 3 : 0;
        const adjacentCell = horizontal ? (side == 0 ? 0 : ny - 1) : (side == 2 ? 0 : nx - 1);
        const minimum = -alongSize / 2 + panelCellDepth(spec, mountedSides, nearSide, adjacentCell);
        const maximum = alongSize / 2 - panelCellDepth(spec, mountedSides, farSide, adjacentCell);
        var first = 0;
        while (first < count)
        {
            const inset = spec.edges[side][first];
            var last = first + 1;
            while (last < count && spec.edges[side][last] == inset)
                last += 1;
            // Vertical row numbers run downward, while the model Y axis runs upward.
            const low = horizontal ? -alongSize / 2 + first * U : alongSize / 2 - last * U;
            const high = horizontal ? -alongSize / 2 + last * U : alongSize / 2 - first * U;
            const a = max(low - FIT / 2, minimum);
            const b = min(high + FIT / 2, maximum);
            const along = (a + b) / 2;
            const length = b - a;
            const tall = inset ? corners : (mountedSides[side] || fit == PanelFit.INTERFIT);
            const h = tall ? height : WALL;
            const depth = panelCellDepth(spec, mountedSides, side, first);
            if (first > 0)
            {
                const previousTall = spec.edges[side][first - 1] ? corners :
                        (mountedSides[side] || fit == PanelFit.INTERFIT);
                const step = abs(depth - panelCellDepth(spec, mountedSides, side, first - 1));
                if (tall == previousTall && step > 0.000001 * millimeter)
                {
                    // Offset walls at the same height share a narrow neck at the run overlap.
                    const neck = step < WALL ? WALL - step : FIT;
                    chamferWidth = min(chamferWidth, neck * 0.49);
                }
            }
            const insetAdjustment = WALL + FIT / 2 - depth;
            const wallPosition = sign * (edge + WALL / 2 + FIT / 2 - depth);
            const run = id + ("edge" ~ side) + ("run" ~ first);
            if (inset && insetAdjustment < -0.000001 * millimeter && fit == PanelFit.INTERFIT)
            {
                const trim = run + "plateTrim";
                const depth = -insetAdjustment;
                // Reach the original plate corners even when the neighboring wall moves inward.
                const trimA = low <= -alongSize / 2 ? min(a, -alongSize / 2 + WALL + FIT / 2) : a;
                const trimB = high >= alongSize / 2 ? max(b, alongSize / 2 - WALL - FIT / 2) : b;
                const trimAlong = (trimA + trimB) / 2;
                makeBox(context, trim,
                        horizontal ? vector(trimAlong, sign * (edge - depth / 2), WALL / 2) :
                                     vector(sign * (edge - depth / 2), trimAlong, WALL / 2),
                        horizontal ? vector(trimB - trimA, depth, WALL + 0.1 * millimeter) :
                                     vector(depth, trimB - trimA, WALL + 0.1 * millimeter));
                cut(context, trim + "cut", target, body(trim));
            }
            const wall = run + "wall";
            makeBox(context, wall,
                    horizontal ? vector(along, wallPosition, WALL + h / 2) : vector(wallPosition, along, WALL + h / 2),
                    horizontal ? vector(length, WALL, h) : vector(WALL, length, h));
            if (edgeChamfers)
            {
                outerFaces = append(outerFaces, panelTrackFace(context, wall, horizontal ?
                        vector(along, wallPosition + sign * WALL / 2, WALL + h / 2) :
                        vector(wallPosition + sign * WALL / 2, along, WALL + h / 2)));
                for (var end in [a, b])
                    endFaces = append(endFaces, panelTrackFace(context, wall, horizontal ?
                            vector(end, wallPosition, WALL + h / 2) : vector(wallPosition, end, WALL + h / 2)));
            }
            if (insetAdjustment > 0.000001 * millimeter)
            {
                const ledge = run + "ledge";
                const outside = edge + insetAdjustment;
                const inside = edge - WALL;
                makeBox(context, ledge,
                        horizontal ? vector(along, sign * (outside + inside) / 2, WALL / 2) :
                                     vector(sign * (outside + inside) / 2, along, WALL / 2),
                        horizontal ? vector(length, outside - inside, WALL) :
                                     vector(outside - inside, length, WALL));
                if (edgeChamfers)
                {
                    outerFaces = append(outerFaces, panelTrackFace(context, ledge, horizontal ?
                            vector(along, sign * outside, WALL / 2) : vector(sign * outside, along, WALL / 2)));
                    for (var end in [a, b])
                        endFaces = append(endFaces, panelTrackFace(context, ledge, horizontal ?
                                vector(end, sign * (outside + inside) / 2, WALL / 2) :
                                vector(sign * (outside + inside) / 2, end, WALL / 2)));
                }
                pieces = append(pieces, body(ledge));
            }
            pieces = append(pieces, body(wall));
            if (tall)
                for (var cell = first; cell < last; cell += 1)
                {
                    const station = horizontal ? (cell + 0.5) * U - alongSize / 2 :
                                                alongSize / 2 - (cell + 0.5) * U;
                    if (station - a <= PIN / 2 + PIN_BEVEL || b - station <= PIN / 2 + PIN_BEVEL)
                        throw regenError("An inset leaves too little room for a pin hole. Reduce the neighboring side inset.");
                    const z = WALL + h / 2 + (fit == PanelFit.FULL_COVER ? WALL / 2 + FIT / 2 : FIT / 2);
                    pins = append(pins, { "side" : side, "cell" : cell,
                            "center" : horizontal ? vector(station, wallPosition, z) : vector(wallPosition, station, z),
                            "axis" : horizontal ? PullAxis.Y : PullAxis.X, "entrySign" : -sign });
                }
            first = last;
        }
    }
    add(context, id + "joinOutline", target, qUnion(pieces));
    if (edgeChamfers)
        panelChamferOutline(context, id + "outlineChamfer", outerFaces, endFaces, chamferWidth);
    for (var pin in pins)
        panelPassage(context, id + "pins" + ("edge" ~ pin.side) + ("pin" ~ pin.cell),
                pin.center, pin.axis, pin.entrySign, target, mates);
}

// Track faces rather than edges so the final outline includes new corner intersections.
function panelTrackFace(context is Context, source is Id, point is Vector) returns Query
{
    return startTracking(context, { "subquery" : qClosestTo(qCreatedBy(source, EntityType.FACE), point),
            "trackPartialDependency" : true });
}

function panelChamferOutline(context is Context, id is Id, outerFaces is array, endFaces is array, width is ValueWithUnits)
{
    const outerEdges = qAdjacent(qEntityFilter(qUnion(outerFaces), EntityType.FACE), AdjacencyType.EDGE, EntityType.EDGE);
    const allEdges = qAdjacent(qEntityFilter(qUnion([qUnion(outerFaces), qUnion(endFaces)]), EntityType.FACE),
            AdjacencyType.EDGE, EntityType.EDGE);
    var selected = [];
    for (var edge in evaluateQuery(context, qEdgeConvexityTypeFilter(allEdges, EdgeConvexityType.CONVEX)))
    {
        const tangent = evEdgeTangentLine(context, { "edge" : edge, "parameter" : 0.5 }).direction;
        const bounds = evBox3d(context, { "topology" : edge, "tight" : true });
        if ((abs(tangent[2]) < 0.000001 && bounds.minCorner[2] > WALL + 0.000001 * millimeter) ||
                (abs(tangent[2]) > 0.999999 && bounds.maxCorner[2] - bounds.minCorner[2] > WALL &&
                 size(evaluateQuery(context, qIntersection([edge, outerEdges]))) > 0))
            selected = append(selected, edge);
    }
    var top = 0 * millimeter;
    for (var edge in selected)
        top = max(top, evBox3d(context, { "topology" : edge, "tight" : true }).maxCorner[2]);
    var fitWidth = width;
    for (var edge in selected)
    {
        const bounds = evBox3d(context, { "topology" : edge, "tight" : true });
        if (abs(bounds.maxCorner[2] - top) < 0.000001 * millimeter &&
                abs(bounds.maxCorner[2] - bounds.minCorner[2]) < 0.000001 * millimeter)
        {
            const length = norm(bounds.maxCorner - bounds.minCorner);
            if (length < 2 * fitWidth - 0.000001 * millimeter)
                fitWidth = length * 0.49;
        }
    }
    opChamfer(context, id + "rim", { "entities" : qUnion(selected),
            "chamferType" : ChamferType.EQUAL_OFFSETS, "width" : fitWidth });
    var bottom = [];
    for (var edge in evaluateQuery(context, qEdgeConvexityTypeFilter(outerEdges, EdgeConvexityType.CONVEX)))
    {
        const bounds = evBox3d(context, { "topology" : edge, "tight" : true });
        if (abs(bounds.maxCorner[2]) < 0.000001 * millimeter && abs(bounds.minCorner[2]) < 0.000001 * millimeter)
            bottom = append(bottom, edge);
    }
    if (size(bottom) > 0)
        opChamfer(context, id + "base", { "entities" : qUnion(bottom),
                "chamferType" : ChamferType.EQUAL_OFFSETS, "width" : fitWidth });
}
