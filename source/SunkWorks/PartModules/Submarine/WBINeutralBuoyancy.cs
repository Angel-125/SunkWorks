using System;
using System.Collections.Generic;
using System.Globalization;
using KSP.Localization;
using UnityEngine;
using WildBlueCore;

namespace SunkWorks.Submarine
{
    /// <summary>
    /// Provides low-frequency, automatic neutral buoyancy for underwater bases.
    /// The module captures the parts that belong to the base when enabled and also
    /// adopts parts subsequently attached through EVA construction. Docked vessels
    /// are deliberately not added to the controlled set.
    /// </summary>
    /// <example>
    /// <code>
    /// MODULE
    /// {
    ///     name = WBINeutralBuoyancy
    ///     updateInterval = 1
    ///     minimumSubmergedPortion = 0.95
    ///     minimumBuoyancy = 0.01
    ///     maximumBuoyancy = 50
    /// }
    /// </code>
    /// </example>
    [KSPModule("Neutral Buoyancy")]
    public class WBINeutralBuoyancy : WBIBasePartModule
    {
        const string kGroupName = "NeutralBuoyancy";
        const string kControlledPartNode = "CONTROLLED_PART";
        const float kMinimumInterval = 0.1f;
        const float kDefaultDeadband = 0.005f;
        const float kMessageDuration = 4f;

        class ControlledPart
        {
            public uint persistentId;
            public float originalBuoyancy;
            public float appliedBuoyancy;
        }

        /// <summary>
        /// Enables automatic neutral buoyancy. If a ModuleGroundPart is installed on
        /// this part, neutral buoyancy waits until that ground part is deployed.
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_SUNKWORKS_neutralBuoyancyEnabled", isPersistant = true, groupName = kGroupName, groupDisplayName = "#LOC_SUNKWORKS_neutralBuoyancyGroup")]
        [UI_Toggle(enabledText = "#LOC_SUNKWORKS_on", disabledText = "#LOC_SUNKWORKS_off")]
        public bool neutralBuoyancyEnabled;

        /// <summary>
        /// Number of seconds between mass and buoyancy recalculations.
        /// </summary>
        [KSPField]
        public float updateInterval = 1f;

        /// <summary>
        /// Minimum submerged fraction required before a part is adjusted.
        /// </summary>
        [KSPField]
        public float minimumSubmergedPortion = 0.95f;

        /// <summary>
        /// Minimum Part.buoyancy multiplier the controller may apply.
        /// </summary>
        [KSPField]
        public float minimumBuoyancy = 0.01f;

        /// <summary>
        /// Maximum Part.buoyancy multiplier the controller may apply.
        /// </summary>
        [KSPField]
        public float maximumBuoyancy = 50f;

        /// <summary>
        /// Current controller status displayed in the PAW.
        /// </summary>
        [KSPField(guiActive = true, guiName = "#LOC_SUNKWORKS_neutralBuoyancyStatus", groupName = kGroupName, groupDisplayName = "#LOC_SUNKWORKS_neutralBuoyancyGroup")]
        public string statusDisplay = string.Empty;

        /// <summary>
        /// Number of base parts owned by this controller.
        /// </summary>
        [KSPField(guiActive = true, guiName = "#LOC_SUNKWORKS_neutralBuoyancyParts", groupName = kGroupName, groupDisplayName = "#LOC_SUNKWORKS_neutralBuoyancyGroup")]
        public int controlledPartCount;

        /// <summary>
        /// Difference between controlled weight and buoyancy, expressed as tonnes-equivalent.
        /// </summary>
        [KSPField(guiActive = true, guiName = "#LOC_SUNKWORKS_neutralBuoyancyError", groupName = kGroupName, groupDisplayName = "#LOC_SUNKWORKS_neutralBuoyancyGroup")]
        public string buoyancyErrorDisplay = "--";

        /// <summary>
        /// Deployment state of a ModuleGroundPart installed on this same part.
        /// </summary>
        [KSPField(guiActive = true, guiName = "#LOC_SUNKWORKS_groundAnchorStatus", groupName = kGroupName, groupDisplayName = "#LOC_SUNKWORKS_neutralBuoyancyGroup")]
        public string groundAnchorStatusDisplay = string.Empty;

        readonly Dictionary<uint, ControlledPart> controlledParts = new Dictionary<uint, ControlledPart>();
        ModuleGroundPart groundPart;
        bool controllerWasActive;
        bool fieldStateInitialized;
        bool previousEnabled;
        double nextUpdateTime;

        /// <summary>
        /// Toggles neutral buoyancy through an action group.
        /// </summary>
        [KSPAction("#LOC_SUNKWORKS_toggleNeutralBuoyancy")]
        public void ToggleNeutralBuoyancyAction(KSPActionParam param)
        {
            neutralBuoyancyEnabled = !neutralBuoyancyEnabled;
        }

        /// <summary>
        /// Recalculates controlled-part buoyancy immediately.
        /// </summary>
        [KSPEvent(guiActive = true, guiName = "#LOC_SUNKWORKS_recalculateNeutralBuoyancy", groupName = kGroupName, groupDisplayName = "#LOC_SUNKWORKS_neutralBuoyancyGroup")]
        public void RecalculateNeutralBuoyancy()
        {
            nextUpdateTime = 0;
            if (controllerWasActive)
                updateNeutralBuoyancy();
        }

        /// <summary>
        /// Gets the module display name.
        /// </summary>
        public override string GetModuleDisplayName()
        {
            return Localizer.Format("#LOC_SUNKWORKS_neutralBuoyancyGroup");
        }

        /// <summary>
        /// Gets the editor description.
        /// </summary>
        public override string GetInfo()
        {
            return Localizer.Format("#LOC_SUNKWORKS_neutralBuoyancyInfo");
        }

        /// <summary>
        /// Loads the persistent controlled-part ownership and original buoyancy values.
        /// </summary>
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            controlledParts.Clear();

            ConfigNode[] partNodes = node.GetNodes(kControlledPartNode);
            for (int index = 0; index < partNodes.Length; index++)
            {
                uint persistentId;
                float originalBuoyancy;
                float appliedBuoyancy;
                if (!uint.TryParse(partNodes[index].GetValue("persistentId"), NumberStyles.Integer, CultureInfo.InvariantCulture, out persistentId) ||
                    !float.TryParse(partNodes[index].GetValue("originalBuoyancy"), NumberStyles.Float, CultureInfo.InvariantCulture, out originalBuoyancy))
                    continue;

                if (!float.TryParse(partNodes[index].GetValue("appliedBuoyancy"), NumberStyles.Float, CultureInfo.InvariantCulture, out appliedBuoyancy))
                    appliedBuoyancy = originalBuoyancy;

                controlledParts[persistentId] = new ControlledPart
                {
                    persistentId = persistentId,
                    originalBuoyancy = originalBuoyancy,
                    appliedBuoyancy = appliedBuoyancy
                };
            }
        }

        /// <summary>
        /// Saves controlled-part ownership so docked visitors are not adopted after reload.
        /// </summary>
        public override void OnSave(ConfigNode node)
        {
            processEnabledState();
            base.OnSave(node);

            foreach (ControlledPart controlledPart in controlledParts.Values)
            {
                ConfigNode partNode = node.AddNode(kControlledPartNode);
                partNode.AddValue("persistentId", controlledPart.persistentId.ToString(CultureInfo.InvariantCulture));
                partNode.AddValue("originalBuoyancy", controlledPart.originalBuoyancy.ToString("R", CultureInfo.InvariantCulture));
                partNode.AddValue("appliedBuoyancy", controlledPart.appliedBuoyancy.ToString("R", CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Initializes the controller and its vessel-change notifications.
        /// </summary>
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            groundPart = part.FindModuleImplementing<ModuleGroundPart>();
            Fields["groundAnchorStatusDisplay"].guiActive = groundPart != null;
            updateInterval = Mathf.Max(kMinimumInterval, updateInterval);
            minimumSubmergedPortion = Mathf.Clamp01(minimumSubmergedPortion);
            minimumBuoyancy = Mathf.Max(0f, minimumBuoyancy);
            maximumBuoyancy = Mathf.Max(minimumBuoyancy, maximumBuoyancy);

            GameEvents.onVesselWasModified.Add(onVesselWasModified);
            GameEvents.OnEVAConstructionModePartAttached.Add(onEVAConstructionModePartAttached);
            GameEvents.OnEVAConstructionModePartDetached.Add(onEVAConstructionModePartDetached);

            previousEnabled = neutralBuoyancyEnabled;
            fieldStateInitialized = true;
            updateGroundAnchorStatus();
            processEnabledState();
        }

        /// <summary>
        /// Removes event handlers.
        /// </summary>
        public void OnDestroy()
        {
            GameEvents.onVesselWasModified.Remove(onVesselWasModified);
            GameEvents.OnEVAConstructionModePartAttached.Remove(onEVAConstructionModePartAttached);
            GameEvents.OnEVAConstructionModePartDetached.Remove(onEVAConstructionModePartDetached);
        }

        /// <summary>
        /// Detects toggle and ground-deployment transitions without performing the
        /// expensive buoyancy calculation every frame.
        /// </summary>
        public void Update()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            processEnabledState();
        }

        /// <summary>
        /// Performs the low-frequency automatic mass and buoyancy update.
        /// </summary>
        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || !controllerWasActive || vessel == null || vessel.packed || !vessel.loaded)
                return;
            if (Planetarium.GetUniversalTime() < nextUpdateTime)
                return;

            nextUpdateTime = Planetarium.GetUniversalTime() + updateInterval;
            updateNeutralBuoyancy();
        }

        void processEnabledState()
        {
            if (!fieldStateInitialized || !HighLogic.LoadedSceneIsFlight)
                return;

            bool groundReady = updateGroundAnchorStatus();
            bool shouldBeActive = neutralBuoyancyEnabled && groundReady;

            if (neutralBuoyancyEnabled != previousEnabled)
            {
                previousEnabled = neutralBuoyancyEnabled;
                if (neutralBuoyancyEnabled)
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_SUNKWORKS_neutralBuoyancyEnabledMessage"), kMessageDuration, ScreenMessageStyle.UPPER_CENTER);
            }

            if (shouldBeActive && !controllerWasActive)
            {
                controllerWasActive = true;
                disableDiveComputers();
                if (controlledParts.Count == 0)
                    captureVesselParts();
                restoreAppliedBuoyancy();
                statusDisplay = Localizer.Format("#LOC_SUNKWORKS_neutralBuoyancyCalculating");
                nextUpdateTime = 0;
                debugLog(" Activated with " + controlledParts.Count + " controlled parts.");
            }
            else if (!shouldBeActive && controllerWasActive)
            {
                controllerWasActive = false;
                restoreOriginalBuoyancy();
                if (!neutralBuoyancyEnabled)
                    controlledParts.Clear();
                controlledPartCount = controlledParts.Count;
                buoyancyErrorDisplay = "--";
                statusDisplay = neutralBuoyancyEnabled
                    ? Localizer.Format("#LOC_SUNKWORKS_neutralBuoyancyWaitingForAnchor")
                    : Localizer.Format("#LOC_SUNKWORKS_neutralBuoyancyDisabled");
                debugLog(" Deactivated; restored original buoyancy.");
            }
            else if (!neutralBuoyancyEnabled)
            {
                statusDisplay = Localizer.Format("#LOC_SUNKWORKS_neutralBuoyancyDisabled");
            }
            else if (!groundReady)
            {
                statusDisplay = Localizer.Format("#LOC_SUNKWORKS_neutralBuoyancyWaitingForAnchor");
            }

            Events["RecalculateNeutralBuoyancy"].active = controllerWasActive;
        }

        bool updateGroundAnchorStatus()
        {
            if (groundPart == null)
                return true;

            bool deployed = groundPart.Fields.GetValue<bool>("deployedOnGround");
            groundAnchorStatusDisplay = deployed
                ? Localizer.Format("#LOC_SUNKWORKS_groundAnchorDeployed")
                : Localizer.Format("#LOC_SUNKWORKS_groundAnchorNotDeployed");
            return deployed;
        }

        void captureVesselParts()
        {
            if (vessel == null)
                return;

            for (int index = 0; index < vessel.parts.Count; index++)
                addControlledPart(vessel.parts[index]);

            controlledPartCount = controlledParts.Count;
        }

        void addControlledPart(Part vesselPart)
        {
            if (vesselPart == null || controlledParts.ContainsKey(vesselPart.persistentId))
                return;

            controlledParts.Add(vesselPart.persistentId, new ControlledPart
            {
                persistentId = vesselPart.persistentId,
                originalBuoyancy = vesselPart.buoyancy,
                appliedBuoyancy = vesselPart.buoyancy
            });
        }

        void updateNeutralBuoyancy()
        {
            if (!controllerWasActive || vessel == null)
                return;

            disableDiveComputers();

            int adjustedParts = 0;
            int waitingParts = 0;
            int limitedParts = 0;
            double massTotal = 0;
            double buoyancyTotal = 0;
            CelestialBody body = vessel.mainBody;

            for (int index = 0; index < vessel.parts.Count; index++)
            {
                Part vesselPart = vessel.parts[index];
                ControlledPart controlledPart;
                if (!controlledParts.TryGetValue(vesselPart.persistentId, out controlledPart))
                    continue;

                PartBuoyancy partBuoyancy = vesselPart.GetComponent<PartBuoyancy>();
                if (partBuoyancy == null || partBuoyancy.displacement <= 0 ||
                    vesselPart.submergedPortion < minimumSubmergedPortion || body == null || !body.ocean)
                {
                    waitingParts++;
                    continue;
                }

                double depthScale = partBuoyancy.maxDepth >= PhysicsGlobals.BuoyancyScaleAboveDepth
                    ? 1.0
                    : Math.Max(0.0, partBuoyancy.maxDepth / PhysicsGlobals.BuoyancyScaleAboveDepth);
                double unitBuoyancy = partBuoyancy.displacement * body.oceanDensity * depthScale * PhysicsGlobals.BuoyancyScalar;
                if (unitBuoyancy <= 0)
                {
                    waitingParts++;
                    continue;
                }

                double partMass = vesselPart.mass + vesselPart.GetResourceMass();
                float desiredBuoyancy = (float)(partMass / unitBuoyancy);
                float clampedBuoyancy = Mathf.Clamp(desiredBuoyancy, minimumBuoyancy, maximumBuoyancy);
                if (!Mathf.Approximately(desiredBuoyancy, clampedBuoyancy))
                    limitedParts++;

                float denominator = Mathf.Max(Mathf.Abs(vesselPart.buoyancy), 0.001f);
                if (Mathf.Abs(clampedBuoyancy - vesselPart.buoyancy) / denominator >= kDefaultDeadband)
                    vesselPart.buoyancy = clampedBuoyancy;

                controlledPart.appliedBuoyancy = vesselPart.buoyancy;
                massTotal += partMass;
                buoyancyTotal += unitBuoyancy * vesselPart.buoyancy;
                adjustedParts++;
            }

            controlledPartCount = controlledParts.Count;
            double error = buoyancyTotal - massTotal;
            buoyancyErrorDisplay = error.ToString("+0.000;-0.000;0.000", CultureInfo.InvariantCulture) + " t";

            if (adjustedParts == 0)
                statusDisplay = Localizer.Format("#LOC_SUNKWORKS_neutralBuoyancyWaitingForWater");
            else if (limitedParts > 0)
                statusDisplay = Localizer.Format("#LOC_SUNKWORKS_neutralBuoyancyLimited", limitedParts.ToString());
            else if (waitingParts > 0)
                statusDisplay = Localizer.Format("#LOC_SUNKWORKS_neutralBuoyancyPartial", adjustedParts.ToString(), waitingParts.ToString());
            else
                statusDisplay = Localizer.Format("#LOC_SUNKWORKS_neutralBuoyancyActive");

            debugLog(" Update: controlled=" + controlledParts.Count +
                " adjusted=" + adjustedParts +
                " waiting=" + waitingParts +
                " limited=" + limitedParts +
                " mass=" + massTotal.ToString("F3", CultureInfo.InvariantCulture) +
                " buoyancy=" + buoyancyTotal.ToString("F3", CultureInfo.InvariantCulture) +
                " error=" + error.ToString("F4", CultureInfo.InvariantCulture) + "t");
        }

        void disableDiveComputers()
        {
            List<WBIDiveComputer> diveComputers = vessel.FindPartModulesImplementing<WBIDiveComputer>();
            bool disabledAny = false;
            for (int index = 0; index < diveComputers.Count; index++)
            {
                if (!diveComputers[index].divingControlEnabled)
                    continue;

                diveComputers[index].divingControlEnabled = false;
                disabledAny = true;
            }

            if (disabledAny)
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_SUNKWORKS_neutralBuoyancyDiveControlDisabled"), kMessageDuration, ScreenMessageStyle.UPPER_CENTER);
                debugLog(" Disabled " + diveComputers.Count + " vessel dive computer(s).");
            }
        }

        void restoreAppliedBuoyancy()
        {
            if (vessel == null)
                return;

            for (int index = 0; index < vessel.parts.Count; index++)
            {
                ControlledPart controlledPart;
                if (controlledParts.TryGetValue(vessel.parts[index].persistentId, out controlledPart))
                    vessel.parts[index].buoyancy = controlledPart.appliedBuoyancy;
            }
        }

        void restoreOriginalBuoyancy()
        {
            if (vessel == null)
                return;

            for (int index = 0; index < vessel.parts.Count; index++)
            {
                ControlledPart controlledPart;
                if (controlledParts.TryGetValue(vessel.parts[index].persistentId, out controlledPart))
                    vessel.parts[index].buoyancy = controlledPart.originalBuoyancy;
            }
        }

        void onVesselWasModified(Vessel modifiedVessel)
        {
            if (modifiedVessel == vessel)
                nextUpdateTime = 0;
        }

        void onEVAConstructionModePartAttached(Vessel hostVessel, Part attachedPart)
        {
            if (!controllerWasActive || hostVessel != vessel || attachedPart == null)
                return;

            addControlledPart(attachedPart);
            controlledPartCount = controlledParts.Count;
            nextUpdateTime = 0;
            debugLog(" Added EVA construction part " + attachedPart.partInfo.title + " (" + attachedPart.persistentId + ").");
        }

        void onEVAConstructionModePartDetached(Vessel hostVessel, Part detachedPart)
        {
            if (!controllerWasActive || detachedPart == null)
                return;

            ControlledPart controlledPart;
            if (!controlledParts.TryGetValue(detachedPart.persistentId, out controlledPart))
                return;

            detachedPart.buoyancy = controlledPart.originalBuoyancy;
            controlledParts.Remove(detachedPart.persistentId);
            controlledPartCount = controlledParts.Count;
            nextUpdateTime = 0;
            debugLog(" Removed EVA construction part " + detachedPart.partInfo.title + " (" + detachedPart.persistentId + ").");
        }
    }
}
