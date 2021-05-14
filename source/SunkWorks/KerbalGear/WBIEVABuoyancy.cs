using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunkWorks.KerbalGear
{
    /// <summary>
    /// A Utility class to alter a kerbal's buoyancy.
    /// </summary>
    public class WBIEVABuoyancy: PartModule
    {
        /// <summary>
        /// The buoyancy override
        /// </summary>
        [KSPField]
        public float buoyancyOverride = 1.25f;

        [KSPField(isPersistant = true)]
        float originalBuoyancy = 1f;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            part.buoyancy = buoyancyOverride;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            originalBuoyancy = part.buoyancy;
            part.buoyancy = buoyancyOverride;
        }

        /// <summary>
        /// Overrides OnInactive. Called when an inventory item is unequipped and the module is disabled.
        /// </summary>
        public override void OnInactive()
        {
            base.OnInactive();
            part.buoyancy = originalBuoyancy;
        }

        /// <summary>
        /// Overrides OnActive. Called when an inventory item is equipped and the module is enabled.
        /// </summary>
        public override void OnActive()
        {
            base.OnActive();
            part.buoyancy = buoyancyOverride;
        }
    }
}
