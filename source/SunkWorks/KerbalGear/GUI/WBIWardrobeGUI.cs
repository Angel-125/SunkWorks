using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KSP.Localization;
using UnityEngine;
using Expansions.Missions;
using Expansions.Serenity;

namespace SunkWorks.KerbalGear
{
    public class WBISuitCombo : SuitCombo
    {
        public bool isStockSuit = true;
    }

    public class WBIWardrobeGUI: Dialog<WBIWardrobeGUI>
    {
        #region Constants
        const string kWardrobeIconNode = "WARDROBE_IMAGE";
        #endregion

        #region Fields
        public Part part;
        #endregion

        #region Housekeeping
        SuitCombos suitCombos;
        SuitCombo[] maleSuitCombos;
        SuitCombo[] femaleSuitCombos;
        SuitCombo[] defaultSuits;
        SuitCombo[] vintageSuits;
        SuitCombo[] futureSuits;
        SuitCombo selectedCombo = null;
        List<ProtoCrewMember> crewList = null;
        ProtoCrewMember selectedCrew = null;
        Vector2 crewListScrollPos = Vector2.zero;
        Vector2 suitListScrollPos = Vector2.zero;
        GUILayoutOption[] crewPanelWidth = new GUILayoutOption[] { GUILayout.Width(200) };
        GUILayoutOption[] suitPanelWidth = new GUILayoutOption[] { GUILayout.Width(200) };
        GUILayoutOption[] suitPreviewPanelWidth = new GUILayoutOption[] { GUILayout.Width(300) };
        Texture2D suitSprite = null;
        Dictionary<string, string> wardrobeIcons = null;
        #endregion

        #region Constructors
        public WBIWardrobeGUI() :
        base("Suit Switcher", 700, 400)
        {
            WindowTitle = Localizer.Format("#LOC_SUNKWORKS_suitSwitcherTitle");
            Resizable = false;
        }
        #endregion

        #region Overrides
        public override void SetVisible(bool newValue)
        {
            base.SetVisible(newValue);

            if (newValue)
            {
                // Get the suit combos
                suitCombos = GameDatabase.Instance.GetComponent<SuitCombos>();
                List<SuitCombo> maleSuits = new List<SuitCombo>();
                List<SuitCombo> femaleSuits = new List<SuitCombo>();

                // Get stock suits
                SuitCombo suitCombo;
                int count = suitCombos.StockCombos.Count;
                for (int index = 0; index < count; index++)
                {
                    suitCombo = suitCombos.StockCombos[index];
                    if (suitCombo.gender.ToLower() == "male")
                        maleSuits.Add(suitCombo);
                    else
                        femaleSuits.Add(suitCombo);
                }

                // Get extra suits
                count = suitCombos.ExtraCombos.Count;
                for (int index = 0; index < count; index++)
                {
                    suitCombo = suitCombos.ExtraCombos[index];
                    if (suitCombo.gender.ToLower() == "male")
                        maleSuits.Add(suitCombo);
                    else
                        femaleSuits.Add(suitCombo);
                }
                maleSuitCombos = maleSuits.ToArray();
                femaleSuitCombos = femaleSuits.ToArray();

                // Get wardrobe icons
                getWardrobeIcons();

                for (int index = 0; index < maleSuitCombos.Length; index++)
                    Debug.Log("[WBIWardrobeGUI] - " + maleSuitCombos[index].name + " " + Localizer.Format(maleSuitCombos[index].displayName));

                for (int index = 0; index < femaleSuitCombos.Length; index++)
                    Debug.Log("[WBIWardrobeGUI] - " + femaleSuitCombos[index].name + " " + Localizer.Format(femaleSuitCombos[index].displayName));

                // Get crew list
                crewList = part.protoModuleCrew;
                if (crewList != null && crewList.Count > 0)
                {
                    selectedCrew = crewList[0];
                    selectedCombo = suitCombos.GetCombo(selectedCrew.ComboId);
                    updateSuitCombos();
                }
            }
        }
        #endregion

        #region Drawing
        protected override void DrawWindowContents(int windowId)
        {
            GUILayout.BeginHorizontal();

            // Draw list of kerbals
            drawCrewList();

            // Draw suit selection list
            drawSuitSelectionList();

            drawSuitPreviewPanel();

            GUILayout.EndHorizontal();
        }

        void drawSuitPreviewPanel()
        {
            GUILayout.BeginVertical();

            GUILayout.BeginScrollView(Vector2.zero, suitPreviewPanelWidth);

            ProtoCrewMember.KerbalSuit suitType = ProtoCrewMember.KerbalSuit.Default;

            if (selectedCombo != null)
            {
                string suitTypeString = string.Empty;
                switch (selectedCombo.suitType.ToLower())
                {
                    case "vintage":
                        suitType = ProtoCrewMember.KerbalSuit.Vintage;
                        suitTypeString = Localizer.Format("#autoLOC_8012022");
                        break;
                    case "future":
                        suitType = ProtoCrewMember.KerbalSuit.Future;
                        suitTypeString = Localizer.Format("#autoLOC_8012023");
                        break;
                    default:
                        suitType = ProtoCrewMember.KerbalSuit.Default;
                        suitTypeString = Localizer.Format("#autoLOC_8012021");
                        break;
                }
                GUILayout.Label("<color=white>" + Localizer.Format(selectedCombo.displayName) + " " + suitTypeString + "</color>");
            }

            // Suit sprite
            if (suitSprite != null)
                GUILayout.Label(suitSprite);

            GUILayout.EndScrollView();

            if (GUILayout.Button(Localizer.Format("#LOC_SUNKWORKS_suitSwitcherSelectSuit")) && selectedCrew != null && selectedCombo != null)
            {
                selectedCrew.ComboId = selectedCombo.name;
                if (!string.IsNullOrEmpty(selectedCombo.suitTexture))
                    selectedCrew.SuitTexturePath = selectedCombo.suitTexture;
                if (!string.IsNullOrEmpty(selectedCombo.normalTexture))
                    selectedCrew.NormalTexturePath = selectedCombo.normalTexture;

                selectedCrew.suit = suitType;
                switch (selectedCombo.suitType.ToLower())
                {
                    case "vintage":
                        selectedCrew.suit = ProtoCrewMember.KerbalSuit.Vintage;
                        break;
                    case "future":
                        selectedCrew.suit = ProtoCrewMember.KerbalSuit.Future;
                        break;
                    default:
                        selectedCrew.suit = ProtoCrewMember.KerbalSuit.Default;
                        break;
                }

                selectedCrew.UseStockTexture = suitCombos.StockCombos.Contains(selectedCombo);
            }

            GUILayout.EndVertical();
        }

        void drawSuitSelectionList()
        {
            GUILayout.BeginVertical();

            if (selectedCrew != null)
                GUILayout.Label("<color=white>" + selectedCrew.name + " - " + selectedCrew.trait + "</color>");

            suitListScrollPos = GUILayout.BeginScrollView(suitListScrollPos, suitPanelWidth);

            SuitCombo suitCombo;

            // Default
            GUILayout.Label("<color=white><b>" + Localizer.Format("#autoLOC_8012021") + "</b></color>");
            for (int index = 0; index < defaultSuits.Length; index++)
            {
                suitCombo = defaultSuits[index];
                if (GUILayout.Button(Localizer.Format(suitCombo.displayName)))
                {
                    selectedCombo = suitCombo;
                    updateSuitSprite();
                }
            }

            // Vintage
            GUILayout.Label("<color=white><b>" + Localizer.Format("#autoLOC_8012022") + "</b></color>");
            for (int index = 0; index < vintageSuits.Length; index++)
            {
                suitCombo = vintageSuits[index];
                if (GUILayout.Button(Localizer.Format(suitCombo.displayName)))
                {
                    selectedCombo = suitCombo;
                    updateSuitSprite();
                }
            }

            // Future
            GUILayout.Label("<color=white><b>" + Localizer.Format("#autoLOC_8012023") + "</b></color>");
            for (int index = 0; index < futureSuits.Length; index++)
            {
                suitCombo = futureSuits[index];
                if (GUILayout.Button(Localizer.Format(suitCombo.displayName)))
                {
                    selectedCombo = suitCombo;
                    updateSuitSprite();
                }
            }

            GUILayout.EndScrollView();

            GUILayout.EndVertical();
        }

        void drawCrewList()
        {
            if (crewList == null)
                return;
            else if (crewList.Count == 0)
                return;

            int count = crewList.Count;

            GUILayout.BeginVertical();

            crewListScrollPos = GUILayout.BeginScrollView(crewListScrollPos, crewPanelWidth);

            for (int index = 0; index < count; index++)
            {
                if (GUILayout.Button(crewList[index].name))
                {
                    selectedCrew = crewList[index];
                    selectedCombo = suitCombos.GetCombo(selectedCrew.ComboId);
                    updateSuitCombos();
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }
        #endregion

        #region Helpers
        void getWardrobeIcons()
        {
            wardrobeIcons = new Dictionary<string, string>();
            ConfigNode[] wardrobes = GameDatabase.Instance.GetConfigNodes(kWardrobeIconNode);
            ConfigNode node;

            for (int index = 0; index < wardrobes.Length; index++)
            {
                node = wardrobes[index];
                if (!node.HasValue("name") || !node.HasValue("image"))
                    continue;
                if (!wardrobeIcons.ContainsKey(node.GetValue("name")))
                    wardrobeIcons.Add(node.GetValue("name"), node.GetValue("image"));
            }
        }

        void updateSuitCombos()
        {
            List<SuitCombo> defaultCombos = new List<SuitCombo>();
            List<SuitCombo> vintageCombos = new List<SuitCombo>();
            List<SuitCombo> futureCombos = new List<SuitCombo>();
            SuitCombo[] combos = selectedCrew.gender == ProtoCrewMember.Gender.Male ? maleSuitCombos : femaleSuitCombos;
            SuitCombo combo;

            for (int index = 0; index < combos.Length; index++)
            {
                combo = combos[index];

                switch (combo.suitType.ToLower())
                {
                    case "vintage":
                        vintageCombos.Add(combo);
                        break;

                    case "future":
                        futureCombos.Add(combo);
                        break;

                    default:
                        defaultCombos.Add(combo);
                        break;
                }
            }

            defaultSuits = defaultCombos.ToArray();
            vintageSuits = vintageCombos.ToArray();
            futureSuits = futureCombos.ToArray();
        }

        void updateSuitSprite()
        {
            if (selectedCombo != null && wardrobeIcons.ContainsKey(selectedCombo.name))
            {
                suitSprite = GameDatabase.Instance.GetTexture(wardrobeIcons[selectedCombo.name], false);
            }

            else if (selectedCombo != null && !string.IsNullOrEmpty(selectedCombo.sprite))
            {
                suitSprite = GameDatabase.Instance.GetTexture(selectedCombo.sprite, false);
            }

            else
            {
                suitSprite = null;
            }
        }
        #endregion
    }
}
