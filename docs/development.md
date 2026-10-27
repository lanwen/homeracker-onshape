# Development

The source files are in `src/`.
Local commands create files and run Python tests.
Onshape compiles and runs FeatureScript.
Use a terminal in the repository root for all commands below.

## Build the script

The build task requires mise and Python 3.12.
Each source fragment contains part of the script.
The bundle combines the source files into one file.

To create the bundle, run:

```sh
mise run build
```

This command creates `dist/homeracker.fs` and `dist/homeracker.sources.json`.
It does not need `.venv`, upload the script, or call Onshape.
Git ignores `dist/` because these files come from the source.

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
The code fragments do not need separate Onshape tabs or imports.
The feature icon uses one versioned SVG import, which is already in the source.

After each source change, run `mise run build`.
Commit the source changes and related tests or documentation.
Do not edit or commit the generated files in `dist/`.

After the build, make sure that the generated files match the source:

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

## Run local tests

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

To build the script and run the local tests:

```sh
mise run build
mise run test
```

A reference mesh is a shape from the upstream design.
The tests cover the local tools and available reference meshes.
The local tests do not compile or run FeatureScript.
See [Compatibility](compatibility.md) for the reference setup and numerical comparison.

## Test geometry in Onshape

Regeneration rebuilds a part from its input values.
The validation task creates a script with comparison cases.
It does not execute those cases locally or call Onshape.

After local setup, create the comparison script:

```sh
mise run validation
```

This task rebuilds the bundle from the current source.
It writes `artifacts/homeracker-validation.fs` with an extra Homeracker comparisons feature.
That feature creates the named comparison parts when you run it in Onshape.
The generated JSON records the source hashes and selected case names.

The validation script is for a development document only.
To use a smaller batch, pass a case name with `--case`.
The cases are in [comparisons/cases.json](../comparisons/cases.json).

## Use a local build in a Feature Studio

A Feature Studio stores FeatureScript code.
Use this procedure to test your own changes in an editable Onshape document.
Use `dist/homeracker.fs` from the local build or `homeracker.fs` from a release.

1. Open an editable Onshape document.
2. Create a Feature Studio in that document.
3. Paste the contents of the script into the Feature Studio.
4. Click Commit in the Feature Studio.
5. Open the Custom features menu in a Part Studio of that workspace.
6. Select Homeracker.
7. Set the part type and its inputs.
8. Click the green checkmark to create the part.

To run the comparison cases, use `artifacts/homeracker-validation.fs` instead.
Select Homeracker comparisons in the Part Studio.
The feature makes sure that each case produces one solid and prints its volume and bounds.

## Releases and shared features

The [latest release](https://github.com/lanwen/homeracker-onshape/releases/latest) contains the files that users can download.
The complete script works in one Feature Studio.
The other release files describe that script:

- `homeracker.fs`: Complete FeatureScript
- `homeracker.sources.json`: Source line map
- `SHA256SUMS`: SHA-256 hashes of the release files
- `logo.svg`: Logo for the Onshape feature icon

Releases use semantic version tags, such as `v0.1.0`.
The header and source line map contain the same version.
`VERSION` sets the starting version for the next release.
The first semantic release is `v0.1.0`.
Later source changes on `main` increase the patch number automatically.
To select a new minor or major version, set `VERSION` above the latest release.
For example, use `0.2.0` for new features or `1.0.0` for the first stable interface.
A rerun for the same commit keeps its original version.
Old `bundle-<commit SHA>` releases remain available.

Local builds use `VERSION` by default.
To reproduce a published release, use its version explicitly:

```sh
python3.12 scripts/build.py --version 0.1.0
```

The release notes identify the exact source commit.
Pushes to `main` trigger releases when source, build, asset, mise, or workflow files change.
Documentation-only changes do not trigger a release.
The workflow also supports `workflow_dispatch` for a manual release.

### Sync a release to Onshape

GitHub releases do not update Onshape. You can ask for an MCP sync or do it yourself:

1. Download `homeracker.fs` from the release and open the shared Homeracker Feature Studio.
2. Replace its code with the downloaded file, then click Commit.
3. Create a document version with the release tag as its name, such as `v0.1.0`.
   Add the GitHub release link to its description.

For an existing linked feature, select the new document version in Onshape.

To share the feature with other documents, create a version of its Onshape document.
Give users at least `View, copy & export` access to that document.
Users can then add Homeracker through Add custom features.
See the [Onshape instructions](https://cad.onshape.com/help/Content/PartStudio/add_custom_features.htm) for sharing and version updates.

## Set the feature icon

Onshape stores the SVG in an imported image tab.
The import namespace names the image source.
The feature's `Icon` annotation reads that image through `BLOB_DATA`.
The import in `src/preamble.fs` points to a versioned image tab.

If you need an image tab in your own Onshape document:

1. Import `logo.svg` from a release into your Onshape document.
2. Create a version of that document.
3. Use the Feature Studio import control to select the SVG tab from that version.
4. Replace the image import in `src/preamble.fs` with the import that Onshape creates.
5. Keep the existing import namespace so that the `Icon` annotation can find the image.
6. Run `mise run build`.
7. Paste the complete script into the Feature Studio.
8. Click Commit.

A GitHub file URL cannot replace the Onshape image import.
See the [Onshape icon documentation](https://cad.onshape.com/FsDoc/uispec.html#icons) for the import format.
