using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SunkWorks.Submarine.PartModules
{
    /// <summary>
    /// A small helper class to update a part's resource when a part variant is applied.
    /// </summary>
    public class SWResourceVariants: PartModule
    {
        /// <summary>
        /// The name of the resource to update.
        /// </summary>
        [KSPField]
        public string resourceName = string.Empty;

        /// <summary>
        /// A flag to indicate whether or not to only update the max amount.
        /// </summary>
        [KSPField]
        public bool updateMaxOnly;

        public void OnDestroy()
        {
            GameEvents.onVariantApplied.Remove(onVariantApplied);
        }

        public override void OnAwake()
        {
            GameEvents.onVariantApplied.Add(onVariantApplied);
        }

        private void onVariantApplied(Part variantPart, PartVariant variant)
        {
            if (variantPart != part)
                return;
            if (string.IsNullOrEmpty(resourceName))
                return;
            if (!part.Resources.Contains(resourceName))
                return;
            string amountString = variant.GetExtraInfoValue(resourceName);
            if (string.IsNullOrEmpty(amountString))
                return;

            double amount = 0;
            if (double.TryParse(amountString, out amount))
            {
                part.Resources[resourceName].maxAmount = amount;
                if (!updateMaxOnly)
                    part.Resources[resourceName].amount = amount;
                MonoUtilities.RefreshContextWindows(part);
                GameEvents.onPartResourceListChange.Fire(part);
            }
        }
    }
}
