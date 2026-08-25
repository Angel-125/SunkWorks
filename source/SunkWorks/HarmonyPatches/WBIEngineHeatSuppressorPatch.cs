using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SunkWorks.PartModules.Structural;
using UnityEngine;

namespace SunkWorks
{
    /// <summary>
    /// Prevents stock engine exhaust from heating or pushing parts that contain a
    /// <see cref="WBIModuleEngineHeatSuppressor"/>. The exhaust ray still strikes
    /// the protected part, so it continues to shield parts behind it.
    /// </summary>
    [HarmonyPatch(typeof(ModuleEngines), nameof(ModuleEngines.EngineExhaustDamage))]
    internal static class WBIEngineHeatSuppressorPatch
    {
        static readonly MethodInfo stockAddSkinThermalFlux = AccessTools.Method(
            typeof(Part),
            nameof(Part.AddSkinThermalFlux),
            new[] { typeof(double) });

        static readonly MethodInfo filteredAddSkinThermalFlux = AccessTools.Method(
            typeof(WBIEngineHeatSuppressorPatch),
            nameof(AddSkinThermalFluxUnlessSuppressed));

        static readonly MethodInfo stockAddForceAtPosition = AccessTools.Method(
            typeof(Part),
            nameof(Part.AddForceAtPosition),
            new[] { typeof(Vector3d), typeof(Vector3d) });

        static readonly MethodInfo filteredAddForceAtPosition = AccessTools.Method(
            typeof(WBIEngineHeatSuppressorPatch),
            nameof(AddForceAtPositionUnlessSuppressed));

        /// <summary>
        /// Redirects the two stock exhaust-heat calls and the exhaust-force call to
        /// receiver-aware filters. Replacing calls by signature avoids depending on
        /// compiler-generated local variables or branch labels in ModuleEngines.
        /// </summary>
        static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            int heatCallCount = 0;
            int forceCallCount = 0;

            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.Calls(stockAddSkinThermalFlux))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = filteredAddSkinThermalFlux;
                    heatCallCount++;
                }
                else if (instruction.Calls(stockAddForceAtPosition))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = filteredAddForceAtPosition;
                    forceCallCount++;
                }

                yield return instruction;
            }

            if (heatCallCount != 2 || forceCallCount != 1)
            {
                Debug.LogWarning(
                    "[SunkWorks] Engine heat suppressor patch found " +
                    heatCallCount + " exhaust heat call(s) and " +
                    forceCallCount + " exhaust force call(s); expected 2 and 1.");
            }
        }

        static void AddSkinThermalFluxUnlessSuppressed(Part targetPart, double kilowatts)
        {
            // Stock splashback commonly submits zero flux. Avoid a module lookup
            // when forwarding the call could not change the thermal state anyway.
            if (targetPart == null || kilowatts == 0.0)
                return;

            if (FindSuppressor(targetPart) != null)
                return;

            targetPart.AddSkinThermalFlux(kilowatts);
        }

        static void AddForceAtPositionUnlessSuppressed(
            Part targetPart,
            Vector3d force,
            Vector3d position)
        {
            if (targetPart == null || FindSuppressor(targetPart) != null)
                return;

            targetPart.AddForceAtPosition(force, position);
        }

        static WBIModuleEngineHeatSuppressor FindSuppressor(Part targetPart)
        {
            // Part.FindModuleImplementing caches successful searches by module type.
            // Protected parts therefore pay for a module-list scan only on the first
            // exhaust interaction and use KSP's own invalidated cache thereafter.
            return targetPart.FindModuleImplementing<WBIModuleEngineHeatSuppressor>();
        }
    }
}
