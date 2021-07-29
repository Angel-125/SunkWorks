using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunkWorks.Utility
{
    public class SWPartModule: PartModule
    {
        /// <summary>
        /// ID of the module. Used to find the proper config node.
        /// </summary>
        [KSPField]
        public string moduleID = string.Empty;

        /// <summary>
        /// Retrieves the module's config node from the part config.
        /// </summary>
        /// <returns>A ConfigNode for the part module.</returns>
        public ConfigNode getPartConfigNode()
        {
            if (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight)
                return null;
            if (this.part.partInfo.partConfig == null)
                return null;
            ConfigNode[] nodes = this.part.partInfo.partConfig.GetNodes("MODULE");
            ConfigNode partConfigNode = null;
            ConfigNode node = null;
            string moduleName;
            string nodeModuleID;

            //Get the config node.
            for (int index = 0; index < nodes.Length; index++)
            {
                node = nodes[index];
                if (node.HasValue("name"))
                {
                    moduleName = node.GetValue("name");
                    if (moduleName == this.ClassName)
                    {
                        if (node.HasValue("moduleID") && !string.IsNullOrEmpty(moduleID))
                        {
                            nodeModuleID = node.GetValue("moduleID");
                            if (moduleID == nodeModuleID)
                            {
                                partConfigNode = node;
                                break;
                            }
                        }
                        else
                        {
                            partConfigNode = node;
                            break;
                        }
                    }
                }
            }

            return partConfigNode;
        }

        /// <summary>
        /// Loads the desired FloatCurve from the desired config node.
        /// </summary>
        /// <param name="curve">The FloatCurve to load</param>
        /// <param name="curveNodeName">The name of the curve to load</param>
        /// <param name="defaultCurve">An optional default curve to use in case the curve's node doesn't exist in the part module's config.</param>
        protected void loadCurve(FloatCurve curve, string curveNodeName, ConfigNode defaultCurve = null)
        {
            if (curve.Curve.length > 0)
                return;
            ConfigNode[] nodes = this.part.partInfo.partConfig.GetNodes("MODULE");
            ConfigNode engineNode = null;
            ConfigNode node = null;
            string moduleName;

            //Get the switcher config node.
            for (int index = 0; index < nodes.Length; index++)
            {
                node = nodes[index];
                if (node.HasValue("name"))
                {
                    moduleName = node.GetValue("name");
                    if (moduleName == this.ClassName)
                    {
                        engineNode = node;
                        break;
                    }
                }
            }
            if (engineNode == null)
                return;

            if (engineNode.HasNode(curveNodeName))
            {
                node = engineNode.GetNode(curveNodeName);
                curve.Load(node);
            }
            else if (defaultCurve != null)
            {
                curve.Load(defaultCurve);
            }
        }
    }
}
