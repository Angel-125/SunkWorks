            
Draws a high-visibility wireframe over the active body's live terrain meshes. Stock PQS meshes are used normally; when Parallax is loaded, the renderer uses its active subdivided child mesh so the wireframe follows the enhanced terrain.
        
## Methods


### Start
Creates the runtime material and subscribes to camera rendering.

### Update
Finds the enabled Sonar View belonging to the active vessel.

### OnDestroy
Unsubscribes and destroys resources when leaving flight.

### LateUpdate
Attaches the final-camera command buffer after normal scene updates. Its contents are populated at render time, after KSP has finished moving recycled PQS tiles for the current floating-origin frame.

