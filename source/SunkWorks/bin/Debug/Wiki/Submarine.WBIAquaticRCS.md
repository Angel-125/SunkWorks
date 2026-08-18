            
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

