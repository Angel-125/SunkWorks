            
Generates a tapered supercavity behind a submerged part. The model transform's local +Z axis is the cavitator's forward/nose direction.
            
            
> #### Example
```

            MODULE
            {
                name = WBISupercavitator
                cavityTransformName = cavitatorTransform
                minimumCavitySpeed = 20
                fullCavitySpeed = 40
                cavitatorRadius = 0.25
                maximumCavityRadius = 2.5
                cavityLengthRadiusMultiplier = 10
                cavityStraightRadiusMultiplier = 2
                residualWaterDrag = 0.05
            }
            
```

            
        
## Fields

### cavityEnabled
Enables cavity generation. Resource requirements can be added later without changing the vessel-level geometry or drag patch.
### cavityTransformName
Model transform at the cavitator. Its local +Z axis points toward the nose. The part transform is used when this is blank or cannot be found.
### minimumCavitySpeed
Speed in m/s at which cavity formation begins.
### fullCavitySpeed
Speed in m/s at which the cavity reaches full strength and size.
### cavityLengthRadiusMultiplier
Full cavity length measured in maximum cavity radii.
### cavitatorRadius
Cavity radius at the cavitator in metres.
### cavityTipRadius
Radius of the cavity at its exact origin. Zero places the rounded nose tip directly at the cavitator transform.
### maximumCavityRadius
Maximum full-strength cavity radius in metres.
### cavityExpansionFraction
Fraction of cavity length used to expand to maximum radius.
### cavityStraightRadiusMultiplier
Length of the constant-radius cavity body, measured in maximum cavity radii.
### velocityAlignment
How strongly the cavity axis follows water-relative velocity instead of the cavitator transform. Zero follows the transform; one follows velocity.
### fullStrengthAngle
Angle of attack in degrees below which alignment has full strength.
### collapseAngle
Angle of attack in degrees at which the cavity collapses.
### fullStrengthSubmergedPortion
Submerged fraction needed for full cavity strength.
### residualWaterDrag
Fraction of stock water drag retained by a completely cavity-covered part. The cavitator itself is never protected by its own cavity.
### formationGasRate
Fraction of each INPUT_RESOURCE rate consumed while the cavity is growing.
### sustainGasRate
Fraction of each INPUT_RESOURCE rate consumed while the cavity is stable.
### depthGasMultiplier
Fractional sustain-rate increase for each atmosphere of ambient pressure.
### maneuverGasMultiplier
Maximum sustain-rate multiplier while pitch, yaw, or roll input is applied.
### debugMode
Enables rate-limited cavity diagnostics in KSP.log.
### verboseDebug
Logs the final drag multiplier for every covered part.
### debugLogInterval
Minimum seconds between diagnostic snapshots.
### cavityStatus
Current cavity state shown in the part action window.
### 
Shows or hides the cavity visualization.
### 
Shows or hides the animated foam layer in flight.
### 
Opacity of the translucent cavity shell.
### 
Part EFFECTS group driven by the supercavitator's normalized cavity scale.
### 
RGB color encoded as a KSP ConfigNode color string.
### 
Number of subdivisions along the cavity.
### 
Number of subdivisions around the cavity.
### 
Width in metres of the cavity origin and expansion-end rings.
### 
GameDatabase URL of the transparent, tileable foam texture.
### 
Length in metres represented by one repeat of the foam texture.
### 
Maximum downstream speed of the animated foam in metres per second.
### 
Minimum downstream speed of the animated foam in metres per second.
### 
Multiple of fullCavitySpeed at which the foam reaches foamFlowSpeed.
### 
RGB tint applied to the animated foam texture.
### 
Opacity multiplier applied to the animated foam texture.
## Properties

### CavityScale
Current normalized cavity size from zero (no cavity) to one (full cavity).
## Methods


### OnLoad(ConfigNode)
Loads INPUT_RESOURCE definitions from the module configuration.

### ToggleCavityAction(KSPActionParam)
Toggles the cavitator through an action group.

### OnStart(PartModule.StartState)
Initializes and clamps the configurable cavity parameters.

### GetInfo
Gets the editor description.

### CanMaintainCavity(System.Single)
Consumes the resources needed to grow or sustain the requested cavity.

### 
Initializes the optional cavity renderer.

### 
Updates the live cavity or editor preview.

### 
Toggles the cavity visualization through an action group.

### 
Destroys runtime-created Unity objects.

