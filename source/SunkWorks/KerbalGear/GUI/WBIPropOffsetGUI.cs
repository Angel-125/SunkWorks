using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP.Localization;

namespace SunkWorks.KerbalGear
{
    #region delegates
    public delegate Transform GetAttachTransformDelegate(BodyLocations bodyLocation);
    #endregion

    public class WBIPropOffsetGUI: Dialog<WBIPropOffsetGUI>
    {
        public Dictionary<string, List<WBIWearableProp>> wearablePartProps;
        public GetAttachTransformDelegate getAttachTransform;
        public KerbalEVA kerbalEVA;

        #region Housekeeping
        float offsetX;
        float offsetY;
        float offsetZ;
        float offsetRoll;
        float offsetPitch;
        float offsetYaw;

        float offsetXDelta;
        float offsetYDelta;
        float offsetZDelta;
        float offsetRollDelta;
        float offsetPitchDelta;
        float offsetYawDelta;

        string offsetXString;
        string offsetYString;
        string offsetZString;
        string offsetRollString;
        string offsetPitchString;
        string offsetYawString;

        Vector3 positionOffset = Vector3.zero;
        Vector3 rotationOffset = Vector3.zero;

        int buttonGroupIndex;
        string[] buttonTexts = new string[] { "0.1", "0.01", "0.001", "0.0001", "1", "5" };
        float[] deltaOffsets = new float[] { 0.1f, 0.01f, 0.001f, 0.0001f, 1f, 5f };
        GUILayoutOption[] propPanelOptions = new GUILayoutOption[] { GUILayout.Width(165) };
        Vector2 scrollPos;

        string selectedPropName = string.Empty;
        GameObject selectedProp = null;
        WBIWearableProp wearableProp;
        string[] partPropNames = null;
        #endregion

        #region Constructors
        public WBIPropOffsetGUI() :
        base("Prop Offsets", 635, 400)
        {
            WindowTitle = Localizer.Format("#LOC_SUNKWORKS_propOffsetTitle");
            Resizable = false;
        }
        #endregion

        public override void SetVisible(bool newValue)
        {
            base.SetVisible(newValue);

            if (newValue)
            {
                partPropNames = wearablePartProps.Keys.ToArray();

                List<WBIWearableProp> wearableProps = wearablePartProps[partPropNames[0]];
                wearableProp = wearableProps[0];

                selectedPropName = wearableProp.name;
                selectedProp = wearableProp.prop;

                setInitialOffsets();
            }
        }

        protected override void DrawWindowContents(int windowId)
        {
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();

            // Current prop
            GUILayout.Label(selectedPropName);

            // Selectable props to edit
            scrollPos = GUILayout.BeginScrollView(scrollPos, propPanelOptions);

            List<WBIWearableProp> wearableProps;
            int count;
            WBIWearableProp partProp;
            for (int index = 0; index < partPropNames.Length; index++)
            {
                GUILayout.Label(string.Format("<color=white>{0:s}</color>", partPropNames[index]));

                wearableProps = wearablePartProps[partPropNames[index]];
                count = wearableProps.Count;
                for (int propIndex = 0; propIndex < count; propIndex++)
                {
                    partProp = wearableProps[propIndex];
                    if (GUILayout.Button(partProp.name))
                    {
                        wearableProp = partProp;
                        selectedPropName = wearableProp.name;
                        selectedProp = wearableProp.prop;
                        setInitialOffsets();
                    }
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.BeginVertical();

            // Delta buttons
            buttonGroupIndex = GUILayout.SelectionGrid(buttonGroupIndex, buttonTexts, buttonTexts.Length);

            // Offsets
            bool updateNeeded = false;
            offsetXString = drawOffsetControls("#LOC_SUNKWORKS_OffsetX", offsetXString, ref offsetXDelta, ref updateNeeded);
            offsetYString = drawOffsetControls("#LOC_SUNKWORKS_OffsetY", offsetYString, ref offsetYDelta, ref updateNeeded);
            offsetZString = drawOffsetControls("#LOC_SUNKWORKS_OffsetZ", offsetZString, ref offsetZDelta, ref updateNeeded);
            offsetRollString = drawOffsetControls("#LOC_SUNKWORKS_OffsetRoll", offsetRollString, ref offsetRollDelta, ref updateNeeded);
            offsetPitchString = drawOffsetControls("#LOC_SUNKWORKS_OffsetPitch", offsetPitchString, ref offsetPitchDelta, ref updateNeeded);
            offsetYawString = drawOffsetControls("#LOC_SUNKWORKS_OffsetYaw", offsetYawString, ref offsetYawDelta, ref updateNeeded);

            // Update position offset
            positionOffset.x = offsetXDelta;
            positionOffset.y = offsetYDelta;
            positionOffset.z = offsetZDelta;

            // Update rotation offset
            rotationOffset.x = offsetRollDelta;
            rotationOffset.y = offsetPitchDelta;
            rotationOffset.z = offsetYawDelta;

            // Update the meshTransform's position and rotation.
            if (updateNeeded)
            {
                wearableProp.meshTransform.localPosition = positionOffset;
                wearableProp.meshTransform.localEulerAngles = rotationOffset;
            }

            // Copy offsets to clipboard button
            if (GUILayout.Button(Localizer.Format("#LOC_SUNKWORKS_copyOffsetsButton")))
            {
                StringBuilder outputString = new StringBuilder();
                outputString.AppendLine(string.Format("positionOffset = {0:n4}, {1:n4}, {2:n4}", offsetXDelta, offsetYDelta, offsetZDelta));
                outputString.AppendLine(string.Format("rotationOffset = {0:n4}, {1:n4}, {2:n4}", offsetRollDelta, offsetPitchDelta, offsetYawDelta));
                string offsetString = outputString.ToString();

                if (kerbalEVA.ModuleInventoryPartReference.ContainsPart(WBIWearablesController.kJetpackPartName))
                    offsetString = offsetString.Replace("positionOffset", "positionOffsetJetpack");

                GUIUtility.systemCopyBuffer = offsetString;
            }

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private string drawOffsetControls(string offsetLabel, string offsetText, ref float offsetDelta, ref bool updateNeeded)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format(offsetLabel));
            string offsetValue = GUILayout.TextField(offsetText);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<"))
            {
                offsetDelta -= deltaOffsets[buttonGroupIndex];
                offsetValue = offsetDelta.ToString();
                updateNeeded = true;
            }
            if (GUILayout.Button(">"))
            {
                offsetDelta += deltaOffsets[buttonGroupIndex];
                offsetValue = offsetDelta.ToString();
                updateNeeded = true;
            }
            GUILayout.EndHorizontal();

            float value = 0;
            if (offsetValue != offsetText && float.TryParse(offsetValue, out value))
            {
                offsetDelta = value;
                updateNeeded = true;
            }

            return offsetValue;
        }

        private void setInitialOffsets()
        {
            if (kerbalEVA.ModuleInventoryPartReference.ContainsPart(WBIWearablesController.kJetpackPartName) && wearableProp.bodyLocation == BodyLocations.backOrJetpack)
            {
                offsetXString = wearableProp.positionOffsetJetpack.x.ToString();
                offsetYString = wearableProp.positionOffsetJetpack.y.ToString();
                offsetZString = wearableProp.positionOffsetJetpack.z.ToString();

                offsetXDelta = wearableProp.positionOffsetJetpack.x;
                offsetYDelta = wearableProp.positionOffsetJetpack.y;
                offsetZDelta = wearableProp.positionOffsetJetpack.z;
            }
            else
            {
                offsetXString = wearableProp.positionOffset.x.ToString();
                offsetYString = wearableProp.positionOffset.y.ToString();
                offsetZString = wearableProp.positionOffset.z.ToString();

                offsetXDelta = wearableProp.positionOffset.x;
                offsetYDelta = wearableProp.positionOffset.y;
                offsetZDelta = wearableProp.positionOffset.z;
            }

            offsetRollString = wearableProp.rotationOffset.x.ToString();
            offsetPitchString = wearableProp.rotationOffset.y.ToString();
            offsetYawString = wearableProp.rotationOffset.z.ToString();

            offsetRollDelta = wearableProp.rotationOffset.x;
            offsetPitchDelta = wearableProp.rotationOffset.y;
            offsetYawDelta = wearableProp.rotationOffset.z;
        }
    }
}