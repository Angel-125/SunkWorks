            
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

