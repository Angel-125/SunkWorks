using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KSP.Localization;
using SunkWorks.Submarine;

namespace SunkWorks.KerbalGear
{
    /// <summary>
    /// Controls the kerbal's buoyancy and swim speed, with the ability to increase diving depth when wearing the proper suit.
    /// Hard mode includes limited air supply.
    /// </summary>
    public class WBIEVADiveComputer: PartModule
    {
        #region Constants
        const float kVerticalSpeedTrigger = 0.005f;
        const string kKerbalDive = "SCUBADiveGroup";
        #endregion

        #region Fields
        /// <summary>
        /// Displays the buoyancy control state.
        /// </summary>
        [KSPField(guiActive = true, guiName = "#LOC_SUNKWORKS_scubaBuoyancyState")]
        public string buoyancyControlStateDisplay = string.Empty;

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
        /// </summary>
        [KSPField]
        public string suitMaxPressures = string.Empty;

        #region Hard Mode Fields
        /// <summary>
        /// In seconds, how long a kerbal can hold is/her breath if the kerbal isn't wearing a helmet.
        /// If the kerbal runs out of breath then he/she will start drowning.
        /// </summary>
        public float holdBreathDuration = 360f;

        /// <summary>
        /// In seconds, how long a kerbal has to reach the surface before dying of drowing.
        /// </summary>
        public float drowningDuration = 10f;

        /// <summary>
        /// In seconds, how long the air supply lasts.
        /// This duration will be cut in half for every 10m of depth unless wearing an atmospheric diving suit.
        /// </summary>
        public float airSupplyDuration = 3600f;

        /// <summary>
        /// How many seconds of air supply to recarge per second of being on the surface.
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
        double maxPressureOverride = 0;
        BallastVentStates ventState = BallastVentStates.Closed;
        bool setInitialValues = false;
        bool isActive = false;

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
        [KSPEvent(guiActive = true, guiName = "#LOC_SUNKWORKS_scubaSink")]
        public void Sink()
        {
            ventState = BallastVentStates.FloodingBallast;
            maintainDepth = false;
            updateUI();
        }

        /// <summary>
        /// Vents ballast, floating the kerbal.
        /// </summary>
        [KSPEvent(guiActive = true, guiName = "#LOC_SUNKWORKS_scubaSwim")]
        public void Swim()
        {
            ventState = BallastVentStates.VentingBallast;
            maintainDepth = false;
            updateUI();
        }

        /// <summary>
        /// Neutralizes buoyancy.
        /// </summary>
        [KSPEvent(guiActive = true, guiName = "#LOC_SUNKWORKS_scubaNeutral")]
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
            if (!HighLogic.LoadedSceneIsFlight || kerbalEVA == null || !vessel.Splashed || !isActive)
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

                if (vessel.verticalSpeed > kVerticalSpeedTrigger)
                    ventState = BallastVentStates.FloodingBallast;
                else if (vessel.verticalSpeed <= kVerticalSpeedTrigger)
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

            // Set buoyancy
            if (vessel.Splashed || vessel.altitude <= 0.0f)
            {
                currentBuoyancy = 0.5f;
                maintainDepth = true;
            }
            part.buoyancy = currentBuoyancy;

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

            // Load EVA overrides for carried cargo parts
            if (kerbalEVA.ModuleInventoryPartReference != null && kerbalEVA.ModuleInventoryPartReference.storedParts.Count > 0)
            {
                ModuleInventoryPart inventory = kerbalEVA.ModuleInventoryPartReference;
                int[] keys = inventory.storedParts.Keys.ToArray();

                for (int index = 0; index < keys.Length; index++)
                    updatePartOverrides(inventory.storedParts[keys[index]].partName);
            }

            // Set initial values if needed.
            if (setInitialValues)
            {
                kerbalEVA.swimSpeed = originalSwimSpeed * swimSpeedMultiplier;
                updateMaxPressure();
            }

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
            updateMaxPressure();

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
        #endregion

        #region Helpers
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

        void updateMaxPressure()
        {
            if (maxPressureOverride > 0)
            {
                part.maxPressure = maxPressureOverride;
            }
            else if (divingSuitPressures != null)
            {
                ProtoCrewMember crew = vessel.GetVesselCrew()[0];
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
