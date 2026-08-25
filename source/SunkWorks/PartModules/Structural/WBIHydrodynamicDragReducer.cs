using SunkWorks.Submarine;
using UnityEngine;

namespace SunkWorks.Structural
{
    /// <summary>
    /// Passively reduces vessel-wide water drag according to the hull's
    /// length-to-beam ratio. The vessel supercavitation controller applies the
    /// calculated multiplier after stock PartBuoyancy has calculated water drag.
    /// </summary>
    [KSPModule("Hydrodynamic Drag Reducer")]
    public class WBIHydrodynamicDragReducer : PartModule
    {
        const float kMinimumDimension = 0.0001f;

        /// <summary>Full hull length in meters when no procedural hull is present.</summary>
        [KSPField]
        public float hullLength = 1.0f;

        /// <summary>Full hull beam in meters when no procedural hull is present.</summary>
        [KSPField]
        public float hullBeam = 1.0f;

        /// <summary>Slenderness ratio at which drag reduction begins.</summary>
        [KSPField]
        public float minimumSlendernessRatio = 1.5f;

        /// <summary>Slenderness ratio at which drag reduction reaches its maximum.</summary>
        [KSPField]
        public float maximumSlendernessRatio = 6.0f;

        /// <summary>Maximum fraction of stock water drag removed.</summary>
        [KSPField]
        public float maximumDragReduction = 0.20f;

        /// <summary>Enables rate-limited diagnostic logging.</summary>
        [KSPField]
        public bool debugMode;

        /// <summary>Minimum time between diagnostic messages, in seconds.</summary>
        [KSPField]
        public float debugLogInterval = 5f;

        /// <summary>Current flight UI representation of the hull ratio.</summary>
        [KSPField(guiActive = true, guiName = "Hull Slenderness")]
        public string slendernessDisplay = "0.00:1";

        /// <summary>Current flight UI representation of the active reduction.</summary>
        [KSPField(guiActive = true, guiName = "Hydrodynamic Drag Reduction")]
        public string dragReductionDisplay = "0%";

        WBIModuleProceduralHull proceduralHull;
        bool configurationValid;

        /// <summary>The currently resolved length-to-beam ratio.</summary>
        internal float SlendernessRatio
        {
            get
            {
                float length;
                float beam;
                getHullDimensions(out length, out beam);
                return isPositiveFinite(length) && isPositiveFinite(beam)
                    ? length / beam
                    : 0f;
            }
        }

        /// <summary>The configured reduction before checking water contact.</summary>
        internal float DragReduction
        {
            get
            {
                if (!configurationValid)
                    return 0f;

                float ratio = SlendernessRatio;
                if (!isPositiveFinite(ratio))
                    return 0f;

                float interpolation = Mathf.InverseLerp(
                    minimumSlendernessRatio, maximumSlendernessRatio, ratio);
                float reduction = Mathf.SmoothStep(0f, maximumDragReduction, interpolation);
                return Mathf.Clamp(reduction, 0f, maximumDragReduction);
            }
        }

        /// <summary>Whether this module can participate in the vessel-wide election.</summary>
        internal bool IsOperational
        {
            get
            {
                return configurationValid && enabled && part != null &&
                    part.State != PartStates.DEAD;
            }
        }

        /// <summary>Resolves procedural dimensions and validates configuration.</summary>
        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            proceduralHull = part.FindModuleImplementing<WBIModuleProceduralHull>();
            hideDimensionFieldsWhenProcedural();
            validateConfiguration();
            updateDisplays();
        }

        /// <summary>Refreshes the read-only flight displays.</summary>
        public override void OnUpdate()
        {
            base.OnUpdate();
            updateDisplays();
        }

        void validateConfiguration()
        {
            configurationValid = true;

            float resolvedLength;
            float resolvedBeam;
            getHullDimensions(out resolvedLength, out resolvedBeam);

            if (!isPositiveFinite(resolvedLength))
            {
                configurationValid = false;
                logConfigurationWarning((proceduralHull != null
                    ? "WBIModuleProceduralHull.hullLength"
                    : "hullLength") + " must be greater than zero.");
            }
            if (!isPositiveFinite(resolvedBeam))
            {
                configurationValid = false;
                logConfigurationWarning((proceduralHull != null
                    ? "WBIModuleProceduralHull.beam"
                    : "hullBeam") + " must be greater than zero.");
            }

            if (float.IsNaN(minimumSlendernessRatio) ||
                float.IsInfinity(minimumSlendernessRatio))
            {
                configurationValid = false;
                logConfigurationWarning("minimumSlendernessRatio must be finite.");
            }
            if (float.IsNaN(maximumSlendernessRatio) ||
                float.IsInfinity(maximumSlendernessRatio) ||
                maximumSlendernessRatio <= minimumSlendernessRatio)
            {
                configurationValid = false;
                logConfigurationWarning("maximumSlendernessRatio must be greater than minimumSlendernessRatio.");
            }

            if (float.IsNaN(maximumDragReduction) || float.IsInfinity(maximumDragReduction))
            {
                maximumDragReduction = 0f;
                configurationValid = false;
                logConfigurationWarning("maximumDragReduction must be finite.");
            }
            else
            {
                maximumDragReduction = Mathf.Clamp01(maximumDragReduction);
            }

            debugLogInterval = Mathf.Max(0.1f, debugLogInterval);
        }

        void hideDimensionFieldsWhenProcedural()
        {
            if (proceduralHull == null)
                return;

            BaseField lengthField = Fields["hullLength"];
            BaseField beamField = Fields["hullBeam"];
            if (lengthField != null)
            {
                lengthField.guiActive = false;
                lengthField.guiActiveEditor = false;
            }
            if (beamField != null)
            {
                beamField.guiActive = false;
                beamField.guiActiveEditor = false;
            }
        }

        void getHullDimensions(out float length, out float beam)
        {
            if (proceduralHull != null)
            {
                length = proceduralHull.hullLength;
                beam = proceduralHull.beam;
                return;
            }

            length = hullLength;
            beam = hullBeam;
        }

        void updateDisplays()
        {
            float ratio = SlendernessRatio;
            slendernessDisplay = ratio > 0f ? ratio.ToString("F2") + ":1" : "0.00:1";

            bool effectActive = false;
            if (HighLogic.LoadedSceneIsFlight && vessel != null)
            {
                WBISupercavitationController controller;
                effectActive = WBISupercavitationController.TryGetController(vessel, out controller) &&
                    controller.IsActiveHydrodynamicDragReducer(this) && vesselIsInWater();
            }

            float activeReduction = effectActive ? DragReduction : 0f;
            dragReductionDisplay = activeReduction > 0f
                ? (activeReduction * 100f).ToString("F1") + "%"
                : "0%";
        }

        bool vesselIsInWater()
        {
            if (vessel == null || vessel.mainBody == null || !vessel.mainBody.ocean)
                return false;

            for (int index = 0; index < vessel.parts.Count; index++)
            {
                Part vesselPart = vessel.parts[index];
                if (vesselPart != null && vesselPart.WaterContact &&
                    vesselPart.submergedPortion > 0.0)
                {
                    return true;
                }
            }
            return false;
        }

        void logConfigurationWarning(string message)
        {
            string partTitle = part != null && part.partInfo != null
                ? part.partInfo.title
                : "unknown part";
            Debug.LogWarning("[SunkWorks] WBIHydrodynamicDragReducer on " +
                partTitle + ": " + message + " Drag reduction is disabled.");
        }

        static bool isPositiveFinite(float value)
        {
            return value > kMinimumDimension && !float.IsNaN(value) &&
                !float.IsInfinity(value);
        }
    }
}
