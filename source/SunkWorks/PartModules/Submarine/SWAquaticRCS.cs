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
        #endregion

        #region Housekeeping
        Transform[] intakeTransforms;
        Transform propellerTransform;
        float originalThrustPower;
        float currentRotationAngle;
        Vector3 rotationAxis = new Vector3(0, 0, 1);

        [KSPField]
        float fxPower;
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);

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

        protected override void UpdatePowerFX(bool running, int idx, float power)
        {
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
        #endregion
    }
}
