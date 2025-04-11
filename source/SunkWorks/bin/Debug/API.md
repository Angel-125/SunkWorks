# SunkWorks


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

### updateUI
Updates the Part Action Window.

# Submarine.WBIAquaticEngine
            
This class is an engine that only runs underwater. It needs no resource intake; if underwater then it'll auto-replenish the part's resource reserves.
        
## Fields

### isReverseThrust
Flag to indicate whether or not the engine is in reverse-thrust mode.
### isUnderwater
Flag to indicate whether or not the engine is underwater
### waterResourceName
Name of the water resource to fill if the part is underwater and it has the resource in question.

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
### reconfigureSkill
The skill required to reconfigure the ballast tank
### reconfigureRank
Skill rank needed to reconfigure the ballast tank.
### tankTypeIndex
Index for the tank types.
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
### onAutoTrimUpdated
Indicates that the user has requested a change to auto-trim.
### onDiveControlUpdated
Indicates that the user has requested a change to dive control.
### onTriggerAndFluidRatesUpdated
Event to synchronize triggers and fluid rates
### debugMode
Debug flag
### autoTrimEnabled
Indicates whether or not to automatically keep the boat level.
### divingControlEnabled
Indicates whether or not to enable dive control.
### maintainDepth
Indicates whether or not to maintain current depth
### diveStateString
Display string for current state of the dive computer
### hullIntegrity
Display string for current state of the dive computer
### pitchAngle
Current pitch angle of the boat.
### rollAngle
Current roll angle of the boat.
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
### maneuverState
Debug maneuver states
### vesselIsManeuvering
Flag to indicate that the vessel is maneuvering
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


### ToggleAutoTrimAction(KSPActionParam)
Toggle auto trim action
> #### Parameters
> **param:** 


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


# Submarine.WBIPressureOverride
            
A helpful vessel module to handle overriding the maximum hull pressure of a vessel's parts.
        
## Fields

### maxPressureOverride
Overrides how much pressure the vessel can take.
### diveComputers
List of dive computers
### partCount
Current vessel part count