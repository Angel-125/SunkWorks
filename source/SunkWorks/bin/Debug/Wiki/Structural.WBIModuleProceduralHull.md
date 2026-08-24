            
Generates a flat-bottomed boat hull by lofting calculated cross-section stations. The authored model supplies four renderers/materials; their reference meshes are replaced with per-part runtime meshes for the upper hull, lower hull, deck, and railings.
        
## Fields

### widthAxis
Part-local port-to-starboard direction.
### lengthAxis
Part-local bow-to-stern direction.
### downAxis
Part-local direction from the deck toward the bottom.
### textureDensityU
Longitudinal texture density, in source-image pixels per meter.
### textureDensityV
Transverse, vertical, or surface-direction texture density, in source-image pixels per meter.
### debugMode
Shows mesh-tessellation controls in the editor PAW.
### showWireframe
Draws the final procedural render meshes as white triangle edges.
## Methods


### RebuildHullEvent
Regenerates all visual and collision geometry from the persisted parameters.

### RebuildHullForAnalysis
Rebuilds this hull and its drag cube for editor tools that need current geometry. The caller is responsible for visiting each craft part, so symmetry is not expanded here.

