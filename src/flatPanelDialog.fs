// Flat-panel UI state is kept as plain Onshape parameter maps at this boundary.
function addFlatPanelManipulators(context is Context, id is Id, definition is map)
{
    var w = definition.flatWidthUnits * U;
    var h = definition.flatHeightUnits * U;
    var z = definition.flatThickness / 2 + 1 * millimeter;
    var selected = [];
    var flags = [definition.flatCutNW, definition.flatCutNE, definition.flatCutSW, definition.flatCutSE];
    for (var i in [0, 1, 2, 3])
        if (flags[i]) selected = append(selected, i);
    addManipulators(context, id, { "flatCorners" : togglePointsManipulator({
            "points" : [vector(-w / 2, h / 2, z), vector(w / 2, h / 2, z),
                        vector(-w / 2, -h / 2, z), vector(w / 2, -h / 2, z)],
            "selectedIndices" : selected, "suppressedIndices" : [] }) });
}

function flatPanelManipulatorChange(definition is map, newManipulators is map) returns map
{
    if (newManipulators.flatCorners != undefined)
    {
        var fields = ["flatCutNW", "flatCutNE", "flatCutSW", "flatCutSE"];
        for (var i in [0, 1, 2, 3])
            definition[fields[i]] = isIn(i, newManipulators.flatCorners.selectedIndices);
    }
    return definition;
}

// Seed the dialog array through editing logic; defineFeature defaults only apply
// to programmatic calls. Once the list is edited, leave the user's holes alone.
function cornerPinHoles(columns is number, rows is number) returns array
{
    var holes = [pinHoleDefinition(pinCell(1, 1))];
    if (columns > 1)
        holes = append(holes, pinHoleDefinition(pinCell(columns, 1)));
    if (rows > 1)
    {
        holes = append(holes, pinHoleDefinition(pinCell(1, rows)));
        if (columns > 1)
            holes = append(holes, pinHoleDefinition(pinCell(columns, rows)));
    }
    return holes;
}

function flatPanelEditLogic(context is Context, id is Id, oldDefinition is map,
        definition is map, isCreating is boolean, specifiedParameters is map) returns map
{
    // At capacity the labels-only view has remove controls and no Add button.
    if (definition.kind == HomerackerKind.FLAT_PANEL && oldDefinition.flatAtHoleCapacity == true &&
            definition.flatFullHoles != undefined && definition.flatFullHoles != oldDefinition.flatFullHoles)
    {
        var remaining = [];
        for (var row in definition.flatFullHoles)
            for (var hole in oldDefinition.flatHoles)
                if (hole.holeColumn == row.fullHoleColumn && hole.holeRow == row.fullHoleRow)
                    remaining = append(remaining, hole);
        definition.flatHoles = remaining;
    }
    var addedHoles = oldDefinition.kind == HomerackerKind.FLAT_PANEL &&
            oldDefinition.flatHoles != undefined && definition.flatHoles != undefined &&
            size(definition.flatHoles) > size(oldDefinition.flatHoles);
    if (isCreating && definition.kind == HomerackerKind.FLAT_PANEL &&
            specifiedParameters.flatHoles != true)
    {
        var enteringFlatPanel = oldDefinition.kind != HomerackerKind.FLAT_PANEL;
        var previousCorners = oldDefinition.flatWidthUnits != undefined && oldDefinition.flatHeightUnits != undefined ?
                cornerPinHoles(oldDefinition.flatWidthUnits, oldDefinition.flatHeightUnits) : [];
        if ((enteringFlatPanel && (definition.flatHoles == undefined || definition.flatHoles == [])) ||
                definition.flatHoles == previousCorners)
            definition.flatHoles = cornerPinHoles(definition.flatWidthUnits, definition.flatHeightUnits);
    }
    if (definition.kind == HomerackerKind.FLAT_PANEL && addedHoles)
    {
        var filled = oldDefinition.flatHoles;
        var available = flatPanelHoleCells(definition);
        for (var i = size(oldDefinition.flatHoles); i < size(definition.flatHoles); i += 1)
        {
            for (var cell in available)
            {
                if (!hasPinCell(filled, cell))
                {
                    filled = append(filled, cell);
                    break;
                }
            }
        }
        if (size(filled) == size(oldDefinition.flatHoles) && size(filled) > 0)
            filled[0].holeRevision = (filled[0].holeRevision == undefined ? 0 : filled[0].holeRevision) + 1;
        definition.flatHoles = filled;
    }
    if (definition.kind == HomerackerKind.FLAT_PANEL)
    {
        var available = flatPanelHoleCells(definition);
        definition.flatNoHoleCells = size(available) == 0;
        // A changed notch or grid may cover holes that were previously usable.
        if (definition.flatWidthUnits != oldDefinition.flatWidthUnits ||
                definition.flatHeightUnits != oldDefinition.flatHeightUnits ||
                definition.flatCutNW != oldDefinition.flatCutNW || definition.flatCutNE != oldDefinition.flatCutNE ||
                definition.flatCutSW != oldDefinition.flatCutSW || definition.flatCutSE != oldDefinition.flatCutSE ||
                definition.flatCornerClearance != oldDefinition.flatCornerClearance)
        {
            var kept = [];
            for (var hole in definition.flatHoles)
                if (hasPinCell(available, hole)) kept = append(kept, hole);
            definition.flatHoles = kept;
        }
    }
    if (definition.kind == HomerackerKind.FLAT_PANEL)
    {
        var available = flatPanelHoleCells(definition);
        definition.flatAtHoleCapacity = size(available) > 0 && size(definition.flatHoles) >= size(available);
        var fullRows = [];
        if (definition.flatAtHoleCapacity)
            for (var hole in definition.flatHoles)
                fullRows = append(fullRows, { "fullHoleColumn" : hole.holeColumn, "fullHoleRow" : hole.holeRow });
        definition.flatFullHoles = fullRows;
    }
    return definition;
}

// List the usable edge cells in row order, excluding connector clearances.
function flatPanelHoleCells(definition is map) returns array
{
    var cells = [];
    var columns = definition.flatWidthUnits;
    var rows = definition.flatHeightUnits;
    for (var row = 1; row <= rows; row += 1)
        for (var column = 1; column <= columns; column += 1)
        {
            if (row != 1 && row != rows && column != 1 && column != columns) continue;
            var cell = pinCell(column, row);
            if (flatHoleFits(columns * U, rows * U, definition.flatThickness, definition.flatChamfers,
                    [definition.flatCutNW, definition.flatCutNE, definition.flatCutSW, definition.flatCutSE],
                    definition.flatCornerClearance, cell))
                cells = append(cells, pinHoleDefinition(cell));
        }
    return cells;
}
