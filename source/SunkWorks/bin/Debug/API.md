# SunkWorks


# KerbalGear.EVARagdollBuoyancyPatchLoader
            
Applies SunkWorks ballast to the separate buoyancy force used by stock EVA ragdolls. Stock KerbalEVA does not include Part.buoyancy in that calculation.
        
## Methods


### Awake
Installs the EVA ragdoll buoyancy patch once Harmony and SunkWorks have loaded.

# KerbalGear.WBIModuleEVADiveComputer
            
Controls the kerbal's buoyancy and swim speed, with the ability to increase diving depth when wearing the proper suit. Hard mode includes limited air supply. This module must be included in a KERBAL_EVA_MODULES config node, NOT in a kerbal config.
            
            
> #### Example
```

            KERBAL_EVA_MODULES
            {
                MODULE
                {
                    name = WBIModuleEVADiveComputer
                    maxPositiveBuoyancy = 1.1
                    buoyancyControlRate = 20
                    suitMaxPressures = wbiOBealeWetsuitM,3000;wbiOBealeWetsuitF,3000;wbiAtmoDivingSuitM,7000;wbiAtmoDivingSuitF,7000
                    holdBreathDuration = 360
                    drowningDuration = 10
                    airSupplyDuration = 3600
                    airRechargeRate = 600
                }
            }
            
```

            
        
## Fields

### buoyancyControlStateDisplay
Displays the buoyancy control state.
### maxPositiveBuoyancy
Max positive buoyancy.
### buoyancyControlRate
How fast to control buoyancy, in percentage per second.
### swimSpeedMultiplier
How much to multiply the swim speed by when this module is enabled.
### suitMaxPressures
In kPA, the maximum pressure that the kerbal can take if he/she is wearing a designated suit. Format: 'name of the suit','max pressure';'name of another suit','max pressure of the other suit' NOTE: If a carried cargo part has an EVA_OVERRIDES node, then the values in that node will override the suit pressures. The O'Beale suit enables diving to 300m on Kerbin, which is pretty close to the deepest dive record set by Ahmed Gabr in 2014. The DeepSea suit enables kerbals to dive to 700m on Kerbin, which is akin to an Atmospheric Diving Suit that keeps its occupant at a pressure of 1atm.
### holdBreathDuration
(Hard Mode) In seconds, how long a kerbal can hold is/her breath if the kerbal isn't wearing a helmet. If the kerbal runs out of breath then he/she will start drowning.
### drowningDuration
(Hard Mode) In seconds, how long a kerbal has to reach the surface before dying of drowing.
### airSupplyDuration
(Hard Mode) In seconds, how long the air supply lasts. This duration will be cut in half for every 10m of depth unless wearing an atmospheric diving suit.
### airRechargeRate
(Hard Mode) How many seconds of air supply to recarge per second of being on the surface.
### currentBuoyancy
Current buoyancy level.
### maintainDepth
Flag indicating if we should maintain depth.
## Properties

### IsDiveComputerActive
Indicates whether this dive computer currently owns the EVA buoyancy overrides.
### RagdollBuoyancyScale
Scales stock EVA ragdoll buoyancy to match the ballast selected by this dive computer. A scale of one preserves stock behavior; zero removes ragdoll buoyancy.
## Methods


### Sink
Floods ballast, sinking the kerbal.

### Swim
Vents ballast, floating the kerbal.

### SetNeutralBuoyancy
Neutralizes buoyancy.

### FixedUpdate
Controls buoyancy over a fixed unit of time.

### OnStart(PartModule.StartState)
Overrides OnStart
> #### Parameters
> **state:** The StartState.


### OnActive
Overrides OnActive. Called when an inventory item is equipped and the module is enabled.

### OnInactive
Overrides OnInactive. Called when an inventory item is unequipped and the module is disabled.

### OnKerbalGearInventoryChanged(ModuleInventoryPart)
Recalculates gear-specific EVA overrides without cycling the active dive computer.
> #### Parameters
> **changedInventory:** The EVA inventory whose contents changed.


### refreshInventoryOverrides(ModuleInventoryPart)
Rebuilds the maximum buoyancy, swim-speed, and pressure overrides from current inventory contents.
> #### Parameters
> **inventory:** The EVA inventory containing KerbalGear parts.


### applyActiveOverrides
Applies the currently calculated inventory overrides while preserving the diver's ballast state.

### updatePartOverrides(System.String)
Accumulates the strongest EVA overrides supplied by one carried cargo part.
> #### Parameters
> **partName:** The internal part name whose EVA_OVERRIDES node is inspected.


### updateMaxPressure
Applies the cargo override, configured suit limit, or original EVA pressure limit in priority order.

### updateUI
Updates the Part Action Window.

# Submarine.WBIAquaticEngine
            
This class is an engine that only runs underwater. It needs no resource intake; if underwater then it'll auto-replenish the part's resource reserves.
        
## Fields

### isReverseThrust
Flag to indicate whether or not the engine is in reverse-thrust mode.
### supercavitationCheckInterval
Seconds between supercavity coverage checks.
### supercavitationCoverageThreshold
Coverage fraction at which this engine loses access to water.
### isUnderwater
Flag to indicate whether or not the engine is underwater
### waterResourceName
Name of the water resource to fill if the part is underwater and it has the resource in question.
## Methods


### ModifyFlow
Removes engine flow at the source unless this pumpjet is submerged in liquid and outside a supercavity. ModuleEngines checks this value before requesting propellants, so the environmental cutoff still works when Infinite Propellant bypasses resource requests.

# Submarine.WBIAquaticRCS
            
An aquatic RCS part module derived from ModuleRCSFX that supports animated props.
            
            
> #### Example
```

            MODULE
            {
                name = WBIAquaticRCS
                debugMode = false
                intakeTransformName = intakeTransform
                propellerTransformName = Screw
                propellerRPM = 30
                ...
                // Standard ModuleRCSFX here...
            }
            
```

            
        
## Fields

### debugMode
Flag to enable debug mode.
### intakeTransformName
Name of the part's intake transform.
### propellerTransformName
Name of the part's propeller (if any).
### propellerRPM
Rotations Per Minute for the propeller.
### supercavitationCheckInterval
Seconds between supercavity coverage checks.
### supercavitationCoverageThreshold
Coverage fraction at which this RCS unit loses access to water.
## Methods


### OnUpdate
Removes RCS power before its next physics update when the part is inside a supercavity. The actual vessel coverage query remains rate limited.

# Submarine.BallastTankTypes
            
Type of ballast tank. This is used for auto-triming the boat.
        
## Fields

### Ballast
Generic ballast tank. Does not trim.
### ForwardTrim
Forward trim tank
### ForwardPort
Forward-port trim
### ForwardStarboard
Forward-starboard trim
### PortTrim
Port trim tank
### StarboardTrim
Starboard trim tank
### AftTrim
Aft trim tank
### AftPort
Aft-port trim
### AftStarboard
Aft-starboard trim.

# Submarine.BallastVentStates
            
Vent states of the ballast tank
        
## Fields

### Closed
Tank is closed
### FloodingBallast
Tank is flooding ballast
### VentingBallast
Tank is venting ballast

# Submarine.WBIBallastTank
            
This part module enables a part to become a ballast tank. The tank controls the part's buoyancy. The more ballast resource the part has, the less buoyancy it has, and vice-versa. A ballast tank can be configured for general ballast use or as a trim tank that helps keep the vessel upright.
            
            
> #### Example
```

            MODULE
            {
                name = WBIBallastTank
                updateSymmetryTanks = false
                intakeTransformName = intakeTransform
                ballastResourceName = IntakeLqd
                fullFillRate = 20.0
                fullVentRate = 10.0
            }
            
```

            
        
## Fields

### debugMode
Debug flag
### intakeTransformName
Name of the part's intake transform.
### ballastResourceName
Ballast resource
### addBallastEffect
Name of the venting effect to play when the tank is taking on ballast.
### ventBallastEffect
Name of the venting effect to play when the tank is venting ballast.
### fullFillRate
How many seconds to fill the ballast tank
### fullVentRate
How many seconds to vent the ballast tank
### tankType
Type of ballast tank
### tankTypeString
Current display state of the ballast tank
### ventState
Current state of the ballast tank
### ventStateString
Current display state of the ballast tank
### updateSymmetryTanks
Flag to indicate whether or not to update symmetry tanks.
### fluidTransferPercentage
Percentage of the overall ballast fluid transfer rate
### commandedFluidTransferPercentage
Transfer rate currently requested by a dive computer. This is kept separate from fluidTransferPercentage so automatic control cannot overwrite the player's saved manual transfer rate.
### useCommandedFluidTransferRate
Indicates whether the current vent operation is using a dive-computer command rate instead of the player's manual transfer rate.
### reconfigureSkill
The skill required to reconfigure the ballast tank
### reconfigureRank
Skill rank needed to reconfigure the ballast tank.
### tankTypeIndex
Index for the tank types.
### buoyancyStateInitialized
Indicates whether tankBouyancy contains a flight-saved value. Editor saves leave this false so a newly launched vessel derives buoyancy from its ballast amount.
### isConverted
Flag to indicate whether or not the fuel tank has been converted to ballast tank.
### updatePAW
Flag to indicate that we need to update the PAW
### hostPart
The part that is hosting the WBIBallastTank.
### ballastResource
The PartResource containing the ballast.
### onBallastTankUpdated
Signifies that the ballast has been updated
## Methods


### ConvertToBallastTank
Converts the host part to a ballast tank.

### RestoreResourceCapacity
Restores the host part's resource storage capacity.

### FloodBallast
Floods the ballast tank

### VentBallast
Vents ballast tank

### CloseVents
Close ballast vents

### EmergencySurface
Emergency surface

### FloodBallastAction(KSPActionParam)
Action to flood ballast tank
> #### Parameters
> **param:** 


### VentBallastAction(KSPActionParam)
Action to vent ballast tank
> #### Parameters
> **param:** 


### CloseVentsAction(KSPActionParam)
Close ballast vents action
> #### Parameters
> **param:** 


### EmergencySurfaceAction(KSPActionParam)
Emergency surface action
> #### Parameters
> **param:** 


### DumpBallast(System.Boolean)
Dumps ballast
> #### Parameters
> **updateSymmetryParts:** A bool indicating whether or not to update symmetry parts


### SetVentState(SunkWorks.Submarine.BallastVentStates,System.Single)
Sets the vent state
> #### Parameters
> **state:** The new BallastVentStates

> **fluidTransferRate:** A float containing the new fluid transfer percentage


### GetActiveFluidTransferPercentage
Returns the transfer percentage currently driving the ballast tank.

### CanTrimForward
Indicates that the tank can be used for forward trim.
> #### Return value
> True if it can be used for trim, false if not.

### CanTrimAft
Indicates that the tank can be used for aft trim.
> #### Return value
> True if it can be used for trim, false if not.

### CanTrimPort
Indicates that the tank can be used for portside trim.
> #### Return value
> True if it can be used for trim, false if not.

### CanTrimStarboard
Indicates that the tank can be used for starboard trim.
> #### Return value
> True if it can be used for trim, false if not.

### OnDestroy
Handles the OnDestroy event

### OnAwake
Handles OnAwake event

### OnLoad(ConfigNode)
Restores the selector index from the persisted tank type. Older craft files only contain tankType, so tankType remains the authoritative saved value.

### OnSave(ConfigNode)
Keeps the persistent selector index synchronized with the selected tank type.

### GetModuleDisplayName
Gets the module display name.
> #### Return value
> A string containing the display name.

### GetInfo
Gets the module description.
> #### Return value
> A string containing the module description.

### OnStart(PartModule.StartState)
Handles the OnStart event.
> #### Parameters
> **state:** A StartState containing the starting state.


### FixedUpdate
Handles FixedUpdate

### Update
Handles the Update event.

# Submarine.WBIDiveComputer
            
A handy dive computer to help boats dive, surface, and maintain trim.
            
            
> #### Example
```

            MODULE
            {
                name = WBIDiveComputer
                debugMode = true
                maxPressureOverride = 6000
             }
            
```

            
        
## Fields

### onFloodBallast
Indicates that the user has requested to flood the boat's ballast.
### onVentBallast
Indicates that the user has requested to vent the boat's ballast.
### onCloseVents
Indicates that the user has requested to close the boat's ballast vents.
### onEmergencySurface
Indicates that the user has requested an emergency surface.
### onMaintainDepthUpdated
Indicates that the user has requested a change to maintain depth.
### onMaintainNeutralBuoyancyUpdated
Indicates that the user has requested a change to maintain neutral buoyancy.
### onAutoTrimUpdated
Indicates that the user has requested a change to auto-trim.
### onDiveControlUpdated
Indicates that the user has requested a change to dive control.
### onTriggerAndFluidRatesUpdated
Event to synchronize triggers and fluid rates
### autoTrimEnabled
Indicates whether or not to automatically keep the boat level.
### trimStatusString
Indicates whether auto-trim has any configured trim tanks to control.
### divingControlEnabled
Indicates whether or not to enable dive control.
### maintainDepth
Indicates whether or not to maintain current depth
### maintainNeutralBuoyancy
Indicates whether the main ballast system should automatically balance vessel buoyancy with vessel mass without attempting to stop vertical motion.
### neutralBuoyancyStatusString
Current neutral-buoyancy controller status.
### neutralBuoyancyErrorString
Current buoyancy minus vessel mass, expressed as tonnes-equivalent.
### targetDepth
Depth below sea level that the dive computer will maintain, in meters. A negative value indicates that no target has been captured yet.
### diveStateString
Display string for current state of the dive computer
### hullIntegrity
Display string for current state of the dive computer
### pitchAngle
Current pitch angle of the boat.
### rollAngle
Current roll angle of the boat.
### yawAngle
Current vessel heading, used as the vessel's yaw diagnostic.
### rollAngleTrigger
Roll angle that will trigger auto-trim. 0 is level, so anything that is +- this value will trigger auto-trim.
### pitchAngleTrigger
Pitch angle that will trigger auto-trim. 0 is level, so anything that is +- this value will trigger auto-trim.
### verticalSpeedTrigger
If maintainDepth is enabled, then when the vertical speed reaches +- the speed trigger, the boat will attempt to maintain depth.
### rollFluidRate
Roll-trim's fluid transfer rate (percent)
### pitchFluidRate
Pitch-trim's fluid transfer rate (percent)
### ballastFluidRate
Ballast's fluid transfer rate (percent)
### ventState
Current vent state of the boat's ballast system.
### maxPressureOverride
Override maximum pressure in kPA. Parts have a default of 4000kPA, which gives them a collapse death of 400m on Kerbin. This override gives you a way to alter that collapse depth without modifying individual parts. If multiple dive computers are found on the boat, then the highest max pressure will be used. If there is a mismatch between the part's maxPressure and the dive computer's maxPressureOverride, then both will be set to the highest value.
### minControlledBuoyancy
Min controlled buoyancy for buoyancy controlled parts.
### depthGain
Converts depth error into a desired vertical speed.
### verticalSpeedGain
Converts vertical-speed error into ballast transfer percentage.
### maxDepthHoldSpeed
Maximum ascent or descent speed requested by depth hold.
### depthDeadband
Depth error, in meters, inside which only vertical-speed damping is used.
### verticalSpeedFilterTime
Time constant used to smooth the vertical-speed signal.
### neutralBuoyancyUpdateInterval
Number of seconds between neutral-buoyancy controller updates.
### neutralBuoyancyDeadband
Fraction of vessel mass inside which neutral buoyancy closes the main ballast vents.
### neutralBuoyancyGain
Converts fractional buoyancy error into main-ballast transfer percentage.
### neutralMinimumSubmergedPortion
Minimum submerged fraction required for every buoyant part before neutral control runs.
### trimRateDamping
Number of seconds of angular motion used to damp trim commands.
### trimRateFilterTime
Time constant used to smooth measured pitch and roll rates.
### debugLogInterval
Number of real-time seconds between periodic diagnostic snapshots. Set to zero to log every physics update.
### maneuverState
Debug maneuver states
### vesselIsManeuvering
Flag to indicate that the vessel is maneuvering
### prevBuoyancy
Last vessel-wide buoyancy applied to parts that are not ballast tanks. Persisting this value lets a disabled dive computer restore the saved buoyancy without running the ballast controller during vessel load.
## Properties

### isActiveDiveComputer
Determines whether or not the computer is the active computer on the vessel that is controlling the dive.
## Methods


### FloodBallast
Floods the ballast tank

### VentBallast
Vents ballast tank

### CloseVents
Close ballast vents

### EmergencySurface
Activates emergency surface, telling all ballast tanks to immediately dump their ballast. This affects parts marked as ballast or trim tanks.

### FloodBallastAction(KSPActionParam)
Action to flood ballast tank
> #### Parameters
> **param:** 


### VentBallastAction(KSPActionParam)
Action to vent ballast tank
> #### Parameters
> **param:** 


### CloseVentsAction(KSPActionParam)
Close ballast vents action
> #### Parameters
> **param:** 


### EmergencySurfaceAction(KSPActionParam)
Emergency surface action
> #### Parameters
> **param:** 


### ToggleMaintainDepthAction(KSPActionParam)
Toggle maintain depth action
> #### Parameters
> **param:** 


### ToggleMaintainNeutralBuoyancyAction(KSPActionParam)
Toggle maintain neutral buoyancy action.

### ToggleAutoTrimAction(KSPActionParam)
Toggle auto trim action
> #### Parameters
> **param:** 


### IsDiveControlEnabled(Vessel)
Returns whether the vessel's master dive computer permits ballast updates. Vessels without a dive computer retain normal standalone ballast-tank behavior.

# Submarine.WBINeutralBuoyancy
            
Provides low-frequency, automatic neutral buoyancy for underwater bases. The module captures the parts that belong to the base when enabled and also adopts parts subsequently attached through EVA construction. Docked vessels are deliberately not added to the controlled set.
            
            
> #### Example
```

            MODULE
            {
                name = WBINeutralBuoyancy
                updateInterval = 1
                minimumSubmergedPortion = 0.95
                minimumBuoyancy = 0.01
                maximumBuoyancy = 50
            }
            
```

            
        
## Fields

### neutralBuoyancyEnabled
Enables automatic neutral buoyancy. If a ModuleGroundPart is installed on this part, neutral buoyancy waits until that ground part is deployed.
### updateInterval
Number of seconds between mass and buoyancy recalculations.
### minimumSubmergedPortion
Minimum submerged fraction required before a part is adjusted.
### minimumBuoyancy
Minimum Part.buoyancy multiplier the controller may apply.
### maximumBuoyancy
Maximum Part.buoyancy multiplier the controller may apply.
### statusDisplay
Current controller status displayed in the PAW.
### controlledPartCount
Number of base parts owned by this controller.
### buoyancyErrorDisplay
Difference between controlled weight and buoyancy, expressed as tonnes-equivalent.
### groundAnchorStatusDisplay
Deployment state of a ModuleGroundPart installed on this same part.
## Methods


### ToggleNeutralBuoyancyAction(KSPActionParam)
Toggles neutral buoyancy through an action group.

### RecalculateNeutralBuoyancy
Recalculates controlled-part buoyancy immediately.

### GetModuleDisplayName
Gets the module display name.

### GetInfo
Gets the editor description.

### OnLoad(ConfigNode)
Loads the persistent controlled-part ownership and original buoyancy values.

### OnSave(ConfigNode)
Saves controlled-part ownership so docked visitors are not adopted after reload.

### OnStart(PartModule.StartState)
Initializes the controller and its vessel-change notifications.

### OnDestroy
Removes event handlers.

### Update
Detects toggle and ground-deployment transitions without performing the expensive buoyancy calculation every frame.

### FixedUpdate
Performs the low-frequency automatic mass and buoyancy update.

# Submarine.WBISonarRanger
            
When underwater it's hard to see the terrain ahead and the seabed below. This part module helps avoid collisions with the terrain and seabed.
            
            
> #### Example
```

            MODULE
            {
                name = WBISonarRanger
                seabedPingRange = 50
                shoalPingRange = 150
                
                // Use standard EFFECT config node for these effects.
                pingEffectSeabedName = pingSeabed
                pingEffectShoalName = pingShoal
            }
            
```

            
        
## Fields

### depthBelowKeel
How far it is to the bottom of the sea. Perhaps one should voyage there...
### rangeToTerrainDisplay
Range to terrain, in meters.
### seabedPingActive
Toggle switch for the seabed proximity alarm
### seabedPingRange
Minimum range at which to play the seabed ping, if enabled.
### shoalPingActive
Toggle switch for the seabed proximity alarm
### shoalPingRange
Minimum range at which to play the seabed ping, if enabled.
### pingEffectSeabedName
Name of the effect to play when in proximity to the seabed.
### pingEffectShoalName
Name of the effect to play when in proximity to a shoal.
## Methods


### ToggleSeabedPingAction(KSPActionParam)
Action to toggle the seabed proximity alarm on/off
> #### Parameters
> **param:** A KSPActionParam


### ToggleShoalPingAction(KSPActionParam)
Action to toggle the seabed proximity alarm on/off
> #### Parameters
> **param:** A KSPActionParam


# Submarine.Supercavity
            
Describes a supercavity generated by a for the current physics tick.
        

# Submarine.WBISupercavitator
            
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

# Submarine.WBISupercavitationController
            
Calculates all supercavity coverage for one loaded vessel once per physics tick.
        
## Methods


### TryGetController(Vessel,SunkWorks.Submarine.WBISupercavitationController@)
Gets the registered supercavitation controller for a loaded vessel without repeatedly searching its VesselModules.

### GetActivation
Limits this controller to loaded vessels in the flight scene.

### ShouldBeActive
Returns whether this vessel is currently available for physics.

### GetWaterDragMultiplier(Part)
Returns the stock-water-drag multiplier for a vessel part.

### GetSupercavityCoverage(Part)
Returns the current normalized supercavity coverage of a vessel part. Zero is returned when the part is not covered or the vessel has no active supercavitator.

# Submarine.WBISupercavitationDragPatch
            
Reduces the stock PartBuoyancy translational damping after it has calculated water drag. Cavity geometry is calculated once per vessel per physics tick.
        

# Submarine.WBISupercavitationEnginePressure
            
Temporarily substitutes atmospheric-only pressure while a stock engine inside a supercavity calculates thrust. Other part systems continue to see the real hydrostatic pressure.
        

# Submarine.WBISupercavitatorFX
            
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

# Submarine.WBIPressureOverride
            
A helpful vessel module to handle overriding the maximum hull pressure of a vessel's parts.
        
## Fields

### maxPressureOverride
Overrides how much pressure the vessel can take.
### diveComputers
List of dive computers
### partCount
Current vessel part count

# SunkWorksSettings
            
Difficulty settings for SunkWorks gameplay features.
        
## Fields

### supercavitationFlameout
When enabled, aquatic engines and aquatic RCS cannot operate while their parts are covered by a supercavity.
## Properties

### SupercavitationFlameoutEnabled
Indicates whether supercavitation should disable aquatic propulsion. Defaults to enabled when no game is loaded.