            
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

