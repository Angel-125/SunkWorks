using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace SunkWorks.KerbalGear
{
    /// <summary>
    /// Applies SunkWorks ballast to the separate buoyancy force used by stock EVA ragdolls.
    /// Stock KerbalEVA does not include Part.buoyancy in that calculation.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public sealed class EVARagdollBuoyancyPatchLoader : MonoBehaviour
    {
        const string HarmonyId = "com.wildblueindustries.sunkworks.evaragdollbuoyancy";

        /// <summary>
        /// Installs the EVA ragdoll buoyancy patch once Harmony and SunkWorks have loaded.
        /// </summary>
        public void Awake()
        {
            Harmony harmony = new Harmony(HarmonyId);
            harmony.PatchAll(typeof(EVARagdollBuoyancyPatchLoader).Assembly);
        }
    }

    [HarmonyPatch]
    internal static class EVARagdollBuoyancyPatch
    {
        static readonly FieldInfo ragdollBuoyancyField = AccessTools.Field(
            typeof(PhysicsGlobals),
            nameof(PhysicsGlobals.BuoyancyKerbalsRagdoll));

        static readonly MethodInfo adjustBuoyancyMethod = AccessTools.Method(
            typeof(EVARagdollBuoyancyPatch),
            nameof(adjustRagdollBuoyancy));

        static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(KerbalEVA), "IntegrateRagdollRigidbodyForces");
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool patched = false;

            foreach (CodeInstruction instruction in instructions)
            {
                yield return instruction;

                if (instruction.opcode == OpCodes.Ldsfld &&
                    Equals(instruction.operand, ragdollBuoyancyField))
                {
                    // Stack before: stock ragdoll buoyancy coefficient.
                    // Stack after: coefficient adjusted for this KerbalEVA's active dive computer.
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, adjustBuoyancyMethod);
                    patched = true;
                }
            }

            if (!patched)
                Debug.LogError("[SunkWorks] Unable to patch KerbalEVA ragdoll buoyancy; stock method layout was not recognized.");
        }

        static float adjustRagdollBuoyancy(float stockBuoyancy, KerbalEVA kerbalEVA)
        {
            if (kerbalEVA == null || kerbalEVA.part == null)
                return stockBuoyancy;

            WBIModuleEVADiveComputer diveComputer =
                kerbalEVA.part.FindModuleImplementing<WBIModuleEVADiveComputer>();

            if (diveComputer == null || !diveComputer.IsDiveComputerActive)
                return stockBuoyancy;

            return stockBuoyancy * diveComputer.RagdollBuoyancyScale;
        }
    }
}
