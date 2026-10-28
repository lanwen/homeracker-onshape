# HomeRacker for Onshape

Make individual supports, connectors, and side panels with the Homeracker custom feature.
Each grid unit is 15 mm. Choose a part type, then set its size and options.
Hover over a setting in Onshape for a short explanation.
See the [HomeRacker model guide](https://homeracker.org/models/) for more detail on the original parameters.

## Add the feature

In an editable Part Studio, open **Add custom features** from the toolbar.
Paste this document link into the search box and select **Homeracker**:

```text
https://cad.onshape.com/documents/98b88f085d92f997adf9129c/w/d000ce49db154cdce9e482df/e/6d2975b4f296af4b2ed01e1f
```

Use **Homeracker** from the custom features toolbar to create a part.
Set **Type** to Support, Connector, or Panel. Confirm the feature when the preview looks right.

## Support

Supports are the straight bars between connectors. Their square holes take lock pins.

- **Length in grid units**: Sets the bar length, from 1 to 50 units. For example, 3 units makes a 45 mm bar.
- **X pin holes**: Adds holes across the bar in a second direction, perpendicular to the standard holes.

A support is drawn along the Y axis. A 3-unit support without X pin holes is a useful starting point.

## Connector

Connectors join supports along one or more axes. Each arm has a socket for a support.

- **Dimensions**: Chooses how many axes the connector spans. Use 1 for a straight connection, 2 for a flat connection, or 3 for a connection in three dimensions.
- **Directions**: Sets the number of arms. The range is 1-2 arms for one dimension, 2-4 for two dimensions, or 3-6 for three dimensions. Values outside that range are adjusted automatically.
- **Pull through axis**: Opens the centre along X, Y, or Z so a support can pass through. Choose None for a connector with socket stops.

For an elbow, choose 2 dimensions and 2 directions. For a three-arm corner, choose 3 dimensions and 3 directions.
Arms are added in a fixed order; use the preview to check their positions.

## Panel

Panels cover the sides of the rack. They have a 2 mm face and mounts for lock pins.

- **Grid width / Grid height**: Sets the size in 15 mm units, from 2 to 50 units on each axis. These are grid sizes; the finished outline also depends on the fit and mounts.
- **Panel fit**: InterFit sits between the supports and can be removed. Full cover overlaps the frame and needs to be assembled with it.
- **Adjacent panel clearance**: Appears for Full cover only. Sets the gap between neighbouring full cover panels. Enter a length, such as 0.5 mm.
- **Face chamfers**: Bevels the perimeter of the flat panel face. Turn it off to leave that perimeter square. Mounts and pin-hole entrances always stay beveled.
- **Corner mounts**: Adds full-height corner mounts with pin holes. Turn it off when a panel on an adjacent side needs those holes. Short corner outlines remain.
- **North / South / East / West mounts**: Adds the mounting strip on each edge. On the panel face, north is +Y, south is -Y, east is +X, and west is -X. Strips appear on edges longer than 2 grid units. InterFit keeps an edge wall and pin holes when a strip is off.

Start with a 4 x 3 InterFit panel. Check the preview before disabling mounts, especially where two panels meet.

## Put parts together

**Mate connectors** adds assembly points to any part type. It starts enabled; turn it off if you do not need them.
The points belong to the part and move with it when you insert the part into an assembly.

- **Supports** have points at both ends and at each pin-hole centre.
- **Connectors** have a point at each socket floor. Mate a support end to one of these points to seat it fully.
- **Panels** have points at their pin-hole centres and at the centre of the front face.

Use an assembly mate to join two points. Flip or reorient the axes in the mate dialog when needed.
Pin-hole points align the hole axes; you may need an offset for wall thickness and fit clearance.

Parts and new feature entries get names based on their type and size, such as **HomeRacker Support 3U**.
A feature name you set yourself stays as you set it.

## About this feature

Based on [HomeRacker](https://github.com/kellerlabs/homeracker) by Patrick Pötz (kellervater).
The Onshape port is maintained by Kirill Merkushev in the [homeracker-onshape project](https://github.com/lanwen/homeracker-onshape).
