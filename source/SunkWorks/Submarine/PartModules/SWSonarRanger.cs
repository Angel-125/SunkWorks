using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KSP.Localization;
using UnityEngine;

namespace SunkWorks.Submarine
{
    /// <summary>
    /// When underwater it's hard to see the terrain ahead and the seabed below.
    /// This part module helps avoid collisions with the terrain and seabed.
    /// </summary>
    public class SWSonarRanger : PartModule
    {
        #region Constants
        const float kMaxSonarDistance = 1500f;
        const float kUnderwaterSoundSpeed = 1500f;
        const float kEnchoPingLevel = 0.5f;
        const double kMinPingDelay = 0.5f;
        #endregion

        #region Fields
        /// <summary>
        /// How far it is to the bottom of the sea. Perhaps one should voyage there...
        /// </summary>
        [KSPField(guiActive = true, guiFormat = "f1", guiUnits = "m", guiName = "#LOC_SUNKWORKS_keelDepth")]
        public float depthBelowKeel;

        /// <summary>
        /// Range to terrain, in meters.
        /// </summary>
        [KSPField(guiActive = true, guiName = "#LOC_SUNKWORKS_terrainRange")]
        public string rangeToTerrainDisplay;

        /// <summary>
        /// Toggle switch for the seabed proximity alarm
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_SUNKWORKS_seabedPingToggle", isPersistant = true)]
        [UI_Toggle(enabledText = "#LOC_SUNKWORKS_on", disabledText = "#LOC_SUNKWORKS_off")]
        public bool seabedPingActive;

        /// <summary>
        /// Minimum range at which to play the seabed ping, if enabled.
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_SUNKWORKS_seabedPingRange", isPersistant = true)]
        [UI_FloatRange(maxValue = 1500, minValue = 50f, scene = UI_Scene.All, stepIncrement = 50f)]
        public float seabedPingRange = 100f;

        /// <summary>
        /// Toggle switch for the seabed proximity alarm
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_SUNKWORKS_shoalPingToggle", isPersistant = true)]
        [UI_Toggle(enabledText = "#LOC_SUNKWORKS_on", disabledText = "#LOC_SUNKWORKS_off")]
        public bool shoalPingActive;

        /// <summary>
        /// Minimum range at which to play the seabed ping, if enabled.
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_SUNKWORKS_shoalPingRange", isPersistant = true)]
        [UI_FloatRange(maxValue = 1500, minValue = 50f, scene = UI_Scene.All, stepIncrement = 50f)]
        public float shoalPingRange = 100f;

        /// <summary>
        /// Name of the effect to play when in proximity to the seabed.
        /// </summary>
        [KSPField]
        public string pingEffectSeabedName = string.Empty;

        /// <summary>
        /// Name of the effect to play when in proximity to a shoal.
        /// </summary>
        [KSPField]
        public string pingEffectShoalName = string.Empty;
        #endregion

        #region Housekeeping
        RaycastHit terrainHit;
        LayerMask layerMask = -1;
        float rangeToTerrain;
        double seabedPingTime = 0;
        double shoalPingTime = 0;
        #endregion

        #region Actions
        /// <summary>
        /// Action to toggle the seabed proximity alarm on/off
        /// </summary>
        /// <param name="param">A KSPActionParam</param>
        [KSPAction("#LOC_SUNKWORKS_seabedPingToggle")]
        public void ToggleSeabedPingAction(KSPActionParam param)
        {
            seabedPingActive = !seabedPingActive;
        }

        /// <summary>
        /// Action to toggle the seabed proximity alarm on/off
        /// </summary>
        /// <param name="param">A KSPActionParam</param>
        [KSPAction("#LOC_SUNKWORKS_shoalPingToggle")]
        public void ToggleShoalPingAction(KSPActionParam param)
        {
            shoalPingActive = !shoalPingActive;
        }
        #endregion

        #region Overrides
        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            updateSonarInputs();
            updateSonarPings();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            updateUI();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            updateUI();
        }
        #endregion

        #region Helpers
        void updateSonarPings()
        {
            if (seabedPingActive && depthBelowKeel <= seabedPingRange)
                updateSonarPing(pingEffectSeabedName, depthBelowKeel, ref seabedPingTime);

            // Offset the shoal ping time a bit.
            if (shoalPingTime <= 0)
                shoalPingTime = Planetarium.GetUniversalTime() + 1f;

            if (shoalPingActive && rangeToTerrain > 0 && rangeToTerrain <= shoalPingRange)
                updateSonarPing(pingEffectShoalName, rangeToTerrain, ref shoalPingTime);
        }

        void updateSonarPing(string pingEffect, float range, ref double pingTime)
        {
            // Calculate ping frequency, accounting for travel time to and from the boat.
            double pingFrequency = (range / kUnderwaterSoundSpeed) * 2;
            if (pingFrequency < kMinPingDelay)
                pingFrequency = kMinPingDelay;

            // Send the ping
            if (Planetarium.GetUniversalTime() >= pingTime)
            {
                pingTime = Planetarium.GetUniversalTime() + pingFrequency;
                part.Effect(pingEffect, 1, -1);
            }
        }

        void updateSonarInputs()
        {
            // Update keel depth
            depthBelowKeel = part.vessel.heightFromTerrain;

            // Update sonar ranging
            if (Physics.Raycast(vessel.ReferenceTransform.position, vessel.ReferenceTransform.up, out terrainHit, kMaxSonarDistance, layerMask))
            {
                // See if we found the ground. 15 = Local Scenery, 28 = TerrainColliders
                if (terrainHit.collider.gameObject.layer == 15 || terrainHit.collider.gameObject.layer == 28)
                {
                    rangeToTerrain = terrainHit.distance;
                    rangeToTerrainDisplay = Localizer.Format("#LOC_SUNKWORKS_terrainRangeDist", new string[1] { string.Format("{0:n1}", terrainHit.distance) });
                }
            }
            else
            {
                rangeToTerrain = -1;
                rangeToTerrainDisplay = Localizer.Format("#LOC_SUNKWORKS_terrainRangeNA");
            }
        }

        void updateUI()
        {
            Fields["seabedPingRange"].guiActive = seabedPingActive;
            Fields["shoalPingRange"].guiActive = shoalPingActive;
        }
        #endregion
    }
}
