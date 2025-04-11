            
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


