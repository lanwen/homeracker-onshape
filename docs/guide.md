# HomeRacker for Onshape

Make individual supports, connectors, sleeves, and panels with the Homeracker custom feature.
Each grid unit is 15 mm. Choose a part type, then set its size and options.
Hover over a setting in Onshape for a short explanation.
See the [HomeRacker model guide](https://homeracker.org/models/) for more detail on the original parameters.

Based on [HomeRacker](https://github.com/kellerlabs/homeracker) by Patrick Pötz (kellervater).
The [community blank panel](https://github.com/kellerlabs/homeracker-community/tree/main/models/blank_panel) also helped inform the flat mounting concept.
The Onshape port is maintained by Kirill Merkushev in the [homeracker-onshape project](https://github.com/lanwen/homeracker-onshape).

## Add the feature

In an editable Part Studio, open **Add custom features** from the toolbar.
Paste this document link into the search box and select **Homeracker**:

```text
https://cad.onshape.com/documents/98b88f085d92f997adf9129c/w/d000ce49db154cdce9e482df/e/6d2975b4f296af4b2ed01e1f
```

Use **Homeracker** from the custom features toolbar to create a part.
Set **Type** to the part you want. Confirm the feature when the preview looks right.
Size and primary options are grouped under **Shape**. Expand **Mounts** or **Connector notches** when you need those settings.

## Support

Supports are the straight bars between connectors. Their square holes take lock pins.

- **Length in grid units**: Sets the bar length, from 1 to 50 units. For example, 3 units makes a 45 mm bar.
- **X pin holes**: Adds holes across the bar in a second direction, perpendicular to the standard holes.

A support is drawn along the Y axis. A 3-unit support without X pin holes is a useful starting point.

Assembly points start with both support ends.
Set Pin mates to None, Selected rows, or All rows.
For Selected rows, add a row and choose its opening sides.
Row 1 is the hole nearest the -Y end.
The row number cannot exceed the support length in grid units.
X opening controls appear when X pin holes are enabled.

## Connector

Connectors join supports along one or more axes. Each arm has a socket for a support.

- **Connector shape**: Choose End (2U), Straight (3U), Elbow, T-shape, X-shape, Corner 3-way, Corner 4-way, 5-way, or 6-way.
- **Core pin holes**: With a pull-through enabled, choose each closed wall that needs a pin hole. Each hole is 4 mm square with beveled entrances. Only walls without sockets appear.
- **Pull through axis**: Choose X, Y, or Z to open a support-sized passage through the centre. Choose No pull-through (closed center) to keep the socket stops.

Arms are added in a fixed order; use the preview to check their positions.

## Panel

Panels cover the sides of the rack. They have a 2 mm face and raised edge walls with sideways pin holes.
Choose **Flat panel** for holes through the flat face.

- **Grid width / Grid height**: Sets the size in 15 mm units, from 2 to 50 units on each axis. These are grid sizes; the finished outline also depends on the fit and mounts.
- **Panel fit**: InterFit sits between the supports and can be removed. Full cover overlaps the frame and needs to be assembled with it.
- **Adjacent panel clearance**: Appears for Full cover only. Sets the gap between neighbouring full cover panels. Enter a length, such as 0.5 mm.
- **Face chamfers**: Bevels the perimeter of the flat panel face. Turn it off to leave that perimeter square. Mounts and pin-hole entrances always stay beveled.
- **Corner mounts**: Adds full-height corner mounts with pin holes. Turn it off when a panel on an adjacent side needs those holes. Short corner outlines remain.
- **North / South / East / West mounts**: Adds the mounting strip on each edge. On the panel face, north is +Y, south is -Y, east is +X, and west is -X. Strips appear on edges longer than 2 grid units. InterFit keeps an edge wall and pin holes when a strip is off.

Start with a 4 x 3 InterFit panel. Check the preview before disabling mounts, especially where two panels meet.

## Sleeve

A sleeve slides around three sides of a support. Its side holes take lock pins.

- **Length in grid units**: Sets the sleeve length from 1 to 20 units. A small fit allowance makes it 0.2 mm shorter than that grid length.
- **Sleeve chamfers**: Bevels the sleeve edges. Pin-hole entrances always stay beveled.

The open side points along +Y; the length runs along Z. Use the support-axis or pin mate points to position it on a support.

## Flat panel

A flat mounting blank for building your own device mount. Pin holes are fixed at 4 mm square and go through the face at edge grid cells.
After creating the blank, use Onshape sketches, extrudes, and cuts to add the shapes your device needs.

- **Grid width / Grid height**: Sets the full rectangle size along X and Y in 15 mm units. A 4 x 3 unit plate is 60 x 45 mm.
- **Thickness**: Sets the plate thickness. Start with 2 mm and increase it when your mount needs more stiffness.
- **Face chamfers**: Starts enabled and bevels the front perimeter. Front pin-hole entrances stay beveled; the back stays square.
- **Connector notches**: Toggle top-left, top-right, bottom-left, or bottom-right to clear an L-shaped connector footprint at that corner, including the next cell along both edges. Click the corner points in the model, or expand the Connector notches group in the dialog. Notch clearance defaults to 0.2 mm and accepts -2 to +2 mm. Positive values make more room; negative values tighten the notch.
- **Pin holes**: Each new entry takes the next free edge grid cell. Add disappears when all usable cells are filled; remove a hole to bring it back. Starts with one hole in each corner grid cell. Each collapsed row shows its column and row, such as C1 R3. Expand a row to edit it, or remove the row to delete a hole.
- **Column / Row**: Places the hole at the centre of a 15 mm grid cell. Columns count from the left; rows count from the top, so C1 R1 is the top-left cell. A 4 x 3 unit plate has columns 1-4 and rows 1-3. Holes must use column 1 or 4, or row 1 or 3.

A new 4 x 3 unit plate (60 x 45 mm) starts with four 4 mm holes at C1 R1, C4 R1, C1 R3, and C4 R3.
Enabling a notch removes holes it covers. New holes skip those cells.

## Put parts together

**Mate connectors** adds assembly points to any part type. It starts enabled; turn it off if you do not need them.
The points belong to the part and move with it when you insert the part into an assembly.

- **Supports** have points at both ends. Pin mates adds points only at the selected hole openings, or at every opening with All rows.
- **Connectors** keep seating points for closed sockets. Arm pin holes have points at their inner openings. Core pin holes have points inside the pull-through. These points face into the socket or passage. A pull-through also has an outward point at each end.
- **Panels** have points at pin-hole openings on the outer mounting-wall surfaces.
- **Sleeves** have a support-axis point, an inner-back point, and a point at each pin-hole station.
- **Flat panels** have a point at each hole on the square back surface.

For a sliding support, use its end point and a connector Pull-through opening point in a Slider mate.
The support can move along the passage without rotating.
For a fixed position, use a Fastened mate between a support pin opening and the connector's inner pin opening.
This mate aligns the holes and the support length.
It places the support against the selected wall, with 0.2 mm clearance at the opposite wall.

Use an assembly mate to join two points. Flip or reorient the axes in the mate dialog when needed.
Panel points meet support openings at the contact surface.
Sleeve pin points align the hole axes. Use a mate offset for the sleeve wall thickness when needed.

Parts and new feature entries get names based on their type and size, such as **HomeRacker Support 3U**.
A feature name you set yourself stays as you set it.
