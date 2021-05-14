using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SunkWorks.KerbalGear
{
    #region WBIWearableProp
    /// <summary>
    /// Represents an instance of a wearable prop. One WBIWearableProp corresponds to a part's WBIWearableItem part module.
    /// Since WBIWearableItem is created in relation to the part prefab, we use WBIWearableProp per kerbal on EVA.
    /// </summary>
    public struct WBIWearableProp
    {
        /// <summary>
        /// The game object representing the prop.
        /// </summary>
        public GameObject prop;

        /// <summary>
        /// The physical prop mesh.
        /// </summary>
        public Transform meshTransform;

        /// <summary>
        /// Name of the prop.
        /// </summary>
        public string name;

        /// <summary>
        /// Location of the prop on the kerbal's body.
        /// </summary>
        public BodyLocations bodyLocation;

        /// <summary>
        /// Position offset of the prop.
        /// </summary>
        public Vector3 positionOffset;

        /// <summary>
        /// Position offset of the prop if the kerbal has a jetpack and bodyLocation is backOrJetpack.
        /// </summary>
        public Vector3 positionOffsetJetpack;

        /// <summary>
        /// Rotation offset of the prop.
        /// </summary>
        public Vector3 rotationOffset;
    }
    #endregion

    /// <summary>
    /// A utility class to handle wearable items and the part modules associated with them.
    /// </summary>
    public class WBIWearablesController: PartModule
    {
        #region Constants
        public const string kJetpackPartName = "evaJetpack";
        #endregion

        #region Fields
        /// <summary>
        /// Flag to turn on/off debug mode.
        /// </summary>
        [KSPField]
        public bool debugMode;
        #endregion

        #region Housekeeping
        KerbalEVA kerbalEVA;
        ModuleInventoryPart inventory;
        WBIPropOffsetGUI propOffsetView = null;
        Dictionary<string, List<WBIWearableProp>> wearablePartProps;
        Dictionary<string, string[]> wearablePartModules;
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            List<WBIWearablesController> controllers = part.FindModulesImplementing<WBIWearablesController>();
            if (controllers[0] != this)
            {
                return;
            }

            if (!HighLogic.LoadedSceneIsFlight)
                return;

            getKerbalModules();
            setupWearableParts();

            GameEvents.onModuleInventoryChanged.Add(onModuleInventoryChanged);
            GameEvents.onModuleInventorySlotChanged.Add(onModuleInventorySlotChanged);

            onModuleInventoryChanged(inventory);

            if (debugMode)
            {
                Events["ShowPropOffsetView"].guiActive = true;
                propOffsetView = new WBIPropOffsetGUI();
                propOffsetView.getAttachTransform = getAttachTransform;
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (kerbalEVA == null)
                return;
            hidePackMeshes();
        }
        #endregion

        #region Events
        /// <summary>
        /// Debug button that shows the prop offset view.
        /// </summary>
        [KSPEvent(guiName = "#LOC_SUNKWORKS_propOffsetButton")]
        public void ShowPropOffsetView()
        {
            propOffsetView.wearablePartProps = wearablePartProps;
            propOffsetView.kerbalEVA = kerbalEVA;
            propOffsetView.SetVisible(true);
        }
        #endregion

        #region Helpers
        public Transform getAttachTransform(BodyLocations bodyLocation)
        {
            string transformName = string.Empty;

            switch (bodyLocation)
            {
                case BodyLocations.back:
                case BodyLocations.backOrJetpack:
                    transformName = "bn_jetpack01";
                    break;

                case BodyLocations.leftFoot:
                    transformName = "bn_l_foot01";
                    break;

                case BodyLocations.rightFoot:
                    transformName = "bn_r_foot01";
                    break;

                case BodyLocations.leftBicep:
                    transformName = "bn_l_elbow_a01";
                    break;

                case BodyLocations.rightBicep:
                    transformName = "bn_r_elbow_a01";
                    break;

                default:
                    return null;
            }

            Transform transform = kerbalEVA.part.GetComponentsInChildren<Transform>(true).Where(t => t.name == transformName).FirstOrDefault();
            return transform;
        }

        private void onModuleInventoryChanged(ModuleInventoryPart partInventory)
        {
            if (!HighLogic.LoadedSceneIsFlight || partInventory != inventory)
                return;

            // Hide all wearable props and disable their part modules.
            hideAllProps();

            StoredPart storedPart;
            int[] storedPartKeys = inventory.storedParts.Keys.ToArray();
            string[] moduleNames;
            List<WBIWearableProp> wearableProps;
            WBIWearableProp wearableProp;
            int count;

            for (int index = 0; index < storedPartKeys.Length; index++)
            {
                storedPart = inventory.storedParts[storedPartKeys[index]];

                // Enable props
                if (wearablePartProps.ContainsKey(storedPart.partName))
                {
                    wearableProps = wearablePartProps[storedPart.partName];
                    count = wearableProps.Count;
                    for (int propIndex = 0; propIndex < count; propIndex++)
                    {
                        wearableProp = wearableProps[propIndex];

                        wearableProp.prop.SetActive(true);

                        wearableProp.meshTransform.localEulerAngles = wearableProp.rotationOffset;
                        if (wearableProp.bodyLocation != BodyLocations.backOrJetpack)
                            wearableProp.meshTransform.localPosition = wearableProp.positionOffset;
                        else
                            wearableProp.meshTransform.localPosition = inventory.ContainsPart(kJetpackPartName) ? wearableProp.positionOffsetJetpack : wearableProp.positionOffset;
                    }
                }

                // Enable the part modules
                if (wearablePartModules.ContainsKey(storedPart.partName))
                {
                    moduleNames = wearablePartModules[storedPart.partName];
                    for (int moduleIndex = 0; moduleIndex < moduleNames.Length; moduleIndex++)
                    {
                        if (part.Modules.Contains(moduleNames[moduleIndex]))
                        {
                            part.Modules[moduleNames[moduleIndex]].moduleIsEnabled = true;
                            part.Modules[moduleNames[moduleIndex]].enabled = true;
                            part.Modules[moduleNames[moduleIndex]].OnActive();
                        }
                    }
                }
            }
        }

        private void onModuleInventorySlotChanged(ModuleInventoryPart partInventory, int slotIndex)
        {
            onModuleInventoryChanged(partInventory);
        }

        private void hideAllProps()
        {
            // Hide the props
            List<WBIWearableProp> wearableProps;
            string[] keys = wearablePartProps.Keys.ToArray();
            int count;
            for (int index = 0; index < keys.Length; index++)
            {
                wearableProps = wearablePartProps[keys[index]];
                count = wearableProps.Count;
                for (int propIndex = 0; propIndex < count; propIndex++)
                {
                    wearableProps[propIndex].prop.SetActive(false);
                }
            }

            // Disable the part modules
            string[] evaModules;
            keys = wearablePartModules.Keys.ToArray();
            for (int index = 0; index < keys.Length; index++)
            {
                evaModules = wearablePartModules[keys[index]];
                for (int moduleIndex = 0; moduleIndex < evaModules.Length; moduleIndex++)
                {
                    if (part.Modules.Contains(evaModules[moduleIndex]))
                    {
                        part.Modules[evaModules[moduleIndex]].OnInactive();
                        part.Modules[evaModules[moduleIndex]].moduleIsEnabled = false;
                        part.Modules[evaModules[moduleIndex]].enabled = false;
                    }
                }
            }
        }

        private void hidePackMeshes()
        {
            kerbalEVA.BackpackTransform.gameObject.SetActive(false);
            kerbalEVA.BackpackStTransform.gameObject.SetActive(false);
            kerbalEVA.StorageTransform.gameObject.SetActive(false);
            kerbalEVA.StorageSlimTransform.gameObject.SetActive(false);

            List<FlagDecal> flags = part.FindModulesImplementing<FlagDecal>();
            int flagCount = flags.Count;
            for (int flagIndex = 0; flagIndex < flagCount; flagIndex++)
            {
                flags[flagIndex].flagDisplayed = false;
                flags[flagIndex].UpdateDisplay();
            }

            if (inventory.ContainsPart("evaChute"))
            {
                kerbalEVA.ChuteJetpackTransform.gameObject.SetActive(false);
                kerbalEVA.ChuteStTransform.gameObject.SetActive(false);
                kerbalEVA.ChuteContainerTransform.gameObject.SetActive(true);
            }
            else
            {
                kerbalEVA.ChuteJetpackTransform.gameObject.SetActive(false);
                kerbalEVA.ChuteStTransform.gameObject.SetActive(false);
                kerbalEVA.ChuteContainerTransform.gameObject.SetActive(false);
            }
        }

        private void setupWearableParts()
        {
            List<AvailablePart> cargoParts = PartLoader.Instance.GetAvailableAndPurchaseableCargoParts();
            AvailablePart availablePart;
            List<WBIWearableItem> wearableItems;
            WBIWearableItem wearableItem;
            int count = cargoParts.Count;
            int itemCount;
            Transform anchorTransform;
            GameObject prefab;
            GameObject prop;
            Transform attachTransform;
            Collider[] colliders;
            WBIWearableProp wearableProp;
            List<WBIWearableProp> wearableProps;

            wearablePartProps = new Dictionary<string, List<WBIWearableProp>>();
            wearablePartModules = new Dictionary<string, string[]>();

            for (int index = 0; index < count; index++)
            {
                availablePart = cargoParts[index];
                if (availablePart.partPrefab.HasModuleImplementing<WBIWearableItem>())
                {
                    wearableItems = availablePart.partPrefab.FindModulesImplementing<WBIWearableItem>();

                    // Setup our wearable props for this part.
                    wearableProps = new List<WBIWearableProp>();
                    wearablePartProps.Add(availablePart.name, wearableProps);

                    // Setup the props- Special thanks to Vali and Issac for showing the way how!
                    itemCount = wearableItems.Count;
                    for (int itemIndex = 0; itemIndex < itemCount; itemIndex++)
                    {
                        wearableItem = wearableItems[itemIndex];

                        // Create new wearable prop instance.
                        wearableProp = new WBIWearableProp();
                        wearableProp.name = wearableItem.moduleID;
                        wearableProp.bodyLocation = wearableItem.bodyLocation;
                        wearableProp.positionOffset = wearableItem.positionOffset;
                        wearableProp.positionOffsetJetpack = wearableItem.positionOffsetJetpack;
                        wearableProp.rotationOffset = wearableItem.rotationOffset;

                        // Setup part module names
                        if (!string.IsNullOrEmpty(wearableItem.evaModules))
                        {
                            string[] evaModules = wearableItem.evaModules.Split(new char[] { ';' });
                            wearablePartModules.Add(availablePart.name, evaModules);
                        }

                        // Get the attachment transform
                        attachTransform = getAttachTransform(wearableItem.bodyLocation);

                        // Get the anchor transform and prefab
                        anchorTransform = availablePart.partPrefab.FindModelTransform(wearableItem.anchorTransform);
                        if (anchorTransform == null)
                            continue;
                        prefab = anchorTransform.gameObject;

                        // Create instance
                        prop = Instantiate(prefab, attachTransform.position, attachTransform.rotation, kerbalEVA.transform);
                        prop.name = wearableItem.moduleID;
                        wearableProp.prop = prop;

                        // Add the TrackingRigObject. The tracking rig moves the prop (GameObject) associated with the anchorTransform.
                        TrackRigObject trackRig = prop.AddComponent<TrackRigObject>();
                        trackRig.target = attachTransform;
                        trackRig.keepInitialOffset = false;
                        trackRig.trackingMode = TrackRigObject.TrackMode.LateUpdate;

                        // Now we need the child mesh that we'll apply the position and rotation offsets to.
                        Transform meshTransform = prop.transform.Find(wearableItem.meshTransform);
                        wearableProp.meshTransform = meshTransform;

                        /*
                        meshTransform.localEulerAngles = wearableItem.rotationOffset;
                        if (wearableItem.bodyLocation != BodyLocations.backOrJetpack)
                            meshTransform.localPosition = wearableItem.positionOffset;
                        else
                            meshTransform.localPosition = inventory.ContainsPart(kJetpackPartName) ? wearableItem.positionOffsetJetpack : wearableItem.positionOffset;
                        */

                        // Remove colliders
                        colliders = prop.GetComponentsInChildren<Collider>(true);
                        for (int colliderIndex = 0; colliderIndex < colliders.Length; colliderIndex++)
                            DestroyImmediate(colliders[colliderIndex]);

                        // Hide the prop for now.
                        prop.SetActive(false);

                        // Add the wearable prop to our list.
                        wearablePartProps[availablePart.name].Add(wearableProp);
                    }
                }
            }
        }

        private void getKerbalModules()
        {
            kerbalEVA = part.FindModuleImplementing<KerbalEVA>();
            inventory = kerbalEVA.ModuleInventoryPartReference;
        }
        #endregion
    }
}