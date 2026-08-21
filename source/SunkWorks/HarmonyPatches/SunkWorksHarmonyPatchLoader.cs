using HarmonyLib;
using UnityEngine;

namespace SunkWorks
{
    /// <summary>
    /// Installs all Harmony patches contained in the SunkWorks assembly.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public sealed class SunkWorksHarmonyPatchLoader : MonoBehaviour
    {
        const string HarmonyId = "com.wildblueindustries.sunkworks";

        /// <summary>
        /// Installs the SunkWorks Harmony patches when the assembly loads.
        /// </summary>
        public void Awake()
        {
            Harmony harmony = new Harmony(HarmonyId);
            harmony.PatchAll(typeof(SunkWorksHarmonyPatchLoader).Assembly);
        }
    }
}
