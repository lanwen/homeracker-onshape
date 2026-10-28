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
    annotation { "Name" : "None" } NONE,
    annotation { "Name" : "X" } X,
    annotation { "Name" : "Y" } Y,
    annotation { "Name" : "Z" } Z
}
