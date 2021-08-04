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
        #region Housekeeping
        public string name = string.Empty;
        public string displayName = string.Empty;
        public string primaryColor = string.Empty;
        public string secondaryColor = string.Empty;
        public float mass = 0;
        public float cost = 0;
        public SWTextureVariant textureVariant = null;
        public SWMeshVariant meshVariant = null;
        public SWMeshVariant ladderVariant = null;
        public string animationName = string.Empty;
        public bool animationIsEnabled = false;
        public Dictionary<string, string> extraInfo = new Dictionary<string, string>();
        #endregion

        #region API
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
            if (node.HasValue("mass"))
                float.TryParse(node.GetValue("mass"), out mass);
            if (node.HasValue("cost"))
                float.TryParse(node.GetValue("cost"), out cost);

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

            if (node.HasNode("LADDERS"))
            {
                ladderVariant = new SWMeshVariant();
                ladderVariant.load(node.GetNode("LADDERS"));
            }

            if (node.HasNode("ANIMATION"))
            {
                loadAnimationVariant(node.GetNode("ANIMATION"));
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

            if (ladderVariant != null)
                ladderVariant.applyVariant(part, false);

            applyAnimationVariant(part);

            MonoUtilities.RefreshContextWindows(part);
        }
        #endregion

        #region Helpers
        private void loadAnimationVariant(ConfigNode node)
        {
            if (node.HasValue("name") && node.HasValue("enabled"))
            {
                animationName = node.GetValue("name");
                bool.TryParse(node.GetValue("enabled"), out animationIsEnabled);
            }
        }

        private void applyAnimationVariant(Part part)
        {
            if (string.IsNullOrEmpty(animationName))
                return;

            List<ModuleAnimateGeneric> animations = part.FindModulesImplementing<ModuleAnimateGeneric>();
            int count = animations.Count;
            for (int index = 0; index < count; index++)
            {
                if (animations[index].animationName == animationName)
                {
                    animations[index].moduleIsEnabled = animationIsEnabled;
                    animations[index].enabled = animationIsEnabled;
                    animations[index].isEnabled = animationIsEnabled;
                    return;
                }
            }
        }
        #endregion
    }
}
