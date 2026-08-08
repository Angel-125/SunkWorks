using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KSP.Localization;
using SunkWorks.Submarine;
using UnityEngine;
using WildBlueCore.KerbalGear;

namespace SunkWorks.KerbalGear
{
    /// <summary>
    /// Controls the kerbal's buoyancy and swim speed, with the ability to increase diving depth when wearing the proper suit.
    /// Hard mode includes limited air supply. This module must be included in a KERBAL_EVA_MODULES config node, NOT in a kerbal config.
    /// </summary>
    /// <example>
    /// <code>
    /// KERBAL_EVA_MODULES
    /// {
    ///     MODULE
    ///     {
    ///         name = WBIModuleEVADiveComputer
    ///         maxPositiveBuoyancy = 1.1
    ///         buoyancyControlRate = 20
    ///         suitMaxPressures = wbiOBealeWetsuitM,3000;wbiOBealeWetsuitF,3000;wbiAtmoDivingSuitM,7000;wbiAtmoDivingSuitF,7000
    ///         holdBreathDuration = 360
    ///         drowningDuration = 10
    ///         airSupplyDuration = 3600
    ///         airRechargeRate = 600
    ///     }
    /// }
    /// </code>
    /// </example>
    public class WBIModuleEVADiveComputer : PartModule, IKerbalGearInventoryListener
    {
        #region Constants
        const float kVerticalSpeedTrigger = 0.005f;
        #endregion

        #region Fields
        /// <summary>
        /// Displays the buoyancy control state.
        /// </summary>
        [KSPField(guiActive = true, guiName = "#LOC_SUNKWORKS_scubaBuoyancyState", groupName = "#LOC_SUNKWORKS_scubaGearTitle", groupDisplayName = "#LOC_SUNKWORKS_scubaGearTitle")]
        public string buoyancyControlStateDisplay = string.Empty;

        /// <summary>
        /// In m/s, the rate at which a kerbal dives or ascends.
        /// </summary>
//        [KSPField(guiActive = true, guiName = "#LOC_SUNKWORKS_scubaDiveSpeed", guiFormat = "N2", guiUnits = "m/s", groupName = "#LOC_SUNKWORKS_scubaGearTitle", groupDisplayName = "#LOC_SUNKWORKS_scubaGearTitle")]
//        [UI_FloatRange(stepIncrement = 0.005f, minValue = 0.005f, maxValue = 1f)]
//        public float diveSpeed = 1f;

        /// <summary>
        /// Max positive buoyancy.
        /// </summary>
        [KSPField]
        public float maxPositiveBuoyancy = 1.1f;

        /// <summary>
        /// How fast to control buoyancy, in percentage per second.
        /// </summary>
        [KSPField]
        public float buoyancyControlRate = 20f;

        /// <summary>
        /// How much to multiply the swim speed by when this module is enabled.
        /// </summary>
        [KSPField]
        public float swimSpeedMultiplier = 2f;

        /// <summary>
        /// In kPA, the maximum pressure that the kerbal can take if he/she is wearing a designated suit.  
        /// Format: 'name of the suit','max pressure';'name of another suit','max pressure of the other suit'  
        /// NOTE: If a carried cargo part has an EVA_OVERRIDES node, then the values in that node will override the suit pressures.  
        /// The O'Beale suit enables diving to 300m on Kerbin, which is pretty close to the deepest dive record set by Ahmed Gabr in 2014.  
        /// The DeepSea suit enables kerbals to dive to 700m on Kerbin, which is akin to an Atmospheric Diving Suit that keeps its occupant at a pressure of 1atm.
        /// </summary>
        [KSPField]
        public string suitMaxPressures = string.Empty;

        #region Hard Mode Fields
        /// <summary>
        /// (Hard Mode) In seconds, how long a kerbal can hold is/her breath if the kerbal isn't wearing a helmet.
        /// If the kerbal runs out of breath then he/she will start drowning.
        /// </summary>
        public float holdBreathDuration = 360f;

        /// <summary>
        /// (Hard Mode) In seconds, how long a kerbal has to reach the surface before dying of drowing.
        /// </summary>
        public float drowningDuration = 10f;

        /// <summary>
        /// (Hard Mode) In seconds, how long the air supply lasts.
        /// This duration will be cut in half for every 10m of depth unless wearing an atmospheric diving suit.
        /// </summary>
        public float airSupplyDuration = 3600f;

        /// <summary>
        /// (Hard Mode) How many seconds of air supply to recarge per second of being on the surface.
        /// </summary>
        public float airRechargeRate = 600f;
        #endregion

        #endregion

        #region Housekeeping
        /// <summary>
        /// Current buoyancy level.
        /// </summary>
        [KSPField(isPersistant = true)]
        public float currentBuoyancy = 1f;

        /// <summary>
        /// Flag indicating if we should maintain depth.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool maintainDepth = false;

        KerbalEVA kerbalEVA;
        float originalSwimSpeed;
        float originalBuoyancy;
        double originalMaxPressure;
        float configuredMaxPositiveBuoyancy;
        float configuredSwimSpeedMultiplier;
        double maxPressureOverride = 0;
        BallastVentStates ventState = BallastVentStates.Closed;
        bool setInitialValues = false;
        bool isActive = false;

        /// <summary>
        /// Indicates whether this dive computer currently owns the EVA buoyancy overrides.
        /// </summary>
        internal bool IsDiveComputerActive => isActive;

        /// <summary>
        /// Scales stock EVA ragdoll buoyancy to match the ballast selected by this dive computer.
        /// A scale of one preserves stock behavior; zero removes ragdoll buoyancy.
        /// </summary>
        internal float RagdollBuoyancyScale
        {
            get
            {
                if (!isActive)
                    return 1f;

                if (currentBuoyancy <= Mathf.Epsilon)
                    return 0f;

                if (originalBuoyancy <= Mathf.Epsilon)
                    return currentBuoyancy;

                return Mathf.Max(0f, currentBuoyancy / originalBuoyancy);
            }
        }

        string ballastStateVentDisplay = string.Empty;
        string ballastStateMaintainDisplay = string.Empty;
        string ballastStateFillDisplay = string.Empty;
        string ballastStateClosedDisplay = string.Empty;
        string diveStateSurfacing = string.Empty;
        string diveStateDiving = string.Empty;

        Dictionary<string, float> divingSuitPressures = null;
        #endregion

        #region Events
        /// <summary>
        /// Floods ballast, sinking the kerbal.
        /// </summary>
        [KSPEvent(guiActive = true, guiName = "#LOC_SUNKWORKS_scubaSink", groupName = "#LOC_SUNKWORKS_scubaGearTitle", groupDisplayName = "#LOC_SUNKWORKS_scubaGearTitle")]
        public void Sink()
        {
            ventState = BallastVentStates.FloodingBallast;
            maintainDepth = false;
            updateUI();
        }

        /// <summary>
        /// Vents ballast, floating the kerbal.
        /// </summary>
        [KSPEvent(guiActive = true, guiName = "#LOC_SUNKWORKS_scubaSwim", groupName = "#LOC_SUNKWORKS_scubaGearTitle", groupDisplayName = "#LOC_SUNKWORKS_scubaGearTitle")]
        public void Swim()
        {
            ventState = BallastVentStates.VentingBallast;
            maintainDepth = false;
            updateUI();
        }

        /// <summary>
        /// Neutralizes buoyancy.
        /// </summary>
        [KSPEvent(guiActive = true, guiName = "#LOC_SUNKWORKS_scubaNeutral", groupName = "#LOC_SUNKWORKS_scubaGearTitle", groupDisplayName = "#LOC_SUNKWORKS_scubaGearTitle")]
        public void SetNeutralBuoyancy()
        {
            maintainDepth = true;
            updateUI();
        }
        #endregion

        #region Overrides
        /// <summary>
        /// Controls buoyancy over a fixed unit of time.
        /// </summary>
        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || kerbalEVA == null || vessel == null || part == null || !isActive)
                return;

            // Vessel.Splashed is not stable while an EVA kerbal is standing on underwater terrain.
            // Always enforce the selected ballast so a Landed/Splashed transition cannot restore stock buoyancy.
            part.buoyancy = currentBuoyancy;

            // WaterContact is the physical signal we care about. The altitude fallback covers the brief frame
            // in which KSP has changed situation but PartBuoyancy has not refreshed WaterContact yet.
            bool isUnderwater = vessel.mainBody != null && vessel.mainBody.ocean &&
                (part.WaterContact || vessel.altitude < 0.0);
            if (!isUnderwater)
                return;

            // Handle control inputs
            if (GameSettings.EVA_Pack_up.GetKey(false))
            {
                ventState = BallastVentStates.VentingBallast;
                buoyancyControlStateDisplay = diveStateSurfacing;
            }

            else if (GameSettings.EVA_Pack_down.GetKey(false))
            {
                ventState = BallastVentStates.FloodingBallast;
                buoyancyControlStateDisplay = diveStateDiving;
            }

            // Handle buoyancy control if we're maintaining depth.
            else if (maintainDepth)
            {
                buoyancyControlStateDisplay = ballastStateMaintainDisplay;

                // Do not react to collision jitter while resting on the seabed.
                if (part.GroundContact)
                    ventState = BallastVentStates.Closed;
                else if (vessel.verticalSpeed > kVerticalSpeedTrigger)
                    ventState = BallastVentStates.FloodingBallast;
                else if (vessel.verticalSpeed < -kVerticalSpeedTrigger)
                    ventState = BallastVentStates.VentingBallast;
                else
                    ventState = BallastVentStates.Closed;
            }

            else
            {
                updateUI();
            }

            // Reduce buoyancy
            if (ventState == BallastVentStates.FloodingBallast)
            {
                currentBuoyancy -= ((buoyancyControlRate / 100) * TimeWarp.fixedDeltaTime);
                if (currentBuoyancy <= 0f)
                {
                    currentBuoyancy = 0f;
                    ventState = BallastVentStates.Closed;
                    if (!maintainDepth)
                        updateUI();
                }
            }

            // Increase buoyancy
            else if (ventState == BallastVentStates.VentingBallast)
            {
                currentBuoyancy += ((buoyancyControlRate / 100) * TimeWarp.fixedDeltaTime);
                if (currentBuoyancy > maxPositiveBuoyancy)
                {
                    currentBuoyancy = maxPositiveBuoyancy;
                    ventState = BallastVentStates.Closed;
                    if (!maintainDepth)
                        updateUI();
                }
            }

            // Update part buoyancy.
            part.buoyancy = currentBuoyancy;
        }

        /// <summary>
        /// Overrides OnStart
        /// </summary>
        /// <param name="state">The StartState.</param>
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            kerbalEVA = part.FindModuleImplementing<KerbalEVA>();
            if (kerbalEVA == null)
                return;

            // Get original values
            originalSwimSpeed = kerbalEVA.swimSpeed;
            originalBuoyancy = part.buoyancy;
            originalMaxPressure = part.maxPressure;
            configuredMaxPositiveBuoyancy = maxPositiveBuoyancy;
            configuredSwimSpeedMultiplier = swimSpeedMultiplier;

            // Set buoyancy
            if (vessel.Splashed || vessel.altitude <= 0.0f)
            {
                currentBuoyancy = 0.5f;
                maintainDepth = true;
            }

            // Load max pressures for the diving suits
            divingSuitPressures = new Dictionary<string, float>();
            if (!string.IsNullOrEmpty(suitMaxPressures))
            {
                string[] suitPressures = suitMaxPressures.Split(new char[] { ';' });
                string[] suitPressure = null;
                char[] splitChar = new char[] { ',' };
                float suitMaxPressure = 400f;

                for (int index = 0; index < suitPressures.Length; index++)
                {
                    suitPressure = suitPressures[index].Split(splitChar);
                    if (suitPressure.Length != 2)
                        continue;
                    if (float.TryParse(suitPressure[1], out suitMaxPressure))
                        divingSuitPressures.Add(suitPressure[0], suitMaxPressure);
                }
            }

            refreshInventoryOverrides(kerbalEVA.ModuleInventoryPartReference);

            // Set initial values if needed.
            if (setInitialValues)
                applyActiveOverrides();

            // Update UI
            cacheLocalStrings();
            updateUI();
        }

        /// <summary>
        /// Overrides OnActive. Called when an inventory item is equipped and the module is enabled.
        /// </summary>
        public override void OnActive()
        {
            base.OnActive();

            setInitialValues = true;
            isActive = true;
            if (kerbalEVA != null)
            {
                refreshInventoryOverrides(kerbalEVA.ModuleInventoryPartReference);
                applyActiveOverrides();
            }

            Events["Sink"].active = true;
            Events["Swim"].active = true;
            Events["SetNeutralBuoyancy"].active = true;
            Fields["buoyancyControlStateDisplay"].guiActive = true;
        }

        /// <summary>
        /// Overrides OnInactive. Called when an inventory item is unequipped and the module is disabled.
        /// </summary>
        public override void OnInactive()
        {
            base.OnInactive();

            setInitialValues = false;
            isActive = false;

            Events["Sink"].active = false;
            Events["Swim"].active = false;
            Events["SetNeutralBuoyancy"].active = false;
            Fields["buoyancyControlStateDisplay"].guiActive = false;

            if (kerbalEVA == null)
                return;
            kerbalEVA.swimSpeed = originalSwimSpeed;
            part.buoyancy = originalBuoyancy;
            part.maxPressure = originalMaxPressure;
        }

        /// <summary>
        /// Recalculates gear-specific EVA overrides without cycling the active dive computer.
        /// </summary>
        /// <param name="changedInventory">The EVA inventory whose contents changed.</param>
        public void OnKerbalGearInventoryChanged(ModuleInventoryPart changedInventory)
        {
            if (!isActive || kerbalEVA == null || changedInventory == null ||
                changedInventory != kerbalEVA.ModuleInventoryPartReference)
            {
                return;
            }

            refreshInventoryOverrides(changedInventory);
            applyActiveOverrides();
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Rebuilds the maximum buoyancy, swim-speed, and pressure overrides from current inventory contents.
        /// </summary>
        /// <param name="inventory">The EVA inventory containing KerbalGear parts.</param>
        private void refreshInventoryOverrides(ModuleInventoryPart inventory)
        {
            maxPositiveBuoyancy = configuredMaxPositiveBuoyancy;
            swimSpeedMultiplier = configuredSwimSpeedMultiplier;
            maxPressureOverride = 0;

            if (inventory == null || inventory.storedParts.Count <= 0)
                return;

            int[] keys = inventory.storedParts.Keys.ToArray();
            for (int index = 0; index < keys.Length; index++)
                updatePartOverrides(inventory.storedParts[keys[index]].partName);
        }

        /// <summary>
        /// Applies the currently calculated inventory overrides while preserving the diver's ballast state.
        /// </summary>
        private void applyActiveOverrides()
        {
            if (kerbalEVA == null)
                return;

            kerbalEVA.swimSpeed = originalSwimSpeed * swimSpeedMultiplier;
            if (currentBuoyancy > maxPositiveBuoyancy)
                currentBuoyancy = maxPositiveBuoyancy;

            part.buoyancy = currentBuoyancy;
            updateMaxPressure();
        }

        /// <summary>
        /// Accumulates the strongest EVA overrides supplied by one carried cargo part.
        /// </summary>
        /// <param name="partName">The internal part name whose EVA_OVERRIDES node is inspected.</param>
        void updatePartOverrides(string partName)
        {
            // Get the part config
            AvailablePart availablePart = PartLoader.getPartInfoByName(partName);
            if (availablePart == null)
                return;
            ConfigNode node = availablePart.partConfig;
            if (node == null)
                return;

            // Get the EVA_OVERRIDES node
            if (!node.HasNode("EVA_OVERRIDES"))
                return;
            node = node.GetNode("EVA_OVERRIDES");

            // Get the overrides
            double pressureOverride = 0;
            float swimSpeedOverride = 0;
            float buoyancyOverride = 0;
            if (node.HasValue("buoyancy"))
                float.TryParse(node.GetValue("buoyancy"), out buoyancyOverride);
            if (node.HasValue("swimSpeedMultiplier"))
                float.TryParse(node.GetValue("swimSpeedMultiplier"), out swimSpeedOverride);
            if (node.HasValue("maxPressure"))
                double.TryParse(node.GetValue("maxPressure"), out pressureOverride);

            // Set the overrides
            if (buoyancyOverride > maxPositiveBuoyancy)
                maxPositiveBuoyancy = buoyancyOverride;

            if (swimSpeedOverride > swimSpeedMultiplier)
                swimSpeedMultiplier = swimSpeedOverride;

            if (pressureOverride > maxPressureOverride)
                maxPressureOverride = pressureOverride;
        }

        /// <summary>
        /// Applies the cargo override, configured suit limit, or original EVA pressure limit in priority order.
        /// </summary>
        void updateMaxPressure()
        {
            part.maxPressure = originalMaxPressure;
            if (maxPressureOverride > 0)
            {
                part.maxPressure = maxPressureOverride;
            }
            else if (divingSuitPressures != null)
            {
                List<ProtoCrewMember> vesselCrew = vessel.GetVesselCrew();
                ProtoCrewMember crew = vesselCrew.Count > 0 ? vesselCrew[0] : null;
                if (crew != null && divingSuitPressures.ContainsKey(crew.ComboId))
                    part.maxPressure = divingSuitPressures[crew.ComboId];
            }
        }

        void cacheLocalStrings()
        {
            ballastStateClosedDisplay = Localizer.Format("#LOC_SUNKWORKS_tankClosed");
            ballastStateFillDisplay = Localizer.Format("#LOC_SUNKWORKS_tankFilling");
            ballastStateVentDisplay = Localizer.Format("#LOC_SUNKWORKS_tankVenting");
            ballastStateMaintainDisplay = Localizer.Format("#LOC_SUNKWORKS_scubaDepthMaintain");
            diveStateSurfacing = Localizer.Format("#LOC_SUNKWORKS_diveStateSurfacing");
            diveStateDiving = Localizer.Format("#LOC_SUNKWORKS_diveStateDiving");
        }

        /// <summary>
        /// Updates the Part Action Window.
        /// </summary>
        protected virtual void updateUI()
        {
            switch (ventState)
            {
                case BallastVentStates.Closed:
                    buoyancyControlStateDisplay = ballastStateClosedDisplay;
                    break;

                case BallastVentStates.FloodingBallast:
                    buoyancyControlStateDisplay = ballastStateFillDisplay;
                    break;

                case BallastVentStates.VentingBallast:
                    buoyancyControlStateDisplay = ballastStateVentDisplay;
                    break;
            }
        }
        #endregion
    }
}
