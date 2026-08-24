            
Sizes a ballast tank from a percentage of a procedural hull's generated volume.
        
## Fields

### ballastPercent
Percentage of the generated hull volume available for ballast.
## Methods


### OnAwake
Subscribes to editor variant notifications.

### OnStart(PartModule.StartState)
Initializes the slider and calculates the initial capacity.

### Update
Performs the initial calculation after every part module has had an opportunity to start.

### OnDestroy
Unsubscribes from editor variant notifications.

