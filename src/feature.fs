annotation { "Feature Type Name" : "Homeracker", "Feature Type Description" :
        "Create a HomeRacker part. Choose Support, Connector, Panel, Sleeve, or Flat panel, then set its size and mounting options.",
        "Icon" : HomerackerIcon::BLOB_DATA, "Feature Name Template" : "#generatedName" }
export const homeracker = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Type", "Description" :
                    "Choose the part to create. Flat panel is a rectangular blank with pin holes through its face." }
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
            annotation { "Name" : "Face chamfers", "Default" : true, "Description" :
                    "Bevel the perimeter of the flat panel face. Mounts and pin hole entrances always stay beveled." }
            definition.panelChamfers is boolean;
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
        if (definition.kind == HomerackerKind.SLEEVE)
        {
            annotation { "Name" : "Length in grid units", "Description" :
                    "Set the sleeve length in 15 mm units. It wraps around three sides of a support." }
            isInteger(definition.sleeveUnits, { (unitless) : [1, 3, 20] } as IntegerBoundSpec);
            annotation { "Name" : "Sleeve chamfers", "Default" : true, "Description" :
                    "Bevel the sleeve edges and pin hole entrances, as in the original sleeve." }
            definition.sleeveChamfers is boolean;
        }
        if (definition.kind == HomerackerKind.FLAT_PANEL)
        {
            annotation { "Name" : "Width", "Description" : "Set the full rectangle width along X. Enter a length, such as 60 mm." }
            isLength(definition.flatWidth, { (millimeter) : [10, 60, 750] } as LengthBoundSpec);
            annotation { "Name" : "Height", "Description" : "Set the full rectangle height along Y. Enter a length, such as 45 mm." }
            isLength(definition.flatHeight, { (millimeter) : [10, 45, 750] } as LengthBoundSpec);
            annotation { "Name" : "Thickness", "Description" : "Set the plate thickness. Start with 2 mm for a mounting blank." }
            isLength(definition.flatThickness, { (millimeter) : [1, 2, 20] } as LengthBoundSpec);
            annotation { "Name" : "Face chamfers", "Default" : false, "Description" :
                    "Bevel the rectangle perimeter. Pin hole entrances always stay beveled." }
            definition.flatChamfers is boolean;
            annotation { "Name" : "Pin holes", "Item name" : "Pin hole",
                    "Item label template" : "(#holeX, #holeY) / #holeSize",
                    "UIHint" : UIHint.COLLAPSE_ARRAY_ITEMS, "Description" :
                    "Add a hole for each mounting position. X and Y are measured from the panel centre. Use 15 mm steps to match the HomeRacker grid." }
            definition.flatHoles is array;
            for (var hole in definition.flatHoles)
            {
                annotation { "Name" : "X from centre", "Description" : "Move this hole left or right from the panel centre. Negative values go left." }
                isLength(hole.holeX, { (millimeter) : [-375, 0, 375] } as LengthBoundSpec);
                annotation { "Name" : "Y from centre", "Description" : "Move this hole up or down from the panel centre. Negative values go down." }
                isLength(hole.holeY, { (millimeter) : [-375, 0, 375] } as LengthBoundSpec);
                annotation { "Name" : "Square hole size", "Description" : "Set the square opening size. Standard HomeRacker lock pins use 4 mm holes." }
                isLength(hole.holeSize, { (millimeter) : [1, 4, 10] } as LengthBoundSpec);
            }
        }
        annotation { "Name" : "Mate connectors", "Default" : true, "Description" :
                    "Add assembly connection points to this part. These move with the part in an assembly." }
        definition.mateConnectors is boolean;
    }
    {
        var label = "HomeRacker";
        if (definition.kind == HomerackerKind.SUPPORT)
        {
            label = label ~ " Support " ~ definition.supportUnits ~ "U" ~ (definition.xHoles ? " cross holes" : "");
            support(context, id, definition.supportUnits, definition.xHoles, definition.mateConnectors);
        }
        if (definition.kind == HomerackerKind.CONNECTOR)
        {
            var minWays = definition.connectorDimensions;
            var maxWays = 2 * definition.connectorDimensions;
            var ways = max(minWays, min(maxWays, definition.connectorWays));
            label = label ~ " Connector " ~ definition.connectorDimensions ~ "D " ~ ways ~ "-way" ~
                    (definition.pullAxis == PullAxis.NONE ? "" : " pull " ~ definition.pullAxis);
            connector(context, id, definition.connectorDimensions, ways, definition.pullAxis, definition.mateConnectors);
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
            label = label ~ " Flat panel " ~ (definition.flatWidth / millimeter) ~ "x" ~
                    (definition.flatHeight / millimeter) ~ "x" ~ (definition.flatThickness / millimeter) ~ " mm";
            flatPanel(context, id, definition.flatWidth, definition.flatHeight, definition.flatThickness,
                    definition.flatChamfers, definition.flatHoles, definition.mateConnectors);
        }
        setProperty(context, { "entities" : qBodyType(qCreatedBy(id, EntityType.BODY), BodyType.SOLID),
                "propertyType" : PropertyType.NAME, "value" : label });
        setFeatureComputedParameter(context, id, { "name" : "generatedName", "value" : label });
    }, {
        "kind" : HomerackerKind.SUPPORT,
        "sleeveUnits" : 3,
        "sleeveChamfers" : true,
        "flatWidth" : 60 * millimeter,
        "flatHeight" : 45 * millimeter,
        "flatThickness" : 2 * millimeter,
        "flatChamfers" : false,
        "flatHoles" : [],
        "supportUnits" : 3,
        "xHoles" : false,
        "connectorDimensions" : 3,
        "connectorWays" : 6,
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
