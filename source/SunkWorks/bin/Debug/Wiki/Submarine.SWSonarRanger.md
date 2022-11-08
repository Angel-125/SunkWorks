            
When underwater it's hard to see the terrain ahead and the seabed below. This part module helps avoid collisions with the terrain and seabed.
            
            
> #### Example
```

            MODULE
            {
                name = SWSonarRanger
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


