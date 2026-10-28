// A shared two-ended square pin passage with the original HomeRacker lead-in lip.
export type PinPassageSpec typecheck canBePinPassageSpec;

export predicate canBePinPassageSpec(value)
{
    value is map;
    value.center is Vector;
    size(value.center) == 3;
    isLength(value.center[0]);
    isLength(value.center[1]);
    isLength(value.center[2]);
    value.axis is PullAxis;
    value.axis != PullAxis.NONE;
    isLength(value.depth);
    value.depth > 2 * PIN_BEVEL;
}

function pinPassageSpec(center is Vector, axis is PullAxis, depth is ValueWithUnits) returns PinPassageSpec
{
    var spec = { "center" : center, "axis" : axis, "depth" : depth };
    if (!canBePinPassageSpec(spec))
        throw regenError("A pin passage needs a length centre, X/Y/Z axis, and depth greater than its two entrance bevels.");
    return spec as PinPassageSpec;
}

function applyPinPassage(context is Context, id is Id, target is Query, spec is PinPassageSpec)
{
    var straight = id + "straight";
    var dims = spec.axis == PullAxis.X ? vector(spec.depth, PIN, PIN) :
            (spec.axis == PullAxis.Y ? vector(PIN, spec.depth, PIN) : vector(PIN, PIN, spec.depth));
    makeBox(context, straight, spec.center, dims);
    cut(context, id + "straightCut", target, body(straight));
    for (var side in [0, 1])
    {
        var z0 = side == 0 ? -spec.depth / 2 + LEAD_LIP : spec.depth / 2 - PIN_BEVEL;
        var z1 = side == 0 ? -spec.depth / 2 + PIN_BEVEL : spec.depth / 2 - LEAD_LIP;
        var s0 = side == 0 ? PIN + 2 * PIN_BEVEL : PIN;
        var s1 = side == 0 ? PIN : PIN + 2 * PIN_BEVEL;
        var taper = id + ("lead" ~ side);
        squareFrustum(context, taper, z0, s0, z1, s1);
        var rot = rotationAround(line(vector(0, 0, 0) * millimeter, vector(0, 1, 0)), 0 * degree);
        if (spec.axis == PullAxis.X)
            rot = rotationAround(line(vector(0, 0, 0) * millimeter, vector(0, 1, 0)), 90 * degree);
        if (spec.axis == PullAxis.Y)
            rot = rotationAround(line(vector(0, 0, 0) * millimeter, vector(1, 0, 0)), -90 * degree);
        opTransform(context, taper + "place", {
                "bodies" : body(taper + "loft"),
                "transform" : transform(spec.center) * rot });
        // BOSL2's partial-edge cuboid chamfer reaches full width 0.01 mm
        // before the outside face, leaving a short square entry lip.
        var lipOffset = (side == 0 ? -1 : 1) * (spec.depth / 2 - LEAD_LIP / 2);
        var overlapShift = (side == 0 ? 1 : -1) * LEAD_JOIN_OVERLAP / 2;
        lipOffset = lipOffset + overlapShift;
        var lipCenter = spec.center +
                (spec.axis == PullAxis.X ? vector(lipOffset, 0 * millimeter, 0 * millimeter) :
                 (spec.axis == PullAxis.Y ? vector(0 * millimeter, lipOffset, 0 * millimeter) :
                                       vector(0 * millimeter, 0 * millimeter, lipOffset)));
        var lipDims = spec.axis == PullAxis.X ?
                vector(LEAD_LIP + LEAD_JOIN_OVERLAP, PIN + 2 * PIN_BEVEL, PIN + 2 * PIN_BEVEL) :
                (spec.axis == PullAxis.Y ?
                 vector(PIN + 2 * PIN_BEVEL, LEAD_LIP + LEAD_JOIN_OVERLAP, PIN + 2 * PIN_BEVEL) :
                 vector(PIN + 2 * PIN_BEVEL, PIN + 2 * PIN_BEVEL, LEAD_LIP + LEAD_JOIN_OVERLAP));
        makeBox(context, taper + "lip", lipCenter, lipDims);
        add(context, taper + "joinLip", body(taper + "loft"), body(taper + "lip"));
        cut(context, taper + "cut", target, body(taper + "loft"));
    }
}

// A named placement keeps face position, inward/outward normal, and length axis together.
function addPinOpeningMates(context is Context, positiveId is Id, negativeId is Id,
        label is string, owner is Query, placement is map)
{
    var normal = placement.normal;
    var facing = placement.inside ? -normal : normal;
    assemblyMate(context, positiveId, label ~ " + opening", owner,
            placement.center + normal * placement.span / 2, facing, placement.along);
    assemblyMate(context, negativeId, label ~ " - opening", owner,
            placement.center - normal * placement.span / 2, -facing, placement.along);
}
