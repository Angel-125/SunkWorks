using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SunkWorks.KerbalGear
{
    /// <summary>
    /// Special thanks to Vali for figuring out this issue! :)
    /// The Vintage, Standard, and Future suits are all defined in separate part modules that are combined when KSP starts.
    /// The problem is that when Module Manager is used to add part modules to the kerbal, you'll get duplicates.
    /// One solution is to disable or outright remove the duplicate part module, but we have several part modules to manage.
    /// So to get around that problem, the WBIPartModuleUtils adds a custom LoadingSystem that adds any part modules defined by a KERBAL_EVA_MODULES node.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    sealed class WBIKerbalEVAModules : MonoBehaviour
    {
        #region Constants
        const string kKerbalEVAModulesNode = "KERBAL_EVA_MODULES";
        const string kModuleNode = "MODULE";
        const string kNameField = "name";
        #endregion

        /// <summary>
        /// An internal helper class that reads KERVAL_EVA_MODULES for MODULE nodes to add to a kerbal.
        /// </summary>
        class EVAModulesLoader : LoadingSystem
        {
            public override bool IsReady()
            {
                return true;
            }

            public override void StartLoad()
            {
                int count = PartLoader.LoadedPartsList.Count;
                AvailablePart availablePart;
                ConfigNode[] evaNodes;
                ConfigNode evaNode;
                ConfigNode[] evaModules;
                ConfigNode evaModule;
                PartModule partModule;

                for (int index = 0; index < count; index++)
                {
                    // Get the available part
                    availablePart = PartLoader.LoadedPartsList[index];

                    // If the part is a kerbal then load the kerbal modules.
                    if (availablePart.partPrefab.HasModuleImplementing<KerbalEVA>())
                    {
                        // Get the KERVAL_EVA_MODULES nodes.
                        evaNodes = GameDatabase.Instance.GetConfigNodes(kKerbalEVAModulesNode);

                        for (int evaNodeIndex = 0; evaNodeIndex < evaNodes.Length; evaNodeIndex++)
                        {
                            evaNode = evaNodes[evaNodeIndex];

                            // Make sure that we have one or more MODULE nodes.
                            if (!evaNode.HasNode(kModuleNode))
                                continue;

                            // Get the MODULE nodes that we'll add.
                            evaModules = evaNode.GetNodes(kModuleNode);
                            for (int moduleIndex = 0; moduleIndex < evaModules.Length; moduleIndex++)
                            {
                                // Get the MODULE node and make sure it has the name field.
                                evaModule = evaModules[moduleIndex];
                                if (!evaModule.HasValue(kNameField))
                                    continue;

                                // Add the module and load the config.
                                partModule = availablePart.partPrefab.AddModule(evaModule.GetValue(kNameField), true);
                                if (partModule != null)
                                    partModule.Load(evaModule);
                            }
                        }
                    }
                }
            }
        }

        #region Overrides
        public void Awake()
        {
            List<LoadingSystem> loaders = LoadingScreen.Instance.loaders;
            if (loaders != null)
            {
                int count = loaders.Count;
                for (int index = 0; index < count; index++)
                {
                    if (loaders[index] is PartLoader)
                    {
                        GameObject gameObject = new GameObject();
                        EVAModulesLoader modulesLoader = gameObject.AddComponent<EVAModulesLoader>();
                        loaders.Insert(index + 1, modulesLoader);
                        break;
                    }
                }
            }
        }

        #endregion
    }
}
