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
    ///     name = SWDiveComputer
    ///     debugMode = true
    ///     maxPressureOverride = 6000
    ///  }
    /// </code>
    /// </example>
    [KSPModule("Dive Computer")]
    public class SWDiveComputer: BasePartModule
    {
        #region Constants
        const float kMinBuoyancy = 0.01f;
        const float kMaxBuoyancy = 1f;
        const float kMsgDuration = 3.0f;
        const float kMaxDivingControlDepth = -3f;
        const string kGroupName = "DiveComputer";
        const string ICON_PATH = "WildBlueIndustries/000WildBlueTools/Icons/";
        #endregion

        #region GameEvents
        /// <summary>
        /// Indicates that the user has requested to flood the boat's ballast.
        /// </summary>
        public static EventData<Vessel, SWDiveComputer> onFloodBallast = new EventData<Vessel, SWDiveComputer>("onFloodBallast");

        /// <summary>
        /// Indicates that the user has requested to vent the boat's ballast.
        /// </summary>
        public static EventData<Vessel, SWDiveComputer> onVentBallast = new EventData<Vessel, SWDiveComputer>("onVentBallast");

        /// <summary>
        /// Indicates that the user has requested to close the boat's ballast vents.
        /// </summary>
        public static EventData<Vessel, SWDiveComputer> onCloseVents = new EventData<Vessel, SWDiveComputer>("onCloseVents");

        /// <summary>
        /// Indicates that the user has requested an emergency surface.
        /// </summary>
        public static EventData<Vessel, SWDiveComputer> onEmergencySurface = new EventData<Vessel, SWDiveComputer>("onEmergencySurface");

        /// <summary>
        /// Indicates that the user has requested a change to maintain depth.
        /// </summary>
        public static EventData<Vessel, SWDiveComputer, bool> onMaintainDepthUpdated = new EventData<Vessel, SWDiveComputer, bool>("onMaintainDepthUpdated");

        /// <summary>
        /// Indicates that the user has requested a change to auto-trim.
        /// </summary>
        public static EventData<Vessel, SWDiveComputer, bool> onAutoTrimUpdated = new EventData<Vessel, SWDiveComputer, bool>("onAutoTrimUpdated");

        /// <summary>
        /// Indicates that the user has requested a change to dive control.
        /// </summary>
        public static EventData<Vessel, SWDiveComputer, bool> onDiveControlUpdated = new EventData<Vessel, SWDiveComputer, bool>("onDiveControlUpdated");

        /// <summary>
        /// Event to synchronize triggers and fluid rates
        /// </summary>
        public static EventData<Vessel, SWDiveComputer> onTriggerAndFluidRatesUpdated = new EventData<Vessel, SWDiveComputer>("onTriggerAndFluidRatesUpdated");
        #endregion

        #region Fields
        /// <summary>
        /// Debug flag
        /// </summary>
        [KSPField]
        public bool debugMode = false;

        /// <summary>
        /// Indicates whether or not to automatically keep the boat level.
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_SUNKWORKS_autoTrim", isPersistant = true)]
        [UI_Toggle(enabledText = "#LOC_SUNKWORKS_on", disabledText = "#LOC_SUNKWORKS_off")]
        public bool autoTrimEnabled;

        /// <summary>
        /// Indicates whether or not to enable dive control.
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_SUNKWORKS_divingControl", isPersistant = true)]
        [UI_Toggle(enabledText = "#LOC_SUNKWORKS_on", disabledText = "#LOC_SUNKWORKS_off")]
        public bool divingControlEnabled;

        /// <summary>
        /// Indicates whether or not to maintain current depth
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "#LOC_SUNKWORKS_maintainDepth", isPersistant = true)]
        [UI_Toggle(enabledText = "#LOC_SUNKWORKS_on", disabledText = "#LOC_SUNKWORKS_off")]
        public bool maintainDepth;

        /// <summary>
        /// Display string for current state of the dive computer
        /// </summary>
        [KSPField(guiActive = true, guiName = "#LOC_SUNKWORKS_diveState")]
        public string diveStateString = string.Empty;

        /// <summary>
        /// Display string for current state of the dive computer
        /// </summary>
        [KSPField(guiActive = true, guiName = "#LOC_SUNKWORKS_hullIntegrity", guiFormat = "f1", guiUnits = "%")]
        public double hullIntegrity;

        /// <summary>
        /// Current pitch angle of the boat.
        /// </summary>
        [KSPField(guiActive = true, guiName = "#LOC_SUNKWORKS_pitchAngle", guiFormat = "f1")]
        public double pitchAngle;

        /// <summary>
        /// Current roll angle of the boat.
        /// </summary>
        [KSPField(guiActive = true, guiName = "#LOC_SUNKWORKS_rollAngle", guiFormat = "f1")]
        public double rollAngle;

        /// <summary>
        /// Roll angle that will trigger auto-trim. 0 is level, so anything that is +- this value will trigger auto-trim.
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_SUNKWORKS_rollAngleTrigger", isPersistant = true)]
        [UI_FloatRange(maxValue = 5, minValue = 0.0f, scene = UI_Scene.All, stepIncrement = 0.05f)]
        public float rollAngleTrigger = 0.75f;

        /// <summary>
        /// Pitch angle that will trigger auto-trim. 0 is level, so anything that is +- this value will trigger auto-trim.
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_SUNKWORKS_pitchAngleTrigger", isPersistant = true)]
        [UI_FloatRange(maxValue = 5, minValue = 0.0f, scene = UI_Scene.All, stepIncrement = 0.05f)]
        public float pitchAngleTrigger = 0.75f;

        /// <summary>
        /// If maintainDepth is enabled, then when the vertical speed reaches +- the speed trigger, the boat will attempt to maintain depth.
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_SUNKWORKS_verticalSpeedTrigger", isPersistant = true)]
        [UI_FloatRange(maxValue = 1, minValue = 0.0f, scene = UI_Scene.All, stepIncrement = 0.01f)]
        public float verticalSpeedTrigger = 0.1f;

        /// <summary>
        /// Roll-trim's fluid transfer rate (percent)
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_SUNKWORKS_rollFluidRate", isPersistant = true)]
        [UI_FloatRange(maxValue = 100f, minValue = 0.0f, scene = UI_Scene.All, stepIncrement = 1f)]
        public float rollFluidRate = 100f;

        /// <summary>
        /// Pitch-trim's fluid transfer rate (percent)
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_SUNKWORKS_pitchFluidRate", isPersistant = true)]
        [UI_FloatRange(maxValue = 100f, minValue = 0.0f, scene = UI_Scene.All, stepIncrement = 1f)]
        public float pitchFluidRate = 100f;

        /// <summary>
        /// Ballast's fluid transfer rate (percent)
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_SUNKWORKS_ballastFluidRate", isPersistant = true)]
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
        #endregion

        #region Housekeeping
        /// <summary>
        /// Debug maneuver states
        /// </summary>
        [KSPField(guiName = "Roll/Pitch/Yaw", guiFormat = "n3")]
        protected Vector3 maneuverState = Vector3.zero;

        /// <summary>
        /// Flag to indicate that the vessel is maneuvering
        /// </summary>
        public bool vesselIsManeuvering;

        internal bool wasMaintainingDepth;
        internal bool wasAutoTrimming;
        internal bool divingControlWasEnabled;
        List<SWBallastTank> ballastTanks;
        List<SWDiveComputer> diveComputers;
        List<Part> buoyancyControlledParts;
        int buoyancyPartCount;
        int partCount;
        double pitchTriggerUp = 0;
        double pitchTriggerDown = 0;
        double rollTriggerPort = 0;
        double rollTriggerStarboard = 0;
        internal bool pitchControlActive;
        internal bool rollControlActive;
        internal float prevBallastFluidRate;
        internal float prevRollAngleTrigger;
        internal float prevPitchAngleTrigger;
        internal float prevVerticalSpeedTrigger;
        internal float prevRollFluidRate;
        internal float prevPitchFluidRate;
        internal float prevBuoyancy = -1f;
        #endregion

        #region Events
        /// <summary>
        /// Floods the ballast tank
        /// </summary>
        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "#LOC_SUNKWORKS_floodBallast")]
        public void FloodBallast()
        {
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

            ventState = BallastVentStates.FloodingBallast;

            prevBallastFluidRate = ballastFluidRate;
            updateBallastTanksVentState();
            maintainDepth = false;

        }

        /// <summary>
        /// Vents ballast tank
        /// </summary>
        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "#LOC_SUNKWORKS_ventBallast")]
        public void VentBallast()
        {
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

            ventState = BallastVentStates.VentingBallast;

            prevBallastFluidRate = ballastFluidRate;
            updateBallastTanksVentState();
            maintainDepth = false;
        }

        /// <summary>
        /// Close ballast vents
        /// </summary>
        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "#LOC_SUNKWORKS_closeVents")]
        public void CloseVents()
        {
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

            ventState = BallastVentStates.Closed;

            updateBallastTanksVentState();
        }

        /// <summary>
        /// Activates emergency surface, telling all ballast tanks to immediately dump their ballast. This affects parts marked as ballast or trim tanks.
        /// </summary>
        [KSPEvent(guiActive = true, guiName = "#LOC_SUNKWORKS_emergencySurface")]
        public void EmergencySurface()
        {
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

            int count = ballastTanks.Count;
            for (int index = 0; index < count; index++)
            {
                ballastTanks[index].ventState = BallastVentStates.Closed;
                ballastTanks[index].DumpBallast();
            }

            maintainDepth = false;
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
            string message = maintainDepth ? Localizer.Format("#LOC_SUNKWORKS_toggleMaintainDepthActionOn") : Localizer.Format("#LOC_SUNKWORKS_toggleMaintainDepthActionOff");
            ScreenMessages.PostScreenMessage(message, kMsgDuration, ScreenMessageStyle.UPPER_CENTER);

            if (!isActiveDiveComputer)
                onMaintainDepthUpdated.Fire(part.vessel, this, maintainDepth);
        }

        /// <summary>
        /// Toggle auto trim action
        /// </summary>
        /// <param name="param"></param>
        [KSPAction("#LOC_SUNKWORKS_toggleAutoTrimAction")]
        public void ToggleAutoTrimAction(KSPActionParam param)
        {
            autoTrimEnabled = !autoTrimEnabled;
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
            updateDiveControlledParts();

            //Previous states
            wasAutoTrimming = autoTrimEnabled;
            wasMaintainingDepth = maintainDepth;
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
            onAutoTrimUpdated.Add(autoTrimUpdatedEvent);
            onDiveControlUpdated.Add(diveControlUpdatedEvent);
            onTriggerAndFluidRatesUpdated.Add(triggerAndFluidRatesUpdated);
        }

        public void Destroy()
        {
            onFloodBallast.Remove(floodBallastEvent);
            onVentBallast.Remove(ventBallastEvent);
            onCloseVents.Remove(closeVentsEvent);
            onEmergencySurface.Remove(emergencySurfaceEvent);
            onMaintainDepthUpdated.Remove(maintainDepthUpdatedEvent);
            onAutoTrimUpdated.Remove(autoTrimUpdatedEvent);
            onDiveControlUpdated.Remove(diveControlUpdatedEvent);
            onTriggerAndFluidRatesUpdated.Remove(triggerAndFluidRatesUpdated);
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
            if (ballastTanks == null || ballastTanks.Count == 0)
                return;

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

                // Maintain depth if needed.
                updateDepthState();

                // Sync other dive computers
                syncDiveControlComputers();
            }

            // Non-active dive computers monitor their control inputs and inform the master diver of state changes.
            else
            {
                if (divingControlEnabled != divingControlWasEnabled)
                {
                    divingControlWasEnabled = divingControlEnabled;
                    onDiveControlUpdated.Fire(part.vessel, this, divingControlEnabled);
                }
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
                if (!ballastFluidRate.Equals(prevPitchFluidRate))
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
        void floodBallastEvent(Vessel origin, SWDiveComputer diveComputer)
        {
            if (origin != part.vessel && !isActiveDiveComputer)
                return;

            floodBallast();
        }

        void ventBallastEvent(Vessel origin, SWDiveComputer diveComputer)
        {
            if (origin != part.vessel && !isActiveDiveComputer)
                return;

            ventBallast();
        }

        void closeVentsEvent(Vessel origin, SWDiveComputer diveComputer)
        {
            if (origin != part.vessel && !isActiveDiveComputer)
                return;

            closeVents();
        }

        void emergencySurfaceEvent(Vessel origin, SWDiveComputer diveComputer)
        {
            if (origin != part.vessel && !isActiveDiveComputer)
                return;

            emergencySurface();
        }

        void maintainDepthUpdatedEvent(Vessel origin, SWDiveComputer diveComputer, bool isEnabled)
        {
            if (origin != part.vessel && !isActiveDiveComputer)
                return;

            maintainDepth = isEnabled;
        }

        void autoTrimUpdatedEvent(Vessel origin, SWDiveComputer diveComputer, bool isEnabled)
        {
            if (origin != part.vessel && !isActiveDiveComputer)
                return;

            autoTrimEnabled = isEnabled;
        }

        void diveControlUpdatedEvent(Vessel origin, SWDiveComputer diveComputer, bool isEnabled)
        {
            if (origin != part.vessel && !isActiveDiveComputer)
                return;

            divingControlEnabled = isEnabled;
        }

        void triggerAndFluidRatesUpdated(Vessel origin, SWDiveComputer diveComputer)
        {
            if (origin != part.vessel)
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
        #endregion

        #region Helpers
        void updateInputParameters()
        {
            // Get roll and pitch angles.
            rollAngle = 90f - Vector3d.Angle(FlightGlobals.upAxis, vessel.ReferenceTransform.right);
            pitchAngle = 90f - Vector3d.Angle(FlightGlobals.upAxis, vessel.ReferenceTransform.up);

            //Get our pitch & roll triggers
            pitchTriggerDown = pitchAngleTrigger;
            pitchTriggerUp = -pitchAngleTrigger;
            rollTriggerPort = -rollAngleTrigger;
            rollTriggerStarboard = rollAngleTrigger;

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
                diveComputers = part.vessel.FindPartModulesImplementing<SWDiveComputer>();
                ballastTanks = vessel.FindPartModulesImplementing<SWBallastTank>();

                buoyancyControlledParts.Clear();
                for (int index = 0; index < partCount; index++)
                {
                    if (!vessel.Parts[index].Modules.Contains("SWBallastTank"))
                        buoyancyControlledParts.Add(vessel.Parts[index]);
                }
                buoyancyPartCount = buoyancyControlledParts.Count;
            }
        }

        void updateManeuverState()
        {
            maneuverState.x = FlightInputHandler.state.roll;
            maneuverState.y = FlightInputHandler.state.pitch;
            maneuverState.z = FlightInputHandler.state.yaw;

            vesselIsManeuvering = maneuverState.magnitude > 0;

            if (vesselIsManeuvering)
                autoTrimEnabled = false;

            if (FlightInputHandler.state.pitch != 0)
                maintainDepth = false;
        }

        void updateBallastTanksVentState()
        {
            int count = ballastTanks.Count;
            SWBallastTank ballastTank;

            for (int index = 0; index < count; index++)
            {
                ballastTank = ballastTanks[index];
                if (ballastTank.tankType == BallastTankTypes.Ballast)
                    ballastTank.SetVentState(ventState, ballastFluidRate);
            }
        }

        void updateDepthState()
        {
            if (maintainDepth != wasMaintainingDepth)
            {
                wasMaintainingDepth = maintainDepth;
            }
            if (!maintainDepth || !divingControlEnabled || vesselIsManeuvering)
                return;

            //If we're rising up then flood the ballast
            int count = ballastTanks.Count;
            SWBallastTank ballastTank;
            if (part.vessel.verticalSpeed > verticalSpeedTrigger && ventState != BallastVentStates.FloodingBallast)
            {
                ventState = BallastVentStates.FloodingBallast;

                for (int index = 0; index < count; index++)
                {
                    ballastTank = ballastTanks[index];
                    if (ballastTank.tankType == BallastTankTypes.Ballast)
                        ballastTank.SetVentState(BallastVentStates.FloodingBallast, ballastFluidRate);
                }
            }

            //If we're sinking then empty the ballast
            else if (part.vessel.verticalSpeed < -verticalSpeedTrigger && ventState != BallastVentStates.VentingBallast)
            {
                ventState = BallastVentStates.VentingBallast;

                for (int index = 0; index < count; index++)
                {
                    ballastTank = ballastTanks[index];
                    if (ballastTank.tankType == BallastTankTypes.Ballast)
                        ballastTank.SetVentState(BallastVentStates.VentingBallast, ballastFluidRate);
                }
            }

            //All good
            else if ((part.vessel.verticalSpeed <= verticalSpeedTrigger || part.vessel.verticalSpeed >= -verticalSpeedTrigger) && ventState != BallastVentStates.Closed)
            {
                for (int index = 0; index < count; index++)
                {
                    ballastTank = ballastTanks[index];

                    if (ballastTank.tankType == BallastTankTypes.Ballast)
                    {
                        if (ballastTank.ventState != BallastVentStates.Closed)
                            ballastTank.SetVentState(BallastVentStates.Closed, ballastFluidRate);
                    }
                }
            }
        }

        void updateTrimState()
        {
            if (autoTrimEnabled != wasAutoTrimming)
            {
                wasAutoTrimming = autoTrimEnabled;
            }
            if (!autoTrimEnabled)
                return;
            if (vesselIsManeuvering)
                return;

            updatePitchTrim();
            updateRollTrim();
        }

        void updatePitchTrim()
        {
            int count = ballastTanks.Count;
            SWBallastTank trimTank;

            // See if we need to pitch upward
            if (pitchAngle < pitchTriggerUp)
            {
                pitchControlActive = true;

                //Vent the forward trim tanks and flood the aft trim tanks.
                for (int index = 0; index < count; index++)
                {
                    trimTank = ballastTanks[index];
                    if (trimTank.CanTrimForward())
                        trimTank.SetVentState(BallastVentStates.VentingBallast, pitchFluidRate);
                    else if (trimTank.CanTrimAft())
                        trimTank.SetVentState(BallastVentStates.FloodingBallast, pitchFluidRate);
                }
            }

            // See if we need to pitch downward
            else if (pitchAngle > pitchTriggerDown)
            {
                pitchControlActive = true;

                //Vent the aft trim tanks and flood the forward trim tanks.
                for (int index = 0; index < count; index++)
                {
                    trimTank = ballastTanks[index];
                    if (trimTank.CanTrimForward())
                        trimTank.SetVentState(BallastVentStates.FloodingBallast, pitchFluidRate);
                    else if (trimTank.CanTrimAft())
                        trimTank.SetVentState(BallastVentStates.VentingBallast, pitchFluidRate);
                }
            }

            // We're level-ish, close all trim tank vents
            else
            {
                pitchControlActive = false;

                if (!rollControlActive)
                {
                    for (int index = 0; index < count; index++)
                    {
                        trimTank = ballastTanks[index];
                        if (trimTank.CanTrimForward() || trimTank.CanTrimAft())
                        {
                            if (trimTank.ventState != BallastVentStates.Closed)
                                trimTank.SetVentState(BallastVentStates.Closed, pitchFluidRate);
                        }
                    }
                }
            }
        }

        void updateRollTrim()
        {
            int count = ballastTanks.Count;
            SWBallastTank trimTank;

            // See if we need to pitch to port
            if (rollAngle < rollTriggerPort)
            {
                rollControlActive = true;

                //Vent the forward trim tanks and flood the aft trim tanks.
                for (int index = 0; index < count; index++)
                {
                    trimTank = ballastTanks[index];
                    if (trimTank.CanTrimStarboard())
                        trimTank.SetVentState(BallastVentStates.VentingBallast, rollFluidRate);
                    else if (trimTank.CanTrimPort())
                        trimTank.SetVentState(BallastVentStates.FloodingBallast, rollFluidRate);
                }
            }

            // See if we need to pitch to starboard
            else if (rollAngle > rollTriggerStarboard)
            {
                rollControlActive = true;

                //Vent the aft trim tanks and flood the forward trim tanks.
                for (int index = 0; index < count; index++)
                {
                    trimTank = ballastTanks[index];
                    if (trimTank.CanTrimStarboard())
                        trimTank.SetVentState(BallastVentStates.FloodingBallast, rollFluidRate);
                    else if (trimTank.CanTrimPort())
                        trimTank.SetVentState(BallastVentStates.VentingBallast, rollFluidRate);
                }
            }

            // We're level-ish, close all trim tank vents
            else
            {
                rollControlActive = false;

                if (!pitchControlActive)
                {
                    for (int index = 0; index < count; index++)
                    {
                        trimTank = ballastTanks[index];
                        if (trimTank.CanTrimPort() || trimTank.CanTrimStarboard())
                        {
                            if (trimTank.ventState != BallastVentStates.Closed)
                                trimTank.SetVentState(BallastVentStates.Closed, rollFluidRate);
                        }
                    }
                }
            }
        }

        void updateBallastState()
        {
            // Check ballast states. We'll update our state once all the ballast tanks are closed.
            // Different ballast tanks fill/empty at different rates so the dive computer's state
            // needs to detect when all the ballast tanks have finished filling or emptying.
            int count = ballastTanks.Count;
            SWBallastTank ballastTank;
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
            Fields["maintainDepth"].guiActiveEditor = false;
        }

        void updateGUI()
        {
            Fields["rollAngleTrigger"].guiActive = autoTrimEnabled;
            Fields["pitchAngleTrigger"].guiActive = autoTrimEnabled;

            Fields["divingControlEnabled"].guiActive = vessel.altitude > kMaxDivingControlDepth;
            Fields["diveStateString"].guiActive = divingControlEnabled;
            Fields["hullIntegrity"].guiActive = divingControlEnabled;
            Fields["maintainDepth"].guiActive = divingControlEnabled;
            Fields["verticalSpeedTrigger"].guiActive = divingControlEnabled;
            Events["FloodBallast"].active = divingControlEnabled;
            Events["VentBallast"].active = divingControlEnabled;
            Events["CloseVents"].active = divingControlEnabled;
            Events["EmergencySurface"].active = divingControlEnabled;

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
            SWDiveComputer diveComputer;

            for (int index = 0; index < count; index++)
            {
                diveComputer = diveComputers[index];
                if (diveComputer == this)
                    continue;

                diveComputer.ventState = ventState;
                diveComputer.vesselIsManeuvering = vesselIsManeuvering;
                diveComputer.pitchControlActive = pitchControlActive;
                diveComputer.rollControlActive = rollControlActive;
                diveComputer.prevBuoyancy = prevBuoyancy;

                if (pitchFluidRate != prevPitchFluidRate)
                {
                    prevPitchFluidRate = pitchFluidRate;
                    diveComputer.pitchFluidRate = pitchFluidRate;
                    diveComputer.prevPitchFluidRate = prevPitchFluidRate;
                }

                if (rollFluidRate != prevRollFluidRate)
                {
                    prevRollFluidRate = rollFluidRate;
                    diveComputer.rollFluidRate = rollFluidRate;
                    diveComputer.prevRollFluidRate = prevRollFluidRate;
                }

                if (verticalSpeedTrigger != prevVerticalSpeedTrigger)
                {
                    prevVerticalSpeedTrigger = verticalSpeedTrigger;
                    diveComputer.verticalSpeedTrigger = verticalSpeedTrigger;
                    diveComputer.prevVerticalSpeedTrigger = prevVerticalSpeedTrigger;
                }

                if (pitchAngleTrigger != prevPitchAngleTrigger)
                {
                    prevPitchAngleTrigger = pitchAngleTrigger;
                    diveComputer.pitchAngleTrigger = pitchAngleTrigger;
                    diveComputer.prevPitchAngleTrigger = prevPitchAngleTrigger;
                }

                if (rollAngleTrigger != prevRollAngleTrigger)
                {
                    prevRollAngleTrigger = rollAngleTrigger;
                    diveComputer.rollAngleTrigger = rollAngleTrigger;
                    diveComputer.prevRollAngleTrigger = prevRollAngleTrigger;
                }

                if (maintainDepth != wasMaintainingDepth)
                {
                    wasMaintainingDepth = maintainDepth;
                    diveComputer.maintainDepth = maintainDepth;
                    diveComputer.wasMaintainingDepth = wasMaintainingDepth;
                }

                if (autoTrimEnabled != wasAutoTrimming)
                {
                    wasAutoTrimming = autoTrimEnabled;
                    diveComputer.wasMaintainingDepth = wasMaintainingDepth;
                    diveComputer.wasAutoTrimming = wasAutoTrimming;
                }

                if (ballastFluidRate != prevBallastFluidRate)
                {
                    prevBallastFluidRate = ballastFluidRate;
                    diveComputer.ballastFluidRate = ballastFluidRate;
                    diveComputer.prevBallastFluidRate = prevBallastFluidRate;
                }

                if (divingControlEnabled != divingControlWasEnabled)
                {
                    divingControlWasEnabled = divingControlEnabled;
                    diveComputer.divingControlEnabled = divingControlEnabled;
                    diveComputer.divingControlWasEnabled = divingControlWasEnabled;
                }
            }
}
        #endregion
    }
}
