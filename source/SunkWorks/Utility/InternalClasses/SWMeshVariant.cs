using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP.Localization;

namespace SunkWorks.Utility
{
    internal class SWMeshVariant
    {
        public Dictionary<string, bool> activeMeshes = new Dictionary<string, bool>();

        public void load(ConfigNode node)
        {
            int count = node.values.Count;
            string meshName;
            bool isEnabled = false;
            for (int index = 0; index < count; index++)
            {
                meshName = node.values[index].name;
                bool.TryParse(node.values[index].value, out isEnabled);

                activeMeshes.Add(meshName, isEnabled);
            }
        }

        public void applyVariant(Part part, bool applyToMeshes = true)
        {
            string[] keys = activeMeshes.Keys.ToArray();
            string meshName;
            Transform transform;
            Collider collider = null;

            for (int index = 0; index < keys.Length; index++)
            {
                meshName = keys[index];
                transform = part.FindModelTransform(meshName);
                if (transform == null)
                {
                    Debug.Log("[SWMeshVariant] - cannot find a transform named " + meshName);
                    continue;
                }

                if (applyToMeshes)
                    transform.gameObject.SetActive(activeMeshes[meshName]);

                collider = transform.gameObject.GetComponent<Collider>();
                if (collider != null)
                    collider.enabled = activeMeshes[meshName];
            }
        }
    }
}
