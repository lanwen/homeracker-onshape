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
