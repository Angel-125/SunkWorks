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

        /// <summary>
        /// Field indicating whether or not we have applied the part variant.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool variantApplied = false;

        /// <summary>
        /// If, during a part variant update event, the meshSet field is set in EXTRA_INFO, then
        /// we'll record what the meshSet's value is and apply the set IF the value is on our list.
        /// If our meshSets is empty (the default), then we'll ignore any meshSet fields passed in with EXTRA_INFO.
        /// </summary>
        [KSPField]
        public string meshSets = string.Empty;

        /// <summary>
        /// The currently selected mesh set.
        /// </summary>
        [KSPField(isPersistant = true)]
        public string currentMeshSet = string.Empty;
        #endregion

        #region Housekeeping
        public static EventData<SWPartVariants, string, Dictionary<string, string>> onApplyVariantExtraInfo = new EventData<SWPartVariants, string, Dictionary<string, string>>("onApplyVariantExtraInfo");

        List<SWVariant> variants;
        bool isInitialized = false;
        #endregion

        #region Overrides
        /// <summary>
        /// Handles the OnStart event.
        /// </summary>
        /// <param name="state">A StartState containing the starting state.</param>
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!string.IsNullOrEmpty(currentMeshSet))
                variantApplied = true;
            if (variantApplied)
                applyVariant(variantIndex);

            if (!updateSymmetry)
            {
                if (Fields["variantIndex"].uiControlEditor != null)
                    Fields["variantIndex"].uiControlEditor.affectSymCounterparts = UI_Scene.None;
                if (Fields["variantIndex"].uiControlFlight != null)
                    Fields["variantIndex"].uiControlFlight.affectSymCounterparts = UI_Scene.None;
            }
            isInitialized = true;
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
            if (variants == null || newIndex < 0 || newIndex > variants.Count - 1)
                return;

            SWVariant variant = variants[variantIndex];

            variant.applyVariant(part, currentMeshSet);

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
                    variant.applyVariant(part.symmetryCounterparts[index], currentMeshSet);
                }
            }

            if (fireEvents)
            {
                /*
                UI_VariantSelector variantSelector = getVariantSelector();

                GameEvents.onVariantApplied.Fire(part, variantSelector.variants[variantIndex]);
                if (HighLogic.LoadedSceneIsEditor)
                    GameEvents.onEditorVariantApplied.Fire(part, variantSelector.variants[variantIndex]);
                */

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
            if (!shouldRespondToAppliedVariant(variantPart, variant))
                return;

            // If the variant has a meshSet that we recognize, then record it for later.
            string meshSet = variant.GetExtraInfoValue("meshSet");
            if (!string.IsNullOrEmpty(meshSet) && !string.IsNullOrEmpty(meshSets) && meshSets.Contains(meshSet))
            {
                currentMeshSet = meshSet;
                variantApplied = true;
            }

            // Re-apply the variant if we're on the updateVariantModuleIDs list.
            string updateVariants = variant.GetExtraInfoValue("updateVariantModuleIDs");
            if (updateVariants.Contains(moduleID) && variantApplied)
            {
                applyVariant(variantIndex, true);
            }

            // Enable/disable the UI if we're on the list.
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

        private bool shouldRespondToAppliedVariant(Part variantPart, PartVariant variant)
        {
            if (part == null || variantPart == null)
                return false;

            /* This could get complicated real quick. Stock doesn't appear to respond to part varant events unless they come from the same part.
            if (variantPart == part.parent || part.children.Contains(variantPart))
            {
                string meshSet = variant.GetExtraInfoValue("meshSet");
                if (!string.IsNullOrEmpty(meshSet) && !string.IsNullOrEmpty(meshSets) && meshSets.Contains(meshSet))
                    return true;
            }
            */
            if (variantPart != part)
                return false;

            if (string.IsNullOrEmpty(moduleID))
                return false;

            // Part variant events can fire before the part module has been started. Let's ignore them until we're initialized.
            if (!isInitialized)
                return false;

            return true;
        }

        private void onEditorVariantApplied(Part variantPart, PartVariant variant)
        {
            onVariantApplied(variantPart, variant);
        }

        private void onApplyVariantExtraInfoHandler(SWPartVariants orginator, string variantName, Dictionary<string, string> extraInfo)
        {
            if (orginator.part != part || orginator == this || !isInitialized)
                return;

            // If the variant has a meshSet that we recognize, then record it for later.
            if (extraInfo.ContainsKey("meshSet") && meshSets.Contains(extraInfo["meshSet"]))
            {
                currentMeshSet = extraInfo["meshSet"];
            }

            // Re-apply the variant if we're on the updateVariantModuleIDs list.
            if (extraInfo.ContainsKey("updateVariantModuleIDs") && extraInfo["updateVariantModuleIDs"].Contains(moduleID) && variantApplied)
            {
                applyVariant(variantIndex, true);
            }

            // Enable/disable the UI if we're on the list.
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
            return variants[variantIndex].getCost(currentMeshSet);
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
            return variants[variantIndex].getMass(currentMeshSet);
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
