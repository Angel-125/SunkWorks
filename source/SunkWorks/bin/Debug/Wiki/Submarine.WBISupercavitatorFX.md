            
Renders the supercavity as a translucent procedural mesh in flight and as a full-strength preview in the editor.
        
## Fields

### showCavity
Shows or hides the cavity visualization.
### cavityOpacity
Opacity of the translucent cavity shell.
### runningEffect
Part EFFECTS group driven by the supercavitator's normalized cavity scale.
### cavityColor
RGB color encoded as a KSP ConfigNode color string.
### lengthSegments
Number of subdivisions along the cavity.
### radialSegments
Number of subdivisions around the cavity.
### diagnosticRingWidth
Width in metres of the cavity origin and expansion-end rings.
## Methods


### OnStart(PartModule.StartState)
Initializes the optional cavity renderer.

### Update
Updates the live cavity or editor preview.

### ToggleCavityVisualizationAction(KSPActionParam)
Toggles the cavity visualization through an action group.

### OnDestroy
Destroys runtime-created Unity objects.

