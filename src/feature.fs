annotation { "Feature Type Name" : "Homeracker", "Feature Type Description" :
        "Create a HomeRacker part. Choose Support, Connector, Panel, Sleeve, or Flat panel, then set its size and mounting options.",
        "Icon" : HomerackerIcon::BLOB_DATA, "Feature Name Template" : "#generatedName",
        "Manipulator Change Function" : "homerackerManipulatorChange",
        "Editing Logic Function" : "homerackerEditLogic" }
export const homeracker = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Type", "Description" :
                    "Choose the part to create. Flat panel is a rectangular blank with pin holes through its face." }
        definition.kind is HomerackerKind;
        if (definition.kind == HomerackerKind.SUPPORT)
        {
            annotation { "Group Name" : "Shape", "Collapsed By Default" : false }
            {
                annotation { "Name" : "Length in grid units", "Description" :
                        "Each unit adds 15 mm of length. A 3-unit support is 45 mm long." }
                isInteger(definition.supportUnits, { (unitless) : [1, 3, 50] } as IntegerBoundSpec);
                annotation { "Name" : "X pin holes", "Description" :
                        "Add pin holes perpendicular to the standard holes." }
                definition.xHoles is boolean;
            }
        }
        if (definition.kind == HomerackerKind.CONNECTOR)
        {
            annotation { "Group Name" : "Shape", "Collapsed By Default" : false }
            {
                annotation { "Name" : "Connector shape", "Default" : "SIX", "Description" :
                        "Choose the socket layout. End spans 2 grid units; Straight spans 3." }
                definition.connectorShape is ConnectorShape;
                if (definition.connectorShape == ConnectorShape.LEGACY)
                {
                    annotation { "Name" : "Dimensions" }
                    isInteger(definition.connectorDimensions, { (unitless) : [1, 3, 3] } as IntegerBoundSpec);
                    annotation { "Name" : "Directions" }
                    isInteger(definition.connectorWays, { (unitless) : [1, 6, 6] } as IntegerBoundSpec);
                }
                annotation { "Name" : "Pull through axis", "Description" :
                        "Open the connector center along this axis so a support can pass through." }
                definition.pullAxis is PullAxis;
            }
            annotation { "Name" : "Available PositiveX wall", "UIHint" : UIHint.ALWAYS_HIDDEN }
            definition.connectorWallPositiveX is boolean;
            annotation { "Name" : "Available NegativeX wall", "UIHint" : UIHint.ALWAYS_HIDDEN }
            definition.connectorWallNegativeX is boolean;
            annotation { "Name" : "Available PositiveY wall", "UIHint" : UIHint.ALWAYS_HIDDEN }
            definition.connectorWallPositiveY is boolean;
            annotation { "Name" : "Available NegativeY wall", "UIHint" : UIHint.ALWAYS_HIDDEN }
            definition.connectorWallNegativeY is boolean;
            annotation { "Name" : "Available NegativeZ wall", "UIHint" : UIHint.ALWAYS_HIDDEN }
            definition.connectorWallNegativeZ is boolean;
            if (definition.connectorWallPositiveX ||
                    definition.connectorWallNegativeX ||
                    definition.connectorWallPositiveY ||
                    definition.connectorWallNegativeY ||
                    definition.connectorWallNegativeZ)
            {
                annotation { "Group Name" : "Core pin holes", "Collapsed By Default" : true }
                {
                    if (definition.connectorWallPositiveX)
                    {
                        annotation { "Name" : "+X wall", "Description" : "Add a 4 mm square hole through this wall into the pull-through. Both entrances stay beveled." }
                        definition.connectorCorePositiveX is boolean;
                    }
                    if (definition.connectorWallNegativeX)
                    {
                        annotation { "Name" : "-X wall", "Description" : "Add a 4 mm square hole through this wall into the pull-through. Both entrances stay beveled." }
                        definition.connectorCoreNegativeX is boolean;
                    }
                    if (definition.connectorWallPositiveY)
                    {
                        annotation { "Name" : "+Y wall", "Description" : "Add a 4 mm square hole through this wall into the pull-through. Both entrances stay beveled." }
                        definition.connectorCorePositiveY is boolean;
                    }
                    if (definition.connectorWallNegativeY)
                    {
                        annotation { "Name" : "-Y wall", "Description" : "Add a 4 mm square hole through this wall into the pull-through. Both entrances stay beveled." }
                        definition.connectorCoreNegativeY is boolean;
                    }
                    if (definition.connectorWallNegativeZ)
                    {
                        annotation { "Name" : "-Z wall", "Description" : "Add a 4 mm square hole through this wall into the pull-through. Both entrances stay beveled." }
                        definition.connectorCoreNegativeZ is boolean;
                    }
                }
            }

        }
        if (definition.kind == HomerackerKind.PANEL)
        {
            annotation { "Group Name" : "Shape", "Collapsed By Default" : false }
            {
                annotation { "Name" : "Grid width", "Description" :
                        "Set the panel width in 15 mm units. Use at least 2 units." }
                isInteger(definition.panelX, { (unitless) : [2, 3, 50] } as IntegerBoundSpec);
                annotation { "Name" : "Grid height", "Description" :
                        "Set the panel height in 15 mm units. Use at least 2 units." }
                isInteger(definition.panelY, { (unitless) : [2, 3, 50] } as IntegerBoundSpec);
                annotation { "Name" : "Panel fit", "Description" :
                        "InterFit sits between the supports and is removable. Full cover overlaps the frame and needs assembly with the frame." }
                definition.panelFit is PanelFit;
                if (definition.panelFit == PanelFit.FULL_COVER)
                {
                    annotation { "Name" : "Adjacent panel clearance", "Description" :
                        "Set the gap between adjacent full cover panels. Enter a length, such as 0.5 mm." }
                    isLength(definition.panelClearance, { (millimeter) : [0, 0, 5] } as LengthBoundSpec);
                }
                annotation { "Name" : "Face chamfers", "Default" : true, "Description" :
                        "Bevel the perimeter of the flat panel face. Mounts and pin hole entrances always stay beveled." }
                definition.panelChamfers is boolean;
            }

            annotation { "Group Name" : "Mounts", "Collapsed By Default" : true }
            {
                annotation { "Name" : "Corner mounts", "Description" :
                        "Add corner mounts with pin holes. Disable these on large panels if adjacent panels need the same pin holes." }
                definition.cornerMounts is boolean;
                annotation { "Name" : "North mounts", "Description" :
                        "Add the mounting strip on the +Y edge of the panel face." }
                definition.northMounts is boolean;
                annotation { "Name" : "South mounts", "Description" :
                        "Add the mounting strip on the -Y edge of the panel face." }
                definition.southMounts is boolean;
                annotation { "Name" : "East mounts", "Description" :
                        "Add the mounting strip on the +X edge of the panel face." }
                definition.eastMounts is boolean;
                annotation { "Name" : "West mounts", "Description" :
                        "Add the mounting strip on the -X edge of the panel face." }
                definition.westMounts is boolean;
            }
        }
        if (definition.kind == HomerackerKind.SLEEVE)
        {
            annotation { "Group Name" : "Shape", "Collapsed By Default" : false }
            {
                annotation { "Name" : "Length in grid units", "Description" :
                        "Set the sleeve length in 15 mm units. It wraps around three sides of a support." }
                isInteger(definition.sleeveUnits, { (unitless) : [1, 3, 20] } as IntegerBoundSpec);
                annotation { "Name" : "Sleeve chamfers", "Default" : true, "Description" :
                        "Bevel the sleeve edges. Pin hole entrances always stay beveled." }
                definition.sleeveChamfers is boolean;
            }
        }
        if (definition.kind == HomerackerKind.FLAT_PANEL)
        {
            annotation { "Group Name" : "Shape", "Collapsed By Default" : false }
            {
                annotation { "Name" : "Grid width", "Description" : "Set the rectangle width along X in 15 mm units. A width of 4 units is 60 mm." }
                isInteger(definition.flatWidthUnits, { (unitless) : [1, 4, 50] } as IntegerBoundSpec);
                annotation { "Name" : "Grid height", "Description" : "Set the rectangle height along Y in 15 mm units. A height of 3 units is 45 mm." }
                isInteger(definition.flatHeightUnits, { (unitless) : [1, 3, 50] } as IntegerBoundSpec);
                annotation { "Name" : "Thickness", "Description" : "Set the plate thickness. Start with 2 mm for a mounting blank." }
                isLength(definition.flatThickness, { (millimeter) : [1, 2, 20] } as LengthBoundSpec);
                annotation { "Name" : "Face chamfers", "Default" : true, "Description" :
                        "Bevel the front face perimeter. The back stays square; front pin hole entrances always stay beveled." }
                definition.flatChamfers is boolean;
            }
            annotation { "Group Name" : "Connector notches", "Collapsed By Default" : true }
            {
                annotation { "Name" : "Top left", "Description" : "Clear the top-left connector footprint. You can also click its corner point in the model." }
                definition.flatCutNW is boolean;
                annotation { "Name" : "Top right", "Description" : "Clear the top-right connector footprint." }
                definition.flatCutNE is boolean;
                annotation { "Name" : "Bottom left", "Description" : "Clear the bottom-left connector footprint." }
                definition.flatCutSW is boolean;
                annotation { "Name" : "Bottom right", "Description" : "Clear the bottom-right connector footprint." }
                definition.flatCutSE is boolean;
                annotation { "Name" : "Notch clearance", "Description" : "Set the extra gap around the connector footprint. Positive values add space; negative values tighten the notch. Default: 0.2 mm." }
                isLength(definition.flatCornerClearance, { (millimeter) : [-2, 0.2, 2] } as LengthBoundSpec);
            }
            annotation { "Name" : "No available edge cells", "UIHint" : UIHint.ALWAYS_HIDDEN }
            definition.flatNoHoleCells is boolean;
            annotation { "Name" : "All edge cells filled", "UIHint" : UIHint.ALWAYS_HIDDEN }
            definition.flatAtHoleCapacity is boolean;
            if (!definition.flatNoHoleCells && !definition.flatAtHoleCapacity)
            {
                annotation { "Name" : "Pin holes", "Item name" : "Pin hole",
                        "Item label template" : "C#holeColumn R#holeRow",
                        "UIHint" : UIHint.COLLAPSE_ARRAY_ITEMS, "Description" :
                        "Add one hole at each edge grid position. Use the first or last row or column. Column 1 and row 1 start at the top-left cell." }
                definition.flatHoles is array;
                for (var hole in definition.flatHoles)
                {
                    annotation { "Name" : "Column", "Description" : "Choose a grid column, counted from the left. It must be within the panel grid width." }
                    isInteger(hole.holeColumn, { (unitless) : [1, 1, 50] } as IntegerBoundSpec);
                    annotation { "Name" : "Row", "Description" : "Choose a grid row, counted from the top. It must be within the panel grid height." }
                    isInteger(hole.holeRow, { (unitless) : [1, 1, 50] } as IntegerBoundSpec);
                    annotation { "Name" : "Hole list revision", "UIHint" : UIHint.ALWAYS_HIDDEN }
                    isInteger(hole.holeRevision, { (unitless) : [0, 0, 1000000000] } as IntegerBoundSpec);
                }
            }
            if (!definition.flatNoHoleCells && definition.flatAtHoleCapacity)
            {
                annotation { "Name" : "Pin holes", "Item name" : "Pin hole",
                        "Item label template" : "C#fullHoleColumn R#fullHoleRow", "Show labels only" : true,
                        "Description" : "All usable edge cells have holes. Remove a hole to free its position." }
                definition.flatFullHoles is array;
                for (var hole in definition.flatFullHoles)
                {
                    annotation { "Name" : "Filled column" }
                    isInteger(hole.fullHoleColumn, { (unitless) : [1, 1, 50] } as IntegerBoundSpec);
                    annotation { "Name" : "Filled row" }
                    isInteger(hole.fullHoleRow, { (unitless) : [1, 1, 50] } as IntegerBoundSpec);
                }
            }

        }
        annotation { "Group Name" : "Assembly points", "Collapsed By Default" : false }
        {
            annotation { "Name" : "Mate connectors", "Default" : true, "Description" :
                        "Add assembly connection points that move with this part. Supports always include both end points." }
            definition.mateConnectors is boolean;
            if (definition.kind == HomerackerKind.SUPPORT && definition.mateConnectors)
            {
                annotation { "Name" : "Pin mates", "Default" : "NONE", "Description" :
                        "None keeps only the two end points. Selected rows adds points at chosen hole openings. All rows adds every opening." }
                definition.supportPinMates is SupportPinMates;
                if (definition.supportPinMates == SupportPinMates.SELECTED)
                {
                    annotation { "Name" : "Pin mate rows", "Item name" : "Pin mate row",
                            "Item label template" : "Row #mateRow", "UIHint" : UIHint.COLLAPSE_ARRAY_ITEMS,
                            "Description" : "Choose hole rows and opening sides. Row 1 is nearest the -Y end. Each row is 15 mm apart." }
                    definition.supportMateRows is array;
                    for (var row in definition.supportMateRows)
                    {
                        annotation { "Name" : "Row", "Description" :
                                "Count holes from the -Y end. Use a row within the support length in grid units." }
                        isInteger(row.mateRow, { (unitless) : [1, 1, 50] } as IntegerBoundSpec);
                        annotation { "Name" : "+Z opening", "Default" : true, "Description" :
                                "Add a point at the opening on the +Z face, pointing outward." }
                        row.positiveZ is boolean;
                        annotation { "Name" : "-Z opening", "Description" :
                                "Add a point at the opening on the -Z face, pointing outward." }
                        row.negativeZ is boolean;
                        if (definition.xHoles)
                        {
                            annotation { "Name" : "+X opening", "Description" :
                                    "Add a point at the opening on the +X face, pointing outward." }
                            row.positiveX is boolean;
                            annotation { "Name" : "-X opening", "Description" :
                                    "Add a point at the opening on the -X face, pointing outward." }
                            row.negativeX is boolean;
                        }
                    }
                }
            }
        }
    }
    {
        var label = "HomeRacker";
        if (definition.kind == HomerackerKind.SUPPORT)
        {
            label = label ~ " Support " ~ definition.supportUnits ~ "U" ~ (definition.xHoles ? " cross holes" : "");
            support(context, id, definition.supportUnits, definition.xHoles, definition.mateConnectors,
                    definition.supportPinMates, definition.supportMateRows);
        }
        if (definition.kind == HomerackerKind.CONNECTOR)
        {
            var spec = definition.connectorShape == ConnectorShape.LEGACY ?
                    connectorSpec(definition.connectorDimensions, definition.connectorWays,
                            definition.connectorDimensions ~ "D " ~ definition.connectorWays ~ "-way") :
                    connectorShapeSpec(definition.connectorShape);
            label = label ~ " Connector " ~ spec.name ~
                    (definition.pullAxis == PullAxis.NONE ? "" : " pull " ~ definition.pullAxis);
            connector(context, id, spec, definition.pullAxis,
                    corePinHoles(definition.connectorCorePositiveX, definition.connectorCoreNegativeX,
                            definition.connectorCorePositiveY, definition.connectorCoreNegativeY, definition.connectorCoreNegativeZ),
                    definition.mateConnectors);
        }
        if (definition.kind == HomerackerKind.PANEL)
        {
            label = label ~ " Panel " ~ definition.panelX ~ "x" ~ definition.panelY ~
                    (definition.panelFit == PanelFit.INTERFIT ? " InterFit" : " Full cover");
            panel(context, id, definition.panelX, definition.panelY,
                    definition.panelFit,
                    definition.panelFit == PanelFit.FULL_COVER ? definition.panelClearance : 0 * millimeter,
                    definition.cornerMounts, definition.northMounts, definition.southMounts,
                    definition.eastMounts, definition.westMounts, definition.panelChamfers, definition.mateConnectors);
        }
        if (definition.kind == HomerackerKind.SLEEVE)
        {
            label = label ~ " Sleeve " ~ definition.sleeveUnits ~ "U";
            sleeve(context, id, definition.sleeveUnits, definition.sleeveChamfers, definition.mateConnectors);
        }
        if (definition.kind == HomerackerKind.FLAT_PANEL)
        {
            label = label ~ " Flat panel " ~ definition.flatWidthUnits ~ "x" ~ definition.flatHeightUnits ~ "U " ~
                    (definition.flatThickness / millimeter) ~ " mm thick";
            flatPanel(context, id, definition.flatWidthUnits * U, definition.flatHeightUnits * U, definition.flatThickness,
                    definition.flatChamfers, definition.flatHoles,
                    [definition.flatCutNW, definition.flatCutNE, definition.flatCutSW, definition.flatCutSE],
                    definition.flatCornerClearance, definition.mateConnectors);
            addFlatPanelManipulators(context, id, definition);
        }
        setProperty(context, { "entities" : qBodyType(qCreatedBy(id, EntityType.BODY), BodyType.SOLID),
                "propertyType" : PropertyType.NAME, "value" : label });
        setFeatureComputedParameter(context, id, { "name" : "generatedName", "value" : label });
    }, {
        "kind" : HomerackerKind.SUPPORT,
        "sleeveUnits" : 3,
        "sleeveChamfers" : true,
        "flatWidthUnits" : 4,
        "flatHeightUnits" : 3,
        "flatThickness" : 2 * millimeter,
        "flatChamfers" : true,
        "flatHoles" : cornerPinHoles(4, 3),
        "flatNoHoleCells" : false,
        "flatAtHoleCapacity" : false,
        "flatFullHoles" : [],
        "flatCutNW" : false,
        "flatCutNE" : false,
        "flatCutSW" : false,
        "flatCutSE" : false,
        "flatCornerClearance" : 0.2 * millimeter,
        "supportUnits" : 3,
        "xHoles" : false,
        "supportPinMates" : SupportPinMates.NONE,
        "supportMateRows" : [],
        "connectorShape" : ConnectorShape.LEGACY,
        "connectorDimensions" : 3,
        "connectorWays" : 6,
        "connectorWallPositiveX" : false,
        "connectorWallNegativeX" : false,
        "connectorWallPositiveY" : false,
        "connectorWallNegativeY" : false,
        "connectorWallNegativeZ" : false,
        "connectorCorePositiveX" : false,
        "connectorCoreNegativeX" : false,
        "connectorCorePositiveY" : false,
        "connectorCoreNegativeY" : false,
        "connectorCoreNegativeZ" : false,
        "pullAxis" : PullAxis.NONE,
        "panelX" : 4,
        "panelY" : 3,
        "panelFit" : PanelFit.INTERFIT,
        "panelChamfers" : true,
        "mateConnectors" : true,
        "panelClearance" : 0 * millimeter,
        "cornerMounts" : true,
        "northMounts" : true,
        "southMounts" : true,
        "eastMounts" : true,
        "westMounts" : true
    });

export function homerackerManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    if (newManipulators.flatCorners != undefined)
        return flatPanelManipulatorChange(definition, newManipulators);
    return definition;
}

export function homerackerEditLogic(context is Context, id is Id, oldDefinition is map,
        definition is map, isCreating is boolean, specifiedParameters is map) returns map
{
    if (definition.kind == HomerackerKind.CONNECTOR)
        return connectorEditLogic(definition);
    if (definition.kind == HomerackerKind.FLAT_PANEL)
        return flatPanelEditLogic(context, id, oldDefinition, definition, isCreating, specifiedParameters);
    return definition;
}
