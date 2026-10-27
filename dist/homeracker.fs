FeatureScript 3095;
import(path : "onshape/std/geometry.fs", version : "3095.0");

// Derived from HomeRacker's OpenSCAD models at
// 5a64b663ef2d91678d70b85e85df1bcc30c090d5 (Patrick Pötz, MIT).
// Upstream: https://github.com/kellerlabs/homeracker
// Model artifacts: CC BY-SA 4.0; see upstream models/LICENSE and this repo NOTICE.
//
// Copyright 2025 Patrick Pötz (kellervater)
// Copyright 2026 Patrick Pötz (kellervater)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the “Software”), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

const U = 15 * millimeter;
const WALL = 2 * millimeter;
const FIT = 0.2 * millimeter;
const EDGE = 1 * millimeter;
const PIN = 4 * millimeter;
const PIN_BEVEL = 0.8 * millimeter;
const LEAD_LIP = 0.01 * millimeter;
const LEAD_JOIN_OVERLAP = 0.001 * millimeter;
const ARM = U + 2 * WALL + FIT;
const ARM_INNER = ARM - 2 * WALL;

export enum HomerackerKind
{
    annotation { "Name" : "Support" } SUPPORT,
    annotation { "Name" : "Connector" } CONNECTOR,
    annotation { "Name" : "Panel" } PANEL
}

export enum PanelFit
{
    annotation { "Name" : "InterFit" } INTERFIT,
    annotation { "Name" : "Full cover" } FULL_COVER
}

export enum PullAxis
{
    annotation { "Name" : "None" } NONE,
    annotation { "Name" : "X" } X,
    annotation { "Name" : "Y" } Y,
    annotation { "Name" : "Z" } Z
}

function makeBox(context is Context, id is Id, center is Vector, dimensions is Vector)
{
    fCuboid(context, id, {
        "corner1" : center - dimensions / 2,
        "corner2" : center + dimensions / 2
    });
}

function body(id is Id) returns Query
{
    return qCreatedBy(id, EntityType.BODY);
}

function add(context is Context, id is Id, targets is Query, tools is Query)
{
    opBoolean(context, id, { "tools" : qUnion([targets, tools]),
            "operationType" : BooleanOperationType.UNION });
}

function cut(context is Context, id is Id, targets is Query, tools is Query)
{
    opBoolean(context, id, { "targets" : targets, "tools" : tools,
            "operationType" : BooleanOperationType.SUBTRACTION });
}

function bevel(context is Context, id is Id, source is Id, amount is ValueWithUnits)
{
    opChamfer(context, id, { "entities" : qCreatedBy(source, EntityType.EDGE),
            "chamferType" : ChamferType.EQUAL_OFFSETS, "width" : amount });
}

function bevelExceptPlane(context is Context, id is Id, source is Id,
        amount is ValueWithUnits, excluded is Plane)
{
    var edges = qCreatedBy(source, EntityType.EDGE);
    opChamfer(context, id, {
            "entities" : qSubtraction(edges, qCoincidesWithPlane(edges, excluded)),
            "chamferType" : ChamferType.EQUAL_OFFSETS, "width" : amount });
}

function bevelOnPlane(context is Context, id is Id, source is Id,
        amount is ValueWithUnits, selected is Plane)
{
    var edges = qCreatedBy(source, EntityType.EDGE);
    opChamfer(context, id, {
            "entities" : qCoincidesWithPlane(edges, selected),
            "chamferType" : ChamferType.EQUAL_OFFSETS, "width" : amount });
}

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

function bevelAt(context is Context, id is Id, source is Id, point is Vector,
        amount is ValueWithUnits)
{
    opChamfer(context, id, {
            "entities" : qClosestTo(qCreatedBy(source, EntityType.EDGE), point),
            "chamferType" : ChamferType.EQUAL_OFFSETS, "width" : amount });
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

// A square section in the XY plane. Lofting consecutive pairs rather than the
// full stack keeps the OpenSCAD prismoid's straight faces and sharp transitions.
function squareSection(context is Context, id is Id, z is ValueWithUnits,
        side is ValueWithUnits)
{
    var sk = newSketchOnPlane(context, id, {
            "sketchPlane" : plane(vector(0 * millimeter, 0 * millimeter, z), vector(0, 0, 1)) });
    skRectangle(sk, "square", {
            "firstCorner" : vector(-side / 2, -side / 2),
            "secondCorner" : vector(side / 2, side / 2) });
    skSolve(sk);
}

function squareFrustum(context is Context, id is Id,
        z0 is ValueWithUnits, side0 is ValueWithUnits,
        z1 is ValueWithUnits, side1 is ValueWithUnits)
{
    squareSection(context, id + "lower", z0, side0);
    squareSection(context, id + "upper", z1, side1);
    opLoft(context, id + "loft", {
            "profileSubqueries" : [qSketchRegion(id + "lower"),
                                   qSketchRegion(id + "upper")] });
    opDeleteBodies(context, id + "deleteProfiles", {
            "entities" : qUnion([body(id + "lower"), body(id + "upper")]) });
}

function passage(context is Context, id is Id, center is Vector,
        axis is PullAxis, target is Query)
{
    var straight = id + "straight";
    var dims = axis == PullAxis.X ? vector(ARM, PIN, PIN) :
            (axis == PullAxis.Y ? vector(PIN, ARM, PIN) : vector(PIN, PIN, ARM));
    makeBox(context, straight, center, dims);
    cut(context, id + "straightCut", target, body(straight));
    for (var side in [0, 1])
    {
        var z0 = side == 0 ? -ARM / 2 + LEAD_LIP : ARM / 2 - PIN_BEVEL;
        var z1 = side == 0 ? -ARM / 2 + PIN_BEVEL : ARM / 2 - LEAD_LIP;
        var s0 = side == 0 ? PIN + 2 * PIN_BEVEL : PIN;
        var s1 = side == 0 ? PIN : PIN + 2 * PIN_BEVEL;
        var taper = id + ("lead" ~ side);
        squareFrustum(context, taper, z0, s0, z1, s1);
        var rot = rotationAround(line(vector(0, 0, 0) * millimeter, vector(0, 1, 0)), 0 * degree);
        if (axis == PullAxis.X)
            rot = rotationAround(line(vector(0, 0, 0) * millimeter, vector(0, 1, 0)), 90 * degree);
        if (axis == PullAxis.Y)
            rot = rotationAround(line(vector(0, 0, 0) * millimeter, vector(1, 0, 0)), -90 * degree);
        opTransform(context, taper + "place", {
                "bodies" : body(taper + "loft"),
                "transform" : transform(center) * rot });
        // BOSL2's partial-edge cuboid chamfer reaches full width 0.01 mm
        // before the outside face, leaving a short square entry lip.
        var lipOffset = (side == 0 ? -1 : 1) * (ARM / 2 - LEAD_LIP / 2);
        var overlapShift = (side == 0 ? 1 : -1) * LEAD_JOIN_OVERLAP / 2;
        lipOffset = lipOffset + overlapShift;
        var lipCenter = center +
                (axis == PullAxis.X ? vector(lipOffset, 0 * millimeter, 0 * millimeter) :
                 (axis == PullAxis.Y ? vector(0 * millimeter, lipOffset, 0 * millimeter) :
                                       vector(0 * millimeter, 0 * millimeter, lipOffset)));
        var lipDims = axis == PullAxis.X ?
                vector(LEAD_LIP + LEAD_JOIN_OVERLAP, PIN + 2 * PIN_BEVEL, PIN + 2 * PIN_BEVEL) :
                (axis == PullAxis.Y ?
                 vector(PIN + 2 * PIN_BEVEL, LEAD_LIP + LEAD_JOIN_OVERLAP, PIN + 2 * PIN_BEVEL) :
                 vector(PIN + 2 * PIN_BEVEL, PIN + 2 * PIN_BEVEL, LEAD_LIP + LEAD_JOIN_OVERLAP));
        makeBox(context, taper + "lip", lipCenter, lipDims);
        add(context, taper + "joinLip", body(taper + "loft"), body(taper + "lip"));
        cut(context, taper + "cut", target, body(taper + "loft"));
    }
}

function panelPassage(context is Context, id is Id, center is Vector,
        axis is PullAxis, entrySign is number, target is Query)
{
    var straight = id + "straight";
    var dims = axis == PullAxis.X ? vector(WALL + 1 * millimeter, PIN, PIN) :
            vector(PIN, WALL + 1 * millimeter, PIN);
    makeBox(context, straight, center, dims);
    cut(context, id + "straightCut", target, body(straight));
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

function support(context is Context, id is Id, units is number, xHoles is boolean)
{
    var bar = id + "bar";
    makeBox(context, bar, vector(0, 0, 0) * millimeter, vector(U, units * U, U));
    bevel(context, id + "edgeChamfer", bar, EDGE);
    for (var i = 0; i < units; i += 1)
    {
        var c = vector(0 * millimeter, (i - (units - 1) / 2) * U, 0 * millimeter);
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
        ways is number, pull is PullAxis)
{
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
        var pass = id + "pullThrough";
        var p = 3 * U;
        var dims = pull == PullAxis.X ? vector(p, ARM_INNER, ARM_INNER) :
                (pull == PullAxis.Y ? vector(ARM_INNER, p, ARM_INNER) :
                vector(ARM_INNER, ARM_INNER, p));
        makeBox(context, pass, vector(0, 0, 0) * millimeter, dims);
        cut(context, id + "pullCut", body(core), body(pass));
    }
}

// A panel is drawn with its face in XY. The 2 mm plate lies at Z = 0..2 mm;
// positive Z points into the rack. The panel footprint and mount heights are
// the formulae in models/panel/lib/panel.scad.
function panel(context is Context, id is Id, nx is number, ny is number,
        fit is PanelFit, clearance is ValueWithUnits, corners is boolean,
        north is boolean, south is boolean, east is boolean, west is boolean)
{
    var w = nx * U - 2 * WALL - FIT;
    var d = ny * U - 2 * WALL - FIT;
    var base = id + "base";
    makeBox(context, base, vector(0 * millimeter, 0 * millimeter, WALL / 2),
            vector(w, d, WALL));
    bevelExceptPlane(context, id + "baseChamfer", base, EDGE,
            plane(vector(0 * millimeter, 0 * millimeter, WALL), vector(0, 0, 1)));

    if (fit == PanelFit.FULL_COVER)
    {
        var cover = id + "cover";
        // Upstream cover extends half a unit on each side beyond the grid.
        makeBox(context, cover, vector(0 * millimeter, 0 * millimeter, WALL / 2),
                vector((nx + 1) * U - clearance,
                       (ny + 1) * U - clearance, WALL));
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
                        PullAxis.X, -sx, body(base));
                panelPassage(context, hy,
                        vector(sx * (w / 2 - strip / 2 + (WALL + FIT) / 2),
                               sy * (d / 2 - WALL / 2), z),
                        PullAxis.Y, -sy, body(base));
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
                            PullAxis.Y, -sy, body(base));
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
                            PullAxis.X, -sx, body(base));
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

annotation { "Feature Type Name" : "Homeracker", "Feature Type Description" :
        "Parametric HomeRacker support, connector, or panel" }
export const homeracker = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Type" }
        definition.kind is HomerackerKind;
        if (definition.kind == HomerackerKind.SUPPORT)
        {
            annotation { "Name" : "Length in grid units" }
            isInteger(definition.supportUnits, { (unitless) : [1, 3, 50] } as IntegerBoundSpec);
            annotation { "Name" : "X pin holes" }
            definition.xHoles is boolean;
        }
        if (definition.kind == HomerackerKind.CONNECTOR)
        {
            annotation { "Name" : "Dimensions" }
            isInteger(definition.connectorDimensions, { (unitless) : [1, 3, 3] } as IntegerBoundSpec);
            annotation { "Name" : "Directions" }
            isInteger(definition.connectorWays, { (unitless) : [1, 6, 6] } as IntegerBoundSpec);
            annotation { "Name" : "Pull through axis" }
            definition.pullAxis is PullAxis;
        }
        if (definition.kind == HomerackerKind.PANEL)
        {
            annotation { "Name" : "Grid width" }
            isInteger(definition.panelX, { (unitless) : [2, 3, 50] } as IntegerBoundSpec);
            annotation { "Name" : "Grid height" }
            isInteger(definition.panelY, { (unitless) : [2, 3, 50] } as IntegerBoundSpec);
            annotation { "Name" : "Panel fit" }
            definition.panelFit is PanelFit;
            if (definition.panelFit == PanelFit.FULL_COVER)
            {
                annotation { "Name" : "Adjacent panel clearance" }
                isLength(definition.panelClearance, { (millimeter) : [0, 0, 5] } as LengthBoundSpec);
            }
            annotation { "Name" : "Corner mounts" }
            definition.cornerMounts is boolean;
            annotation { "Name" : "North mounts" }
            definition.northMounts is boolean;
            annotation { "Name" : "South mounts" }
            definition.southMounts is boolean;
            annotation { "Name" : "East mounts" }
            definition.eastMounts is boolean;
            annotation { "Name" : "West mounts" }
            definition.westMounts is boolean;
        }
    }
    {
        if (definition.kind == HomerackerKind.SUPPORT)
            support(context, id, definition.supportUnits, definition.xHoles);
        if (definition.kind == HomerackerKind.CONNECTOR)
        {
            var minWays = definition.connectorDimensions;
            var maxWays = 2 * definition.connectorDimensions;
            var ways = max(minWays, min(maxWays, definition.connectorWays));
            connector(context, id, definition.connectorDimensions, ways, definition.pullAxis);
        }
        if (definition.kind == HomerackerKind.PANEL)
            panel(context, id, definition.panelX, definition.panelY,
                    definition.panelFit,
                    definition.panelFit == PanelFit.FULL_COVER ? definition.panelClearance : 0 * millimeter,
                    definition.cornerMounts, definition.northMounts, definition.southMounts,
                    definition.eastMounts, definition.westMounts);
    }, {
        "kind" : HomerackerKind.SUPPORT,
        "supportUnits" : 3,
        "xHoles" : false,
        "connectorDimensions" : 3,
        "connectorWays" : 6,
        "pullAxis" : PullAxis.NONE,
        "panelX" : 4,
        "panelY" : 3,
        "panelFit" : PanelFit.INTERFIT,
        "panelClearance" : 0 * millimeter,
        "cornerMounts" : true,
        "northMounts" : true,
        "southMounts" : true,
        "eastMounts" : true,
        "westMounts" : true
    });
