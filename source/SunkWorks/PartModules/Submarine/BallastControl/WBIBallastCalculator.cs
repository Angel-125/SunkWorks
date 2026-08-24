using SunkWorks.Structural;
using UnityEngine;
using WildBlueCore;

namespace SunkWorks.Submarine
{
    /// <summary>
    /// Sizes a ballast tank from a percentage of a procedural hull's generated volume.
    /// </summary>
    [KSPModule("Ballast Calculator")]
    public class WBIBallastCalculator : PartModule
    {
        const double kLitersPerCubicMeter = 1000.0;

        /// <summary>Percentage of the generated hull volume available for ballast.</summary>
        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Ballast Percent",
            groupName = WBIBallastTank.kBallastGroup,
            groupDisplayName = "#LOC_SUNKWORKS_ballastTank")]
        [UI_FloatRange(minValue = 0f, maxValue = 100f, stepIncrement = 1f,
            scene = UI_Scene.Editor)]
        public float ballastPercent = 100f;

        bool initialized;
        bool initialUpdatePending;

        /// <summary>Subscribes to editor variant notifications.</summary>
        public override void OnAwake()
        {
            base.OnAwake();
            if (HighLogic.LoadedSceneIsEditor)
                GameEvents.onEditorVariantApplied.Add(OnEditorVariantApplied);
        }

        /// <summary>Initializes the slider and calculates the initial capacity.</summary>
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsEditor)
                return;

            BaseField percentField = Fields["ballastPercent"];
            if (percentField != null && percentField.uiControlEditor != null)
                percentField.uiControlEditor.onFieldChanged = OnBallastPercentChanged;

            initialized = true;
            initialUpdatePending = true;
        }

        /// <summary>
        /// Performs the initial calculation after every part module has had an opportunity to start.
        /// </summary>
        public void Update()
        {
            if (!initialUpdatePending || !HighLogic.LoadedSceneIsEditor)
                return;
            initialUpdatePending = false;
            UpdateBallastCapacity();
        }

        /// <summary>Unsubscribes from editor variant notifications.</summary>
        public void OnDestroy()
        {
            GameEvents.onEditorVariantApplied.Remove(OnEditorVariantApplied);
        }

        void OnEditorVariantApplied(Part modifiedPart, PartVariant variant)
        {
            if (initialized && modifiedPart == part)
                UpdateBallastCapacity();
        }

        void OnBallastPercentChanged(BaseField field, object oldValue)
        {
            if (!initialized)
                return;
            ballastPercent = Mathf.Clamp(ballastPercent, 0f, 100f);
            UpdateBallastCapacity();
        }

        void UpdateBallastCapacity()
        {
            if (part == null || PartResourceLibrary.Instance == null)
                return;

            WBIModuleProceduralHull proceduralHull =
                part.FindModuleImplementing<WBIModuleProceduralHull>();
            WBIBallastTank ballastTank = part.FindModuleImplementing<WBIBallastTank>();
            if (proceduralHull == null || ballastTank == null ||
                string.IsNullOrEmpty(ballastTank.ballastResourceName))
                return;

            PartResourceDefinitionList definitions =
                PartResourceLibrary.Instance.resourceDefinitions;
            if (definitions == null || !definitions.Contains(ballastTank.ballastResourceName))
            {
                Debug.LogWarning("[SunkWorks] WBIBallastCalculator could not find resource definition " +
                    ballastTank.ballastResourceName + " on " + part.name);
                return;
            }

            PartResourceDefinition definition = definitions[ballastTank.ballastResourceName];
            double availableVolume = Mathf.Max(0f, proceduralHull.generatedVolume) *
                Mathf.Clamp01(ballastPercent * 0.01f);
            double maxAmount = CalculateResourceUnits(availableVolume, definition);
            if (double.IsNaN(maxAmount) || double.IsInfinity(maxAmount) || maxAmount < 0.0)
                return;

            PartResource resource = part.Resources.Contains(ballastTank.ballastResourceName)
                ? part.Resources[ballastTank.ballastResourceName]
                : null;
            double fillFraction = resource != null && resource.maxAmount > 0.0
                ? Mathf.Clamp01((float)(resource.amount / resource.maxAmount))
                : 0.0;

            if (resource == null)
            {
                resource = part.Resources.Add(ballastTank.ballastResourceName, 0.0, maxAmount,
                    true, true, false, true, PartResource.FlowMode.Both);
            }
            else
            {
                resource.maxAmount = maxAmount;
                resource.amount = maxAmount * fillFraction;
                GameEvents.onPartResourceListChange.Fire(part);
            }

            ballastTank.RefreshBallastResource();
            ballastTank.updatePAW = true;
            MonoUtilities.RefreshContextWindows(part);
        }

        static double CalculateResourceUnits(double cubicMeters,
            PartResourceDefinition definition)
        {
            if (definition == null || cubicMeters <= 0.0)
                return 0.0;

            // KSP resource volume is litres per unit.
            if (definition.volume > 0f)
                return cubicMeters * kLitersPerCubicMeter / definition.volume;

            // Density is tonnes per unit. This fallback assumes water-equivalent ballast
            // at one tonne per cubic metre, matching IntakeLqd's physical role.
            if (definition.density > 0f)
                return cubicMeters / definition.density;

            return 0.0;
        }
    }
}
