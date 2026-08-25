            
Passively reduces vessel-wide water drag according to the hull's length-to-beam ratio. The vessel supercavitation controller applies the calculated multiplier after stock PartBuoyancy has calculated water drag.
        
## Fields

### hullLength
Full hull length in meters when no procedural hull is present.
### hullBeam
Full hull beam in meters when no procedural hull is present.
### minimumSlendernessRatio
Slenderness ratio at which drag reduction begins.
### maximumSlendernessRatio
Slenderness ratio at which drag reduction reaches its maximum.
### maximumDragReduction
Maximum fraction of stock water drag removed.
### debugMode
Enables rate-limited diagnostic logging.
### debugLogInterval
Minimum time between diagnostic messages, in seconds.
### slendernessDisplay
Current flight UI representation of the hull ratio.
### dragReductionDisplay
Current flight UI representation of the active reduction.
## Properties

### SlendernessRatio
The currently resolved length-to-beam ratio.
### DragReduction
The configured reduction before checking water contact.
### IsOperational
Whether this module can participate in the vessel-wide election.
## Methods


### OnStart(PartModule.StartState)
Resolves procedural dimensions and validates configuration.

### OnUpdate
Refreshes the read-only flight displays.

