using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.Localization;

namespace SunkWorks.Utility
{
    /// <summary>
    /// Helper part module to handle part mesh and texture switching. Stock ModulePartVariants doesn't cooperate with multiple ModulePartVariants in the same part, so this class
    /// gets around the issue and adds a few enhancements.
    /// </summary>
    public class SWPartVariants: SWPartModule, IPartCostModifier, IPartMassModifier
    {
        #region Fields
        /// <summary>
        /// Index for the texture variants.
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = true, isPersistant = true)]
        [UI_VariantSelector(affectSymCounterparts = UI_Scene.All, controlEnabled = true, scene = UI_Scene.All)]
        public int variantIndex;

        /// <summary>
        /// Flag to indicate if the symmetry parts should also apply the selected variant. Default is true.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool updateSymmetry = true;

        /// <summary>
        /// Flag to indicate whether the variant can be applied post launch. Default is false.
        /// </summary>
        [KSPField]
        public bool allowFieldUpdate = false;

        [KSPField(isPersistant = true)]
        public bool variantApplied = false;
        #endregion

        #region Housekeeping
        public static EventData<SWPartVariants, string, Dictionary<string, string>> onApplyVariantExtraInfo = new EventData<SWPartVariants, string, Dictionary<string, string>>("onApplyVariantExtraInfo");

        List<SWVariant> variants;
        #endregion

        #region Overrides
        /// <summary>
        /// Handles the OnStart event.
        /// </summary>
        /// <param name="state">A StartState containing the starting state.</param>
        public override void OnStart(StartState state)
        {
            if (variantApplied)
                applyVariant(variantIndex);
        }

        /// <summary>
        /// Handles OnAwake event
        /// </summary>
        public override void OnAwake()
        {
            setupVariants();
            GameEvents.onVariantApplied.Add(onVariantApplied);
            if (HighLogic.LoadedSceneIsEditor)
                GameEvents.onEditorVariantApplied.Add(onEditorVariantApplied);

            onApplyVariantExtraInfo.Add(onApplyVariantExtraInfoHandler);
            Fields["variantIndex"].guiActive = allowFieldUpdate;
        }

        /// <summary>
        /// Handles the OnDestroy event
        /// </summary>
        public void OnDestroy()
        {
            GameEvents.onVariantApplied.Remove(onVariantApplied);
            if (HighLogic.LoadedSceneIsEditor)
                GameEvents.onEditorVariantApplied.Remove(onEditorVariantApplied);
            onApplyVariantExtraInfo.Remove(onApplyVariantExtraInfoHandler);
        }

        /// <summary>
        /// Gets the module display name.
        /// </summary>
        /// <returns>A string containing the display name.</returns>
        public override string GetModuleDisplayName()
        {
            return Localizer.Format("#LOC_SUNKWORKS_textureVariants");
        }

        /// <summary>
        /// Gets the module description.
        /// </summary>
        /// <returns>A string containing the module description.</returns>
        public override string GetInfo()
        {
            return Localizer.Format("#LOC_SUNKWORKS_textureVariantsInfo");
        }
        #endregion

        #region API
        public void applyVariant(int newIndex, bool fireEvents = false)
        {
            if (newIndex < 0 || newIndex > variants.Count - 1)
                return;

            SWVariant variant = variants[variantIndex];

            variant.applyVariant(part);

            if (updateSymmetry)
            {
                int count = part.symmetryCounterparts.Count;
                SWPartVariants partVariant;
                for (int index = 0; index < count; index++)
                {
                    partVariant = part.symmetryCounterparts[index].FindModuleImplementing<SWPartVariants>();
                    partVariant.variantIndex = variantIndex;
                    partVariant.variantApplied = true;
                    variant = partVariant.variants[variantIndex];
                    variant.applyVariant(part.symmetryCounterparts[index]);
                }
            }

            if (fireEvents)
            {
                UI_VariantSelector variantSelector = getVariantSelector();

                GameEvents.onVariantApplied.Fire(part, variantSelector.variants[variantIndex]);
                if (HighLogic.LoadedSceneIsEditor)
                    GameEvents.onEditorVariantApplied.Fire(part, variantSelector.variants[variantIndex]);

                if (variant.extraInfo.Count > 0)
                    onApplyVariantExtraInfo.Fire(this, variant.name, variant.extraInfo);
            }

            variantApplied = true;
        }
        #endregion

        #region Helpers
        private void setupVariants()
        {
            ConfigNode node = getPartConfigNode();
            if (node == null)
                return;
            loadVariantConfigs(node);
            if (variants.Count == 0)
                return;

            UI_VariantSelector variantSelector = getVariantSelector();

            variantSelector.onFieldChanged += new Callback<BaseField, object>(this.onVariantChanged);

            // Setup variant list
            variantSelector.variants = new List<PartVariant>();
            int count = variants.Count;
            for (int index = 0; index < count; index++)
            {
                variantSelector.variants.Add(variants[index].getPartVariant());
            }
        }

        private void loadVariantConfigs(ConfigNode node)
        {
            variants = new List<SWVariant>();
            if (!node.HasNode("VARIANT"))
                return;
            ConfigNode[] variantNodes = node.GetNodes("VARIANT");
            SWVariant variant;

            for (int index = 0; index < variantNodes.Length; index++)
            {
                variant = new SWVariant();
                variant.Load(variantNodes[index]);
                if (!string.IsNullOrEmpty(variant.name))
                    variants.Add(variant);
            }
        }

        private UI_VariantSelector getVariantSelector()
        {
            UI_VariantSelector variantSelector = null;
            int count = Fields.Count;
            string fieldNames = string.Empty;
            for (int index = 0; index < count; index++)
                fieldNames += Fields[index].name + ";";

            // Setup variant selector
            if (HighLogic.LoadedSceneIsFlight)
                variantSelector = Fields["variantIndex"].uiControlFlight as UI_VariantSelector;
            else //if (HighLogic.LoadedSceneIsEditor)
                variantSelector = Fields["variantIndex"].uiControlEditor as UI_VariantSelector;

            return variantSelector;
        }

        private void onVariantChanged(BaseField baseField, object obj)
        {
            applyVariant(variantIndex, true);
        }

        private void onVariantApplied(Part variantPart, PartVariant variant)
        {
            if (part == null || variantPart == null)
                return;
            if (variantPart != part)
                return;
            if (string.IsNullOrEmpty(moduleID))
                return;

            // Re-apply the variant.
            string updateVariants = variant.GetExtraInfoValue("updateVariantModuleIDs");
            if (updateVariants.Contains(moduleID) && variantApplied)
            {
                applyVariant(variantIndex, true);
            }

            // Enable/disable the UI
            string enabledVariants = variant.GetExtraInfoValue("enableVariantModuleIDs");
            string disabledVariants = variant.GetExtraInfoValue("disableVariantModuleIDs");
            if (enabledVariants.Contains(moduleID))
            {
                Fields["variantIndex"].guiActive = allowFieldUpdate;
                Fields["variantIndex"].guiActiveEditor = true;
            }

            else if (disabledVariants.Contains(moduleID))
            {
                Fields["variantIndex"].guiActive = false;
                Fields["variantIndex"].guiActiveEditor = false;
            }
        }

        private void onEditorVariantApplied(Part variantPart, PartVariant variant)
        {
            onVariantApplied(variantPart, variant);
        }

        private void onApplyVariantExtraInfoHandler(SWPartVariants orginator, string variantName, Dictionary<string, string> extraInfo)
        {
            if (orginator.part != part)
                return;

            // Re-apply the variant
            if (extraInfo.ContainsKey("updateVariantModuleIDs") && extraInfo["updateVariantModuleIDs"].Contains(moduleID) && variantApplied)
            {
                applyVariant(variantIndex, true);
            }

            // Enable/disable the UI
            if (extraInfo.ContainsKey("enableVariantModuleIDs") && extraInfo["enableVariantModuleIDs"].Contains(moduleID))
            {
                Fields["variantIndex"].guiActive = allowFieldUpdate;
                Fields["variantIndex"].guiActiveEditor = true;
            }
            else if (extraInfo.ContainsKey("disableVariantModuleIDs") && extraInfo["disableVariantModuleIDs"].Contains(moduleID))
            {
                Fields["variantIndex"].guiActive = false;
                Fields["variantIndex"].guiActiveEditor = false;
            }
        }

        #region IPartCostModifier
        /// <summary>
        /// Returns the Module cost modifier. It is added to the part's total cost.
        /// </summary>
        /// <param name="defaultCost">Default cost of the part</param>
        /// <param name="sit">The situation in which the call is being made.</param>
        /// <returns>A float containing the modified cost.</returns>
        public float GetModuleCost(float defaultCost, ModifierStagingSituation sit)
        {
            if (variants == null || variants.Count == 0)
                return 0;
            return variants[variantIndex].cost;
        }

        /// <summary>
        /// Describes when the part modifier changes.
        /// </summary>
        /// <returns>A ModifierChangeWhen indicating when the modifier is applied.</returns>
        public ModifierChangeWhen GetModuleCostChangeWhen()
        {
            return HighLogic.LoadedSceneIsFlight ? ModifierChangeWhen.FIXED : ModifierChangeWhen.CONSTANTLY;
        }
        #endregion

        #region IPartMassModifier
        /// <summary>
        /// Returns the Module cost modifier. It is added to the part's total mass.
        /// </summary>
        /// <param name="defaultMass">Default mass of the part</param>
        /// <param name="sit">The situation in which the call is being made.</param>
        /// <returns>A float containing the modified mass.</returns>
        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit)
        {
            if (variants == null || variants.Count == 0)
                return 0;
            return variants[variantIndex].mass;
        }

        /// <summary>
        /// Describes when the part modifier changes.
        /// </summary>
        /// <returns>A ModifierChangeWhen indicating when the modifier is applied.</returns>
        public ModifierChangeWhen GetModuleMassChangeWhen()
        {
            return HighLogic.LoadedSceneIsFlight ? ModifierChangeWhen.FIXED : ModifierChangeWhen.CONSTANTLY;
        }
        #endregion

        #endregion
    }
}
