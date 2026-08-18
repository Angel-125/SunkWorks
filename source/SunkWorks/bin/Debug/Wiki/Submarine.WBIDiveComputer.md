            
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
Returns whether the vessel's master dive computer has automatic dive control enabled. Vessels without a dive computer retain normal standalone ballast-tank behavior. Individual WBIBallastTank player commands may still operate while this returns false.

