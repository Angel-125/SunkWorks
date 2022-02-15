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
        public SWMeshVariant colliderVariant = null;
        public string animationName = string.Empty;
        public bool animationIsEnabled = false;
        public Dictionary<string, string> extraInfo = new Dictionary<string, string>();
        public Dictionary<string, SWVariant> meshSets = null;
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

            if (node.HasNode("MESH_SET"))
            {
                loadMeshSets(node.GetNodes("MESH_SET"));
            }

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

            if (node.HasNode("COLLIDERS"))
            {
                colliderVariant = new SWMeshVariant();
                colliderVariant.load(node.GetNode("COLLIDERS"));
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

        public void applyVariant(Part part, string meshSet)
        {
            Debug.Log("[SWVariant] -  Applying variant: " + name);
            if (!string.IsNullOrEmpty(meshSet))
                Debug.Log("[SWVariant] -  For mesh set: " + meshSet);

            // Mesh sets are applied before any other fields in the variant.
            if (!string.IsNullOrEmpty(meshSet) && meshSets != null && meshSets.ContainsKey(meshSet))
            {
                meshSets[meshSet].applyVariant(part, meshSet);
            }

            if (textureVariant != null)
                textureVariant.applyVariant(part);

            // Now apply any game object variants
            if (meshVariant != null)
                meshVariant.applyVariant(part);

            // Now apply any colliders.
            if (colliderVariant != null)
                colliderVariant.applyVariant(part, false);

            applyAnimationVariant(part);

            MonoUtilities.RefreshContextWindows(part);
        }

        public float getCost(string meshSet)
        {
            if (!string.IsNullOrEmpty(meshSet) && meshSets != null && meshSets.ContainsKey(meshSet))
                return cost + meshSets[meshSet].cost;
            else
                return cost;
        }

        public float getMass(string meshSet)
        {
            if (!string.IsNullOrEmpty(meshSet) && meshSets != null && meshSets.ContainsKey(meshSet))
                return mass + meshSets[meshSet].mass;
            else
                return mass;
        }
        #endregion

        #region Helpers
        private void loadMeshSets(ConfigNode[] nodes)
        {
             meshSets = new Dictionary<string, SWVariant>();
            SWVariant variant;
            for (int index = 0; index < nodes.Length; index++)
            {
                variant = new SWVariant();
                variant.Load(nodes[index]);
                if (!meshSets.ContainsKey(variant.name))
                    meshSets.Add(variant.name, variant);
            }
        }

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
