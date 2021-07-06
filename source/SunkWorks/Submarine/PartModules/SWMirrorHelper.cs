using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP.Localization;

namespace SunkWorks.Submarine
{
    /// <summary>
    /// A helper class to mirror meshes, especially those that are node-attached.
    /// </summary>
    public class SWMirrorHelper: PartModule
    {
        [KSPField(isPersistant = true)]
        public bool isMirrored;

        /// <summary>
        /// The mesh to show when mirroring is turned off.
        /// </summary>
        [KSPField]
        public string meshName = string.Empty;

        /// <summary>
        /// The mesh to show when mirroring is turned on.
        /// </summary>
        [KSPField]
        public string mirrorMeshName = string.Empty;

        /// <summary>
        /// Name of the variant to watch for. When it is selected, we'll enable the ability to mirror the meshes in the editor.
        /// </summary>
        [KSPField]
        public string variantName = string.Empty;

        private Transform meshTransform = null;
        private Transform mirroredMeshTransform = null;
        private ModulePartVariants partVariant;

        public override void OnAwake()
        {
            base.OnAwake();
            GameEvents.onVariantApplied.Add(onVariantApplied);

            if (!string.IsNullOrEmpty(variantName))
            {
                List<ModulePartVariants> variants = part.FindModulesImplementing<ModulePartVariants>();
                int count = variants.Count;
                for (int index = 0; index < count; index++)
                {
                    if (variants[index].HasVariant(variantName))
                    {
                        partVariant = variants[index];
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(meshName) || string.IsNullOrEmpty(mirrorMeshName))
                return;

            meshTransform = part.FindModelTransform(meshName);
            mirroredMeshTransform = part.FindModelTransform(mirrorMeshName);
        }

        public void OnDestroy()
        {
            GameEvents.onVariantApplied.Remove(onVariantApplied);
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            Events["ToggleMeshMirror"].active = HighLogic.LoadedSceneIsEditor;
            Events["ToggleMeshMirror"].guiName = isMirrored ? Localizer.Format("#LOC_SUNKWORKS_showMirrorMesh") : Localizer.Format("#LOC_SUNKWORKS_showNormalMesh");

            if (meshTransform == null || mirroredMeshTransform == null || string.IsNullOrEmpty(variantName) || partVariant == null || partVariant.SelectedVariant.Name != variantName)
                return;

            toggleTransforms();
        }

        [KSPEvent(guiActiveEditor = true, guiName = "#LOC_SUNKWORKS_showNormalMesh")]
        public void ToggleMeshMirror()
        {
            isMirrored = !isMirrored;
            Events["ToggleMeshMirror"].guiName = isMirrored ? Localizer.Format("#LOC_SUNKWORKS_showMirrorMesh") : Localizer.Format("#LOC_SUNKWORKS_showNormalMesh");
            toggleTransforms();
        }

        private void onVariantApplied(Part variantPart, PartVariant variant)
        {
            if (variantPart != part || partVariant == null || !HighLogic.LoadedSceneIsEditor || meshTransform == null || mirroredMeshTransform == null)
                return;

            Events["ToggleMeshMirror"].active = partVariant.SelectedVariant.Name == variantName;
            if (partVariant.SelectedVariant.Name == variantName)
                toggleTransforms();
        }

        private void toggleTransforms()
        {
            meshTransform.gameObject.SetActive(!isMirrored);
            mirroredMeshTransform.gameObject.SetActive(isMirrored);
        }
    }
}
