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
    annotation { "Name" : "Panel" } PANEL,
    annotation { "Name" : "Sleeve" } SLEEVE,
    annotation { "Name" : "Flat panel" } FLAT_PANEL
}

export enum PanelFit
{
    annotation { "Name" : "InterFit" } INTERFIT,
    annotation { "Name" : "Full cover" } FULL_COVER
}

export enum PullAxis
{
    annotation { "Name" : "No pull-through (closed center)" } NONE,
    annotation { "Name" : "X" } X,
    annotation { "Name" : "Y" } Y,
    annotation { "Name" : "Z" } Z
}

export enum ConnectorShape
{
    annotation { "Name" : "End (2U)" } END,
    annotation { "Name" : "Straight (3U)" } STRAIGHT,
    annotation { "Name" : "Elbow" } ELBOW,
    annotation { "Name" : "T-shape" } TEE,
    annotation { "Name" : "X-shape" } CROSS,
    annotation { "Name" : "Corner 3-way" } CORNER3,
    annotation { "Name" : "Corner 4-way" } CORNER4,
    annotation { "Name" : "5-way" } FIVE,
    annotation { "Name" : "6-way" } SIX,
    annotation { "Name" : "Custom dimensions", "Hidden" : true } LEGACY
}

function connectorShapeSpec(shape is ConnectorShape) returns array
{
    if (shape == ConnectorShape.END) return [1, 1, "End"];
    if (shape == ConnectorShape.STRAIGHT) return [1, 2, "Straight"];
    if (shape == ConnectorShape.ELBOW) return [2, 2, "Elbow"];
    if (shape == ConnectorShape.TEE) return [2, 3, "T-shape"];
    if (shape == ConnectorShape.CROSS) return [2, 4, "X-shape"];
    if (shape == ConnectorShape.CORNER3) return [3, 3, "Corner 3-way"];
    if (shape == ConnectorShape.CORNER4) return [3, 4, "Corner 4-way"];
    if (shape == ConnectorShape.FIVE) return [3, 5, "5-way"];
    return [3, 6, "6-way"];
}
