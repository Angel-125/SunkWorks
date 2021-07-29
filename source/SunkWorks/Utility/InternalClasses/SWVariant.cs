using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP.Localization;

namespace SunkWorks.Utility
{
    internal class SWVariant
    {
        public string name = string.Empty;
        public string displayName = string.Empty;
        public string primaryColor = string.Empty;
        public string secondaryColor = string.Empty;
        public SWTextureVariant textureVariant = null;
        public SWMeshVariant meshVariant = null;
        public Dictionary<string, string> extraInfo = new Dictionary<string, string>();

        public void Load(ConfigNode node)
        {
            if (node.HasValue("name"))
                name = node.GetValue("name");
            if (node.HasValue("displayName"))
                displayName = node.GetValue("displayName");
            if (node.HasValue("primaryColor"))
                primaryColor = node.GetValue("primaryColor");
            if (node.HasValue("secondaryColor"))
                secondaryColor = node.GetValue("secondaryColor");

            if (node.HasNode("TEXTURES"))
            {
                textureVariant = new SWTextureVariant();
                textureVariant.load(node.GetNode("TEXTURES"));
            }

            if (node.HasNode("GAMEOBJECTS"))
            {
                meshVariant = new SWMeshVariant();
                meshVariant.load(node.GetNode("GAMEOBJECTS"));
            }

            if (node.HasNode("EXTRA_INFO"))
            {
                extraInfo.Clear();
                ConfigNode nodeExtraInfo = node.GetNode("EXTRA_INFO");
                int count = nodeExtraInfo.values.Count;
                string key;
                string keyValue;
                for (int index = 0; index < count; index++)
                {
                    key = nodeExtraInfo.values[index].name;
                    keyValue = nodeExtraInfo.values[index].value;

                    if (!extraInfo.ContainsKey(key))
                        extraInfo.Add(key, keyValue);
                }
            }
        }

        public PartVariant getPartVariant()
        {
            PartVariant variant = new PartVariant(name, Localizer.Format(displayName), null);

            if (!string.IsNullOrEmpty(primaryColor))
                variant.PrimaryColor = primaryColor;

            if (!string.IsNullOrEmpty(secondaryColor))
                variant.SecondaryColor = secondaryColor;

            return variant;
        }

        public void applyVariant(Part part)
        {
            Debug.Log("[SWVariant] -  Applying variant: " + name);

            if (textureVariant != null)
                textureVariant.applyVariant(part);

            if (meshVariant != null)
                meshVariant.applyVariant(part);
        }
    }
}
