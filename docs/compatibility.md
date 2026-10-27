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
| Connector | 1–3 dimensions, directions, optional X/Y/Z pull through |
| Panel | X/Y grid size, InterFit or full cover, per-edge and corner mounts |

For connectors, the Dimensions field sets the number of axes that the connector spans.
The Directions field sets the number of arms.
The arm order is +Z, −Z, +X, −X, +Y, −Y.
If the direction count is outside the permitted range, the feature uses the nearest permitted count.

Panels need a grid of at least 2 × 2 units.
InterFit panels sit between the supports.
Full cover panels overlap the frame.

Corner mounts can block pin holes for a panel on an adjacent 90° edge.
For large panels, turn off corner mounts if adjacent panels need the same pin holes.
The edge mount inputs control the mounting strips.
On the panel face, north is +Y and south is −Y.
East is +X and west is −X.

## Geometry details

The support's square pin passage follows the upstream cross sections.
It measures 5.6 mm at each entrance, 4 mm at ±6.7 mm, and 4.8 mm at its center.
A chamfer is a beveled edge.
The support has 1 mm chamfers on its outer edges.

Connector arm dimensions and direction selection follow the OpenSCAD construction.
The connector also follows its hollow arm design and trim for the printing base.
Its square pin holes have 0.8 mm lead-ins.
A tetrahedron has four triangular faces.
Five- and six-way connectors include this shape for printing.

The panel follows the upstream grid dimensions and uses a 2 mm surface.
It has 13 × 2 mm L-shaped corner plates and square lock pin passages.
It also has optional intermediate edge mounts with bridges and gap fillers.
Full cover panels include a plate that overlaps the frame.
The connector's inner cavity chamfer lies on the arm-side plane.
The outer arm chamfer excludes that plane.

## Current results and limits

The feature creates supports, connectors, and plain panels.
It does not create lock pins or panel cutout variants.
It does not expose the OpenSCAD `optimal_orientation` control for print rotation.

The feature compiles in Onshape with standard library `3095.0`.
Regeneration rebuilds a part from its input values.
All 15 comparison cases regenerate through the Onshape MCP server without notices.
Each case produces one solid.

Part bounds describe the smallest box around a part.
The part bounds match the SCAD references, and volume differences are below 0.005%.
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

The cases in [comparisons/cases.json](../comparisons/cases.json) cover supports, cross holes, connector arms, pull-throughs, both panel fits, minimum panels, and partial mounts.
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
