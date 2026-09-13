# Changelog

## 5.2.0

- Added a clear **Export / Exportar** button to the Output section while keeping `Ctrl+E` as a shortcut.
- Added optional AsciiForge attribution to PowerShell, HTML, JSON and standalone C# exports. It is enabled by default and can be disabled.
- Added `OUTPUT-NOTICE.md`: generated animations may be used freely, while attribution is explicitly requested when reasonably possible.
- Expanded Warp Grid 3D with procedural heightfield terrain: terrain height, scale, smoothness, valley depth and fine detail.
- Added six terrain-focused Warp Grid 3D presets: Mountain Range, Rolling Hills, Deep Valleys, Rugged Peaks, Soft Dunes and Alien Highlands.
- Refactored I/O/export code out of the main form and moved effect IDs and shader-uniform mappings into dedicated registries.
- Replaced parallel uniform arrays with paired uniform bindings.
- Added OpenGL uniform-location caching to reduce repeated driver lookups during rendering.
- Kept the SDF Lab orbit camera and finite repetition changes from 5.1.2.
- Updated project and release metadata to 5.2.0.

## 5.1.2

- Reworked SDF Lab repetition so repeated scenes are finite and the camera no longer starts inside repeated geometry.
- Added SDF camera yaw, pitch and copy spacing controls.
- Added direct mouse orbit in SDF Lab: drag the preview to rotate the camera and use the mouse wheel to zoom.
- Retuned all SDF Lab presets for readable outside views of repeated forms.

## 5.1.1

- Added live Spanish / English UI switching.
- Added English parameter labels, tooltips, dialogs and repository README.
- Kept effect and preset identifiers language-neutral for compatible presets/exports.

## 5.1.0

- Added 11 new GPU effect families: 3D Shapes, 3D Terrain, SDF Lab, Flow Field, Lightning, Black Hole, Strange Attractor, Voronoi Cells, Snowstorm, DNA Helix and Warp Grid 3D.
- Added dozens of presets for the new engines.
- 3D Shapes supports sphere, cube, octahedron, torus, cylinder, capsule and pyramid with XYZ rotation, camera, perspective and lighting controls.
- Kept the 5.0.5 drift fix, simple control tooltips, wave/water engines and expanded Cellular Automaton.
