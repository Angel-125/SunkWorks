using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.Localization;

namespace SunkWorks.Submarine
{
    #region Enumerators
    /// <summary>
    /// Type of ballast tank. This is used for auto-triming the boat.
    /// </summary>
    public enum BallastTankTypes
    {
        /// <summary>
        /// Generic ballast tank. Does not trim.
        /// </summary>
        Ballast,

        /// <summary>
        /// Forward trim tank
        /// </summary>
        ForwardTrim,

        /// <summary>
        /// Forward-port trim
        /// </summary>
        ForwardPort,

        /// <summary>
        /// Forward-starboard trim
        /// </summary>
        ForwardStarboard,

        /// <summary>
        /// Port trim tank
        /// </summary>
        PortTrim,

        /// <summary>
        /// Starboard trim tank
        /// </summary>
        StarboardTrim,

        /// <summary>
        /// Aft trim tank
        /// </summary>
        AftTrim,

        /// <summary>
        /// Aft-port trim
        /// </summary>
        AftPort,

        /// <summary>
        /// Aft-starboard trim.
        /// </summary>
        AftStarboard
    }

    /// <summary>
    /// Vent states of the ballast tank
    /// </summary>
    public enum BallastVentStates
    {
        /// <summary>
        /// Tank is closed
        /// </summary>
        Closed,

        /// <summary>
        /// Tank is flooding ballast
        /// </summary>
        FloodingBallast,

        /// <summary>
        /// Tank is venting ballast
        /// </summary>
        VentingBallast
    }
    #endregion

    /// <summary>
    /// Represents a ballast tank
    /// </summary>
    [KSPModule("Ballast Tank")]
    public class SWBallastTank: PartModule
    {
        #region Constants
        public const string kBallastGroup = "Ballast";
        const float kMessageDuration = 5f;
        const float kDefaultRate = 10f;
        const float kMinBuoyancy = 0.01f;
        #endregion

        #region Fields
        /// <summary>
        /// Debug flag
        /// </summary>
        [KSPField]
        public bool debugMode = false;

        /// <summary>
        /// Name of the part's intake transform.
        /// </summary>
        [KSPField]
        public string intakeTransformName = "intakeTransform";

        /// <summary>
        /// Ballast resource
        /// </summary>
        [KSPField]
        public string ballastResourceName = "IntakeLqd";

        /// <summary>
        /// Name of the venting effect to play when the tank is taking on ballast.
        /// </summary>
        [KSPField]
        public string addBallastEffect = string.Empty;

        /// <summary>
        /// Name of the venting effect to play when the tank is venting ballast.
        /// </summary>
        [KSPField]
        public string ventBallastEffect = string.Empty;

        /// <summary>
        /// How many seconds to fill the ballast tank
        /// </summary>
        [KSPField]
        public double fullFillRate = kDefaultRate;

        /// <summary>
        /// How many seconds to vent the ballast tank
        /// </summary>
        [KSPField]
        public double fullVentRate = kDefaultRate;

        /// <summary>
        /// Type of ballast tank
        /// </summary>
        [KSPField(isPersistant = true)]
        public BallastTankTypes tankType;

        /// <summary>
        /// Current display state of the ballast tank
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_SUNKWORKS_tankType", groupName = kBallastGroup, groupDisplayName = "#LOC_SUNKWORKS_ballastTank")]
        public string tankTypeString;

        /// <summary>
        /// Current state of the ballast tank
        /// </summary>
        [KSPField(isPersistant = true)]
        public BallastVentStates ventState;

        /// <summary>
        /// Current display state of the ballast tank
        /// </summary>
        [KSPField(guiActive = true, guiName = "#LOC_SUNKWORKS_ventState", groupName = kBallastGroup, groupDisplayName = "#LOC_SUNKWORKS_ballastTank")]
        public string ventStateString;

        /// <summary>
        /// Flag to indicate whether or not to update symmetry tanks.
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = true, isPersistant = true, guiName = "#LOC_SUNKWORKS_updateSymmetryTanks", groupName = kBallastGroup, groupDisplayName = "#LOC_SUNKWORKS_ballastTank")]
        [UI_Toggle(enabledText = "#LOC_SUNKWORKS_on", disabledText = "#LOC_SUNKWORKS_off")]
        public bool updateSymmetryTanks = true;

        /// <summary>
        /// Percentage of the overall ballast fluid transfer rate
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_SUNKWORKS_fluidTransferPercentage", isPersistant = true)]
        [UI_FloatRange(maxValue = 100f, minValue = 0.0f, scene = UI_Scene.All, stepIncrement = 1f)]
        public float fluidTransferPercentage = 100f;

        /// <summary>
        /// The skill required to reconfigure the ballast tank
        /// </summary>
        [KSPField]
        public string reconfigureSkill = "ConverterSkill";

        /// <summary>
        /// Skill rank needed to reconfigure the ballast tank.
        /// </summary>
        [KSPField]
        public int reconfigureRank = 0;

        /// <summary>
        /// Index for the tank types.
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = true)]
        [UI_VariantSelector(affectSymCounterparts = UI_Scene.All, controlEnabled = true, scene = UI_Scene.All)]
        public int tankTypeIndex;

        [KSPField()]
        float tankBouyancy = 1f;
        #endregion

        #region Housekeeping
        /// <summary>
        /// Flag to indicate whether or not the fuel tank has been converted to ballast tank.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool isConverted = false;

        /// <summary>
        /// Flag to indicate that we need to update the PAW
        /// </summary>
        public bool updatePAW;

        /// <summary>
        /// The part that is hosting the SWBallastTank.
        /// </summary>
        public Part hostPart;

        /// <summary>
        /// The PartResource containing the ballast.
        /// </summary>
        public PartResource ballastResource;

        UIPartActionWindow actionWindow;
        float baseBuoyancy = 0.0f;
        Transform[] intakeTransforms;
        bool intakeIsUnderwater = false;
        double fillRate;
        double ventRate;
        #endregion

        #region Events
        /// <summary>
        /// Signifies that the ballast has been updated
        /// </summary>
        public static EventData<SWBallastTank, BallastTankTypes, BallastVentStates, bool> onBallastTankUpdated = new EventData<SWBallastTank, BallastTankTypes, BallastVentStates, bool>("onBallastTankUpdated");

        /// <summary>
        /// Converts the host part to a ballast tank.
        /// </summary>
        [KSPEvent(guiActiveEditor = true, guiActiveUnfocused = true, unfocusedRange = 5.0f, guiName = "#LOC_SUNKWORKS_convertToBallast", groupName = kBallastGroup, groupDisplayName = "#LOC_SUNKWORKS_ballastTank")]
        public void ConvertToBallastTank()
        {
            if (hostPart == null)
            {
                getHostPart();
                if (hostPart == null)
                    return;
            }
            // if there is no definition for the ballast resource then we're done.
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
            if (!definitions.Contains(ballastResourceName))
            {
                Debug.Log("[SWBallastTankConverter] - No definition found for " + ballastResourceName);
                return;
            }

            // If the host part has no resources then we're done.
            if (hostPart.Resources.Count == 0)
            {
                ScreenMessages.PostScreenMessage("#LOC_SUNKWORKS_noResourcesToConvert", kMessageDuration, ScreenMessageStyle.UPPER_CENTER);
                return;
            }

            // If we require skills to reconfigure, then check now
            if (!hasSufficientSkill())
                return;

            PartResourceDefinition definition;
            PartResource resource;
            double maxAmount = 0;

            // Calculate total resource volume.
            int count = hostPart.Resources.Count;
            for (int index = 0; index < count; index++)
            {
                resource = hostPart.Resources[index];
                definition = definitions[resource.resourceName];
                maxAmount += (definition.density * resource.maxAmount);
            }

            // Remove all part resources
            hostPart.Resources.Clear();

            // Calculate the total units for the ballast resource.
            definition = definitions[ballastResourceName];
            maxAmount = maxAmount / definition.density;

            // Now add the ballast resource.
            PartResource ballastResource = hostPart.Resources.Add(ballastResourceName, 0, maxAmount, true, true, false, true, PartResource.FlowMode.Both);

            Events["RestoreResourceCapacity"].active = true;
            Events["ConvertToBallastTank"].active = false;

            // Update symmetry parts
            SWBallastTank ballastTank;
            count = part.symmetryCounterparts.Count;
            for (int index = 0; index < count; index++)
            {
                ballastTank = part.symmetryCounterparts[index].FindModuleImplementing<SWBallastTank>();
                ballastTank.Events["RestoreResourceCapacity"].active = true;
                ballastTank.Events["ConvertToBallastTank"].active = false;
                ballastTank.isConverted = true;
                ballastTank.updatePAW = true;
            }
            ballastTank = part.symmetryCounterparts[0].FindModuleImplementing<SWBallastTank>();
            onBallastTankUpdated.Fire(ballastTank, ballastTank.tankType, ballastTank.ventState, ballastTank.isConverted);

            count = hostPart.symmetryCounterparts.Count;
            Part symmetryPart;
            for (int index = 0; index < count; index++)
            {
                symmetryPart = hostPart.symmetryCounterparts[index];
                symmetryPart.Resources.Clear();
                symmetryPart.Resources.Add(ballastResourceName, 0, maxAmount, true, true, false, true, PartResource.FlowMode.Both);

                MonoUtilities.RefreshContextWindows(symmetryPart);
                GameEvents.onPartResourceListChange.Fire(symmetryPart);
            }

            // Now update our PAW
            isConverted = true;
            updatePAW = true;
            MonoUtilities.RefreshContextWindows(hostPart);
            GameEvents.onPartResourceListChange.Fire(hostPart);
            onBallastTankUpdated.Fire(this, tankType, ventState, isConverted);
        }

        /// <summary>
        /// Restores the host part's resource storage capacity.
        /// </summary>
        [KSPEvent(guiActiveEditor = true, guiActiveUnfocused = true, unfocusedRange = 5.0f, guiName = "#LOC_SUNKWORKS_restoreResources", groupName = kBallastGroup, groupDisplayName = "#LOC_SUNKWORKS_ballastTank")]
        public void RestoreResourceCapacity()
        {
            if (hostPart == null)
            {
                getHostPart();
                if (hostPart == null)
                    return;
            }
            hostPart.Resources.Clear();

            // Update symmetry parts
            int symmetryPartCount = hostPart.symmetryCounterparts.Count;
            for (int index = 0; index < symmetryPartCount; index++)
                hostPart.symmetryCounterparts[index].Resources.Clear();

            SWBallastTank ballastTank;
            symmetryPartCount = part.symmetryCounterparts.Count;
            for (int index = 0; index < symmetryPartCount; index++)
            {
                ballastTank = part.symmetryCounterparts[index].FindModuleImplementing<SWBallastTank>();
                ballastTank.Events["RestoreResourceCapacity"].active = false;
                ballastTank.Events["ConvertToBallastTank"].active = true;
                ballastTank.isConverted = true;
                ballastTank.updatePAW = true;
            }
            ballastTank = part.symmetryCounterparts[0].FindModuleImplementing<SWBallastTank>();
            onBallastTankUpdated.Fire(ballastTank, ballastTank.tankType, ballastTank.ventState, ballastTank.isConverted);

            // Restore resources
            PartResource resource;
            int count = hostPart.partInfo.partPrefab.Resources.Count;
            ConfigNode node;
            symmetryPartCount = hostPart.symmetryCounterparts.Count;
            for (int index = 0; index < count; index++)
            {
                resource = hostPart.partInfo.partPrefab.Resources[index];
                node = new ConfigNode("RESOURCE");
                resource.Save(node);
                if (HighLogic.LoadedSceneIsFlight && node.HasValue("maxAmount"))
                    node.RemoveValue("maxAmount");
                hostPart.Resources.Add(node);

                // Update symmetry parts
                for (int symmetryIndex = 0; symmetryIndex < symmetryPartCount; symmetryIndex++)
                    hostPart.symmetryCounterparts[symmetryIndex].Resources.Add(node);
            }

            // Update PAW
            Events["RestoreResourceCapacity"].active = false;
            Events["ConvertToBallastTank"].active = true;

            isConverted = false;
            updatePAW = true;

            MonoUtilities.RefreshContextWindows(hostPart);
            GameEvents.onPartResourceListChange.Fire(hostPart);

            symmetryPartCount = hostPart.symmetryCounterparts.Count;
            for (int index = 0; index < symmetryPartCount; index++)
            {
                MonoUtilities.RefreshContextWindows(hostPart.symmetryCounterparts[index]);
                GameEvents.onPartResourceListChange.Fire(hostPart.symmetryCounterparts[index]);
            }
            onBallastTankUpdated.Fire(this, tankType, ventState, isConverted);
        }

        /// <summary>
        /// Floods the ballast tank
        /// </summary>
        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "#LOC_SUNKWORKS_floodBallast", groupName = kBallastGroup, groupDisplayName = "#LOC_SUNKWORKS_ballastTank")]
        public void FloodBallast()
        {
            ventState = BallastVentStates.FloodingBallast;
            updateGUI();
            updateSymmetryVentState();
            onBallastTankUpdated.Fire(this, tankType, ventState, isConverted);
        }

        /// <summary>
        /// Vents ballast tank
        /// </summary>
        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "#LOC_SUNKWORKS_ventBallast", groupName = kBallastGroup, groupDisplayName = "#LOC_SUNKWORKS_ballastTank")]
        public void VentBallast()
        {
            ventState = BallastVentStates.VentingBallast;
            updateGUI();
            updateSymmetryVentState();
            onBallastTankUpdated.Fire(this, tankType, ventState, isConverted);
        }

        /// <summary>
        /// Close ballast vents
        /// </summary>
        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "#LOC_SUNKWORKS_closeVents", groupName = kBallastGroup, groupDisplayName = "#LOC_SUNKWORKS_ballastTank")]
        public void CloseVents()
        {
            ventState = BallastVentStates.Closed;
            updateGUI();
            updateSymmetryVentState();
            onBallastTankUpdated.Fire(this, tankType, ventState, isConverted);
        }

        /// <summary>
        /// Emergency surface
        /// </summary>
        [KSPEvent(guiActive = true, guiName = "#LOC_SUNKWORKS_emergencySurface", groupName = kBallastGroup, groupDisplayName = "#LOC_SUNKWORKS_ballastTank")]
        public void EmergencySurface()
        {
            DumpBallast();
            onBallastTankUpdated.Fire(this, tankType, ventState, isConverted);
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
            if (ventState == BallastVentStates.FloodingBallast)
                ventState = BallastVentStates.Closed;
            else
                ventState = BallastVentStates.FloodingBallast;

            updateGUI();
        }

        /// <summary>
        /// Action to vent ballast tank
        /// </summary>
        /// <param name="param"></param>
        [KSPAction("#LOC_SUNKWORKS_ventBallast")]
        public void VentBallastAction(KSPActionParam param)
        {
            if (ventState == BallastVentStates.VentingBallast)
                ventState = BallastVentStates.Closed;
            else
                ventState = BallastVentStates.VentingBallast;

            updateGUI();
        }

        /// <summary>
        /// Close ballast vents action
        /// </summary>
        /// <param name="param"></param>
        [KSPAction("#LOC_SUNKWORKS_closeVents")]
        public void CloseVentsAction(KSPActionParam param)
        {
            ventState = BallastVentStates.Closed;
            updateGUI();
            updateSymmetryVentState();
        }

        /// <summary>
        /// Emergency surface action
        /// </summary>
        /// <param name="param"></param>
        [KSPAction("#LOC_SUNKWORKS_emergencySurface")]
        public void EmergencySurfaceAction(KSPActionParam param)
        {
            DumpBallast();
        }
        #endregion

        #region API
        /// <summary>
        /// Dumps ballast
        /// </summary>
        /// <param name="updateSymmetryParts">A bool indicating whether or not to update symmetry parts</param>
        public void DumpBallast(bool updateSymmetryParts = true)
        {
            //Set the vent state
            ventState = BallastVentStates.Closed;
            updateSymmetryVentState();

            //Clear the resource's ballast.
            if (ballastResource != null && ballastResource.flowState)
                ballastResource.amount = 0.0f;

            //Clear the ballast on symmetry parts
            if (!updateSymmetryParts)
                return;
            int count = part.symmetryCounterparts.Count;
            SWBallastTank ballastTank;
            for (int index = 0; index < count; index++)
            {
                ballastTank = part.symmetryCounterparts[index].FindModuleImplementing<SWBallastTank>();
                if (ballastTank != null && ballastTank.ballastResource != null && ballastResource.flowState)
                    ballastTank.ballastResource.amount = 0.0f;
            }
        }

        /// <summary>
        /// Sets the vent state
        /// </summary>
        /// <param name="state">The new BallastVentStates</param>
        /// <param name="fluidTransferRate">A float containing the new fluid transfer percentage</param>
        public void SetVentState(BallastVentStates state, float fluidTransferRate)
        {
            BallastVentStates prevState = ventState;

            // If we've finished venting or finished flooding then we're done.
            if ((state == BallastVentStates.VentingBallast && ballastResource.amount <= 0) || (state == BallastVentStates.FloodingBallast && ballastResource.amount >= ballastResource.maxAmount))
                return;

            // Record vent state & transfer rate
            ventState = state;
            fluidTransferPercentage = fluidTransferRate;

            // Update UI if needed.
            if (ventState != prevState)
                updateGUI();
        }

        /// <summary>
        /// Indicates that the tank can be used for forward trim.
        /// </summary>
        /// <returns>True if it can be used for trim, false if not.</returns>
        public bool CanTrimForward()
        {
            return tankType == BallastTankTypes.ForwardTrim || tankType == BallastTankTypes.ForwardPort || tankType == BallastTankTypes.ForwardStarboard;
        }

        /// <summary>
        /// Indicates that the tank can be used for aft trim.
        /// </summary>
        /// <returns>True if it can be used for trim, false if not.</returns>
        public bool CanTrimAft()
        {
            return tankType == BallastTankTypes.AftTrim || tankType == BallastTankTypes.AftPort || tankType == BallastTankTypes.AftStarboard;
        }

        /// <summary>
        /// Indicates that the tank can be used for portside trim.
        /// </summary>
        /// <returns>True if it can be used for trim, false if not.</returns>
        public bool CanTrimPort()
        {
            return tankType == BallastTankTypes.PortTrim || tankType == BallastTankTypes.ForwardPort || tankType == BallastTankTypes.AftPort;
        }

        /// <summary>
        /// Indicates that the tank can be used for starboard trim.
        /// </summary>
        /// <returns>True if it can be used for trim, false if not.</returns>
        public bool CanTrimStarboard()
        {
            return tankType == BallastTankTypes.StarboardTrim || tankType == BallastTankTypes.ForwardStarboard || tankType == BallastTankTypes.AftStarboard;
        }
        #endregion

        #region Overrides
        /// <summary>
        /// Handles the OnDestroy event
        /// </summary>
        public void OnDestroy()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                GameEvents.OnEVAConstructionModePartAttached.Remove(onEVAConstructionModePartAttached);
                GameEvents.OnEVAConstructionModePartDetached.Remove(onEVAConstructionModePartDetached);
            }
            if (HighLogic.LoadedSceneIsEditor)
            {
                GameEvents.onEditorPartPlaced.Remove(onEditorPartPlaced);
                GameEvents.onEditorPartEvent.Remove(onEditorPartEvent);
            }
            GameEvents.onPartActionUIShown.Remove(onPartActionUIShown);
            GameEvents.onPartActionUIDismiss.Remove(onPartActionUIDismiss);
            GameEvents.onPartResourceListChange.Remove(onPartResourceListChange);
            SWBallastTank.onBallastTankUpdated.Remove(OnBallastTankUpdated);
        }

        /// <summary>
        /// Handles OnAwake event
        /// </summary>
        public override void OnAwake()
        {
            base.OnAwake();

            PartVariant variant = new PartVariant(BallastTankTypes.Ballast.ToString(), Localizer.Format("#LOC_SUNKWORKS_tankTypeBallast"), null);
            variant.PrimaryColor = "#ffffff";
            variant.SecondaryColor = "#000000";
            part.baseVariant = variant;

            if (HighLogic.LoadedSceneIsFlight)
            {
                GameEvents.OnEVAConstructionModePartAttached.Add(onEVAConstructionModePartAttached);
                GameEvents.OnEVAConstructionModePartDetached.Remove(onEVAConstructionModePartDetached);
            }
            if (HighLogic.LoadedSceneIsEditor)
            {
                GameEvents.onEditorPartPlaced.Add(onEditorPartPlaced);
                GameEvents.onEditorPartEvent.Add(onEditorPartEvent);
            }
            GameEvents.onPartActionUIShown.Add(onPartActionUIShown);
            GameEvents.onPartActionUIDismiss.Add(onPartActionUIDismiss);
            GameEvents.onPartResourceListChange.Add(onPartResourceListChange);
            SWBallastTank.onBallastTankUpdated.Add(OnBallastTankUpdated);
        }

        /// <summary>
        /// Gets the module display name.
        /// </summary>
        /// <returns>A string containing the display name.</returns>
        public override string GetModuleDisplayName()
        {
            return Localizer.Format("#LOC_SUNKWORKS_ballastTank");
        }

        /// <summary>
        /// Gets the module description.
        /// </summary>
        /// <returns>A string containing the module description.</returns>
        public override string GetInfo()
        {
            return Localizer.Format("#LOC_SUNKWORKS_ballastInfo");
        }

        /// <summary>
        /// Handles the OnStart event.
        /// </summary>
        /// <param name="state">A StartState containing the starting state.</param>
        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            if (!HighLogic.LoadedSceneIsFlight && !HighLogic.LoadedSceneIsEditor)
                return;

            // Setup tank selector
            setupTankSelector();

            // Setup fill rate
            setupFillRate();

            getHostPart();

            // Strip non-ballast resources for converted tanks.
            removeNonBallastResources();

            // Setup buoyancy
            setupHostPartBuoyancy();

            Fields["tankBouyancy"].guiActive = debugMode;
            Fields["tankBouyancy"].guiActiveEditor = false;
        }

        /// <summary>
        /// Handles FixedUpdate
        /// </summary>
        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || part.ShieldedFromAirstream)
                return;
            if (!part.vessel.Splashed || ballastResource == null || !ballastResource.flowState)
            {
                part.Effect(addBallastEffect, 0.0f);
                part.Effect(ventBallastEffect, 0.0f);
                return;
            }
            if (hostPart == null)
            {
                getHostPart();
                if (hostPart == null || ballastResource == null)
                    return;
            }
            if (ventState == BallastVentStates.Closed)
            {
                part.Effect(addBallastEffect, 0.0f);
                part.Effect(ventBallastEffect, 0.0f);
                return;
            }

            //Update ballast amount
            updateBallastResource();

            //Update our bouyancy
            tankBouyancy = (float)(ballastResource.amount / ballastResource.maxAmount);
            hostPart.buoyancy = baseBuoyancy * (1 - tankBouyancy);
            if (hostPart.buoyancy < kMinBuoyancy)
                hostPart.buoyancy = kMinBuoyancy;
            tankBouyancy = hostPart.buoyancy;

            //Now update effects
            updateEffects();
        }

        /// <summary>
        /// Handles the Update event.
        /// </summary>
        public void Update()
        {
            if (!HighLogic.LoadedSceneIsFlight && !HighLogic.LoadedSceneIsEditor)
                return;
            if (actionWindow == null || !updatePAW)
                return;

            // When changing a part's list of resources, the part action window may not be updated properly, so we need to manually refresh the window.
            actionWindow.CreatePartList(true);
            updatePAW = false;
        }
        #endregion

        #region Helpers
        protected bool hasSufficientSkill()
        {
            if (!HighLogic.LoadedSceneIsFlight || string.IsNullOrEmpty(reconfigureSkill))
                return true;

            //Check the crew roster
            ProtoCrewMember[] vesselCrew = vessel.GetVesselCrew().ToArray();
            for (int index = 0; index < vesselCrew.Length; index++)
            {
                if (vesselCrew[index].HasEffect(reconfigureSkill))
                {
                    if (reconfigureRank > 0)
                    {
                        int crewRank = vesselCrew[index].experienceTrait.CrewMemberExperienceLevel();
                        if (crewRank >= reconfigureRank)
                            return true;
                    }
                    else
                    {
                        return true;
                    }
                }
            }

            //Insufficient skill
            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_SUNKWORKS_insufficientReconfigureSkill"));
            return false;
        }

        void getHostPart()
        {
            if ((!HighLogic.LoadedSceneIsFlight && !HighLogic.LoadedSceneIsEditor) || string.IsNullOrEmpty(ballastResourceName) || part == null)
                return;

            // Get the intake transforms
            if (!string.IsNullOrEmpty(intakeTransformName))
            {
                intakeTransforms = part.FindModelTransforms(intakeTransformName);
            }

            // Get the ballast resource & host part
            if (part.Resources.Contains(ballastResourceName))
            {
                hostPart = part;
            }
            else
            {
                hostPart = part.parent;
            }

            // Finish setup
            if (hostPart != null)
            {
                // Set buoyancy
                baseBuoyancy = hostPart.partInfo.partPrefab.buoyancy;

                // Get the ballast resource
                if (hostPart.Resources.Contains(ballastResourceName))
                {
                    ballastResource = hostPart.Resources[ballastResourceName];
                    fillRate = ballastResource.maxAmount / fullFillRate;
                    ventRate = ballastResource.maxAmount / fullVentRate;
                }
            }

            updateGUI();
        }

        void onEVAConstructionModePartDetached(Vessel hostVessel, Part attachedPart)
        {

        }

        void onEVAConstructionModePartAttached(Vessel hostVessel, Part attachedPart)
        {
            if (attachedPart != part)
                return;

            getHostPart();
        } 

        void onEditorPartPlaced(Part attachedPart)
        {
            if (attachedPart != part)
                return;

            getHostPart();
        }

        void onEditorPartEvent(ConstructionEventType eventType, Part eventPart)
        {
            if (eventType != ConstructionEventType.PartDetached || eventPart.FindModuleImplementing<SWBallastTank>() == null)
                return;
            getHostPart();
        }

        void onPartActionUIShown(UIPartActionWindow window, Part actionPart)
        {
            if (hostPart == null)
                getHostPart();
            if (actionPart != hostPart)
                return;

            // When changing a part's list of resources, the part action window may not be updated properly, so we need to manually refresh the window.
            // To do that we track the action window.
            actionWindow = window;
        }

        void onPartActionUIDismiss(Part actionPart)
        {
            if (actionPart != hostPart)
                return;
            actionWindow = null;
        }

        void onPartResourceListChange(Part eventPart)
        {
            if (hostPart == null)
                getHostPart();
            if (hostPart != eventPart)
                return;

            if (hostPart.Resources.Contains(ballastResourceName))
            {
                ballastResource = hostPart.Resources[ballastResourceName];
                fillRate = ballastResource.maxAmount / fullFillRate;
                ventRate = ballastResource.maxAmount / fullVentRate;
            }
            else
                ballastResource = null;
        }

        void OnBallastTankUpdated(SWBallastTank ballastTank, BallastTankTypes ballastTankType, BallastVentStates ballastVentState, bool tankIsConverted)
        {
            // Stay in sync with other ballast tanks that are controlling the host part.
            if (ballastTank.hostPart != hostPart)
                return;

            tankType = ballastTankType;
            ventState = ballastVentState;
            isConverted = tankIsConverted;
            updatePAW = true;
            updateGUI();
        }

        void updateSymmetryVentState()
        {
            if (!updateSymmetryTanks)
                return;
            int count = part.symmetryCounterparts.Count;
            SWBallastTank ballastTank;
            for (int index = 0; index < count; index++)
            {
                ballastTank = part.symmetryCounterparts[index].FindModuleImplementing<SWBallastTank>();
                if (ballastTank != null)
                {
                    ballastTank.ventState = ventState;
                    ballastTank.updateGUI();
                }
            }
            ballastTank = part.symmetryCounterparts[0].FindModuleImplementing<SWBallastTank>();
            onBallastTankUpdated.Fire(ballastTank, ballastTank.tankType, ballastTank.ventState, ballastTank.isConverted);
        }

        void updateBallastResource()
        {
            //If we are filling ballast then increase the amount of ballast in the part.
            if (ventState == BallastVentStates.FloodingBallast)
            {
                // Make sure we're underwater.
                if (!part.vessel.mainBody.ocean)
                    return;

                //Make sure at least one of our intake transforms is underwater.
                if (intakeTransforms == null)
                    return;
                if (!this.part.vessel.mainBody.ocean)
                    return;
                intakeIsUnderwater = false;
                for (int index = 0; index < intakeTransforms.Length; index++)
                {
                    if (FlightGlobals.getAltitudeAtPos((Vector3d)intakeTransforms[index].position, this.part.vessel.mainBody) <= 0.0f)
                    {
                        intakeIsUnderwater = true;
                        break;
                    }
                }
                if (!intakeIsUnderwater)
                    return;

                //All good, fill the tank
                if (ballastResource != null)
                    ballastResource.amount += fillRate * (fluidTransferPercentage/100f) * TimeWarp.fixedDeltaTime;

                //Close the vents if we've filled the ballast.
                if (ballastResource.amount >= ballastResource.maxAmount)
                {
                    ballastResource.amount = ballastResource.maxAmount;
                    ventState = BallastVentStates.Closed;
                    updateGUI();
                }
            }

            //If we are venting ballast then reduce the amount of ballast in the part.
            else if (ventState == BallastVentStates.VentingBallast)
            {
                ballastResource.amount -= ventRate * (fluidTransferPercentage / 100f) * TimeWarp.fixedDeltaTime;

                //Close the vents if we've emptied the ballast.
                if (ballastResource.amount <= 0.001f)
                {
                    ballastResource.amount = 0.0f;
                    ventState = BallastVentStates.Closed;
                    updateGUI();
                }
            }
        }

        void updateEffects()
        {
            switch (ventState)
            {
                case BallastVentStates.Closed:
                default:
                    part.Effect(addBallastEffect, 0.0f);
                    part.Effect(ventBallastEffect, 0.0f);
                    break;

                case BallastVentStates.FloodingBallast:
                    part.Effect(addBallastEffect, intakeIsUnderwater ? 1.0f : 0.0f);
                    part.Effect(ventBallastEffect, 0.0f);
                    break;


                case BallastVentStates.VentingBallast:
                    part.Effect(addBallastEffect, 0.0f);
                    part.Effect(ventBallastEffect, 1.0f);
                    break;
            }
        }

        void removeNonBallastResources()
        {
            if (isConverted && hostPart != null && !string.IsNullOrEmpty(ballastResourceName))
            {
                List<PartResource> doomed = new List<PartResource>();
                int count = hostPart.Resources.Count;
                for (int index = 0; index < count; index++)
                {
                    if (hostPart.Resources[index].resourceName != ballastResourceName)
                        doomed.Add(hostPart.Resources[index]);
                }
                count = doomed.Count;
                for (int index = 0; index < count; index++)
                {
                    hostPart.Resources.Remove(doomed[index]);
                }

                if (hostPart != part)
                    Events["RestoreResourceCapacity"].active = true;
            }
        }

        void setupHostPartBuoyancy()
        {
            if (hostPart != null && ballastResource != null)
            {
                if (ballastResource.amount <= 0)
                    hostPart.buoyancy = 1.0f;
                else if (ballastResource.amount >= ballastResource.maxAmount)
                    hostPart.buoyancy = 0f;
            }
            tankBouyancy = hostPart.buoyancy;
        }

        void setupFillRate()
        {
            if (fullFillRate <= 0)
                fullFillRate = kDefaultRate;
            if (fullVentRate <= 0)
                fullVentRate = kDefaultRate;
        }

        void setupTankSelector()
        {
            UI_VariantSelector variantSelector = null;

            int count = Fields.Count;
            string fieldNames = string.Empty;
            for (int index = 0; index < count; index++)
                fieldNames += Fields[index].name + ";";

            // Setup variant selector
            if (HighLogic.LoadedSceneIsFlight)
                variantSelector = this.Fields["tankTypeIndex"].uiControlFlight as UI_VariantSelector;
            else //if (HighLogic.LoadedSceneIsEditor)
                variantSelector = this.Fields["tankTypeIndex"].uiControlEditor as UI_VariantSelector;

            variantSelector.onFieldChanged += new Callback<BaseField, object>(this.onVariantChanged);

            // Setup variant list
            variantSelector.variants = new List<PartVariant>();

            PartVariant variant = new PartVariant(BallastTankTypes.Ballast.ToString(), Localizer.Format("#LOC_SUNKWORKS_tankTypeBallast"), null);
            variant.PrimaryColor = "#ffffff";
            variant.SecondaryColor = "#000000";
            variantSelector.variants.Add(variant);

            variant = new PartVariant(BallastTankTypes.ForwardTrim.ToString(), Localizer.Format("#LOC_SUNKWORKS_tankTypeForwardTrim"), null);
            variant.PrimaryColor = "#ffffff";
            variant.SecondaryColor = "#ffffff";
            variantSelector.variants.Add(variant);

            variant = new PartVariant(BallastTankTypes.ForwardPort.ToString(), Localizer.Format("#LOC_SUNKWORKS_tankTypeFwdPort"), null);
            variant.PrimaryColor = "#ffffff";
            variant.SecondaryColor = "#ff0000";
            variantSelector.variants.Add(variant);

            variant = new PartVariant(BallastTankTypes.ForwardStarboard.ToString(), Localizer.Format("#LOC_SUNKWORKS_tankTypeFwdStarboard"), null);
            variant.PrimaryColor = "#ffffff";
            variant.SecondaryColor = "#00ff00";
            variantSelector.variants.Add(variant);

            variant = new PartVariant(BallastTankTypes.PortTrim.ToString(), Localizer.Format("#LOC_SUNKWORKS_tankTypePortTrim"), null);
            variant.PrimaryColor = "#ff0000";
            variant.SecondaryColor = "#ff0000";
            variantSelector.variants.Add(variant);

            variant = new PartVariant(BallastTankTypes.StarboardTrim.ToString(), Localizer.Format("#LOC_SUNKWORKS_tankTypeStarboardTrim"), null);
            variant.PrimaryColor = "#00ff00";
            variant.SecondaryColor = "#00ff00";
            variantSelector.variants.Add(variant);

            variant = new PartVariant(BallastTankTypes.AftTrim.ToString(), Localizer.Format("#LOC_SUNKWORKS_tankTypeAftTrim"), null);
            variant.PrimaryColor = "#000000";
            variant.SecondaryColor = "#000000";
            variantSelector.variants.Add(variant);

            variant = new PartVariant(BallastTankTypes.AftPort.ToString(), Localizer.Format("#LOC_SUNKWORKS_tankTypeAftPort"), null);
            variant.PrimaryColor = "#000000";
            variant.SecondaryColor = "#ff0000";
            variantSelector.variants.Add(variant);

            variant = new PartVariant(BallastTankTypes.AftStarboard.ToString(), Localizer.Format("#LOC_SUNKWORKS_tankTypeAftStarboard"), null);
            variant.PrimaryColor = "#ffffff";
            variant.SecondaryColor = "#00ff00";
            variantSelector.variants.Add(variant);
        }

        private void onVariantChanged(BaseField baseField, object obj)
        {
            tankType = (BallastTankTypes)Enum.ToObject(typeof(BallastTankTypes), tankTypeIndex);
            updateGUI();
            onBallastTankUpdated.Fire(this, tankType, ventState, isConverted);

            if (updateSymmetryTanks)
            {
                int count = part.symmetryCounterparts.Count;
                SWBallastTank ballastTank;
                for (int index = 0; index < count; index++)
                {
                    ballastTank = part.symmetryCounterparts[index].FindModuleImplementing<SWBallastTank>();
                    if (ballastTank != null)
                    {
                        ballastTank.tankType = tankType;
                        ballastTank.updateGUI();
                    }
                }
                ballastTank = part.symmetryCounterparts[0].FindModuleImplementing<SWBallastTank>();
                onBallastTankUpdated.Fire(ballastTank, ballastTank.tankType, ballastTank.ventState, ballastTank.isConverted);
            }

            if (HighLogic.LoadedSceneIsEditor)
                EditorLogic.fetch.SetBackup();
        }

        void updateGUI()
        {
            if (hostPart != null)
            {
                Events["RestoreResourceCapacity"].active = isConverted;
                Events["ConvertToBallastTank"].active = !isConverted;
            }
            else
            {
                Events["RestoreResourceCapacity"].active = false;
                Events["ConvertToBallastTank"].active = false;
            }

            switch (tankType)
            {
                default:
                case BallastTankTypes.Ballast:
                    tankTypeString = Localizer.Format("#LOC_SUNKWORKS_tankTypeBallast");
                    break;

                case BallastTankTypes.ForwardTrim:
                    tankTypeString = Localizer.Format("#LOC_SUNKWORKS_tankTypeForwardTrim");
                    break;

                case BallastTankTypes.AftTrim:
                    tankTypeString = Localizer.Format("#LOC_SUNKWORKS_tankTypeAftTrim");
                    break;

                case BallastTankTypes.PortTrim:
                    tankTypeString = Localizer.Format("#LOC_SUNKWORKS_tankTypePortTrim");
                    break;

                case BallastTankTypes.StarboardTrim:
                    tankTypeString = Localizer.Format("#LOC_SUNKWORKS_tankTypeStarboardTrim");
                    break;

                case BallastTankTypes.ForwardPort:
                    tankTypeString = Localizer.Format("#LOC_SUNKWORKS_tankTypeFwdPort");
                    break;

                case BallastTankTypes.ForwardStarboard:
                    tankTypeString = Localizer.Format("#LOC_SUNKWORKS_tankTypeFwdStarboard");
                    break;

                case BallastTankTypes.AftPort:
                    tankTypeString = Localizer.Format("#LOC_SUNKWORKS_tankTypeAftPort");
                    break;

                case BallastTankTypes.AftStarboard:
                    tankTypeString = Localizer.Format("#LOC_SUNKWORKS_tankTypeAftStarboard");
                    break;
            }

            switch (ventState)
            {
                default:
                case BallastVentStates.Closed:
                    ventStateString = Localizer.Format("#LOC_SUNKWORKS_tankClosed"); ;
                    break;

                case BallastVentStates.FloodingBallast:
                    if (intakeIsUnderwater)
                        ventStateString = Localizer.Format("#LOC_SUNKWORKS_tankFilling");
                    else
                        ventStateString = Localizer.Format("#LOC_SUNKWORKS_needsUnderwater");
                    break;

                case BallastVentStates.VentingBallast:
                    ventStateString = Localizer.Format("#LOC_SUNKWORKS_tankVenting"); ;
                    break;
            }
        }
        #endregion
    }
}
