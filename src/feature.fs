annotation { "Feature Type Name" : "Homeracker", "Feature Type Description" :
        "Create a HomeRacker support, connector, or side panel. Choose the part type, then set its size and mounts.",
        "Icon" : HomerackerIcon::BLOB_DATA }
export const homeracker = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Type", "Description" :
                    "Choose Support, Connector, or Panel." }
        definition.kind is HomerackerKind;
        if (definition.kind == HomerackerKind.SUPPORT)
        {
            annotation { "Name" : "Length in grid units", "Description" :
                    "Each unit adds 15 mm of length. A 3-unit support is 45 mm long." }
            isInteger(definition.supportUnits, { (unitless) : [1, 3, 50] } as IntegerBoundSpec);
            annotation { "Name" : "X pin holes", "Description" :
                    "Add pin holes perpendicular to the standard holes." }
            definition.xHoles is boolean;
        }
        if (definition.kind == HomerackerKind.CONNECTOR)
        {
            annotation { "Name" : "Dimensions", "Description" :
                    "Choose how many axes the connector spans: 1, 2, or 3." }
            isInteger(definition.connectorDimensions, { (unitless) : [1, 3, 3] } as IntegerBoundSpec);
            annotation { "Name" : "Directions", "Description" :
                    "Choose the number of arms. The feature adjusts the count to the range for the selected dimensions." }
            isInteger(definition.connectorWays, { (unitless) : [1, 6, 6] } as IntegerBoundSpec);
            annotation { "Name" : "Pull through axis", "Description" :
                    "Open the connector center along this axis so a support can pass through." }
            definition.pullAxis is PullAxis;
        }
        if (definition.kind == HomerackerKind.PANEL)
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
