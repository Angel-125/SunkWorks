            
This class is an engine that only runs underwater. It needs no resource intake; if underwater then it'll auto-replenish the part's resource reserves.
        
## Fields

### isReverseThrust
Flag to indicate whether or not the engine is in reverse-thrust mode.
### supercavitationCheckInterval
Seconds between supercavity coverage checks.
### supercavitationCoverageThreshold
Coverage fraction at which this engine loses access to water.
### isUnderwater
Flag to indicate whether or not the engine is underwater
### waterResourceName
Name of the water resource to fill if the part is underwater and it has the resource in question.
## Methods


### ModifyFlow
Removes engine flow at the source unless this pumpjet is submerged in liquid and outside a supercavity. ModuleEngines checks this value before requesting propellants, so the environmental cutoff still works when Infinite Propellant bypasses resource requests.

