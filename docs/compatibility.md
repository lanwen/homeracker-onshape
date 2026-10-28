# Compatibility and comparison

This port derives from [HomeRacker](https://github.com/kellerlabs/homeracker) by Patrick Pötz.
It uses upstream revision `5a64b663ef2d91678d70b85e85df1bcc30c090d5`.
The logo also comes from upstream HomeRacker.
The repository stores it as `assets/homeracker-logo.svg`, and releases include it as `logo.svg`.
See [NOTICE](../NOTICE) and [LICENSE](../LICENSE) for attribution and license terms.

## Dimensions and inputs

One HomeRacker grid unit is 15 mm.
Connectors use 2 mm walls and a total fit allowance of 0.2 mm across each opening.
The fit allowance leaves space between connected parts.
Lock pins hold the parts together through nominal 4 mm square holes.

The Type field controls which inputs appear:

| Type | Inputs |
| --- | --- |
| Support | Length in 15 mm units, optional X pin holes |
| Connector | Named socket shapes, optional X/Y/Z pull-through, separate pin holes in closed core walls |
| Panel | X/Y grid size, InterFit or full cover, per-edge and corner mounts, optional face chamfers |
| Sleeve | Length in 15 mm units, optional sleeve chamfers |
| Flat panel | Rectangle grid width and height, thickness, optional face chamfers, individual edge grid positions with 4 mm square pin holes, connector notches |

Connector shapes select a valid combination of axes and arms.
The arm order is +Z, −Z, +X, −X, +Y, −Y.
Legacy programmatic dimension/direction inputs are checked against their permitted ranges.

Panels need a grid of at least 2 × 2 units.
InterFit panels sit between the supports.
Full cover panels overlap the frame.

Corner mounts can block pin holes for a panel on an adjacent 90° edge.
For large panels, turn off corner mounts if adjacent panels need the same pin holes.
The edge mount inputs control the mounting strips.
On the panel face, north is +Y and south is −Y.
East is +X and west is −X.

## Assembly points and names

Parts and feature entries get names based on their type and size.
Turn off Mate connectors if you do not need assembly points.
Each point belongs to its part and follows it into an assembly.

- Supports have points at both ends. Pin mates controls which hole openings also get points.
- Connectors have half-grid seating points and points at both inner openings of each arm pin hole. Pull-through axes use two opening points.
- Panels have points at the outer mounting-wall openings. Flat panels use the openings on their square back surface.

Pull-through opening points support a Slider mate for movement along the passage.
Support pin points sit at their outer openings and face outward.
Pin mates defaults to None. Selected rows adds only the chosen row and opening sides.
All rows adds both Z openings and both X openings when X pin holes are enabled.
Updating an existing feature uses the new default and can remove pin points referenced by assembly mates.
Set Pin mates to All rows to restore those points.
Connector pin points sit at inner openings and face into the socket or passage.
Their secondary axes follow the support length, socket arm, or pull-through direction.
A Fastened mate between these pin points aligns the holes and places the support against one wall.
The opposite wall retains the full 0.2 mm fit allowance.
A mate connector defines a position and direction. The assembly mate controls movement.
Mate Z axes follow the connection direction. Use Flip primary axis or Reorient secondary axis
in the assembly mate dialog when needed.
Panel pin points sit on their contact surfaces and face outward.
Sleeve pin points align the hole axes. Use a mate offset for the sleeve wall thickness when needed.
Panel Face chamfers controls only the perimeter of the flat plate and starts enabled.
Mounts and pin-hole entrances always stay chamfered.
Full cover corner relief cuts remain, as in the upstream design.

The flat panel is a native rectangular mounting blank with holes through its face.
Each 4 mm square hole has its own grid column and row.
Columns count from the left and rows from the top (C1 R1 is top-left); holes sit at edge grid-cell centres.
Only the first or last row or column can hold a pin hole.
Each corner can be notched, using a dialog checkbox or a clickable point in the model.
Notches clear the L-shaped footprint of a 19.2 mm connector core and its two inward arms.
The signed clearance defaults to 0.2 mm and ranges from -2 to +2 mm.
Hole placement uses the same footprint and excludes covered cells.
The community [blank panel](https://github.com/kellerlabs/homeracker-community/tree/main/models/blank_panel)
helped inform this mounting approach. Rectangle width, height, and hole positions use 15 mm grid units; thickness uses a length.

## Geometry details

The support's square pin passage follows the upstream cross sections.
It measures 5.6 mm at each entrance, 4 mm at ±6.7 mm, and 4.8 mm at its center.
A chamfer is a beveled edge.
The support has 1 mm chamfers on its outer edges.

Connector arm dimensions and direction selection follow the OpenSCAD construction.
The connector also follows its hollow arm design and trim for the printing base.
Its square pin holes have 0.8 mm lead-ins.
With a pull-through enabled, each closed core wall can have a separate 4 mm square pin hole.
Each hole crosses one 2 mm wall and has beveled entrances on both sides.
Sockets and the pull-through axis determine which wall controls appear.
Each enabled hole has an inward mate point at the inner opening.
A tetrahedron has four triangular faces.
Five- and six-way connectors include this shape for printing.

The panel follows the upstream grid dimensions and uses a 2 mm surface.
It has 13 × 2 mm L-shaped corner plates and square lock pin passages.
It also has optional intermediate edge mounts with bridges and gap fillers.
Full cover panels include a plate that overlaps the frame.
The connector's inner cavity chamfer lies on the arm-side plane.
The outer arm chamfer excludes that plane.

## Current results and limits

The feature creates supports, connectors, side panels, sleeves, and flat mounting blanks.

The feature compiles in Onshape with standard library `3095.0`.
Mate connectors attach to a face of their owner part.
This also works in Part Studios that use version 2960, where direct body attachments can fail.
All five part types pass mate attachment and movement tests in versions 2960 and 3095.
Regeneration rebuilds a part from its input values.
The original 15 comparison cases regenerate through the Onshape MCP server without notices.
The checks also verify mate counts, part ownership, and generated part names.
Each case produces one solid.

Part bounds describe the smallest box around a part.
For those 15 cases, part bounds match the SCAD references and volume differences are below 0.005%.
With face chamfers disabled, this port keeps the mount and pin-hole entrance chamfers.
Upstream disables those too, so the six additional cases have an intentional geometry difference.
Four sleeve cases also regenerate with owned mate points.
With sleeve edge chamfers enabled, their bounds match the SCAD references within 0.000001 mm and volume differences are below 0.005%.
With edge chamfers off, this port keeps the pin-hole entrance chamfers; upstream disables them.
Ten native flat-panel cases check individual edge grid positions, connector notches, thin plates, and face chamfers.
The plain, four-hole, and thin-plate cases have analytical volume checks.
After the module refactor, all 35 existing cases retain their recorded v0.1.5 volumes and bounds within 0.000001 mm or mm³.
The 42 core-wall cases regenerate without notices.
They cover every connector shape with no pull-through or X, Y, and Z pull-throughs.
Individual-side cases also cover disabled walls, beveled entrances, and mate positions.
Six fit cases align a support with a single pin-opening mate and produce no solid interference.
These cases cover positive and negative arms and core openings.
Fifteen panel fit cases place a support against panel contact surfaces without gaps or solid interference.
They cover three flat panel thicknesses and corner and edge mounts for both panel fits.
Nine support cases cover end-only points, selected rows and sides, all rows, duplicate selections, and disabled points.
Support pin mates now use opening positions instead of hole centers. Existing assembly mates that reference them move when the feature updates.
Paired volume tests make sure that unavailable holes leave geometry unchanged.
STEP surface comparison remains pending.
The numerical results do not establish fit for your printer and material.

## Create reference meshes

A reference mesh is a shape from the upstream design.
This workflow uses fixed versions of the tools and upstream source.
Use a terminal in this repository for all commands below.

The setup task requires mise, Python 3.12, macOS, and an internet connection.
It installs OpenSCAD 2026.10.23 and BOSL2 v2.0.774 into `.tools/`.
It also installs the Python packages into `.venv`.

Before you create references, prepare a sibling `../homeracker` checkout at revision `5a64b663ef2d91678d70b85e85df1bcc30c090d5`.
The setup task does not create that checkout.
The reference task leaves its source unchanged.

To prepare the tools and references:

```sh
mise run setup
mise run build
mise run reference
mise run test
```

`HOMERACKER_SOURCE` can select another checkout of the same revision.
`OPENSCAD` and `BOSL2_PATH` can select installed tools.
The reference task makes sure that their versions and BOSL2 source match the fixed tool versions.

The cases in [comparisons/cases.json](../comparisons/cases.json) cover supports, cross holes, connector arms, pull-throughs, both panel fits, minimum panels, partial mounts, panels with face chamfers disabled, and sleeves.
[comparisons/flat-panels.json](../comparisons/flat-panels.json) holds native rectangular blank fixtures.
[comparisons/core-pin-holes.json](../comparisons/core-pin-holes.json) covers core walls, individual side controls, and pull-through interactions.
[comparisons/baseline-v0.1.5.json](../comparisons/baseline-v0.1.5.json) records volumes and bounds for 35 existing cases.
These measurements detect changes to existing parts during refactoring.
The reference task saves meshes under `artifacts/reference/`.
It also records the inputs, tool versions, and file hashes there.

## Rendering backends

A backend is the geometry engine that OpenSCAD uses.
Most cases use Manifold.
Five- and six-way connectors use CGAL.

The fixed Manifold build produces a non-manifold six-way connector around its printing tetrahedron.
CGAL produces a continuous surface with open sockets for these connectors.
CGAL also introduces submicron membranes across support holes and separates panel surfaces at coincident boundaries.

The case file records the backend for each case.
The tools do not repair these meshes.
The tests cover continuous surfaces, open support holes, and connector socket stops.

To try a backend for one case:

```sh
mise run reference -- --backend CGAL --case support_1u
mise run reference -- --backend Manifold --case connector_6way
```

These commands write to `artifacts/reference-cgal/` or `artifacts/reference-manifold/`.
They preserve the reference meshes that the comparison uses.
Invalid reference meshes cause a failing exit status with diagnostic metadata.

## Compare STEP exports

The validation task rebuilds `dist/homeracker.fs` from `src/`.
It then writes `artifacts/homeracker-validation.fs` without API calls.
See [Development](development.md#test-geometry-in-onshape) for instructions to create and run this script.

The extra Homeracker comparisons feature creates the named cases in one regeneration.
It makes sure that each case produces one solid.
It prints volumes and bounds in millimeters.
The generated JSON records the source hashes.

For a smaller batch, pass a case name with `--case` to `mise run validation`.
Use the comparison script in a development document only.
Edit the source fragments instead of either generated FeatureScript file.

Export each Onshape part to `artifacts/onshape/<case-name>.step` in its original coordinate frame.
Use the same parameters and orientation as the SCAD calls.
Do not enable optimal print orientation.

To compare the exports with selected references:

```sh
mise run compare -- --case support_3u --case connector_elbow --case panel_interfit_4x3
```

To compare every case, omit the case flags.
The comparison tool writes its results to `artifacts/comparison.json`.
The report includes file hashes and the source of each reference.
Missing exports appear as pending and cause a failing exit status.
They never count as passed.

The tool makes sure that the STEP solids are valid before the comparison.
It uses 0.005 mm for the mesh surface deviation.
It compares closed surfaces, face directions, surface counts, bounds, volume, and distances in both directions.
It uses every mesh vertex and triangle center, plus 8,000 samples per direction.
The sample positions repeat when the inputs stay the same.
Larger areas have a higher chance of a sample.

The default limits are 0.03 mm maximum sampled distance and 0.1% relative volume difference.
The tool does not align, translate, scale, or repair meshes.
The result is a numerical comparison, not proof of exact equality or printed fit.

To compare one pair of files:

```sh
.venv/bin/python scripts/compare.py reference.stl exported.step --output artifacts/pair.json
```

Sleeve pin-hole entrances stay chamfered when sleeve edge chamfers are off.
Flat panels chamfer only their front perimeter and front hole entrances; the back stays square.
Both panel types have mate connectors only at their pin holes.
