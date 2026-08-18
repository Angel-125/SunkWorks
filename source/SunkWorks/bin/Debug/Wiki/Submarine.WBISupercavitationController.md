            
Calculates all supercavity coverage for one loaded vessel once per physics tick.
        
## Methods


### TryGetController(Vessel,SunkWorks.Submarine.WBISupercavitationController@)
Gets the registered supercavitation controller for a loaded vessel without repeatedly searching its VesselModules.

### GetActivation
Limits this controller to loaded vessels in the flight scene.

### ShouldBeActive
Returns whether this vessel is currently available for physics.

### GetWaterDragMultiplier(Part)
Returns the stock-water-drag multiplier for a vessel part.

### GetSupercavityCoverage(Part)
Returns the current normalized supercavity coverage of a vessel part. Zero is returned when the part is not covered or the vessel has no active supercavitator.

