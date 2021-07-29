using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP.Localization;

namespace SunkWorks.Utility
{
    #region SWVariant class
    internal class SWTextureVariant
    {
        public string mainTextureURL = string.Empty;
        public string bumpMapURL = string.Empty;
        public string[] textureTransformNames = null;

        public void load(ConfigNode node)
        {
            if (node.HasValue("mainTextureURL"))
                mainTextureURL = node.GetValue("mainTextureURL");
            if (node.HasValue("bumpMapURL"))
                bumpMapURL = node.GetValue("bumpMapURL");
            if (node.HasValue("transformName"))
                textureTransformNames = node.GetValues("transformName");
        }

        public void applyVariant(Part part)
        {
            // Find all the transforms that will have their texture(s) replaced.
            if (textureTransformNames == null || textureTransformNames.Length == 0)
            {
                Debug.Log("[SWTextureVariant] - No transforms to texture!");
            }

            Transform textureTransform = null;
            Renderer rendererMaterial;
            Texture2D textureForDecal;
            for (int index = 0; index < textureTransformNames.Length; index++)
            {
                textureTransform = part.FindModelTransform(textureTransformNames[index]);
                if (textureTransform == null)
                {
                    Debug.Log("[SWTextureVariant] - Cannot find transform named " + textureTransformNames[index]);
                    continue;
                }

                rendererMaterial = textureTransform.GetComponent<Renderer>();
                if (rendererMaterial == null)
                {
                    Debug.Log("[SWTextureVariant] - Cannot find Renderer for " + textureTransformNames[index]);
                    continue;
                }

                if (!string.IsNullOrEmpty(mainTextureURL))
                {
                    textureForDecal = GameDatabase.Instance.GetTexture(mainTextureURL, false);
                    if (textureForDecal != null)
                        rendererMaterial.material.SetTexture("_MainTex", textureForDecal);
                }

                if (!string.IsNullOrEmpty(bumpMapURL))
                {
                    textureForDecal = GameDatabase.Instance.GetTexture(bumpMapURL, false);
                    if (textureForDecal != null)
                        rendererMaterial.material.SetTexture("_BumpMap", textureForDecal);
                }
            }
        }
    }
    #endregion
}
