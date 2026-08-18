using System;
using UnityEngine;
using KSP.Localization;

namespace SunkWorks.Submarine
{
    /// <summary>
    /// This class is an engine that only runs underwater. It needs no resource intake; if underwater then it'll auto-replenish the part's resource reserves.
    /// </summary>
    public class WBIAquaticEngine: ModuleEnginesFX
    {
        #region Fields
        /// <summary>
        /// Flag to indicate whether or not the engine is in reverse-thrust mode.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool isReverseThrust;

        /// <summary>Seconds between supercavity coverage checks.</summary>
        [KSPField]
        public float supercavitationCheckInterval = 0.5f;

        /// <summary>Coverage fraction at which this engine loses access to water.</summary>
        [KSPField]
        public float supercavitationCoverageThreshold = 0.5f;
        #endregion

        #region Housekeeping
        /// <summary>
        /// Flag to indicate whether or not the engine is underwater
        /// </summary>
        public bool isUnderwater;

        /// <summary>
        /// Name of the water resource to fill if the part is underwater and it has the resource in question.
        /// </summary>
        public string waterResourceName = "IntakeLqd";
        #endregion

        #region Housekeeping
        PartResource waterResource = null;
        double nextSupercavitationCheckTime;
        bool isCoveredBySupercavity;
        #endregion

        #region Events
        [KSPEvent(guiActive = true, guiName = "#LOC_SUNKWORKS_reverseThrust")]
        public void ToggleReverseThrust()
        {
            isReverseThrust = !isReverseThrust;
            reverseThrustTransform();
            updateGUI();
        }

        [KSPAction("#LOC_SUNKWORKS_toggleFwdRevThrust")]
        public void ToggleReverseThrustAction(KSPActionParam param)
        {
            ToggleReverseThrust();
        }
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            supercavitationCheckInterval = Mathf.Max(0.1f, supercavitationCheckInterval);
            supercavitationCoverageThreshold = Mathf.Clamp01(supercavitationCoverageThreshold);
            nextSupercavitationCheckTime = double.MinValue;
            if (isReverseThrust)
                reverseThrustTransform();
            updateGUI();
            if(HighLogic.LoadedSceneIsFlight && !string.IsNullOrEmpty(waterResourceName) && part.Resources.Contains(waterResourceName))
            {
                waterResource = part.Resources[waterResourceName];
            }
        }

        public override bool CheckDeprived(double requiredPropellant, out string propName)
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return base.CheckDeprived(requiredPropellant, out propName);

            // Aquatic engines cannot run inside a supercavity because their water
            // intakes are surrounded by gas.
            isUnderwater = checkUnderwater();
            bool isSupercavitated = checkSupercavitationCoverage();

            // ModuleEngines refreshes Propellant.totalResourceAvailable immediately
            // before calling CheckDeprived. Changing the PartResource amount here and
            // then delegating to the stock implementation therefore leaves the stock
            // availability snapshot stale. Report deprivation directly instead.
            if (!isUnderwater || isSupercavitated)
            {
                if (waterResource != null)
                    waterResource.amount = 0.0;

                propName = waterResource != null && waterResource.info != null
                    ? waterResource.info.displayName
                    : waterResourceName;
                return true;
            }

            //If we're underwater, then let the engine decide if we're deprived of propellants.
            if (waterResource != null)
                waterResource.amount = waterResource.maxAmount;

            return base.CheckDeprived(requiredPropellant, out propName);
        }

        /// <summary>
        /// Removes engine flow at the source when this pumpjet is inside a
        /// supercavity. ModuleEngines checks this value before requesting
        /// propellants, so the cutoff also works when resource requests are
        /// bypassed and prevents any residual thrust during flameout.
        /// </summary>
        protected override float ModifyFlow()
        {
            if (checkSupercavitationCoverage())
                return 0f;

            return base.ModifyFlow();
        }
        #endregion

        #region Helpers
        protected bool checkUnderwater()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return false;
            if (!part.vessel.mainBody.ocean)
                return false;
            if (!part.vessel.Splashed)
                return false;

            int count = thrustTransforms.Count;
            for (int index = 0; index < count; index++)
            {
                if (FlightGlobals.getAltitudeAtPos((Vector3d)thrustTransforms[index].position, part.vessel.mainBody) <= 0.0f)
                    return true;
            }
            return false;
        }

        bool checkSupercavitationCoverage()
        {
            if (!SunkWorks.SunkWorksSettings.SupercavitationFlameoutEnabled ||
                !HighLogic.LoadedSceneIsFlight || vessel == null)
            {
                isCoveredBySupercavity = false;
                return false;
            }

            double currentTime = Planetarium.GetUniversalTime();
            if (currentTime < nextSupercavitationCheckTime)
                return isCoveredBySupercavity;

            nextSupercavitationCheckTime = currentTime + supercavitationCheckInterval;
            WBISupercavitationController controller;
            isCoveredBySupercavity =
                WBISupercavitationController.TryGetController(vessel, out controller) &&
                controller.GetSupercavityCoverage(part) >= supercavitationCoverageThreshold;
            return isCoveredBySupercavity;
        }

        protected void updateGUI()
        {
            Events["ToggleReverseThrust"].guiName = isReverseThrust ? Localizer.Format("#LOC_SUNKWORKS_setForwardThrust") : Localizer.Format("#LOC_SUNKWORKS_setReverseThrust");
        }

        protected void reverseThrustTransform()
        {
            int count = thrustTransforms.Count;
            Transform transform;
            for (int index = 0; index < count; index++)
            {
                transform = thrustTransforms[index];
                transform.Rotate(0, 180.0f, 0);
            }
        }
        #endregion
    }
}
