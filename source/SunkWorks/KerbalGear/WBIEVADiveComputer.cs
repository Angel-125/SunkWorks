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
        public float buoyancyControlRate = 20f;

        /// <summary>
        /// How much to multiply the swim speed by when this module is enabled.
        /// </summary>
        public float swimSpeedMultiplier = 2f;

        /// <summary>
        /// A kerbal will die if diving below 50m without a helmet, and 400m with a helmet.
        /// Wearing an atmospheric diving suit can extend that depth limit.
        /// Wearing an atmospheric diving suit also negates hypothermia.
        /// </summary>
        public string atmoDiveSuitName = "wbiAtmoDiveSuit";

        /// <summary>
        /// In kPA, the maximum pressure that the kerbal can take if he/she is wearing an atmospheric diving suit.
        /// </summary>
        public float atmoSuitMaxPressure = 7000f;

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
        BallastVentStates ventState = BallastVentStates.Closed;
        bool setInitialValues = false;

        string ballastStateVentDisplay = string.Empty;
        string ballastStateMaintainDisplay = string.Empty;
        string ballastStateFillDisplay = string.Empty;
        string ballastStateClosedDisplay = string.Empty;
        string diveStateSurfacing = string.Empty;
        string diveStateDiving = string.Empty;
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
            if (!HighLogic.LoadedSceneIsFlight || kerbalEVA == null || !vessel.Splashed)
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
            part.buoyancy = currentBuoyancy;
            if (vessel.Splashed || vessel.altitude <= 0.0f)
            {
                currentBuoyancy = 0.5f;
                maintainDepth = true;
            }

            // Account for atmospheric diving suit
            if (!string.IsNullOrEmpty(atmoDiveSuitName))
            {

            }

            // Set initial values if needed.
            if (setInitialValues)
            {
                kerbalEVA.swimSpeed = originalSwimSpeed * swimSpeedMultiplier;
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

            Events["Sink"].active = false;
            Events["Swim"].active = false;
            Events["SetNeutralBuoyancy"].active = false;
            Fields["buoyancyControlStateDisplay"].guiActive = false;

            if (kerbalEVA == null)
                return;
            kerbalEVA.swimSpeed = originalSwimSpeed;
            part.buoyancy = originalBuoyancy;
        }
        #endregion

        #region Helpers
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
