using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.Localization;
using System.Reflection;
using WildBlueCore;

namespace SunkWorks.Submarine
{
    /// <summary>
    /// A handy dive computer to help boats dive, surface, and maintain trim.
    /// </summary>
    /// <example>
    /// <code>
    /// MODULE
    /// {
    ///     name = WBIDiveComputer
    ///     debugMode = true
    ///     maxPressureOverride = 6000
    ///  }
    /// </code>
    /// </example>
    [KSPModule("Dive Computer")]
    public class WBIDiveComputer: WBIBasePartModule
    {
        #region Constants
        const float kMinBuoyancy = 0.01f;
        const float kMaxBuoyancy = 1f;
        const float kMsgDuration = 3.0f;
        const string kGroupName = "DiveComputer";
        #endregion

        #region GameEvents
        /// <summary>
        /// Indicates that the user has requested to flood the boat's ballast.
        /// </summary>
        public static EventData<Vessel, WBIDiveComputer> onFloodBallast = new EventData<Vessel, WBIDiveComputer>("onFloodBallast");

        /// <summary>
        /// Indicates that the user has requested to vent the boat's ballast.
        /// </summary>
        public static EventData<Vessel, WBIDiveComputer> onVentBallast = new EventData<Vessel, WBIDiveComputer>("onVentBallast");

        /// <summary>
        /// Indicates that the user has requested to close the boat's ballast vents.
        /// </summary>
        public static EventData<Vessel, WBIDiveComputer> onCloseVents = new EventData<Vessel, WBIDiveComputer>("onCloseVents");

        /// <summary>
        /// Indicates that the user has requested an emergency surface.
        /// </summary>
        public static EventData<Vessel, WBIDiveComputer> onEmergencySurface = new EventData<Vessel, WBIDiveComputer>("onEmergencySurface");

        /// <summary>
        /// Indicates that the user has requested a change to maintain depth.
        /// </summary>
        public static EventData<Vessel, WBIDiveComputer, bool> onMaintainDepthUpdated = new EventData<Vessel, WBIDiveComputer, bool>("onMaintainDepthUpdated");

        /// <summary>
        /// Indicates that the user has requested a change to maintain neutral buoyancy.
        /// </summary>
        public static EventData<Vessel, WBIDiveComputer, bool> onMaintainNeutralBuoyancyUpdated = new EventData<Vessel, WBIDiveComputer, bool>("onMaintainNeutralBuoyancyUpdated");

        /// <summary>
        /// Indicates that the user has requested a change to auto-trim.
        /// </summary>
        public static EventData<Vessel, WBIDiveComputer, bool> onAutoTrimUpdated = new EventData<Vessel, WBIDiveComputer, bool>("onAutoTrimUpdated");

        /// <summary>
        /// Indicates that the user has requested a change to dive control.
        /// </summary>
        public static EventData<Vessel, WBIDiveComputer, bool> onDiveControlUpdated = new EventData<Vessel, WBIDiveComputer, bool>("onDiveControlUpdated");

        /// <summary>
        /// Event to synchronize triggers and fluid rates
        /// </summary>
        public static EventData<Vessel, WBIDiveComputer> onTriggerAndFluidRatesUpdated = new EventData<Vessel, WBIDiveComputer>("onTriggerAndFluidRatesUpdated");
        #endregion

        #region Fields
        /// <summary>
        /// Indicates whether or not to automatically keep the boat level.
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_SUNKWORKS_autoTrim", isPersistant = true, groupName = kGroupName, groupDisplayName = "#LOC_SUNKWORKS_diveComputer")]
        [UI_Toggle(enabledText = "#LOC_SUNKWORKS_on", disabledText = "#LOC_SUNKWORKS_off")]
        public bool autoTrimEnabled;

        /// <summary>
        /// Indicates whether auto-trim has any configured trim tanks to control.
        /// </summary>
        [KSPField(guiActive = true, guiName = "#LOC_SUNKWORKS_trimStatus", groupName = kGroupName, groupDisplayName = "#LOC_SUNKWORKS_diveComputer")]
        public string trimStatusString = string.Empty;

        /// <summary>
        /// Indicates whether or not to enable dive control.
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_SUNKWORKS_divingControl", isPersistant = true, groupName = kGroupName, groupDisplayName = "#LOC_SUNKWORKS_diveComputer")]
        [UI_Toggle(enabledText = "#LOC_SUNKWORKS_on", disabledText = "#LOC_SUNKWORKS_off")]
        public bool divingControlEnabled;

        /// <summary>
        /// Indicates whether or not to maintain current depth
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "#LOC_SUNKWORKS_maintainDepth", isPersistant = true, groupName = kGroupName, groupDisplayName = "#LOC_SUNKWORKS_diveComputer")]
        [UI_Toggle(enabledText = "#LOC_SUNKWORKS_on", disabledText = "#LOC_SUNKWORKS_off")]
        public bool maintainDepth;

        /// <summary>
        /// Indicates whether the main ballast system should automatically balance vessel
        /// buoyancy with vessel mass without attempting to stop vertical motion.
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "#LOC_SUNKWORKS_maintainNeutralBuoyancy", isPersistant = true, groupName = kGroupName, groupDisplayName = "#LOC_SUNKWORKS_diveComputer")]
        [UI_Toggle(enabledText = "#LOC_SUNKWORKS_on", disabledText = "#LOC_SUNKWORKS_off")]
        public bool maintainNeutralBuoyancy;

        /// <summary>
        /// Current neutral-buoyancy controller status.
        /// </summary>
        [KSPField(guiActive = true, guiName = "#LOC_SUNKWORKS_neutralBuoyancyStatus", groupName = kGroupName, groupDisplayName = "#LOC_SUNKWORKS_diveComputer")]
        public string neutralBuoyancyStatusString = string.Empty;

        /// <summary>
        /// Current buoyancy minus vessel mass, expressed as tonnes-equivalent.
        /// </summary>
        [KSPField(guiActive = true, guiName = "#LOC_SUNKWORKS_neutralBuoyancyError", groupName = kGroupName, groupDisplayName = "#LOC_SUNKWORKS_diveComputer")]
        public string neutralBuoyancyErrorString = "--";

        /// <summary>
        /// Depth below sea level that the dive computer will maintain, in meters.
        /// A negative value indicates that no target has been captured yet.
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "#LOC_SUNKWORKS_targetDepth", guiFormat = "F1", guiUnits = " m", isPersistant = true, groupName = kGroupName, groupDisplayName = "#LOC_SUNKWORKS_diveComputer")]
        public double targetDepth = -1.0;

        /// <summary>
        /// Display string for current state of the dive computer
        /// </summary>
        [KSPField(guiActive = true, guiName = "#LOC_SUNKWORKS_diveState", groupName = kGroupName, groupDisplayName = "#LOC_SUNKWORKS_diveComputer")]
        public string diveStateString = string.Empty;

        /// <summary>
        /// Display string for current state of the dive computer
        /// </summary>
        [KSPField(guiActive = true, guiName = "#LOC_SUNKWORKS_hullIntegrity", guiFormat = "f1", guiUnits = "%", groupName = kGroupName, groupDisplayName = "#LOC_SUNKWORKS_diveComputer")]
        public double hullIntegrity;

        /// <summary>
        /// Current pitch angle of the boat.
        /// </summary>
        [KSPField(guiActive = true, guiName = "#LOC_SUNKWORKS_pitchAngle", guiFormat = "f1", groupName = kGroupName, groupDisplayName = "#LOC_SUNKWORKS_diveComputer")]
        public double pitchAngle;

        /// <summary>
        /// Current roll angle of the boat.
        /// </summary>
        [KSPField(guiActive = true, guiName = "#LOC_SUNKWORKS_rollAngle", guiFormat = "f1", groupName = kGroupName, groupDisplayName = "#LOC_SUNKWORKS_diveComputer")]
        public double rollAngle;

        /// <summary>
        /// Current vessel heading, used as the vessel's yaw diagnostic.
        /// </summary>
        [KSPField(guiActive = true, guiName = "Yaw/Heading", guiFormat = "f1", groupName = kGroupName, groupDisplayName = "#LOC_SUNKWORKS_diveComputer")]
        public double yawAngle;

        /// <summary>
        /// Roll angle that will trigger auto-trim. 0 is level, so anything that is +- this value will trigger auto-trim.
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_SUNKWORKS_rollAngleTrigger", isPersistant = true, groupName = kGroupName, groupDisplayName = "#LOC_SUNKWORKS_diveComputer")]
        [UI_FloatRange(maxValue = 5, minValue = 0.0f, scene = UI_Scene.All, stepIncrement = 0.05f)]
        public float rollAngleTrigger = 0.75f;

        /// <summary>
        /// Pitch angle that will trigger auto-trim. 0 is level, so anything that is +- this value will trigger auto-trim.
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_SUNKWORKS_pitchAngleTrigger", isPersistant = true, groupName = kGroupName, groupDisplayName = "#LOC_SUNKWORKS_diveComputer")]
        [UI_FloatRange(maxValue = 5, minValue = 0.0f, scene = UI_Scene.All, stepIncrement = 0.05f)]
        public float pitchAngleTrigger = 0.75f;

        /// <summary>
        /// If maintainDepth is enabled, then when the vertical speed reaches +- the speed trigger, the boat will attempt to maintain depth.
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_SUNKWORKS_verticalSpeedTrigger", isPersistant = true, groupName = kGroupName, groupDisplayName = "#LOC_SUNKWORKS_diveComputer")]
        [UI_FloatRange(maxValue = 1, minValue = 0.0f, scene = UI_Scene.All, stepIncrement = 0.01f)]
        public float verticalSpeedTrigger = 0.1f;

        /// <summary>
        /// Roll-trim's fluid transfer rate (percent)
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_SUNKWORKS_rollFluidRate", isPersistant = true, groupName = kGroupName, groupDisplayName = "#LOC_SUNKWORKS_diveComputer")]
        [UI_FloatRange(maxValue = 100f, minValue = 0.0f, scene = UI_Scene.All, stepIncrement = 1f)]
        public float rollFluidRate = 100f;

        /// <summary>
        /// Pitch-trim's fluid transfer rate (percent)
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_SUNKWORKS_pitchFluidRate", isPersistant = true, groupName = kGroupName, groupDisplayName = "#LOC_SUNKWORKS_diveComputer")]
        [UI_FloatRange(maxValue = 100f, minValue = 0.0f, scene = UI_Scene.All, stepIncrement = 1f)]
        public float pitchFluidRate = 100f;

        /// <summary>
        /// Ballast's fluid transfer rate (percent)
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_SUNKWORKS_ballastFluidRate", isPersistant = true, groupName = kGroupName, groupDisplayName = "#LOC_SUNKWORKS_diveComputer")]
        [UI_FloatRange(maxValue = 100f, minValue = 0.0f, scene = UI_Scene.All, stepIncrement = 1f)]
        public float ballastFluidRate = 100f;

        /// <summary>
        /// Current vent state of the boat's ballast system.
        /// </summary>
        [KSPField(isPersistant = true)]
        public BallastVentStates ventState;

        /// <summary>
        /// Override maximum pressure in kPA. Parts have a default of 4000kPA, which gives them a collapse death of 400m on Kerbin.
        /// This override gives you a way to alter that collapse depth without modifying individual parts. If multiple
        /// dive computers are found on the boat, then the highest max pressure will be used.
        /// If there is a mismatch between the part's maxPressure and the dive computer's maxPressureOverride, then both
        /// will be set to the highest value.
        /// </summary>
        [KSPField]
        public double maxPressureOverride = 6000.0f;

        /// <summary>
        /// Min controlled buoyancy for buoyancy controlled parts.
        /// </summary>
        [KSPField]
        public float minControlledBuoyancy = 0.25f;

        /// <summary>
        /// Converts depth error into a desired vertical speed.
        /// </summary>
        [KSPField]
        public float depthGain = 0.25f;

        /// <summary>
        /// Converts vertical-speed error into ballast transfer percentage.
        /// </summary>
        [KSPField]
        public float verticalSpeedGain = 100f;

        /// <summary>
        /// Maximum ascent or descent speed requested by depth hold.
        /// </summary>
        [KSPField]
        public float maxDepthHoldSpeed = 0.5f;

        /// <summary>
        /// Depth error, in meters, inside which only vertical-speed damping is used.
        /// </summary>
        [KSPField]
        public float depthDeadband = 0.1f;

        /// <summary>
        /// Time constant used to smooth the vertical-speed signal.
        /// </summary>
        [KSPField]
        public float verticalSpeedFilterTime = 0.5f;

        /// <summary>
        /// Number of seconds between neutral-buoyancy controller updates.
        /// </summary>
        [KSPField]
        public float neutralBuoyancyUpdateInterval = 0.5f;

        /// <summary>
        /// Fraction of vessel mass inside which neutral buoyancy closes the main ballast vents.
        /// </summary>
        [KSPField]
        public float neutralBuoyancyDeadband = 0.001f;

        /// <summary>
        /// Converts fractional buoyancy error into main-ballast transfer percentage.
        /// </summary>
        [KSPField]
        public float neutralBuoyancyGain = 1000f;

        /// <summary>
        /// Minimum submerged fraction required for every buoyant part before neutral control runs.
        /// </summary>
        [KSPField]
        public float neutralMinimumSubmergedPortion = 0.95f;

        /// <summary>
        /// Number of seconds of angular motion used to damp trim commands.
        /// </summary>
        [KSPField]
        public float trimRateDamping = 0.35f;

        /// <summary>
        /// Time constant used to smooth measured pitch and roll rates.
        /// </summary>
        [KSPField]
        public float trimRateFilterTime = 0.25f;

        /// <summary>
        /// Number of real-time seconds between periodic diagnostic snapshots.
        /// Set to zero to log every physics update.
        /// </summary>
        [KSPField]
        public float debugLogInterval = 0.5f;
        #endregion

        #region Housekeeping
        /// <summary>
        /// Debug maneuver states
        /// </summary>
        [KSPField(guiName = "Roll/Pitch/Yaw", guiFormat = "n3", groupName = kGroupName, groupDisplayName = "#LOC_SUNKWORKS_diveComputer")]
        protected Vector3 maneuverState = Vector3.zero;

        /// <summary>
        /// Flag to indicate that the vessel is maneuvering
        /// </summary>
        public bool vesselIsManeuvering;

        internal bool wasMaintainingDepth;
        internal bool wasMaintainingNeutralBuoyancy;
        internal bool wasAutoTrimming;
        internal bool divingControlWasEnabled;
        List<WBIBallastTank> ballastTanks;
        Dictionary<WBIBallastTank, BallastTankTypes> knownTankTypes = new Dictionary<WBIBallastTank, BallastTankTypes>();
        List<WBIDiveComputer> diveComputers;
        List<Part> buoyancyControlledParts;
        int buoyancyPartCount;
        int partCount;
        internal bool pitchControlActive;
        internal bool rollControlActive;
        internal float prevBallastFluidRate;
        internal float prevRollAngleTrigger;
        internal float prevPitchAngleTrigger;
        internal float prevVerticalSpeedTrigger;
        internal float prevRollFluidRate;
        internal float prevPitchFluidRate;
        /// <summary>
        /// Last vessel-wide buoyancy applied to parts that are not ballast tanks. Persisting
        /// this value lets a disabled dive computer restore the saved buoyancy without running
        /// the ballast controller during vessel load.
        /// </summary>
        [KSPField(isPersistant = true)]
        public float prevBuoyancy = -1f;
        double filteredVerticalSpeed;
        double previousPitchAngle;
        double previousRollAngle;
        double filteredPitchRate;
        double filteredRollRate;
        bool attitudeRateInitialized;
        bool depthHoldWasSuspended;
        bool trimTanksAvailable;
        bool pitchTrimAvailable;
        bool rollTrimAvailable;
        bool missingTrimWarningDisplayed;
        float nextDebugLogTime;
        double diagnosticCurrentDepth;
        double diagnosticDepthError;
        double diagnosticDesiredVerticalSpeed;
        double diagnosticVelocityError;
        float diagnosticDepthCommand;
        float diagnosticPitchCommand;
        float diagnosticRollCommand;
        string diagnosticDepthState = "Not evaluated";
        string diagnosticNeutralState = "Not evaluated";
        string diagnosticTrimState = "Not evaluated";
        double diagnosticNeutralMass;
        double diagnosticNeutralBuoyancy;
        double diagnosticNeutralError;
        float diagnosticNeutralCommand;
        double nextNeutralBuoyancyUpdateTime;
        bool restoreSavedBuoyancy = true;
        #endregion

        #region Events
        /// <summary>
        /// Floods the ballast tank
        /// </summary>
        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "#LOC_SUNKWORKS_floodBallast", groupName = kGroupName, groupDisplayName = "#LOC_SUNKWORKS_diveComputer")]
        public void FloodBallast()
        {
            if (!IsDiveControlEnabled(part.vessel))
                return;

            // If we aren't the active dive computer then fire the flood ballast event and exit.
            if (!isActiveDiveComputer)
            {
                onFloodBallast.Fire(part.vessel, this);
                return;
            }

            floodBallast();
        }
        void floodBallast()
        {
            if (ballastTanks == null)
                return;

            debugLog(" Manual flood-ballast command received; depth hold and neutral buoyancy will be disabled, and auto-trim remains " + (autoTrimEnabled ? "enabled." : "disabled."));

            ventState = BallastVentStates.FloodingBallast;

            prevBallastFluidRate = ballastFluidRate;
            updateBallastTanksVentState();
            maintainDepth = false;
            wasMaintainingDepth = false;
            maintainNeutralBuoyancy = false;
            wasMaintainingNeutralBuoyancy = false;

        }

        /// <summary>
        /// Vents ballast tank
        /// </summary>
        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "#LOC_SUNKWORKS_ventBallast", groupName = kGroupName, groupDisplayName = "#LOC_SUNKWORKS_diveComputer")]
        public void VentBallast()
        {
            if (!IsDiveControlEnabled(part.vessel))
                return;

            // If we aren't the active dive computer then fire the vent ballast event and exit.
            if (!isActiveDiveComputer)
            {
                onVentBallast.Fire(part.vessel, this);
                return;
            }

            ventBallast();
        }
        void ventBallast()
        {
            if (ballastTanks == null)
                return;

            debugLog(" Manual vent-ballast command received; depth hold and neutral buoyancy will be disabled, and auto-trim remains " + (autoTrimEnabled ? "enabled." : "disabled."));

            ventState = BallastVentStates.VentingBallast;

            prevBallastFluidRate = ballastFluidRate;
            updateBallastTanksVentState();
            maintainDepth = false;
            wasMaintainingDepth = false;
            maintainNeutralBuoyancy = false;
            wasMaintainingNeutralBuoyancy = false;
        }

        /// <summary>
        /// Close ballast vents
        /// </summary>
        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "#LOC_SUNKWORKS_closeVents", groupName = kGroupName, groupDisplayName = "#LOC_SUNKWORKS_diveComputer")]
        public void CloseVents()
        {
            if (!IsDiveControlEnabled(part.vessel))
                return;

            // If we aren't the active dive computer then fire the close vents event and exit.
            if (!isActiveDiveComputer)
            {
                onCloseVents.Fire(part.vessel, this);
                return;
            }

            closeVents();
        }
        void closeVents()
        {
            if (ballastTanks == null)
                return;

            debugLog(" Close main ballast vents command received.");

            ventState = BallastVentStates.Closed;

            updateBallastTanksVentState();
            maintainNeutralBuoyancy = false;
            wasMaintainingNeutralBuoyancy = false;
        }

        /// <summary>
        /// Activates emergency surface, telling all ballast tanks to immediately dump their ballast. This affects parts marked as ballast or trim tanks.
        /// </summary>
        [KSPEvent(guiActive = true, guiName = "#LOC_SUNKWORKS_emergencySurface", groupName = kGroupName, groupDisplayName = "#LOC_SUNKWORKS_diveComputer")]
        public void EmergencySurface()
        {
            if (!IsDiveControlEnabled(part.vessel))
                return;

            // If we aren't the active dive computer then fire the emergency surface event and exit.
            if (!isActiveDiveComputer)
            {
                onEmergencySurface.Fire(part.vessel, this);
                return;
            }

            emergencySurface();
        }
        void emergencySurface()
        {
            if (ballastTanks == null)
                return;

            debugLog(" Emergency surface command received; dumping all main and trim ballast.");

            int count = ballastTanks.Count;
            for (int index = 0; index < count; index++)
            {
                ballastTanks[index].ventState = BallastVentStates.Closed;
                ballastTanks[index].DumpBallast();
            }

            maintainDepth = false;
            wasMaintainingDepth = false;
            maintainNeutralBuoyancy = false;
            wasMaintainingNeutralBuoyancy = false;
        }
        #endregion

        #region Actions
        /// <summary>
        /// Action to flood ballast tank
        /// </summary>
        /// <param name="param"></param>
        [KSPAction("#LOC_SUNKWORKS_floodBallast")]
        public void FloodBallastAction(KSPActionParam param)
        {
            FloodBallast();
        }

        /// <summary>
        /// Action to vent ballast tank
        /// </summary>
        /// <param name="param"></param>
        [KSPAction("#LOC_SUNKWORKS_ventBallast")]
        public void VentBallastAction(KSPActionParam param)
        {
            VentBallast();
        }

        /// <summary>
        /// Close ballast vents action
        /// </summary>
        /// <param name="param"></param>
        [KSPAction("#LOC_SUNKWORKS_closeVents")]
        public void CloseVentsAction(KSPActionParam param)
        {
            CloseVents();
        }

        /// <summary>
        /// Emergency surface action
        /// </summary>
        /// <param name="param"></param>
        [KSPAction("#LOC_SUNKWORKS_emergencySurface")]
        public void EmergencySurfaceAction(KSPActionParam param)
        {
            EmergencySurface();
        }

        /// <summary>
        /// Toggle maintain depth action
        /// </summary>
        /// <param name="param"></param>
        [KSPAction("#LOC_SUNKWORKS_toggleMaintainDepthAction")]
        public void ToggleMaintainDepthAction(KSPActionParam param)
        {
            maintainDepth = !maintainDepth;
            if (maintainDepth)
                maintainNeutralBuoyancy = false;
            debugLog(" Maintain-depth action changed state to " + maintainDepth + ".");
            string message = maintainDepth ? Localizer.Format("#LOC_SUNKWORKS_toggleMaintainDepthActionOn") : Localizer.Format("#LOC_SUNKWORKS_toggleMaintainDepthActionOff");
            ScreenMessages.PostScreenMessage(message, kMsgDuration, ScreenMessageStyle.UPPER_CENTER);

            if (!isActiveDiveComputer)
                onMaintainDepthUpdated.Fire(part.vessel, this, maintainDepth);
        }

        /// <summary>
        /// Toggle maintain neutral buoyancy action.
        /// </summary>
        [KSPAction("#LOC_SUNKWORKS_toggleMaintainNeutralBuoyancyAction")]
        public void ToggleMaintainNeutralBuoyancyAction(KSPActionParam param)
        {
            maintainNeutralBuoyancy = !maintainNeutralBuoyancy;
            if (maintainNeutralBuoyancy)
                maintainDepth = false;
            debugLog(" Maintain-neutral-buoyancy action changed state to " + maintainNeutralBuoyancy + ".");
            string message = maintainNeutralBuoyancy
                ? Localizer.Format("#LOC_SUNKWORKS_toggleMaintainNeutralBuoyancyActionOn")
                : Localizer.Format("#LOC_SUNKWORKS_toggleMaintainNeutralBuoyancyActionOff");
            ScreenMessages.PostScreenMessage(message, kMsgDuration, ScreenMessageStyle.UPPER_CENTER);

            if (!isActiveDiveComputer)
                onMaintainNeutralBuoyancyUpdated.Fire(part.vessel, this, maintainNeutralBuoyancy);
        }

        /// <summary>
        /// Toggle auto trim action
        /// </summary>
        /// <param name="param"></param>
        [KSPAction("#LOC_SUNKWORKS_toggleAutoTrimAction")]
        public void ToggleAutoTrimAction(KSPActionParam param)
        {
            autoTrimEnabled = !autoTrimEnabled;
            debugLog(" Auto-trim action changed state to " + autoTrimEnabled + ".");
            string message = autoTrimEnabled ? Localizer.Format("#LOC_SUNKWORKS_toggleAutoTrimActionOn") : Localizer.Format("#LOC_SUNKWORKS_toggleAutoTrimActionOff");
            ScreenMessages.PostScreenMessage(message, kMsgDuration, ScreenMessageStyle.UPPER_CENTER);

            if (!isActiveDiveComputer)
                onAutoTrimUpdated.Fire(part.vessel, this, autoTrimEnabled);
        }
        #endregion

        #region API
        /// <summary>
        /// Determines whether or not the computer is the active computer on the vessel that is controlling the dive.
        /// </summary>
        public bool isActiveDiveComputer
        {
            get
            {
                return diveComputers[0] == this;
            }
        }

        /// <summary>
        /// Returns whether the vessel's master dive computer permits ballast updates. Vessels
        /// without a dive computer retain normal standalone ballast-tank behavior.
        /// </summary>
        public static bool IsDiveControlEnabled(Vessel vessel)
        {
            if (vessel == null)
                return true;

            List<WBIDiveComputer> vesselDiveComputers = vessel.FindPartModulesImplementing<WBIDiveComputer>();
            if (vesselDiveComputers == null || vesselDiveComputers.Count == 0)
                return true;

            return vesselDiveComputers[0].divingControlEnabled;
        }
        #endregion

        #region Overrides
        public override string GetModuleDisplayName()
        {
            return Localizer.Format("#LOC_SUNKWORKS_diveComputer");
        }

        public override string GetInfo()
        {
            return Localizer.Format("#LOC_SUNKWORKS_diveComputerInfo", new string[1] { string.Format("{0:n1}", maxPressureOverride) });
        }

        public override void OnStart(StartState state)
        {
            // WBIBasePartModule.OnAwake applies WildBlueCore's global debug setting.
            // The dive computer's diagnostics are intentionally controlled by its
            // own part-module config, so restore that explicit value (defaulting to
            // false when the field is omitted) before any diagnostic logging occurs.
            resolveDebugMode();

            //Get max pressure
            if (part.maxPressure > maxPressureOverride)
                maxPressureOverride = part.maxPressure;
            else if (maxPressureOverride > part.maxPressure)
                part.maxPressure = maxPressureOverride;

            base.OnStart(state);
            Fields["maintainDepth"].guiActiveEditor = false;
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            // Get our dive-controlled parts
            buoyancyControlledParts = new List<Part>();
            restoreSavedBuoyancy = true;
            updateDiveControlledParts();

            //Previous states
            wasAutoTrimming = autoTrimEnabled;
            wasMaintainingDepth = maintainDepth;
            wasMaintainingNeutralBuoyancy = maintainNeutralBuoyancy;
            prevBallastFluidRate = ballastFluidRate;
            prevRollAngleTrigger = rollAngleTrigger;
            prevPitchAngleTrigger = pitchAngleTrigger;
            prevVerticalSpeedTrigger = verticalSpeedTrigger;
            prevRollFluidRate = rollFluidRate;
            prevPitchFluidRate = pitchFluidRate;

            setupGUI();

            // Setup dive computer events
            onFloodBallast.Add(floodBallastEvent);
            onVentBallast.Add(ventBallastEvent);
            onCloseVents.Add(closeVentsEvent);
            onEmergencySurface.Add(emergencySurfaceEvent);
            onMaintainDepthUpdated.Add(maintainDepthUpdatedEvent);
            onMaintainNeutralBuoyancyUpdated.Add(maintainNeutralBuoyancyUpdatedEvent);
            onAutoTrimUpdated.Add(autoTrimUpdatedEvent);
            onDiveControlUpdated.Add(diveControlUpdatedEvent);
            onTriggerAndFluidRatesUpdated.Add(triggerAndFluidRatesUpdated);
            WBIBallastTank.onBallastTankUpdated.Add(ballastTankUpdatedEvent);

            debugLog(" OnStart: autoTrim=" + autoTrimEnabled +
                " maintainDepth=" + maintainDepth +
                " maintainNeutralBuoyancy=" + maintainNeutralBuoyancy +
                " divingControl=" + divingControlEnabled +
                " pitchTrigger=" + pitchAngleTrigger.ToString("F3") +
                " rollTrigger=" + rollAngleTrigger.ToString("F3") +
                " pitchRate=" + pitchFluidRate.ToString("F1") + "%" +
                " rollRate=" + rollFluidRate.ToString("F1") + "%" +
                " diagnosticInterval=" + debugLogInterval.ToString("F2") + "s");
        }

        public void OnDestroy()
        {
            onFloodBallast.Remove(floodBallastEvent);
            onVentBallast.Remove(ventBallastEvent);
            onCloseVents.Remove(closeVentsEvent);
            onEmergencySurface.Remove(emergencySurfaceEvent);
            onMaintainDepthUpdated.Remove(maintainDepthUpdatedEvent);
            onMaintainNeutralBuoyancyUpdated.Remove(maintainNeutralBuoyancyUpdatedEvent);
            onAutoTrimUpdated.Remove(autoTrimUpdatedEvent);
            onDiveControlUpdated.Remove(diveControlUpdatedEvent);
            onTriggerAndFluidRatesUpdated.Remove(triggerAndFluidRatesUpdated);
            WBIBallastTank.onBallastTankUpdated.Remove(ballastTankUpdatedEvent);
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            if (part.ShieldedFromAirstream)
                return;

            // Update the input parameters like roll and pitch angle.
            // All dive computers need to do this.
            updateInputParameters();

            // Update the dive controlled parts.
            // All dive computers need to do this.
            updateDiveControlledParts();
            updateTrimTankAvailability(isActiveDiveComputer);

            // Process the master switch before checking for tanks so it remains authoritative
            // even on a docked vessel or base that currently has no ballast tanks.
            if (isActiveDiveComputer && !divingControlEnabled)
            {
                diagnosticDepthState = maintainDepth ? "Suspended: diving control disabled" : "Disabled";
                diagnosticNeutralState = maintainNeutralBuoyancy ? "Suspended: diving control disabled" : "Disabled";
                neutralBuoyancyStatusString = maintainNeutralBuoyancy
                    ? Localizer.Format("#LOC_SUNKWORKS_neutralBuoyancySuspendedDiveControl")
                    : Localizer.Format("#LOC_SUNKWORKS_neutralBuoyancyDisabled");
                diagnosticTrimState = autoTrimEnabled ? "Suspended: diving control disabled" : "Disabled";
                diagnosticDepthCommand = 0;
                diagnosticNeutralCommand = 0;
                diagnosticPitchCommand = 0;
                diagnosticRollCommand = 0;
                pitchControlActive = false;
                rollControlActive = false;
                depthHoldWasSuspended = maintainDepth;
                attitudeRateInitialized = false;
                syncDiveControlComputers();
                logDebugDiagnostics();
                return;
            }
            if (!isActiveDiveComputer && divingControlEnabled != divingControlWasEnabled)
            {
                divingControlWasEnabled = divingControlEnabled;
                onDiveControlUpdated.Fire(part.vessel, this, divingControlEnabled);
            }

            if (ballastTanks == null || ballastTanks.Count == 0)
            {
                diagnosticDepthState = "Unavailable: no ballast tanks";
                diagnosticNeutralState = "Unavailable: no main ballast tanks";
                neutralBuoyancyStatusString = Localizer.Format("#LOC_SUNKWORKS_neutralBuoyancyNoMainBallast");
                neutralBuoyancyErrorString = "--";
                diagnosticTrimState = "Unavailable: no ballast tanks";
                if (isActiveDiveComputer)
                    syncDiveControlComputers();
                logDebugDiagnostics();
                return;
            }

            // Only the active dive computer handles ballast and diving control.
            if (isActiveDiveComputer)
            {
                // Update ballast state
                updateBallastState();
                if (ballastFluidRate != prevBallastFluidRate)
                    updateBallastTanksVentState();

                // Check to see if the vessel is maneuvering.
                updateManeuverState();

                // Update trim if needed.
                updateTrimState();

                resolveBuoyancyControlMode();

                // Maintain depth if needed.
                updateDepthState();

                // Maintain neutral buoyancy if needed. This intentionally runs after depth
                // selection so the two mutually-exclusive modes cannot issue competing commands.
                updateNeutralBuoyancyState();

                // Sync other dive computers
                syncDiveControlComputers();

                // Emit a rate-limited controller snapshot when debugging is enabled.
                logDebugDiagnostics();
            }

            // Non-active dive computers monitor their control inputs and inform the master diver of state changes.
            else
            {
                if (autoTrimEnabled != wasAutoTrimming)
                {
                    wasAutoTrimming = autoTrimEnabled;
                    onAutoTrimUpdated.Fire(part.vessel, this, autoTrimEnabled);
                }
                if (maintainDepth != wasMaintainingDepth)
                {
                    wasMaintainingDepth = maintainDepth;
                    onMaintainDepthUpdated.Fire(part.vessel, this, maintainDepth);
                }
                if (maintainNeutralBuoyancy != wasMaintainingNeutralBuoyancy)
                {
                    wasMaintainingNeutralBuoyancy = maintainNeutralBuoyancy;
                    onMaintainNeutralBuoyancyUpdated.Fire(part.vessel, this, maintainNeutralBuoyancy);
                }

                // These changes just need a single event to update triggers and fluid rates.
                if (!rollAngleTrigger.Equals(prevRollAngleTrigger))
                {
                    prevRollAngleTrigger = rollAngleTrigger;
                    onTriggerAndFluidRatesUpdated.Fire(part.vessel, this);
                }
                if (!pitchAngleTrigger.Equals(prevPitchAngleTrigger))
                {
                    prevPitchAngleTrigger = pitchAngleTrigger;
                    onTriggerAndFluidRatesUpdated.Fire(part.vessel, this);
                }
                if (!verticalSpeedTrigger.Equals(prevVerticalSpeedTrigger))
                {
                    prevVerticalSpeedTrigger = verticalSpeedTrigger;
                    onTriggerAndFluidRatesUpdated.Fire(part.vessel, this);
                }
                if (!rollFluidRate.Equals(prevRollFluidRate))
                {
                    prevRollFluidRate = rollFluidRate;
                    onTriggerAndFluidRatesUpdated.Fire(part.vessel, this);
                }
                if (!pitchFluidRate.Equals(prevPitchFluidRate))
                {
                    prevPitchFluidRate = pitchFluidRate;
                    onTriggerAndFluidRatesUpdated.Fire(part.vessel, this);
                }
                if (!ballastFluidRate.Equals(prevBallastFluidRate))
                {
                    prevBallastFluidRate = ballastFluidRate;
                    onTriggerAndFluidRatesUpdated.Fire(part.vessel, this);
                }
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (HighLogic.LoadedSceneIsFlight && vessel != null)
                updateGUI();
        }
        #endregion

        #region GameEventHandlers
        void floodBallastEvent(Vessel origin, WBIDiveComputer diveComputer)
        {
            if (origin != part.vessel || !isActiveDiveComputer || !divingControlEnabled)
                return;

            floodBallast();
        }

        void ventBallastEvent(Vessel origin, WBIDiveComputer diveComputer)
        {
            if (origin != part.vessel || !isActiveDiveComputer || !divingControlEnabled)
                return;

            ventBallast();
        }

        void closeVentsEvent(Vessel origin, WBIDiveComputer diveComputer)
        {
            if (origin != part.vessel || !isActiveDiveComputer || !divingControlEnabled)
                return;

            closeVents();
        }

        void emergencySurfaceEvent(Vessel origin, WBIDiveComputer diveComputer)
        {
            if (origin != part.vessel || !isActiveDiveComputer || !divingControlEnabled)
                return;

            emergencySurface();
        }

        void maintainDepthUpdatedEvent(Vessel origin, WBIDiveComputer diveComputer, bool isEnabled)
        {
            if (origin != part.vessel || !isActiveDiveComputer)
                return;

            maintainDepth = isEnabled;
            if (maintainDepth)
                maintainNeutralBuoyancy = false;
        }

        void maintainNeutralBuoyancyUpdatedEvent(Vessel origin, WBIDiveComputer diveComputer, bool isEnabled)
        {
            if (origin != part.vessel || !isActiveDiveComputer)
                return;

            maintainNeutralBuoyancy = isEnabled;
            if (maintainNeutralBuoyancy)
                maintainDepth = false;
        }

        void autoTrimUpdatedEvent(Vessel origin, WBIDiveComputer diveComputer, bool isEnabled)
        {
            if (origin != part.vessel || !isActiveDiveComputer)
                return;

            autoTrimEnabled = isEnabled;
        }

        void diveControlUpdatedEvent(Vessel origin, WBIDiveComputer diveComputer, bool isEnabled)
        {
            if (origin != part.vessel || !isActiveDiveComputer)
                return;

            divingControlEnabled = isEnabled;
        }

        void triggerAndFluidRatesUpdated(Vessel origin, WBIDiveComputer diveComputer)
        {
            if (origin != part.vessel || !isActiveDiveComputer)
                return;

            ballastFluidRate = diveComputer.ballastFluidRate;
            prevBallastFluidRate = ballastFluidRate;

            rollFluidRate = diveComputer.rollFluidRate;
            prevRollFluidRate = rollFluidRate;

            pitchFluidRate = diveComputer.pitchFluidRate;
            prevPitchFluidRate = pitchFluidRate;

            rollAngleTrigger = diveComputer.rollAngleTrigger;
            prevRollAngleTrigger = rollAngleTrigger;

            pitchAngleTrigger = diveComputer.pitchAngleTrigger;
            prevPitchAngleTrigger = pitchAngleTrigger;

            verticalSpeedTrigger = diveComputer.verticalSpeedTrigger;
            prevVerticalSpeedTrigger = verticalSpeedTrigger;
        }

        void ballastTankUpdatedEvent(WBIBallastTank ballastTank, BallastTankTypes ballastTankType, BallastVentStates ballastVentState, bool tankIsConverted)
        {
            if (ballastTank == null || ballastTank.part == null || ballastTank.part.vessel != part.vessel)
                return;
            if (ballastTanks == null)
                return;
            if (!divingControlEnabled)
                return;

            bool tankRoleChanged = false;
            int count = ballastTanks.Count;
            for (int index = 0; index < count; index++)
            {
                WBIBallastTank vesselTank = ballastTanks[index];
                BallastTankTypes previousTankType;
                if (!knownTankTypes.TryGetValue(vesselTank, out previousTankType))
                {
                    knownTankTypes[vesselTank] = vesselTank.tankType;
                    continue;
                }
                if (previousTankType == vesselTank.tankType)
                    continue;

                tankRoleChanged = true;
                knownTankTypes[vesselTank] = vesselTank.tankType;
                vesselTank.SetVentState(BallastVentStates.Closed, 0);
                string partTitle = vesselTank.part.partInfo != null ? vesselTank.part.partInfo.title : vesselTank.part.partName;
                debugLog(" Tank role changed on '" + partTitle + "' from " + previousTankType + " to " + vesselTank.tankType + "; vent closed.");
            }
            if (!tankRoleChanged)
                return;

            updateTrimTankAvailability(isActiveDiveComputer);
            debugLog(" Tank-role change recalculated trim authority: pitch=" + pitchTrimAvailable + " roll=" + rollTrimAvailable + ".");
            if (!isActiveDiveComputer || !divingControlEnabled || ballastTanks.Count == 0)
                return;

            updateManeuverState();
            updateTrimState();
            updateDepthState();
            updateNeutralBuoyancyState();
            syncDiveControlComputers();
        }
        #endregion

        #region Helpers
        void resolveDebugMode()
        {
            debugMode = false;

            ConfigNode moduleNode = getPartConfigNode();
            if (moduleNode == null || !moduleNode.HasValue("debugMode"))
                return;

            bool configuredDebugMode;
            if (bool.TryParse(moduleNode.GetValue("debugMode"), out configuredDebugMode))
                debugMode = configuredDebugMode;
        }

        void updateInputParameters()
        {
            // Get roll and pitch angles.
            rollAngle = 90f - Vector3d.Angle(FlightGlobals.upAxis, vessel.ReferenceTransform.right);
            pitchAngle = 90f - Vector3d.Angle(FlightGlobals.upAxis, vessel.ReferenceTransform.up);
            Quaternion northReference = Quaternion.LookRotation((Vector3)vessel.north, (Vector3)vessel.upAxis);
            Quaternion vesselAttitude = Quaternion.Inverse(Quaternion.Euler(90f, 0f, 0f) * Quaternion.Inverse(vessel.ReferenceTransform.rotation) * northReference);
            yawAngle = vesselAttitude.eulerAngles.y;

            // Update hull integrity
            if (part.vessel.Splashed)
                hullIntegrity = 100 - ((part.staticPressureAtm * 100 / part.maxPressure) * 100.0f);
            else
                hullIntegrity = 100.0f;
        }

        void updateDiveControlledParts()
        {
            // Update our list of dive control computers.
            // Update our list of ballast tanks.
            // Update our list of parts that don't have ballast tanks.
            if (partCount != vessel.parts.Count)
            {
                partCount = vessel.parts.Count;
                diveComputers = part.vessel.FindPartModulesImplementing<WBIDiveComputer>();
                ballastTanks = vessel.FindPartModulesImplementing<WBIBallastTank>();

                knownTankTypes.Clear();
                int ballastTankCount = ballastTanks.Count;
                for (int index = 0; index < ballastTankCount; index++)
                    knownTankTypes[ballastTanks[index]] = ballastTanks[index].tankType;

                buoyancyControlledParts.Clear();
                for (int index = 0; index < partCount; index++)
                {
                    if (!vessel.Parts[index].Modules.Contains("WBIBallastTank"))
                        buoyancyControlledParts.Add(vessel.Parts[index]);
                }
                buoyancyPartCount = buoyancyControlledParts.Count;
            }

            // Restore once during vessel startup. Do not reapply this value when docking adds
            // new parts; a disabled master switch must not alter the newly joined vessel.
            if (restoreSavedBuoyancy)
            {
                restoreSavedBuoyancy = false;
                if (prevBuoyancy >= 0)
                {
                    float savedBuoyancy = Mathf.Clamp(prevBuoyancy, kMinBuoyancy, kMaxBuoyancy);
                    prevBuoyancy = savedBuoyancy;
                    for (int index = 0; index < buoyancyPartCount; index++)
                        buoyancyControlledParts[index].buoyancy = savedBuoyancy;
                    debugLog(" Restored saved controlled-part buoyancy " + savedBuoyancy.ToString("F3") + ".");
                }
            }
        }

        void updateManeuverState()
        {
            if (part.vessel == FlightGlobals.ActiveVessel)
            {
                maneuverState.x = FlightInputHandler.state.roll;
                maneuverState.y = FlightInputHandler.state.pitch;
                maneuverState.z = FlightInputHandler.state.yaw;
            }
            else
            {
                maneuverState = Vector3.zero;
            }

            vesselIsManeuvering = maneuverState.magnitude > 0;
        }

        void updateBallastTanksVentState()
        {
            int count = ballastTanks.Count;
            WBIBallastTank ballastTank;

            for (int index = 0; index < count; index++)
            {
                ballastTank = ballastTanks[index];
                if (ballastTank.tankType == BallastTankTypes.Ballast)
                    ballastTank.SetVentState(ventState, ballastFluidRate);
            }
        }

        void updateDepthState()
        {
            diagnosticCurrentDepth = getCurrentDepth();
            diagnosticDepthError = targetDepth >= 0 ? targetDepth - diagnosticCurrentDepth : 0;
            diagnosticDesiredVerticalSpeed = 0;
            diagnosticVelocityError = 0;
            diagnosticDepthCommand = 0;

            if (maintainDepth != wasMaintainingDepth)
            {
                if (maintainDepth)
                    captureTargetDepth();
                else if (!maintainNeutralBuoyancy)
                    setMainBallastCommand(0);
                wasMaintainingDepth = maintainDepth;
            }

            if (!maintainDepth)
            {
                diagnosticDepthState = "Disabled";
                depthHoldWasSuspended = false;
                return;
            }

            if (!divingControlEnabled)
            {
                diagnosticDepthState = "Suspended: diving control disabled";
                setMainBallastCommand(0);
                return;
            }

            if (vesselIsManeuvering)
            {
                diagnosticDepthState = "Suspended: user maneuver input";
                setMainBallastCommand(0);
                depthHoldWasSuspended = true;
                return;
            }

            if (depthHoldWasSuspended)
            {
                captureTargetDepth();
                depthHoldWasSuspended = false;
            }

            if (targetDepth < 0)
                captureTargetDepth();

            double deltaTime = TimeWarp.fixedDeltaTime;
            double filterTime = Math.Max(0.01, verticalSpeedFilterTime);
            double filterAlpha = 1.0 - Math.Exp(-deltaTime / filterTime);
            filteredVerticalSpeed += (part.vessel.verticalSpeed - filteredVerticalSpeed) * filterAlpha;

            diagnosticCurrentDepth = getCurrentDepth();
            double depthError = targetDepth - diagnosticCurrentDepth;
            diagnosticDepthError = depthError;
            double desiredVerticalSpeed = 0;
            if (Math.Abs(depthError) > depthDeadband)
                desiredVerticalSpeed = -Mathf.Clamp((float)(depthGain * depthError), -maxDepthHoldSpeed, maxDepthHoldSpeed);
            diagnosticDesiredVerticalSpeed = desiredVerticalSpeed;

            double velocityError = filteredVerticalSpeed - desiredVerticalSpeed;
            diagnosticVelocityError = velocityError;
            if (Math.Abs(depthError) <= depthDeadband && Math.Abs(filteredVerticalSpeed) <= verticalSpeedTrigger)
            {
                diagnosticDepthState = "Holding: inside depth and vertical-speed deadbands";
                setMainBallastCommand(0);
                return;
            }

            float transferRate = Mathf.Clamp((float)(Math.Abs(velocityError) * verticalSpeedGain), 0, ballastFluidRate);
            if (transferRate <= 0.01f)
            {
                diagnosticDepthState = "Holding: computed command below minimum";
                setMainBallastCommand(0);
            }
            else
            {
                diagnosticDepthCommand = velocityError > 0 ? transferRate : -transferRate;
                diagnosticDepthState = diagnosticDepthCommand > 0 ? "Correcting: flooding main ballast" : "Correcting: venting main ballast";
                setMainBallastCommand(diagnosticDepthCommand);
            }
        }

        void resolveBuoyancyControlMode()
        {
            bool neutralWasJustEnabled = maintainNeutralBuoyancy && !wasMaintainingNeutralBuoyancy;
            bool depthWasJustEnabled = maintainDepth && !wasMaintainingDepth;

            if (neutralWasJustEnabled)
                maintainDepth = false;
            else if (depthWasJustEnabled)
                maintainNeutralBuoyancy = false;
            else if (maintainDepth && maintainNeutralBuoyancy)
                maintainDepth = false;
        }

        void updateNeutralBuoyancyState()
        {
            diagnosticNeutralCommand = 0;

            if (maintainNeutralBuoyancy != wasMaintainingNeutralBuoyancy)
            {
                if (maintainNeutralBuoyancy)
                {
                    maintainDepth = false;
                    nextNeutralBuoyancyUpdateTime = 0;
                    debugLog(" Neutral-buoyancy control enabled; maintain depth disabled.");
                }
                else if (!maintainDepth)
                {
                    setMainBallastCommand(0);
                }
                wasMaintainingNeutralBuoyancy = maintainNeutralBuoyancy;
            }

            if (!maintainNeutralBuoyancy)
            {
                diagnosticNeutralState = "Disabled";
                neutralBuoyancyStatusString = Localizer.Format("#LOC_SUNKWORKS_neutralBuoyancyDisabled");
                neutralBuoyancyErrorString = "--";
                return;
            }

            if (!divingControlEnabled)
            {
                diagnosticNeutralState = "Suspended: diving control disabled";
                neutralBuoyancyStatusString = Localizer.Format("#LOC_SUNKWORKS_neutralBuoyancySuspendedDiveControl");
                return;
            }

            double currentTime = Planetarium.GetUniversalTime();
            if (currentTime < nextNeutralBuoyancyUpdateTime)
                return;
            nextNeutralBuoyancyUpdateTime = currentTime + Math.Max(0.1f, neutralBuoyancyUpdateInterval);

            if (!vessel.Splashed || vessel.mainBody == null || !vessel.mainBody.ocean)
            {
                diagnosticNeutralState = "Waiting: vessel is not underwater";
                neutralBuoyancyStatusString = Localizer.Format("#LOC_SUNKWORKS_neutralBuoyancyWaitingForWater");
                neutralBuoyancyErrorString = "--";
                setMainBallastCommand(0);
                return;
            }

            double ballastAmount = 0;
            double ballastCapacity = 0;
            for (int index = 0; index < ballastTanks.Count; index++)
            {
                WBIBallastTank ballastTank = ballastTanks[index];
                if (ballastTank.tankType != BallastTankTypes.Ballast || ballastTank.ballastResource == null)
                    continue;
                ballastAmount += ballastTank.ballastResource.amount;
                ballastCapacity += ballastTank.ballastResource.maxAmount;
            }

            if (ballastCapacity <= 0)
            {
                diagnosticNeutralState = "Unavailable: no main ballast tanks";
                neutralBuoyancyStatusString = Localizer.Format("#LOC_SUNKWORKS_neutralBuoyancyNoMainBallast");
                neutralBuoyancyErrorString = "--";
                setMainBallastCommand(0);
                return;
            }

            double buoyancyEquivalent = 0;
            int buoyantPartCount = 0;
            int insufficientlySubmergedParts = 0;
            double scaleAboveDepth = Math.Max(0.0001, PhysicsGlobals.BuoyancyScaleAboveDepth);
            float minimumSubmerged = Mathf.Clamp01(neutralMinimumSubmergedPortion);
            for (int index = 0; index < vessel.parts.Count; index++)
            {
                Part vesselPart = vessel.parts[index];
                PartBuoyancy partBuoyancy = vesselPart.GetComponent<PartBuoyancy>();
                if (partBuoyancy == null || partBuoyancy.displacement <= 0)
                    continue;

                buoyantPartCount++;
                if (vesselPart.submergedPortion < minimumSubmerged)
                {
                    insufficientlySubmergedParts++;
                    continue;
                }

                double depthScale = partBuoyancy.maxDepth >= scaleAboveDepth
                    ? 1.0
                    : Math.Max(0.0, partBuoyancy.maxDepth / scaleAboveDepth);
                buoyancyEquivalent += partBuoyancy.displacement * vessel.mainBody.oceanDensity * depthScale *
                    PhysicsGlobals.BuoyancyScalar * vesselPart.buoyancy;
            }

            if (buoyantPartCount == 0 || insufficientlySubmergedParts > 0)
            {
                diagnosticNeutralState = "Waiting: " + insufficientlySubmergedParts + " part(s) not fully submerged";
                neutralBuoyancyStatusString = Localizer.Format("#LOC_SUNKWORKS_neutralBuoyancyWaitingForSubmersion", insufficientlySubmergedParts.ToString());
                neutralBuoyancyErrorString = "--";
                setMainBallastCommand(0);
                return;
            }

            double vesselMass = vessel.GetTotalMass();
            double buoyancyError = buoyancyEquivalent - vesselMass;
            diagnosticNeutralMass = vesselMass;
            diagnosticNeutralBuoyancy = buoyancyEquivalent;
            diagnosticNeutralError = buoyancyError;
            neutralBuoyancyErrorString = buoyancyError.ToString("+0.000;-0.000;0.000") + " t";

            double massDenominator = Math.Max(0.001, vesselMass);
            double normalizedError = buoyancyError / massDenominator;
            if (Math.Abs(normalizedError) <= Math.Max(0.00001f, neutralBuoyancyDeadband))
            {
                diagnosticNeutralState = "Neutral: inside buoyancy deadband";
                neutralBuoyancyStatusString = Localizer.Format("#LOC_SUNKWORKS_neutralBuoyancyActive");
                setMainBallastCommand(0);
                return;
            }

            if (buoyancyError > 0 && ballastAmount >= ballastCapacity - 0.0001)
            {
                diagnosticNeutralState = "Limited: main ballast tanks are full";
                neutralBuoyancyStatusString = Localizer.Format("#LOC_SUNKWORKS_neutralBuoyancyBallastFull");
                setMainBallastCommand(0);
                return;
            }
            if (buoyancyError < 0 && ballastAmount <= 0.0001)
            {
                diagnosticNeutralState = "Limited: main ballast tanks are empty";
                neutralBuoyancyStatusString = Localizer.Format("#LOC_SUNKWORKS_neutralBuoyancyBallastEmpty");
                setMainBallastCommand(0);
                return;
            }

            float maximumTransferRate = Mathf.Max(0, ballastFluidRate);
            if (maximumTransferRate <= 0.01f)
            {
                diagnosticNeutralState = "Suspended: ballast transfer rate is zero";
                neutralBuoyancyStatusString = Localizer.Format("#LOC_SUNKWORKS_neutralBuoyancyTransferDisabled");
                setMainBallastCommand(0);
                return;
            }

            float minimumTransferRate = Mathf.Min(1f, maximumTransferRate);
            float transferRate = Mathf.Clamp((float)(Math.Abs(normalizedError) * Math.Max(0, neutralBuoyancyGain)), minimumTransferRate, maximumTransferRate);
            diagnosticNeutralCommand = buoyancyError > 0 ? transferRate : -transferRate;
            diagnosticNeutralState = diagnosticNeutralCommand > 0
                ? "Correcting: flooding main ballast"
                : "Correcting: venting main ballast";
            neutralBuoyancyStatusString = diagnosticNeutralCommand > 0
                ? Localizer.Format("#LOC_SUNKWORKS_neutralBuoyancyFlooding")
                : Localizer.Format("#LOC_SUNKWORKS_neutralBuoyancyVenting");
            setMainBallastCommand(diagnosticNeutralCommand);
        }

        void updateTrimState()
        {
            if (autoTrimEnabled != wasAutoTrimming)
            {
                bool previousAutoTrimState = wasAutoTrimming;
                wasAutoTrimming = autoTrimEnabled;

                // Auto-trim and neutral buoyancy both issue automatic tank commands. Treat a
                // trim-mode change as an explicit handoff: stop neutral control, close its main
                // ballast command, and let trim recompute from the vessel's current attitude.
                if (maintainNeutralBuoyancy)
                {
                    maintainNeutralBuoyancy = false;
                    wasMaintainingNeutralBuoyancy = false;
                    nextNeutralBuoyancyUpdateTime = 0;
                    setMainBallastCommand(0);
                    debugLog(" Auto-trim changed from " + previousAutoTrimState + " to " + autoTrimEnabled + "; neutral-buoyancy control disabled and main ballast vents closed.");
                }

                attitudeRateInitialized = false;
                filteredPitchRate = 0;
                filteredRollRate = 0;
            }
            if (!autoTrimEnabled || vesselIsManeuvering)
            {
                diagnosticPitchCommand = 0;
                diagnosticRollCommand = 0;
                diagnosticTrimState = !autoTrimEnabled ? "Disabled" : "Suspended: user maneuver input";
                pitchControlActive = false;
                rollControlActive = false;
                closeTrimTankVents();
                attitudeRateInitialized = false;
                filteredPitchRate = 0;
                filteredRollRate = 0;
                return;
            }

            if (!pitchTrimAvailable && !rollTrimAvailable)
            {
                diagnosticPitchCommand = 0;
                diagnosticRollCommand = 0;
                diagnosticTrimState = trimTanksAvailable
                    ? "Unavailable: no complete trim tank pairs"
                    : "Unavailable: no trim tanks configured";
                pitchControlActive = false;
                rollControlActive = false;
                closeTrimTankVents();
                attitudeRateInitialized = false;
                filteredPitchRate = 0;
                filteredRollRate = 0;
                return;
            }

            double deltaTime = Math.Max(0.001, TimeWarp.fixedDeltaTime);
            double pitchRate = 0;
            double rollRate = 0;
            if (attitudeRateInitialized)
            {
                double rateFilterTime = Math.Max(0.01, trimRateFilterTime);
                double rateFilterAlpha = 1.0 - Math.Exp(-deltaTime / rateFilterTime);
                double measuredPitchRate = (pitchAngle - previousPitchAngle) / deltaTime;
                double measuredRollRate = (rollAngle - previousRollAngle) / deltaTime;
                filteredPitchRate += (measuredPitchRate - filteredPitchRate) * rateFilterAlpha;
                filteredRollRate += (measuredRollRate - filteredRollRate) * rateFilterAlpha;
                pitchRate = filteredPitchRate;
                rollRate = filteredRollRate;
            }
            previousPitchAngle = pitchAngle;
            previousRollAngle = rollAngle;
            attitudeRateInitialized = true;

            float pitchCommand = pitchTrimAvailable
                ? getTrimCommand(pitchAngle + pitchRate * trimRateDamping, pitchAngleTrigger, pitchFluidRate)
                : 0;
            float rollCommand = rollTrimAvailable
                ? getTrimCommand(rollAngle + rollRate * trimRateDamping, rollAngleTrigger, rollFluidRate)
                : 0;
            diagnosticPitchCommand = pitchCommand;
            diagnosticRollCommand = rollCommand;
            pitchControlActive = pitchCommand != 0;
            rollControlActive = rollCommand != 0;
            if (!rollTrimAvailable)
                diagnosticTrimState = pitchControlActive ? "Correcting pitch; roll trim unavailable" : "Holding pitch; roll trim unavailable";
            else if (!pitchTrimAvailable)
                diagnosticTrimState = rollControlActive ? "Correcting roll; pitch trim unavailable" : "Holding roll; pitch trim unavailable";
            else if (pitchControlActive && rollControlActive)
                diagnosticTrimState = "Correcting pitch and roll";
            else if (pitchControlActive)
                diagnosticTrimState = "Correcting pitch";
            else if (rollControlActive)
                diagnosticTrimState = "Correcting roll";
            else
                diagnosticTrimState = "Holding: inside attitude deadbands";

            int count = ballastTanks.Count;
            for (int index = 0; index < count; index++)
            {
                WBIBallastTank trimTank = ballastTanks[index];
                if (trimTank.tankType == BallastTankTypes.Ballast)
                    continue;

                float tankCommand = 0;
                if (trimTank.CanTrimForward())
                    tankCommand += pitchCommand;
                else if (trimTank.CanTrimAft())
                    tankCommand -= pitchCommand;

                if (trimTank.CanTrimStarboard())
                    tankCommand += rollCommand;
                else if (trimTank.CanTrimPort())
                    tankCommand -= rollCommand;

                setTankCommand(trimTank, Mathf.Clamp(tankCommand, -100f, 100f));
            }
        }

        void captureTargetDepth()
        {
            targetDepth = getCurrentDepth();
            filteredVerticalSpeed = part.vessel.verticalSpeed;
            debugLog(" Captured target depth " + targetDepth.ToString("F3") + "m at vertical speed " + filteredVerticalSpeed.ToString("F3") + "m/s.");
        }

        void updateTrimTankAvailability(bool notifyPlayer)
        {
            trimTanksAvailable = false;
            bool hasForwardTrim = false;
            bool hasAftTrim = false;
            bool hasPortTrim = false;
            bool hasStarboardTrim = false;
            if (ballastTanks != null)
            {
                int count = ballastTanks.Count;
                for (int index = 0; index < count; index++)
                {
                    WBIBallastTank ballastTank = ballastTanks[index];
                    if (ballastTank.tankType == BallastTankTypes.Ballast)
                        continue;

                    trimTanksAvailable = true;
                    hasForwardTrim |= ballastTank.CanTrimForward();
                    hasAftTrim |= ballastTank.CanTrimAft();
                    hasPortTrim |= ballastTank.CanTrimPort();
                    hasStarboardTrim |= ballastTank.CanTrimStarboard();
                }
            }

            pitchTrimAvailable = hasForwardTrim && hasAftTrim;
            rollTrimAvailable = hasPortTrim && hasStarboardTrim;
            if (pitchTrimAvailable && rollTrimAvailable)
                trimStatusString = Localizer.Format("#LOC_SUNKWORKS_trimStatusReady");
            else if (pitchTrimAvailable)
                trimStatusString = Localizer.Format("#LOC_SUNKWORKS_trimStatusPitchOnly");
            else if (rollTrimAvailable)
                trimStatusString = Localizer.Format("#LOC_SUNKWORKS_trimStatusRollOnly");
            else if (trimTanksAvailable)
                trimStatusString = Localizer.Format("#LOC_SUNKWORKS_trimStatusIncomplete");
            else
                trimStatusString = Localizer.Format("#LOC_SUNKWORKS_trimStatusNoTanks");

            if (!divingControlEnabled || trimTanksAvailable)
            {
                missingTrimWarningDisplayed = false;
                return;
            }

            if (notifyPlayer && !missingTrimWarningDisplayed)
            {
                missingTrimWarningDisplayed = true;
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_SUNKWORKS_trimWarningNoTanks"), kMsgDuration, ScreenMessageStyle.UPPER_CENTER);
                debugLog(" Dive control is enabled without configured trim tanks; auto-trim is unavailable.");
            }
        }

        double getCurrentDepth()
        {
            double altitudeAtCoM = FlightGlobals.getAltitudeAtPos(part.vessel.CoMD, part.vessel.mainBody);
            return Math.Max(0, -altitudeAtCoM);
        }

        float getTrimCommand(double dampedAngle, float angleTrigger, float maxRate)
        {
            double trigger = Math.Max(0.01, angleTrigger);
            double magnitude = Math.Abs(dampedAngle);
            if (magnitude <= trigger)
                return 0;

            float normalizedCommand = Mathf.Clamp((float)((magnitude - trigger) / trigger), 0.1f, 1f);
            return Mathf.Sign((float)dampedAngle) * maxRate * normalizedCommand;
        }

        void setMainBallastCommand(float command)
        {
            BallastVentStates desiredState = BallastVentStates.Closed;
            if (command > 0)
                desiredState = BallastVentStates.FloodingBallast;
            else if (command < 0)
                desiredState = BallastVentStates.VentingBallast;

            ventState = desiredState;
            float transferRate = Math.Abs(command);
            int count = ballastTanks.Count;
            for (int index = 0; index < count; index++)
            {
                WBIBallastTank ballastTank = ballastTanks[index];
                if (ballastTank.tankType == BallastTankTypes.Ballast)
                    ballastTank.SetVentState(desiredState, transferRate);
            }
        }

        void setTankCommand(WBIBallastTank ballastTank, float command)
        {
            if (command > 0)
                ballastTank.SetVentState(BallastVentStates.FloodingBallast, command);
            else if (command < 0)
                ballastTank.SetVentState(BallastVentStates.VentingBallast, -command);
            else
                ballastTank.SetVentState(BallastVentStates.Closed, 0);
        }

        void closeTrimTankVents()
        {
            int count = ballastTanks.Count;
            for (int index = 0; index < count; index++)
            {
                WBIBallastTank ballastTank = ballastTanks[index];
                if (ballastTank.tankType != BallastTankTypes.Ballast && ballastTank.ventState != BallastVentStates.Closed)
                    ballastTank.SetVentState(BallastVentStates.Closed, 0);
            }
        }

        void logDebugDiagnostics()
        {
            if (!debugMode || part == null || part.vessel == null)
                return;
            if (diveComputers != null && diveComputers.Count > 0 && diveComputers[0] != this)
                return;

            float currentTime = Time.realtimeSinceStartup;
            if (currentTime < nextDebugLogTime)
                return;
            nextDebugLogTime = currentTime + Math.Max(0, debugLogInterval);

            bool isActiveVessel = part.vessel == FlightGlobals.ActiveVessel;
            bool sasActionGroup = part.vessel.ActionGroups[KSPActionGroup.SAS];
            bool autopilotEnabled = part.vessel.Autopilot != null && part.vessel.Autopilot.Enabled;
            string sasMode = part.vessel.Autopilot != null ? part.vessel.Autopilot.Mode.ToString() : "Unavailable";
            bool sasDamping = part.vessel.Autopilot != null && part.vessel.Autopilot.SAS != null && part.vessel.Autopilot.SAS.dampingMode;
            FlightCtrlState rawInput = FlightInputHandler.state;
            FlightCtrlState appliedInput = part.vessel.ctrlState;
            Vector3 localAngularVelocity = part.vessel.ReferenceTransform.InverseTransformDirection(part.vessel.angularVelocity);

            debugLog(" [Snapshot] vessel=" + part.vessel.vesselName +
                " activeVessel=" + isActiveVessel +
                " splashed=" + part.vessel.Splashed +
                " shielded=" + part.ShieldedFromAirstream +
                " fixedDeltaTime=" + TimeWarp.fixedDeltaTime.ToString("F3") +
                " ballastTanks=" + (ballastTanks != null ? ballastTanks.Count : 0));
            debugLog(" [Computer] diveControl=" + divingControlEnabled +
                " autoTrim=" + autoTrimEnabled +
                " maintainDepth=" + maintainDepth +
                " maintainNeutralBuoyancy=" + maintainNeutralBuoyancy +
                " maneuvering=" + vesselIsManeuvering +
                " depthSuspended=" + depthHoldWasSuspended +
                " pitchActive=" + pitchControlActive +
                " rollActive=" + rollControlActive +
                " ventState=" + ventState +
                " state='" + diveStateString + "'");
            debugLog(" [SAS] actionGroup=" + sasActionGroup +
                " autopilotEnabled=" + autopilotEnabled +
                " mode=" + sasMode +
                " dampingMode=" + sasDamping);
            debugLog(" [Input raw/user] roll=" + rawInput.roll.ToString("F3") +
                " pitch=" + rawInput.pitch.ToString("F3") +
                " yaw=" + rawInput.yaw.ToString("F3") +
                " rollTrim=" + rawInput.rollTrim.ToString("F3") +
                " pitchTrim=" + rawInput.pitchTrim.ToString("F3") +
                " yawTrim=" + rawInput.yawTrim.ToString("F3"));
            debugLog(" [Input vessel/applied] roll=" + appliedInput.roll.ToString("F3") +
                " pitch=" + appliedInput.pitch.ToString("F3") +
                " yaw=" + appliedInput.yaw.ToString("F3") +
                " rollTrim=" + appliedInput.rollTrim.ToString("F3") +
                " pitchTrim=" + appliedInput.pitchTrim.ToString("F3") +
                " yawTrim=" + appliedInput.yawTrim.ToString("F3"));
            debugLog(" [Attitude] roll=" + rollAngle.ToString("F3") +
                " pitch=" + pitchAngle.ToString("F3") +
                " yaw/heading=" + yawAngle.ToString("F3") +
                " filteredRollRate=" + filteredRollRate.ToString("F3") +
                " filteredPitchRate=" + filteredPitchRate.ToString("F3") +
                " angularVelocityWorld=" + part.vessel.angularVelocity.ToString("F4") +
                " angularVelocityLocalXYZ=" + localAngularVelocity.ToString("F4"));
            debugLog(" [Trim] state='" + diagnosticTrimState +
                "' pitchAuthority=" + pitchTrimAvailable +
                " rollAuthority=" + rollTrimAvailable +
                " pitchTrigger=" + pitchAngleTrigger.ToString("F3") +
                " rollTrigger=" + rollAngleTrigger.ToString("F3") +
                " pitchCommand=" + diagnosticPitchCommand.ToString("F2") + "%" +
                " rollCommand=" + diagnosticRollCommand.ToString("F2") + "%" +
                " pitchRateLimit=" + pitchFluidRate.ToString("F1") + "%" +
                " rollRateLimit=" + rollFluidRate.ToString("F1") + "%");
            debugLog(" [Depth] state='" + diagnosticDepthState +
                "' current=" + diagnosticCurrentDepth.ToString("F3") + "m" +
                " target=" + targetDepth.ToString("F3") + "m" +
                " error=" + diagnosticDepthError.ToString("F3") + "m" +
                " verticalSpeed=" + part.vessel.verticalSpeed.ToString("F3") + "m/s" +
                " filteredVerticalSpeed=" + filteredVerticalSpeed.ToString("F3") + "m/s" +
                " desiredVerticalSpeed=" + diagnosticDesiredVerticalSpeed.ToString("F3") + "m/s" +
                " velocityError=" + diagnosticVelocityError.ToString("F3") + "m/s" +
                " ballastCommand=" + diagnosticDepthCommand.ToString("F2") + "%");
            debugLog(" [Neutral Buoyancy] state='" + diagnosticNeutralState +
                "' mass=" + diagnosticNeutralMass.ToString("F3") + "t" +
                " buoyancy=" + diagnosticNeutralBuoyancy.ToString("F3") + "t" +
                " error=" + diagnosticNeutralError.ToString("F4") + "t" +
                " command=" + diagnosticNeutralCommand.ToString("F2") + "%" +
                " updateInterval=" + neutralBuoyancyUpdateInterval.ToString("F2") + "s");

            if (ballastTanks == null)
                return;

            int count = ballastTanks.Count;
            for (int index = 0; index < count; index++)
            {
                WBIBallastTank ballastTank = ballastTanks[index];
                PartResource resource = ballastTank.ballastResource;
                Vector3 momentArm = part.vessel.ReferenceTransform.InverseTransformDirection(ballastTank.part.transform.position - part.vessel.CoM);
                string partName = ballastTank.part.partInfo != null ? ballastTank.part.partInfo.title : ballastTank.part.partName;
                debugLog(" [Tank " + index + "] part='" + partName +
                    "' flightID=" + ballastTank.part.flightID +
                    " type=" + ballastTank.tankType +
                    " vent=" + ballastTank.ventState +
                    " transfer=" + ballastTank.GetActiveFluidTransferPercentage().ToString("F2") + "%" +
                    " manualTransfer=" + ballastTank.fluidTransferPercentage.ToString("F2") + "%" +
                    " controllerRate=" + ballastTank.useCommandedFluidTransferRate +
                    " amount=" + (resource != null ? resource.amount.ToString("F3") : "n/a") +
                    "/" + (resource != null ? resource.maxAmount.ToString("F3") : "n/a") +
                    " partBuoyancy=" + ballastTank.part.buoyancy.ToString("F3") +
                    " localMomentArmXYZ=" + momentArm.ToString("F3"));
            }
        }

        void updateBallastState()
        {
            // Check ballast states. We'll update our state once all the ballast tanks are closed.
            // Different ballast tanks fill/empty at different rates so the dive computer's state
            // needs to detect when all the ballast tanks have finished filling or emptying.
            int count = ballastTanks.Count;
            WBIBallastTank ballastTank;
            bool ventsAreOpen = false;
            double amount = 0;
            double maxAmount = 0;

            for (int index = 0; index < count; index++)
            {
                ballastTank = ballastTanks[index];

                // Get the current and max ballast.
                if (ballastTank.ballastResource != null && ballastTank.tankType == BallastTankTypes.Ballast)
                {
                    amount += ballastTank.ballastResource.amount;
                    maxAmount += ballastTank.ballastResource.maxAmount;

                    // Check vent state
                    if (ballastTank.ventState != BallastVentStates.Closed)
                        ventsAreOpen = true;
                }
            }

            // Calculate buoyancy for the buoyancy controlled parts and update them.
            if (maxAmount > 0 && part.vessel.Splashed)
            {
                float buoyancy = 1 - ((float)(amount / maxAmount));
                if (buoyancy < kMinBuoyancy)
                    buoyancy = kMinBuoyancy;
                else if (amount <= 0)
                    buoyancy = kMaxBuoyancy;

                if (prevBuoyancy != buoyancy)
                {
                    prevBuoyancy = buoyancy;
                    for (int index = 0; index < buoyancyPartCount; index++)
                        buoyancyControlledParts[index].buoyancy = buoyancy;
                }
            }

            // Check the flag.
            if (!ventsAreOpen)
                ventState = BallastVentStates.Closed;
        }

        void setupGUI()
        {
            Fields["maneuverState"].guiActive = debugMode;
            Fields["pitchAngle"].guiActive = debugMode;
            Fields["rollAngle"].guiActive = debugMode;
            Fields["yawAngle"].guiActive = debugMode;
            Fields["maintainDepth"].guiActiveEditor = false;
        }

        void updateGUI()
        {
            bool controlsVisible = divingControlEnabled;
            bool trimControlsVisible = controlsVisible && autoTrimEnabled;

            // Diving Control is the always-available master switch. Everything else in the
            // dive-computer PAW disappears while it is off, including debug attitude fields.
            Fields["divingControlEnabled"].guiActive = true;
            Fields["autoTrimEnabled"].guiActive = controlsVisible;
            Fields["trimStatusString"].guiActive = controlsVisible;
            Fields["rollAngleTrigger"].guiActive = trimControlsVisible;
            Fields["pitchAngleTrigger"].guiActive = trimControlsVisible;
            Fields["rollFluidRate"].guiActive = trimControlsVisible;
            Fields["pitchFluidRate"].guiActive = trimControlsVisible;
            Fields["ballastFluidRate"].guiActive = controlsVisible;
            Fields["diveStateString"].guiActive = controlsVisible;
            Fields["hullIntegrity"].guiActive = controlsVisible;
            Fields["maintainDepth"].guiActive = controlsVisible;
            Fields["maintainNeutralBuoyancy"].guiActive = controlsVisible;
            Fields["neutralBuoyancyStatusString"].guiActive = controlsVisible && maintainNeutralBuoyancy;
            Fields["neutralBuoyancyErrorString"].guiActive = controlsVisible && maintainNeutralBuoyancy;
            Fields["targetDepth"].guiActive = controlsVisible && maintainDepth && targetDepth >= 0;
            Fields["verticalSpeedTrigger"].guiActive = controlsVisible;
            Fields["maneuverState"].guiActive = controlsVisible && debugMode;
            Fields["pitchAngle"].guiActive = controlsVisible && debugMode;
            Fields["rollAngle"].guiActive = controlsVisible && debugMode;
            Fields["yawAngle"].guiActive = controlsVisible && debugMode;
            Events["FloodBallast"].active = controlsVisible;
            Events["VentBallast"].active = controlsVisible;
            Events["CloseVents"].active = controlsVisible;
            Events["EmergencySurface"].active = controlsVisible;

            switch (ventState)
            {
                case BallastVentStates.Closed:
                default:
                    diveStateString = Localizer.Format("#LOC_SUNKWORKS_diveStateCruising");
                    break;

                case BallastVentStates.FloodingBallast:
                    diveStateString = Localizer.Format("#LOC_SUNKWORKS_diveStateDiving");
                    break;

                case BallastVentStates.VentingBallast:
                    diveStateString = Localizer.Format("#LOC_SUNKWORKS_diveStateSurfacing");
                    break;
            }
        }

        void syncDiveControlComputers()
        {
            int count = diveComputers.Count;
            for (int index = 0; index < count; index++)
            {
                WBIDiveComputer diveComputer = diveComputers[index];
                if (diveComputer == this)
                    continue;

                diveComputer.ventState = ventState;
                diveComputer.vesselIsManeuvering = vesselIsManeuvering;
                diveComputer.pitchControlActive = pitchControlActive;
                diveComputer.rollControlActive = rollControlActive;
                diveComputer.prevBuoyancy = prevBuoyancy;
                diveComputer.pitchFluidRate = pitchFluidRate;
                diveComputer.prevPitchFluidRate = pitchFluidRate;
                diveComputer.rollFluidRate = rollFluidRate;
                diveComputer.prevRollFluidRate = rollFluidRate;
                diveComputer.ballastFluidRate = ballastFluidRate;
                diveComputer.prevBallastFluidRate = ballastFluidRate;
                diveComputer.verticalSpeedTrigger = verticalSpeedTrigger;
                diveComputer.prevVerticalSpeedTrigger = verticalSpeedTrigger;
                diveComputer.pitchAngleTrigger = pitchAngleTrigger;
                diveComputer.prevPitchAngleTrigger = pitchAngleTrigger;
                diveComputer.rollAngleTrigger = rollAngleTrigger;
                diveComputer.prevRollAngleTrigger = rollAngleTrigger;
                diveComputer.maintainDepth = maintainDepth;
                diveComputer.wasMaintainingDepth = maintainDepth;
                diveComputer.targetDepth = targetDepth;
                diveComputer.maintainNeutralBuoyancy = maintainNeutralBuoyancy;
                diveComputer.wasMaintainingNeutralBuoyancy = maintainNeutralBuoyancy;
                diveComputer.neutralBuoyancyStatusString = neutralBuoyancyStatusString;
                diveComputer.neutralBuoyancyErrorString = neutralBuoyancyErrorString;
                diveComputer.autoTrimEnabled = autoTrimEnabled;
                diveComputer.wasAutoTrimming = autoTrimEnabled;
                diveComputer.divingControlEnabled = divingControlEnabled;
                diveComputer.divingControlWasEnabled = divingControlEnabled;
            }

            prevPitchFluidRate = pitchFluidRate;
            prevRollFluidRate = rollFluidRate;
            prevBallastFluidRate = ballastFluidRate;
            prevVerticalSpeedTrigger = verticalSpeedTrigger;
            prevPitchAngleTrigger = pitchAngleTrigger;
            prevRollAngleTrigger = rollAngleTrigger;
            wasMaintainingDepth = maintainDepth;
            wasMaintainingNeutralBuoyancy = maintainNeutralBuoyancy;
            wasAutoTrimming = autoTrimEnabled;
            divingControlWasEnabled = divingControlEnabled;
        }
        #endregion
    }
}
