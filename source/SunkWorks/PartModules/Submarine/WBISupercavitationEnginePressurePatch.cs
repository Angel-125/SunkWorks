using System;
using HarmonyLib;

namespace SunkWorks.Submarine
{
    /// <summary>
    /// Temporarily substitutes atmospheric-only pressure while a stock engine inside
    /// a supercavity calculates thrust. Other part systems continue to see the real
    /// hydrostatic pressure.
    /// </summary>
    internal static class WBISupercavitationEnginePressure
    {
        const float MinimumCoverage = 0.5f;

        internal struct PressureState
        {
            internal bool changed;
            internal Part part;
            internal double originalPressureAtm;
        }

        internal static void Apply(ModuleEngines engine, out PressureState state)
        {
            state = new PressureState();
            if (!HighLogic.LoadedSceneIsFlight || engine == null ||
                engine.part == null || engine.part.vessel == null)
            {
                return;
            }

            WBISupercavitationController controller;
            if (!WBISupercavitationController.TryGetController(
                engine.part.vessel, out controller) ||
                controller.GetSupercavityCoverage(engine.part) < MinimumCoverage)
            {
                return;
            }

            Part enginePart = engine.part;
            CelestialBody body = enginePart.vessel.mainBody;
            if (body == null)
                return;

            double altitude = FlightGlobals.getAltitudeAtPos(
                enginePart.partTransform.position, body);
            double atmosphericPressureAtm = body.GetPressure(Math.Max(0.0, altitude)) *
                PhysicsGlobals.KpaToAtmospheres;

            state.changed = true;
            state.part = enginePart;
            state.originalPressureAtm = enginePart.staticPressureAtm;
            enginePart.staticPressureAtm = atmosphericPressureAtm;
        }

        internal static void Restore(PressureState state)
        {
            if (state.changed && state.part != null)
                state.part.staticPressureAtm = state.originalPressureAtm;
        }
    }

    [HarmonyPatch(typeof(ModuleEngines), "CalculateThrust")]
    internal static class WBISupercavitationModuleEnginesPressurePatch
    {
        static void Prefix(ModuleEngines __instance,
            out WBISupercavitationEnginePressure.PressureState __state)
        {
            WBISupercavitationEnginePressure.Apply(__instance, out __state);
        }

        static void Postfix(WBISupercavitationEnginePressure.PressureState __state)
        {
            WBISupercavitationEnginePressure.Restore(__state);
        }
    }

    [HarmonyPatch(typeof(DeltaVEngineInfo), "CalculateISP")]
    internal static class WBISupercavitationDeltaVISPPressurePatch
    {
        static void Prefix(DeltaVEngineInfo __instance,
            out WBISupercavitationEnginePressure.PressureState __state)
        {
            WBISupercavitationEnginePressure.Apply(__instance.engine, out __state);
        }

        static void Postfix(WBISupercavitationEnginePressure.PressureState __state)
        {
            WBISupercavitationEnginePressure.Restore(__state);
        }
    }

    [HarmonyPatch(typeof(DeltaVEngineInfo), "CalcThrustActual")]
    internal static class WBISupercavitationDeltaVThrustPressurePatch
    {
        static void Prefix(DeltaVEngineInfo __instance,
            out WBISupercavitationEnginePressure.PressureState __state)
        {
            WBISupercavitationEnginePressure.Apply(__instance.engine, out __state);
        }

        static void Postfix(WBISupercavitationEnginePressure.PressureState __state)
        {
            WBISupercavitationEnginePressure.Restore(__state);
        }
    }
}
