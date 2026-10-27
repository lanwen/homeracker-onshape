# HomeRacker for Onshape

HomeRacker for Onshape creates individual supports, connectors, and plain panels.
You choose the part type and its dimensions in one Homeracker custom feature.
The parts use the HomeRacker grid, where one unit is 15 mm.

Connectors use 2 mm walls and a total fit allowance of 0.2 mm across each opening.
The fit allowance leaves space between connected parts.
Lock pins hold the parts together through nominal 4 mm square holes.

## Use the feature in Onshape

The feature is available in the [Homeracker Onshape document](https://cad.onshape.com/documents/98b88f085d92f997adf9129c/w/d000ce49db154cdce9e482df/e/6d2975b4f296af4b2ed01e1f).
You do not need the local development tools to use it.
The file [dist/homeracker.fs](dist/homeracker.fs) contains the complete script for a Feature Studio.
A Feature Studio stores the script.
A Part Studio contains your parts.

To add the script to your own Onshape document:

1. Open an editable Onshape document.
2. Create a Feature Studio in that document.
3. Paste the contents of `dist/homeracker.fs` into the Feature Studio.
4. Click Commit in the Feature Studio.
5. Add the Homeracker custom feature to the Part Studio toolbar.
6. Open the Homeracker feature in a Part Studio.
7. Select the part type in the Type field.
8. Set the dimensions and other inputs for that part.
9. Click the green checkmark to create the part.

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

## Edit and build the script locally

The local commands require mise and Python 3.12.
Use a terminal in this repository for all commands below.
The source files are in `src/`.
Each source fragment contains part of the script.
The bundle combines the source files into one file.

To create the bundle, run:

```sh
mise run build
```

This command creates `dist/homeracker.fs` and `dist/homeracker.sources.json`.
It uses Python 3.12 and does not need the `.venv` directory.
The build task does not upload the script or call Onshape.
Onshape compiles and runs the FeatureScript after you upload it.

Edit the source files by their purpose:

- `src/preamble.fs`: Language version, standard library import, and license notice
- `src/constants.fs`: Dimensions and enums, which are named choices
- `src/geometry.fs`: Shared functions that create geometry
- `src/support.fs`: Support construction
- `src/connector.fs`: Connector construction
- `src/panel.fs`: Panel construction
- `src/feature.fs`: Feature dialog and main function

The bundle uses FeatureScript standard library `3095.0`.
To change the language or standard library version, edit `src/preamble.fs`.
Then run `mise run build`.

The source fragments do not work as separate Feature Studios.
The build task joins them in the order in `scripts/build.py`.
They share function and variable names in the bundle.
The bundle does not need Onshape tab IDs or local imports.

After each source change, run `mise run build`.
Commit the source changes with both generated files in `dist/`.
Do not edit the generated files directly.

To make sure that the generated files match the source, run:

```sh
mise run build:check
```

This task stops with an error if the generated files are missing or out of date.
It does not change those files.
Like the build task, it does not need `.venv` or call Onshape.

The line map in `dist/homeracker.sources.json` links bundle lines to source files.
A SHA-256 hash identifies the contents of a file.
The line map records each fragment's line range and SHA-256 hash.
For an Onshape notice at line N, find the range that contains N in the line map.
The source line is `N - start_line + 1`.

## Prepare local tests and comparison cases

The test and validation tasks need the Python environment in `.venv`.
This environment stores the Python packages for this project.
The setup task currently supports macOS only and needs an internet connection.

Before you run those tasks, create the environment:

```sh
mise run setup
```

The setup task creates `.venv` and installs its Python packages.
It also downloads fixed versions of OpenSCAD and BOSL2 into `.tools/`.
It does not download the upstream HomeRacker source.

To run the local tests, run:

```sh
mise run test
```

The test task first makes sure that the generated bundle matches the source.
A reference mesh is a shape from the upstream design.
The tests cover the local tools and available reference meshes.
They do not compile or run FeatureScript.

To create the comparison script for Onshape, run:

```sh
mise run validation
```

This task rebuilds the bundle from the current source.
It writes `artifacts/homeracker-validation.fs` with an extra Homeracker comparisons feature.
That feature creates the named comparison parts when you run it in Onshape.
The local command only writes files and makes no API calls.

The [comparison workflow](comparisons/README.md) explains how to create the 15 upstream SCAD reference meshes and compare exported STEP files.
That workflow needs the upstream HomeRacker checkout at the specified revision.
After setup, `mise run reference` creates the reference meshes locally.
The comparison tools use STEP files that you export from Onshape.

## Current scope and test results

The feature creates supports, connectors, and plain panels.
It does not create lock pins or panel cutout variants.
It does not expose the OpenSCAD `optimal_orientation` control for print rotation.
The [geometry details](comparisons/README.md#geometry-details) describe the dimensions and construction.

The feature compiles in Onshape.
Regeneration rebuilds a part from its input values.
All 15 comparison cases regenerate through the Onshape MCP server without notices.
Each case produces one solid.

Part bounds describe the smallest box around a part.
The part bounds match the SCAD references, and volume differences are below 0.005%.
STEP surface comparison remains pending.

The numerical results do not establish fit for your printer and material.
The [comparison workflow](comparisons/README.md) states the limits of the numerical comparison.

## Attribution

This port derives from [HomeRacker](https://github.com/kellerlabs/homeracker) by Patrick Pötz.
It uses revision `5a64b663ef2d91678d70b85e85df1bcc30c090d5`.
See [NOTICE](NOTICE) and [LICENSE](LICENSE) for attribution and license terms.
