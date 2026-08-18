            
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

