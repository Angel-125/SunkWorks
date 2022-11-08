            
An aquatic RCS part module derived from ModuleRCSFX that supports animated props.
            
            
> #### Example
```

            MODULE
            {
                name = SWAquaticRCS
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

