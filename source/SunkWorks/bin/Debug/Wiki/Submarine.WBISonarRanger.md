            
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
### sonarViewActive
Enables the high-visibility terrain wireframe for the active vessel. The renderer automatically hides the view above water and in map view.
### sonarViewRange
Sonar View radius around the active vessel, in metres.
### sonarViewColorPicker
Stock PAW color-picker field. The selected value is persisted in so craft and save files remain portable.
### sonarViewColor
Sonar View color encoded as #RRGGBB or #RRGGBBAA.
### sonarViewMaxRange
Maximum range exposed by the Sonar View PAW slider.
### sonarViewOpacity
Wire opacity before the range fade is applied.
### sonarViewFadeStart
Normalized range at which the wireframe begins fading. The default value fades the final twenty percent of the selected range.
### shoalPingRange
Minimum range at which to play the seabed ping, if enabled.
### pingEffectSeabedName
Name of the effect to play when in proximity to the seabed.
### pingEffectShoalName
Name of the effect to play when in proximity to a shoal.
## Properties

### SonarViewColor
Parsed, opacity-adjusted color used by Sonar View.
## Methods


### ToggleSeabedPingAction(KSPActionParam)
Action to toggle the seabed proximity alarm on/off
> #### Parameters
> **param:** A KSPActionParam


### ToggleShoalPingAction(KSPActionParam)
Action to toggle the seabed proximity alarm on/off
> #### Parameters
> **param:** A KSPActionParam


### ToggleSonarViewAction(KSPActionParam)
Toggles Sonar View through an action group.

### PresetColors
Returns useful high-contrast presets for the stock picker.

### GetCurrentColor(System.String)
Supplies the persisted Sonar View color to the stock picker.

### OnColorChanged(UnityEngine.Color,System.String)
Persists changes made with the stock PAW color picker.

