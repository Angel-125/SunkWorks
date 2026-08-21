using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace SunkWorks.Submarine
{
    /// <summary>
    /// Calculates all supercavity coverage for one loaded vessel once per physics tick.
    /// </summary>
    public class WBISupercavitationController : VesselModule
    {
        sealed class ControllerReference
        {
            internal WBISupercavitationController controller;
        }

        static readonly ConditionalWeakTable<Vessel, ControllerReference> controllerRegistry =
            new ConditionalWeakTable<Vessel, ControllerReference>();

        struct DragApplication
        {
            internal float stockDrag;
            internal float appliedDrag;
        }

        readonly Dictionary<Part, float> dragMultipliers = new Dictionary<Part, float>();
        readonly Dictionary<Part, float> cavityCoverages = new Dictionary<Part, float>();
        readonly Dictionary<Part, DragApplication> currentDragApplications =
            new Dictionary<Part, DragApplication>();
        readonly Dictionary<Part, DragApplication> previousDragApplications =
            new Dictionary<Part, DragApplication>();
        readonly List<Supercavity> activeCavities = new List<Supercavity>();
        List<WBISupercavitator> cavitators = new List<WBISupercavitator>();
        int cachedPartCount = -1;
        float lastUpdateFixedTime = float.MinValue;
        double nextDiagnosticTime;

        /// <summary>
        /// Gets the registered supercavitation controller for a loaded vessel without
        /// repeatedly searching its VesselModules.
        /// </summary>
        public static bool TryGetController(Vessel targetVessel,
            out WBISupercavitationController controller)
        {
            controller = null;
            if (targetVessel == null)
                return false;

            ControllerReference reference;
            if (!controllerRegistry.TryGetValue(targetVessel, out reference) ||
                reference == null || reference.controller == null)
            {
                return false;
            }

            controller = reference.controller;
            return true;
        }

        protected override void OnAwake()
        {
            base.OnAwake();
            if (vessel == null)
                return;

            ControllerReference reference;
            if (controllerRegistry.TryGetValue(vessel, out reference))
            {
                reference.controller = this;
            }
            else
            {
                controllerRegistry.Add(vessel, new ControllerReference
                {
                    controller = this
                });
            }
        }

        void OnDestroy()
        {
            if (vessel == null)
                return;

            ControllerReference reference;
            if (controllerRegistry.TryGetValue(vessel, out reference) &&
                reference != null && reference.controller == this)
            {
                controllerRegistry.Remove(vessel);
            }
        }

        /// <summary>Limits this controller to loaded vessels in the flight scene.</summary>
        public override Activation GetActivation()
        {
            return Activation.FlightScene | Activation.LoadedVessels;
        }

        /// <summary>Returns whether this vessel is currently available for physics.</summary>
        public override bool ShouldBeActive()
        {
            return HighLogic.LoadedSceneIsFlight && vessel != null && vessel.loaded;
        }

        /// <summary>Returns the stock-water-drag multiplier for a vessel part.</summary>
        public float GetWaterDragMultiplier(Part vesselPart)
        {
            ensureCoverageCurrent();

            float multiplier;
            return vesselPart != null && dragMultipliers.TryGetValue(vesselPart, out multiplier)
                ? multiplier
                : 1f;
        }

        /// <summary>
        /// Returns the current normalized supercavity coverage of a vessel part.
        /// Zero is returned when the part is not covered or the vessel has no active
        /// supercavitator.
        /// </summary>
        public float GetSupercavityCoverage(Part vesselPart)
        {
            ensureCoverageCurrent();

            float coverage;
            return vesselPart != null && cavityCoverages.TryGetValue(vesselPart, out coverage)
                ? coverage
                : 0f;
        }

        internal void RecordWaterDragApplication(Part vesselPart, float stockDrag, float appliedDrag)
        {
            if (vesselPart == null)
                return;

            currentDragApplications[vesselPart] = new DragApplication
            {
                stockDrag = stockDrag,
                appliedDrag = appliedDrag
            };
        }

        void ensureCoverageCurrent()
        {
            if (lastUpdateFixedTime == Time.fixedTime)
                return;

            lastUpdateFixedTime = Time.fixedTime;
            previousDragApplications.Clear();
            foreach (KeyValuePair<Part, DragApplication> entry in currentDragApplications)
                previousDragApplications[entry.Key] = entry.Value;
            currentDragApplications.Clear();
            dragMultipliers.Clear();
            cavityCoverages.Clear();
            activeCavities.Clear();

            if (vessel == null || !vessel.loaded || vessel.packed)
                return;

            if (cachedPartCount != vessel.parts.Count)
            {
                cachedPartCount = vessel.parts.Count;
                cavitators = vessel.FindPartModulesImplementing<WBISupercavitator>();
            }

            for (int index = 0; index < cavitators.Count; index++)
            {
                Supercavity cavity;
                if (cavitators[index] != null && cavitators[index].TryGetSupercavity(out cavity))
                    activeCavities.Add(cavity);
            }

            for (int cavityIndex = 0; cavityIndex < activeCavities.Count; cavityIndex++)
                applyCavity(activeCavities[cavityIndex]);

            logDiagnostics();
        }

        void logDiagnostics()
        {
            bool diagnosticsEnabled = false;
            bool verbose = false;
            float interval = float.MaxValue;

            for (int index = 0; index < cavitators.Count; index++)
            {
                WBISupercavitator cavitator = cavitators[index];
                if (cavitator == null || !cavitator.debugMode)
                    continue;

                diagnosticsEnabled = true;
                verbose |= cavitator.verboseDebug;
                interval = Mathf.Min(interval, cavitator.debugLogInterval);
            }

            if (!diagnosticsEnabled)
                return;

            double currentTime = Planetarium.GetUniversalTime();
            if (currentTime < nextDiagnosticTime)
                return;
            nextDiagnosticTime = currentTime + Math.Max(0.1f, interval);

            float minimumMultiplier = 1f;
            foreach (float multiplier in dragMultipliers.Values)
                minimumMultiplier = Mathf.Min(minimumMultiplier, multiplier);

            float summedStockRigidbodyDrag = 0f;
            float summedAppliedRigidbodyDrag = 0f;
            foreach (DragApplication application in previousDragApplications.Values)
            {
                summedStockRigidbodyDrag += application.stockDrag;
                summedAppliedRigidbodyDrag += application.appliedDrag;
            }

            int vesselWaterContactParts = 0;
            int vesselSubmergedParts = 0;
            int vesselShieldedParts = 0;
            for (int index = 0; index < vessel.parts.Count; index++)
            {
                Part vesselPart = vessel.parts[index];
                if (vesselPart == null)
                    continue;

                if (vesselPart.WaterContact)
                    vesselWaterContactParts++;
                if (vesselPart.submergedPortion > 0.0)
                    vesselSubmergedParts++;
                if (vesselPart.ShieldedFromAirstream)
                    vesselShieldedParts++;
            }

            int coveredSubmergedParts = 0;
            int coveredZeroSubmergedParts = 0;
            int coveredShieldedParts = 0;
            int coveredPhysicslessParts = 0;
            foreach (Part coveredPart in dragMultipliers.Keys)
            {
                if (coveredPart.submergedPortion > 0.0)
                    coveredSubmergedParts++;
                else
                    coveredZeroSubmergedParts++;
                if (coveredPart.ShieldedFromAirstream)
                    coveredShieldedParts++;
                if (coveredPart.physicalSignificance == Part.PhysicalSignificance.NONE)
                    coveredPhysicslessParts++;
            }

            List<ModuleEngines> engines = vessel.FindPartModulesImplementing<ModuleEngines>();
            int ignitedEngines = 0;
            int producingEngines = 0;
            float totalThrust = 0f;
            float configuredMaximumThrust = 0f;
            for (int index = 0; index < engines.Count; index++)
            {
                ModuleEngines engine = engines[index];
                if (engine == null)
                    continue;

                if (engine.EngineIgnited)
                    ignitedEngines++;
                if (engine.finalThrust > 0f)
                    producingEngines++;
                totalThrust += engine.finalThrust;
                configuredMaximumThrust += engine.maxThrust * engine.thrustPercentage * 0.01f;
            }

            Debug.Log("[WBISupercavitator] vessel='" + vessel.GetDisplayName() +
                "' cavitators=" + cavitators.Count +
                " activeCavities=" + activeCavities.Count +
                " coveredParts=" + dragMultipliers.Count +
                " coveredSubmerged=" + coveredSubmergedParts +
                " coveredZeroSubmerged=" + coveredZeroSubmergedParts +
                " coveredShielded=" + coveredShieldedParts +
                " coveredPhysicsless=" + coveredPhysicslessParts +
                " vesselWaterContact=" + vesselWaterContactParts + "/" + vessel.parts.Count +
                " vesselSubmerged=" + vesselSubmergedParts + "/" + vessel.parts.Count +
                " vesselShielded=" + vesselShieldedParts +
                " dragPatchedParts=" + previousDragApplications.Count +
                " summedStockRbDrag=" + summedStockRigidbodyDrag.ToString("F3") +
                " summedAppliedRbDrag=" + summedAppliedRigidbodyDrag.ToString("F3") +
                " minimumDragMultiplier=" + minimumMultiplier.ToString("F3"));

            Debug.Log("[WBISupercavitator] propulsion engines=" + engines.Count +
                " ignited=" + ignitedEngines +
                " producing=" + producingEngines +
                " thrust=" + totalThrust.ToString("F2") + "kN" +
                " configuredMaximum=" + configuredMaximumThrust.ToString("F2") + "kN");

            for (int index = 0; index < cavitators.Count; index++)
            {
                WBISupercavitator cavitator = cavitators[index];
                if (cavitator == null || !cavitator.debugMode)
                    continue;

                string partTitle = cavitator.part != null && cavitator.part.partInfo != null
                    ? cavitator.part.partInfo.title
                    : "unknown";
                Debug.Log("[WBISupercavitator] cavitator='" + partTitle +
                    "' enabled=" + cavitator.cavityEnabled +
                    " status='" + cavitator.cavityStatus +
                    "' speed=" + cavitator.DiagnosticSpeed.ToString("F2") + "m/s" +
                    " aoa=" + cavitator.DiagnosticAngleOfAttack.ToString("F2") + "deg" +
                    " strength=" + (cavitator.DiagnosticStrength * 100f).ToString("F1") + "%" +
                    " inputResources=" + cavitator.DiagnosticInputResourceCount +
                    " gasMode=" + cavitator.DiagnosticResourceMode +
                    " gasRate=" + cavitator.DiagnosticResourceRate.ToString("F5") + "/s");
            }

            if (!verbose)
                return;

            foreach (KeyValuePair<Part, float> entry in dragMultipliers)
            {
                string partTitle = entry.Key.partInfo != null
                    ? entry.Key.partInfo.title
                    : entry.Key.partName;
                DragApplication dragApplication;
                bool dragWasApplied = previousDragApplications.TryGetValue(
                    entry.Key, out dragApplication);
                Debug.Log("[WBISupercavitator] coveredPart='" + partTitle +
                    "' flightID=" + entry.Key.flightID +
                    " submerged=" + entry.Key.submergedPortion.ToString("F3") +
                    " waterContact=" + entry.Key.WaterContact +
                    " shielded=" + entry.Key.ShieldedFromAirstream +
                    " physicalSignificance=" + entry.Key.physicalSignificance +
                    " dragMultiplier=" + entry.Value.ToString("F3") +
                    " dragApplied=" + dragWasApplied +
                    (dragWasApplied
                        ? " stockRbDrag=" + dragApplication.stockDrag.ToString("F3") +
                          " appliedRbDrag=" + dragApplication.appliedDrag.ToString("F3")
                        : string.Empty));
            }


            for (int index = 0; index < engines.Count; index++)
            {
                ModuleEngines engine = engines[index];
                if (engine == null)
                    continue;

                string partTitle = engine.part != null && engine.part.partInfo != null
                    ? engine.part.partInfo.title
                    : "unknown";
                Debug.Log("[WBISupercavitator] engine='" + partTitle +
                    "' engineID='" + engine.engineID +
                    "' ignited=" + engine.EngineIgnited +
                    " flameout=" + engine.flameout +
                    " throttle=" + (engine.currentThrottle * 100f).ToString("F1") + "%" +
                    " thrust=" + engine.finalThrust.ToString("F2") + "kN" +
                    " configuredMaximum=" +
                    (engine.maxThrust * engine.thrustPercentage * 0.01f).ToString("F2") + "kN");
            }
        }

        void applyCavity(Supercavity cavity)
        {
            for (int partIndex = 0; partIndex < vessel.parts.Count; partIndex++)
            {
                Part candidate = vessel.parts[partIndex];
                if (candidate == null || candidate == cavity.source.part)
                    continue;

                float coverage = calculateCoverage(candidate, cavity) * cavity.strength;
                if (coverage <= 0f)
                    continue;

                float currentCoverage;
                if (!cavityCoverages.TryGetValue(candidate, out currentCoverage) ||
                    coverage > currentCoverage)
                {
                    cavityCoverages[candidate] = coverage;
                }

                float multiplier = Mathf.Lerp(1f, cavity.residualDrag, coverage);
                float currentMultiplier;
                if (!dragMultipliers.TryGetValue(candidate, out currentMultiplier) ||
                    multiplier < currentMultiplier)
                {
                    dragMultipliers[candidate] = multiplier;
                }
            }
        }

        static float calculateCoverage(Part candidate, Supercavity cavity)
        {
            Vector3 localCenter = Vector3.zero;
            Vector3 halfSize = Vector3.zero;
            bool hasDragCubeBounds = candidate.DragCubes != null && !candidate.DragCubes.None;
            if (hasDragCubeBounds)
            {
                localCenter = candidate.DragCubes.WeightedCenter;
                halfSize = candidate.DragCubes.WeightedSize * 0.5f;
                hasDragCubeBounds = halfSize.sqrMagnitude > 1e-6f;
            }

            Transform partTransform = candidate.partTransform;
            if (!hasDragCubeBounds)
            {
                return isInsideCavity(partTransform.position, cavity) ? 1f : 0f;
            }

            int pointsInside = isInsideCavity(
                partTransform.TransformPoint(localCenter), cavity) ? 1 : 0;

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 localPoint = localCenter + new Vector3(
                            halfSize.x * x,
                            halfSize.y * y,
                            halfSize.z * z);
                        if (isInsideCavity(partTransform.TransformPoint(localPoint), cavity))
                            pointsInside++;
                    }
                }
            }

            return pointsInside / 9f;
        }

        static bool isInsideCavity(Vector3 point, Supercavity cavity)
        {
            Vector3 offset = point - cavity.origin;
            float axialDistance = Vector3.Dot(offset, cavity.axis);
            if (axialDistance <= 0f || axialDistance >= cavity.length)
                return false;

            Vector3 radialOffset = offset - cavity.axis * axialDistance;
            float radius = cavity.RadiusAt(axialDistance);
            return radialOffset.sqrMagnitude <= radius * radius;
        }
    }
}
