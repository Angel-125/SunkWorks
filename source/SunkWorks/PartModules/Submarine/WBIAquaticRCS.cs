using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.Localization;

namespace SunkWorks.Submarine
{
    /// <summary>
    /// An aquatic RCS part module derived from ModuleRCSFX that supports animated props.
    /// </summary>
    /// <example>
    /// <code>
    /// MODULE
    /// {
    ///     name = WBIAquaticRCS
    ///     debugMode = false
    ///     intakeTransformName = intakeTransform
    ///     propellerTransformName = Screw
    ///     propellerRPM = 30
    ///     ...
    ///     // Standard ModuleRCSFX here...
    /// }
    /// </code>
    /// </example>
    public class WBIAquaticRCS: ModuleRCSFX
    {
        #region Fields
        /// <summary>
        /// Flag to enable debug mode.
        /// </summary>
        [KSPField]
        public bool debugMode = false;

        /// <summary>
        /// Name of the part's intake transform.
        /// </summary>
        [KSPField]
        public string intakeTransformName = "intakeTransform";

        /// <summary>
        /// Name of the part's propeller (if any).
        /// </summary>
        [KSPField]
        public string propellerTransformName = "Screw";

        /// <summary>
        /// Rotations Per Minute for the propeller.
        /// </summary>
        [KSPField]
        public float propellerRPM = 12f;

        /// <summary>Seconds between supercavity coverage checks.</summary>
        [KSPField]
        public float supercavitationCheckInterval = 0.5f;

        /// <summary>Coverage fraction at which this RCS unit loses access to water.</summary>
        [KSPField]
        public float supercavitationCoverageThreshold = 0.5f;
        #endregion

        #region Housekeeping
        Transform[] intakeTransforms;
        Transform propellerTransform;
        float originalThrustPower;
        float currentRotationAngle;
        Vector3 rotationAxis = new Vector3(0, 0, 1);
        double nextSupercavitationCheckTime;
        bool isCoveredBySupercavity;
        bool isDisabledBySupercavity;

        [KSPField]
        float fxPower;
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            supercavitationCheckInterval = Mathf.Max(0.1f, supercavitationCheckInterval);
            supercavitationCoverageThreshold = Mathf.Clamp01(supercavitationCoverageThreshold);
            nextSupercavitationCheckTime = double.MinValue;

            originalThrustPower = thrusterPower;

            // Get the intake transforms
            if (!string.IsNullOrEmpty(intakeTransformName))
                intakeTransforms = part.FindModelTransforms(intakeTransformName).ToArray();

            // Get propeller transform
            if (!string.IsNullOrEmpty(propellerTransformName))
                propellerTransform = part.FindModelTransform(propellerTransformName);

            Fields["thrusterPower"].guiActive = debugMode;
            Fields["fxPower"].guiActive = debugMode;
        }

        /// <summary>
        /// Removes RCS power before its next physics update when the part is inside a
        /// supercavity. The actual vessel coverage query remains rate limited.
        /// </summary>
        public override void OnUpdate()
        {
            base.OnUpdate();

            if (!HighLogic.LoadedSceneIsFlight)
                return;

            if (checkSupercavitationCoverage())
            {
                isDisabledBySupercavity = true;
                thrusterPower = 0f;
                fxPower = 0f;
            }
            else if (isDisabledBySupercavity)
            {
                isDisabledBySupercavity = false;
                thrusterPower = originalThrustPower;
            }
        }

        protected override void UpdatePowerFX(bool running, int idx, float power)
        {
            if (checkSupercavitationCoverage())
            {
                isDisabledBySupercavity = true;
                thrusterPower = 0f;
                fxPower = 0f;
                base.UpdatePowerFX(false, idx, 0f);
                return;
            }

            if (isDisabledBySupercavity)
            {
                isDisabledBySupercavity = false;
                thrusterPower = originalThrustPower;
            }

            // Make sure at least one of our intake transforms is underwater.
            if (intakeTransforms == null)
                return;
            if (!part.vessel.mainBody.ocean)
            {
                thrusterPower = 0.0f;
                base.UpdatePowerFX(false, idx, power);
                return;
            }
            if (!part.vessel.Splashed)
                return;

            bool intakeIsUnderwater = false;
            for (int index = 0; index < intakeTransforms.Length; index++)
            {
                if (FlightGlobals.getAltitudeAtPos((Vector3d)intakeTransforms[index].position, part.vessel.mainBody) <= 0.0f)
                {
                    intakeIsUnderwater = true;
                    break;
                }
            }
            if (!intakeIsUnderwater)
            {
                thrusterPower = 0.0f;
                base.UpdatePowerFX(false, idx, power);
                return;
            }

            // Update the FX
            thrusterPower = originalThrustPower;
            fxPower = power;
            if (power > 0.10001)
            {
                base.UpdatePowerFX(running, idx, power);

                // Spin prop if needed
                if (propellerTransform != null)
                {
                    float rotationPerFrame = ((propellerRPM * 60.0f) * TimeWarp.fixedDeltaTime) * power;
                    propellerTransform.Rotate(rotationAxis * rotationPerFrame);
                }
            }
            else
            {
                base.UpdatePowerFX(running, idx, 0);
            }

            // Refresh our reserves. This is primarily to simulate intake of IntakeLqd.
            // Why do this? Because ModuleResourceIntake will fill all resource containers on the vessel.
            // So what we do is have the part contain a small amount of IntakeLqd, and make flow for it NO_FLOW.
            int count = propellants.Count;
            Propellant propellant;
            for (int index = 0; index < count; index++)
            {
                propellant = propellants[index];
                if (part.Resources.Contains(propellant.name))
                {
                    part.Resources[propellant.name].amount = part.Resources[propellant.name].maxAmount;
                }
            }
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
        #endregion
    }
}
