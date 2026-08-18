using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.UI.Screens;
using SunkWorks.Submarine;

#pragma warning disable 1591

namespace SunkWorks
{
    /// <summary>
    /// Editor lifecycle, toolbar, and debounced invalidation for trim analysis. Physics remains
    /// in SunkWorksTrimAnalyzer/LongitudinalTrimSolver so this class only coordinates the UI.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public sealed class SunkWorksTrimAnalysisController : MonoBehaviour
    {
        const float RecomputeDelay = 0.15f;
        const float FingerprintInterval = 0.5f;
        SunkWorksTrimAnalysisView view;
        ApplicationLauncherButton launcherButton;
        bool dirty = true;
        float dirtyTime;
        float nextFingerprintTime;
        int lastFingerprint;

        public void Awake()
        {
            view = new SunkWorksTrimAnalysisView();
            view.VisibilityChanged = OnViewVisibilityChanged;
            GameEvents.onGUIApplicationLauncherReady.Add(AddLauncherButton);
            GameEvents.onGUIApplicationLauncherDestroyed.Add(RemoveLauncherButton);
            GameEvents.onEditorShipModified.Add(OnShipModified);
            GameEvents.onEditorPartPlaced.Add(OnPartChanged);
            GameEvents.onEditorPartEvent.Add(OnPartEvent);
            GameEvents.onEditorVariantApplied.Add(OnVariantApplied);
            GameEvents.onPartResourceListChange.Add(OnPartChanged);
            WBIBallastTank.onBallastTankUpdated.Add(OnBallastTankUpdated);
            AddLauncherButton();
        }

        public void OnDestroy()
        {
            GameEvents.onGUIApplicationLauncherReady.Remove(AddLauncherButton);
            GameEvents.onGUIApplicationLauncherDestroyed.Remove(RemoveLauncherButton);
            GameEvents.onEditorShipModified.Remove(OnShipModified);
            GameEvents.onEditorPartPlaced.Remove(OnPartChanged);
            GameEvents.onEditorPartEvent.Remove(OnPartEvent);
            GameEvents.onEditorVariantApplied.Remove(OnVariantApplied);
            GameEvents.onPartResourceListChange.Remove(OnPartChanged);
            WBIBallastTank.onBallastTankUpdated.Remove(OnBallastTankUpdated);
            if (view != null)
                view.SetVisible(false);
            RemoveLauncherButton();
        }

        public void Update()
        {
            if (view == null || !view.IsVisible())
                return;

            if (Time.realtimeSinceStartup >= nextFingerprintTime)
            {
                nextFingerprintTime = Time.realtimeSinceStartup + FingerprintInterval;
                int fingerprint = ComputeCraftFingerprint();
                if (fingerprint != lastFingerprint)
                {
                    lastFingerprint = fingerprint;
                    MarkDirty();
                }
            }

            if (dirty && Time.realtimeSinceStartup - dirtyTime >= RecomputeDelay)
            {
                dirty = false;
                ShipConstruct ship = EditorLogic.fetch != null ? EditorLogic.fetch.ship : null;
                view.Result = SunkWorksTrimAnalyzer.Analyze(ship);
            }
        }

        void AddLauncherButton()
        {
            if (launcherButton != null || !ApplicationLauncher.Ready)
                return;

            // Editor registration is intentionally disabled while the trim report is evaluated.
            // Uncomment this block when the report is ready to be exposed in the VAB/SPH.
            /*
            Texture2D icon = GameDatabase.Instance.GetTexture(
                "WildBlueIndustries/SunkWorks/Icons/SunkWorks", false);
            launcherButton = ApplicationLauncher.Instance.AddModApplication(
                ShowView, HideView, null, null, null, null,
                ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH,
                icon != null ? icon : Texture2D.whiteTexture);
            */
        }

        void RemoveLauncherButton()
        {
            if (launcherButton == null)
                return;
            if (ApplicationLauncher.Instance != null)
                ApplicationLauncher.Instance.RemoveModApplication(launcherButton);
            launcherButton = null;
        }

        void ShowView()
        {
            view.SetVisible(true);
            lastFingerprint = ComputeCraftFingerprint();
            MarkDirty();
        }

        void HideView()
        {
            view.SetVisible(false);
        }

        void OnViewVisibilityChanged(bool isVisible)
        {
            if (!isVisible && launcherButton != null)
                launcherButton.SetFalse(false);
        }

        void MarkDirty()
        {
            dirty = true;
            dirtyTime = Time.realtimeSinceStartup;
        }

        void OnShipModified(ShipConstruct ship) { MarkDirty(); }
        void OnPartChanged(Part part) { MarkDirty(); }
        void OnPartEvent(ConstructionEventType eventType, Part part) { MarkDirty(); }
        void OnVariantApplied(Part part, PartVariant variant) { MarkDirty(); }
        void OnBallastTankUpdated(WBIBallastTank tank, BallastTankTypes type,
            BallastVentStates state, bool converted) { MarkDirty(); }

        static int ComputeCraftFingerprint()
        {
            unchecked
            {
                ShipConstruct ship = EditorLogic.fetch != null ? EditorLogic.fetch.ship : null;
                if (ship == null || ship.parts == null)
                    return 0;
                int hash = 17;
                for (int index = 0; index < ship.parts.Count; index++)
                {
                    Part part = ship.parts[index];
                    hash = hash * 31 + (int)part.craftID;
                    hash = hash * 31 + part.transform.position.GetHashCode();
                    hash = hash * 31 + part.transform.rotation.GetHashCode();
                    hash = hash * 31 + part.mass.GetHashCode();
                    hash = hash * 31 + part.buoyancy.GetHashCode();
                    for (int resourceIndex = 0; resourceIndex < part.Resources.Count; resourceIndex++)
                    {
                        PartResource resource = part.Resources[resourceIndex];
                        hash = hash * 31 + resource.resourceName.GetHashCode();
                        hash = hash * 31 + resource.amount.GetHashCode();
                        hash = hash * 31 + resource.maxAmount.GetHashCode();
                    }
                    List<WBIBallastTank> tanks = part.FindModulesImplementing<WBIBallastTank>();
                    for (int tankIndex = 0; tankIndex < tanks.Count; tankIndex++)
                        hash = hash * 31 + (int)tanks[tankIndex].tankType;
                }
                return hash;
            }
        }
    }
}
#pragma warning restore 1591
