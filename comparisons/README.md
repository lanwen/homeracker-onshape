# Geometry comparison

Run from this repository with the sibling `../homeracker` checkout at revision
`5a64b663ef2d91678d70b85e85df1bcc30c090d5`:

```sh
mise run setup
mise run reference
mise run test
```

Setup downloads checksum-verified OpenSCAD 2026.10.23 for macOS and BOSL2
v2.0.774 into `.tools/`, and installs Python dependencies into `.venv/`.
It requires Python 3.12 and network access. It leaves the upstream checkout
unchanged. `HOMERACKER_SOURCE` can select another checkout of the same revision.
`OPENSCAD` and `BOSL2_PATH` can select installed tools; the renderer version and
BOSL2 SCAD sources are verified against the pinned toolchain.

The cases in [cases.json](cases.json) cover supports, cross holes, connector
topologies, pull-throughs, both enclosure-panel fits, minimum panels, and partial
mounts. References and their input/version/hash metadata appear under
`artifacts/reference/`.

## Rendering backends

Most cases use Manifold. Five- and six-way connectors explicitly use CGAL.
The pinned Manifold build produces a non-manifold six-way connector around its
printing tetrahedron. CGAL produces a continuous surface with open sockets for
these connectors. Conversely, CGAL introduces submicron membranes across
support holes and separates panel surfaces at coincident boundaries. The
manifest records backend selection; the tooling never repairs these meshes.
Tests check continuous surfaces, open support holes, and connector socket stops.

To investigate a backend independently:

```sh
mise run reference -- --backend CGAL --case support_1u
mise run reference -- --backend Manifold --case connector_6way
```

Explicit backend runs go to `artifacts/reference-cgal/` or
`artifacts/reference-manifold/` and preserve the selected comparison baseline.
Invalid references produce a failing exit status with diagnostic metadata.

## STEP exports

`mise run validation` first rebuilds `dist/homeracker.fs` from the fragments in
`src/`, then writes `artifacts/homeracker-validation.fs` without making API calls.
It appends a separate **Homeracker comparisons** feature to the bundle, using
the Onshape definitions in the manifest. This feature creates
all named cases in one regeneration, asserts one solid per case, and prints
volumes and bounds in millimetres. Use `--case <name>` to build a smaller batch.
The generated JSON records both source hashes. The public feature stays in
`dist/homeracker.fs`; the fixture is for a development document only. Edit
the source fragments, not either generated FeatureScript file.

Export each Onshape part in its original coordinate frame to
`artifacts/onshape/<case-name>.step`. Onshape cases must use the same parameters
and orientation as the SCAD calls. Do not enable optimal print orientation.

```sh
mise run compare -- --case support_3u --case connector_elbow --case panel_interfit_4x3
```

Omit the case flags to compare every case. The comparator validates STEP B-reps,
tessellates at 0.005 mm deflection, and checks watertightness, winding, surface
component count, bounds, volume, and distances in both directions. It uses every
mesh vertex and triangle centre plus 8,000 deterministic area-weighted samples
per direction. Default limits are 0.03 mm maximum sampled distance and 0.1%
relative volume difference. No alignment, translation, scaling, or mesh repair
is applied. This is a numerical comparison, not a proof of exact equality or a
physical fit test.

The report is `artifacts/comparison.json`, including file hashes and reference
provenance. Missing exports are reported as pending and return a failing exit
status; they never count as passed. A single pair can also be checked:

```sh
.venv/bin/python scripts/compare.py reference.stl exported.step --output artifacts/pair.json
```
