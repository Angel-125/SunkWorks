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
    public class WBIEVAOverrides : PartModule
    {
        /// <summary>
        /// The buoyancy override
        /// </summary>
        [KSPField]
        public float buoyancyOverride = 1.25f;

        /// <summary>
        /// These inventory parts contain eva overrides that are specified by EVA_OVERRIDES nodes.
        /// </summary>
        [KSPField]
        public string evaOverrideParts = string.Empty;

        KerbalEVA kerbalEVA;
        bool setInitialValues = false;
        double originalMaxPressure;
        float originalSwimSpeed;
        float originalBuoyancy;
        double maxPressureOverride = 0;
        float maxBuoyancy = 0;
        float swimSpeedMultiplier = 0;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            kerbalEVA = part.FindModuleImplementing<KerbalEVA>();
            if (kerbalEVA == null)
                return;

            // Get original values
            originalSwimSpeed = kerbalEVA.swimSpeed;
            originalBuoyancy = part.buoyancy;
            originalMaxPressure = part.maxPressure;

            // Load EVA overrides for carried cargo parts
            if (kerbalEVA.ModuleInventoryPartReference != null && kerbalEVA.ModuleInventoryPartReference.storedParts.Count > 0)
            {
                ModuleInventoryPart inventory = kerbalEVA.ModuleInventoryPartReference;
                int count = inventory.storedParts.Count;

                for (int index = 0; index < count; index++)
                    updatePartOverrides(inventory.storedParts[index].partName);
            }

            // Set initial values if needed.
            if (setInitialValues)
            {
                if (swimSpeedMultiplier > 0)
                    kerbalEVA.swimSpeed = originalSwimSpeed * swimSpeedMultiplier;
                if (buoyancyOverride > 0)
                    part.buoyancy = buoyancyOverride;
                if (maxPressureOverride > 0)
                    part.maxPressure = maxPressureOverride;
            }
        }

        /// <summary>
        /// Overrides OnInactive. Called when an inventory item is unequipped and the module is disabled.
        /// </summary>
        public override void OnInactive()
        {
            base.OnInactive();
            setInitialValues = false;

            if (kerbalEVA == null)
                return;
            kerbalEVA.swimSpeed = originalSwimSpeed;
            part.buoyancy = originalBuoyancy;
            part.maxPressure = originalMaxPressure;
        }

        /// <summary>
        /// Overrides OnActive. Called when an inventory item is equipped and the module is enabled.
        /// </summary>
        public override void OnActive()
        {
            base.OnActive();
            part.buoyancy = buoyancyOverride;
            setInitialValues = true;
        }

        void updatePartOverrides(string partName)
        {
            // Get the part config
            AvailablePart availablePart = PartLoader.getPartInfoByName(partName);
            if (availablePart == null)
                return;
            ConfigNode node = availablePart.partConfig;
            if (node == null)
                return;

            // Get the EVA_OVERRIDES node
            if (!node.HasNode("EVA_OVERRIDES"))
                return;
            node = node.GetNode("EVA_OVERRIDES");

            // Get the overrides
            double pressureOverride = 0;
            float swimSpeedOverride = 0;
            float buoyancyOverride = 0;
            if (node.HasValue("buoyancy"))
                float.TryParse(node.GetValue("buoyancy"), out buoyancyOverride);
            if (node.HasValue("swimSpeedMultiplier"))
                float.TryParse(node.GetValue("swimSpeedMultiplier"), out swimSpeedOverride);
            if (node.HasValue("maxPressure"))
                double.TryParse(node.GetValue("maxPressure"), out pressureOverride);

            // Set the overrides
            if (buoyancyOverride > maxBuoyancy)
                maxBuoyancy = buoyancyOverride;

            if (swimSpeedOverride > swimSpeedMultiplier)
                swimSpeedMultiplier = swimSpeedOverride;

            if (pressureOverride > maxPressureOverride)
                maxPressureOverride = pressureOverride;
        }
    }
}
