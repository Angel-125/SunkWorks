using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.Localization;

namespace SunkWorks.Submarine
{
    public delegate bool RequirementsDelegate(SWAquaticEngine aquaticEngine);

    /// <summary>
    /// This class is an engine that only runs underwater. It needs no resource intake; if underwater then it'll auto-replenish the part's resource reserves.
    /// </summary>
    public class SWAquaticEngine: ModuleEnginesFX
    {
        #region Fields
        /// <summary>
        /// Flag to indicate whether or not the engine is in reverse-thrust mode.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool isReverseThrust;
        #endregion

        #region Housekeeping
        public RequirementsDelegate checkRequirements;
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

            //Check external requirements to see if the engine can run.
            //Example: when the boat's supercavitation effect is on, aquatic engines can't run- they're in a buble of air and the water intakes are dry!
            isUnderwater = checkUnderwater();
            bool requirementsMet = true;
            if (checkRequirements != null)
                requirementsMet = checkRequirements(this);

            //If we're underwater, then let the engine decide if we're deprived of propellants.
            if (isUnderwater && requirementsMet)
            {
                if (waterResource != null)
                    waterResource.amount = waterResource.maxAmount;
                return base.CheckDeprived(requiredPropellant, out propName);
            }

            //Clear our resource reserves, then let the engine decide what to do.
            //This will force the engine to flame out because we don't have any IntakeLqd.
            else
            {
                int count = part.Resources.Count;
                for (int index = 0; index < count; index++)
                    part.Resources[index].amount = 0.0f;
            }

            return base.CheckDeprived(requiredPropellant, out propName);
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
