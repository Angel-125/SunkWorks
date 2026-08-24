using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SunkWorks.Structural
{
    /// <summary>
    /// Generates a flat-bottomed boat hull by lofting calculated cross-section stations.
    /// The authored model supplies four renderers/materials; their reference meshes are
    /// replaced with per-part runtime meshes for the upper hull, lower hull, deck, and railings.
    /// </summary>
    [KSPModule("Procedural Boat Hull")]
    public class WBIModuleProceduralHull : PartModule, IPartMassModifier, IPartCostModifier
    {
        const string kGroupName = "ProceduralHull";
        const string kGroupTitle = "Procedural Hull";
        const float kMinimumDimension = 0.05f;
        const float kMinimumUpperSide = 0.1f;
        const float kMaximumLowerHullDepthRatio = 1f / 3f;

        #region Model setup
        [KSPField]
        public string upperHullTransform = "proceduralUpperHull";

        [KSPField]
        public string lowerHullTransform = "proceduralLowerHull";

        [KSPField]
        public string deckTransform = "proceduralDeck";

        [KSPField]
        public string railingsTransform = "proceduralRailings";

        [KSPField]
        public string colliderHolderTransform = "colliderHolder";

        [KSPField]
        public string referenceColliderTransform = "referenceCollider";

        [KSPField]
        public string deckAnchorTransform = "nodeDeckCenter";

        /// <summary>Part-local port-to-starboard direction.</summary>
        [KSPField]
        public Vector3 widthAxis = Vector3.right;

        /// <summary>Part-local bow-to-stern direction.</summary>
        [KSPField]
        public Vector3 lengthAxis = Vector3.up;

        /// <summary>Part-local direction from the deck toward the bottom.</summary>
        [KSPField]
        public Vector3 downAxis = Vector3.forward;

        /// <summary>Longitudinal texture density, in source-image pixels per meter.</summary>
        [KSPField]
        public float textureDensityU = 400f;

        /// <summary>Transverse, vertical, or surface-direction texture density, in source-image pixels per meter.</summary>
        [KSPField]
        public float textureDensityV = 400f;
        #endregion

        #region Player-facing dimensions
        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Length", groupName = kGroupName, groupDisplayName = kGroupTitle)]
        [UI_FloatRange(minValue = 4f, maxValue = 60f, stepIncrement = 0.1f, scene = UI_Scene.Editor)]
        public float hullLength = 12f;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Beam", groupName = kGroupName, groupDisplayName = kGroupTitle)]
        [UI_FloatRange(minValue = 2f, maxValue = 24f, stepIncrement = 0.1f, scene = UI_Scene.Editor)]
        public float beam = 5f;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Hull Depth", groupName = kGroupName, groupDisplayName = kGroupTitle)]
        [UI_FloatRange(minValue = 0.75f, maxValue = 8f, stepIncrement = 0.1f, scene = UI_Scene.Editor)]
        public float hullDepth = 2.5f;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Chine Radius", groupName = kGroupName, groupDisplayName = kGroupTitle)]
        [UI_FloatRange(minValue = 0.05f, maxValue = 3f, stepIncrement = 0.1f, scene = UI_Scene.Editor)]
        public float chineRadius = 0.5f;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Lower Side Height", groupName = kGroupName, groupDisplayName = kGroupTitle)]
        [UI_FloatRange(minValue = 0f, maxValue = 3f, stepIncrement = 0.1f, scene = UI_Scene.Editor)]
        public float lowerSideHeight = 0.45f;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Side Flare", groupName = kGroupName, groupDisplayName = kGroupTitle)]
        [UI_FloatRange(minValue = 0f, maxValue = 30f, stepIncrement = 0.1f, scene = UI_Scene.Editor)]
        public float sideFlare = 5f;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Bow Length", groupName = kGroupName, groupDisplayName = kGroupTitle)]
        [UI_FloatRange(minValue = 0.5f, maxValue = 15f, stepIncrement = 0.1f, scene = UI_Scene.Editor)]
        public float bowLength = 3f;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Bow Fullness", groupName = kGroupName, groupDisplayName = kGroupTitle)]
        [UI_FloatRange(minValue = 0f, maxValue = 1f, stepIncrement = 0.1f, scene = UI_Scene.Editor)]
        public float bowFullness = 0.55f;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Bow Rake", groupName = kGroupName, groupDisplayName = kGroupTitle)]
        [UI_FloatRange(minValue = 0f, maxValue = 3f, stepIncrement = 0.1f, scene = UI_Scene.Editor)]
        public float bowRake = 0.8f;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Stern Taper Length", groupName = kGroupName, groupDisplayName = kGroupTitle)]
        [UI_FloatRange(minValue = 0f, maxValue = 15f, stepIncrement = 0.1f, scene = UI_Scene.Editor)]
        public float sternTaperLength = 3f;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Transom Beam Ratio", groupName = kGroupName, groupDisplayName = kGroupTitle)]
        [UI_FloatRange(minValue = 0.25f, maxValue = 1f, stepIncrement = 0.1f, scene = UI_Scene.Editor)]
        public float sternBeamRatio = 0.72f;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Stern Fullness", groupName = kGroupName, groupDisplayName = kGroupTitle)]
        [UI_FloatRange(minValue = 0f, maxValue = 1f, stepIncrement = 0.1f, scene = UI_Scene.Editor)]
        public float sternFullness = 0.6f;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Aft Run Length", groupName = kGroupName, groupDisplayName = kGroupTitle)]
        [UI_FloatRange(minValue = 0f, maxValue = 15f, stepIncrement = 0.1f, scene = UI_Scene.Editor)]
        public float aftRunLength = 3f;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Aft Rise", groupName = kGroupName, groupDisplayName = kGroupTitle)]
        [UI_FloatRange(minValue = 0f, maxValue = 4f, stepIncrement = 0.1f, scene = UI_Scene.Editor)]
        public float aftRise = 0.6f;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Length Sections", groupName = kGroupName, groupDisplayName = kGroupTitle)]
        [UI_FloatRange(minValue = 8f, maxValue = 64f, stepIncrement = 1f, scene = UI_Scene.Editor)]
        public float longitudinalSections = 24f;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Chine Segments", groupName = kGroupName, groupDisplayName = kGroupTitle)]
        [UI_FloatRange(minValue = 2f, maxValue = 12f, stepIncrement = 1f, scene = UI_Scene.Editor)]
        public float chineSegments = 5f;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Railings", groupName = kGroupName, groupDisplayName = kGroupTitle)]
        [UI_Toggle(enabledText = "On", disabledText = "Off")]
        public bool railingsEnabled = true;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Railing Height", groupName = kGroupName, groupDisplayName = kGroupTitle)]
        [UI_FloatRange(minValue = 0.1f, maxValue = 1f, stepIncrement = 0.1f, scene = UI_Scene.Editor)]
        public float railingHeight = 1f;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Railing Thickness", groupName = kGroupName, groupDisplayName = kGroupTitle)]
        [UI_FloatRange(minValue = 0.02f, maxValue = 0.2f, stepIncrement = 0.01f, scene = UI_Scene.Editor)]
        public float railingThickness = 0.1f;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Bow Railings", groupName = kGroupName, groupDisplayName = kGroupTitle)]
        [UI_Toggle(enabledText = "On", disabledText = "Off")]
        public bool bowRailings = true;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Stern Railings", groupName = kGroupName, groupDisplayName = kGroupTitle)]
        [UI_Toggle(enabledText = "On", disabledText = "Off")]
        public bool sternRailings = true;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Port Railings", groupName = kGroupName, groupDisplayName = kGroupTitle)]
        [UI_Toggle(enabledText = "On", disabledText = "Off")]
        public bool portRailings = true;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Starboard Railings", groupName = kGroupName, groupDisplayName = kGroupTitle)]
        [UI_Toggle(enabledText = "On", disabledText = "Off")]
        public bool starboardRailings = true;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Buoyancy", groupName = kGroupName, groupDisplayName = kGroupTitle)]
        [UI_FloatRange(minValue = 0.05f, maxValue = 1f, stepIncrement = 0.05f, scene = UI_Scene.All)]
        public float adjustedBuoyancy = 0.7f;
        #endregion

        #region Physical properties
        [KSPField]
        public float massPerSquareMeter = 0.018f;

        [KSPField]
        public float costPerSquareMeter = 18f;

        [KSPField]
        public int colliderSections = 6;

        [KSPField]
        public bool updateDragCubesInEditor = false;

        /// <summary>Shows mesh-tessellation controls in the editor PAW.</summary>
        [KSPField]
        public bool debugMode = false;

        /// <summary>Draws the final procedural render meshes as white triangle edges.</summary>
        [KSPField(isPersistant = true, guiActiveEditor = true, guiName = "Wireframe Overlay", groupName = kGroupName, groupDisplayName = kGroupTitle)]
        [UI_Toggle(enabledText = "On", disabledText = "Off")]
        public bool showWireframe = false;

        [KSPField(guiActiveEditor = true, guiName = "Enclosed Volume", groupName = kGroupName, groupDisplayName = kGroupTitle)]
        public string volumeDisplay = "0 m^3";

        [KSPField(guiActiveEditor = true, guiName = "Hull Area", groupName = kGroupName, groupDisplayName = kGroupTitle)]
        public string areaDisplay = "0 m^2";

        [KSPField(isPersistant = true)]
        public float generatedVolume;

        [KSPField(isPersistant = true)]
        public float generatedArea;
        #endregion

        MeshFilter upperHullFilter;
        MeshFilter lowerHullFilter;
        MeshFilter deckFilter;
        MeshFilter railingsFilter;
        Transform colliderHolder;
        Transform deckAnchor;
        Mesh upperHullMesh;
        Mesh lowerHullMesh;
        Mesh deckMesh;
        Mesh railingsMesh;
        Material wireframeMaterial;
        readonly List<GameObject> wireframeObjects = new List<GameObject>();
        readonly List<Mesh> wireframeMeshes = new List<Mesh>();
        readonly List<Mesh> colliderMeshes = new List<Mesh>();
        readonly string[] editableFields =
        {
            "hullLength", "beam", "hullDepth", "chineRadius", "lowerSideHeight",
            "sideFlare", "bowLength", "bowFullness", "bowRake", "sternTaperLength",
            "sternBeamRatio", "sternFullness", "aftRunLength", "aftRise",
            "longitudinalSections", "chineSegments", "railingsEnabled", "railingHeight",
            "railingThickness", "bowRailings", "sternRailings",
            "portRailings", "starboardRailings", "adjustedBuoyancy", "showWireframe"
        };
        bool isRebuilding;
        bool initialized;

        /// <summary>Regenerates all visual and collision geometry from the persisted parameters.</summary>
        [KSPEvent(guiActiveEditor = true, guiName = "Rebuild Hull", groupName = kGroupName, groupDisplayName = kGroupTitle)]
        public void RebuildHullEvent()
        {
            RebuildHull(true);
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            if (!FindModelObjects())
                return;

            CreateRuntimeMeshes();
            BindEditorFields();
            UpdateDeckNodes();
            initialized = true;
            RebuildHull(false);
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (HighLogic.LoadedSceneIsFlight && part.buoyancy != adjustedBuoyancy)
            {
                part.buoyancy = adjustedBuoyancy;
            }
        }

        void OnDestroy()
        {
            DestroyRuntimeMesh(upperHullMesh);
            DestroyRuntimeMesh(lowerHullMesh);
            DestroyRuntimeMesh(deckMesh);
            DestroyRuntimeMesh(railingsMesh);
            ClearWireframeOverlays();
            if (wireframeMaterial != null)
                UnityEngine.Object.Destroy(wireframeMaterial);
            for (int index = 0; index < colliderMeshes.Count; index++)
                DestroyRuntimeMesh(colliderMeshes[index]);
            colliderMeshes.Clear();
        }

        bool FindModelObjects()
        {
            Transform upper = part.FindModelTransform(upperHullTransform);
            Transform lower = part.FindModelTransform(lowerHullTransform);
            Transform deck = part.FindModelTransform(deckTransform);
            Transform railings = part.FindModelTransform(railingsTransform);
            colliderHolder = part.FindModelTransform(colliderHolderTransform);
            deckAnchor = part.FindModelTransform(deckAnchorTransform);

            if (upper == null || lower == null || deck == null || railings == null || colliderHolder == null)
            {
                Debug.LogError("[SunkWorks] WBIModuleProceduralHull could not find one or more required model transforms on " + part.name);
                return false;
            }

            upperHullFilter = upper.GetComponent<MeshFilter>();
            lowerHullFilter = lower.GetComponent<MeshFilter>();
            deckFilter = deck.GetComponent<MeshFilter>();
            railingsFilter = railings.GetComponent<MeshFilter>();
            if (upperHullFilter == null || lowerHullFilter == null || deckFilter == null || railingsFilter == null)
            {
                Debug.LogError("[SunkWorks] WBIModuleProceduralHull requires MeshFilters on all four procedural render transforms.");
                return false;
            }

            if (deckAnchor == null)
                deckAnchor = deck;

            return true;
        }

        void CreateRuntimeMeshes()
        {
            upperHullMesh = CreateRuntimeMesh(part.name + "_UpperHull");
            lowerHullMesh = CreateRuntimeMesh(part.name + "_LowerHull");
            deckMesh = CreateRuntimeMesh(part.name + "_Deck");
            railingsMesh = CreateRuntimeMesh(part.name + "_Railings");
            upperHullFilter.mesh = upperHullMesh;
            lowerHullFilter.mesh = lowerHullMesh;
            deckFilter.mesh = deckMesh;
            railingsFilter.mesh = railingsMesh;
        }

        static Mesh CreateRuntimeMesh(string meshName)
        {
            Mesh mesh = new Mesh();
            mesh.name = meshName;
            mesh.MarkDynamic();
            return mesh;
        }

        static void DestroyRuntimeMesh(Mesh mesh)
        {
            if (mesh != null)
                UnityEngine.Object.Destroy(mesh);
        }

        void BindEditorFields()
        {
            if (!HighLogic.LoadedSceneIsEditor)
                return;

            BaseField chineSegmentsField = Fields["chineSegments"];
            if (chineSegmentsField != null)
                chineSegmentsField.guiActiveEditor = debugMode;
            BaseField wireframeField = Fields["showWireframe"];
            if (wireframeField != null)
                wireframeField.guiActiveEditor = debugMode;

            for (int index = 0; index < editableFields.Length; index++)
            {
                BaseField field = Fields[editableFields[index]];
                if (field != null && field.uiControlEditor != null)
                    field.uiControlEditor.onFieldChanged = OnHullFieldChanged;
            }
        }

        void OnHullFieldChanged(BaseField field, object oldValue)
        {
            if (!initialized || isRebuilding)
                return;

            RebuildHull(true);
            for (int index = 0; index < part.symmetryCounterparts.Count; index++)
            {
                WBIModuleProceduralHull counterpart = part.symmetryCounterparts[index].FindModuleImplementing<WBIModuleProceduralHull>();
                if (counterpart == null)
                    continue;
                counterpart.CopyParametersFrom(this);
                counterpart.RebuildHull(false);
            }
        }

        void CopyParametersFrom(WBIModuleProceduralHull source)
        {
            hullLength = source.hullLength;
            beam = source.beam;
            hullDepth = source.hullDepth;
            chineRadius = source.chineRadius;
            lowerSideHeight = source.lowerSideHeight;
            sideFlare = source.sideFlare;
            bowLength = source.bowLength;
            bowFullness = source.bowFullness;
            bowRake = source.bowRake;
            sternTaperLength = source.sternTaperLength;
            sternBeamRatio = source.sternBeamRatio;
            sternFullness = source.sternFullness;
            aftRunLength = source.aftRunLength;
            aftRise = source.aftRise;
            longitudinalSections = source.longitudinalSections;
            chineSegments = source.chineSegments;
            railingsEnabled = source.railingsEnabled;
            railingHeight = source.railingHeight;
            railingThickness = source.railingThickness;
            bowRailings = source.bowRailings;
            sternRailings = source.sternRailings;
            portRailings = source.portRailings;
            starboardRailings = source.starboardRailings;
            showWireframe = source.showWireframe;
        }

        void RebuildHull(bool notifyEditor)
        {
            if (!initialized || isRebuilding)
                return;

            isRebuilding = true;
            try
            {
                ConstrainParameters();
                List<HullStation> stations = GenerateStations();

                TextureTiling upperTiling = GetTextureTiling(upperHullFilter);
                TextureTiling lowerTiling = GetTextureTiling(lowerHullFilter);
                TextureTiling deckTiling = GetTextureTiling(deckFilter);
                TextureTiling railingsTiling = GetTextureTiling(railingsFilter);

                MeshBuffers upperBuffers = BuildUpperHull(stations, upperTiling);
                MeshBuffers lowerBuffers = BuildLowerHull(stations, lowerTiling);
                MeshBuffers deckBuffers = BuildDeck(stations, deckTiling);
                MeshBuffers railingsBuffers = BuildRailings(stations, railingsTiling);

                WriteMesh(upperHullMesh, upperHullFilter.transform, upperBuffers);
                WriteMesh(lowerHullMesh, lowerHullFilter.transform, lowerBuffers);
                WriteMesh(deckMesh, deckFilter.transform, deckBuffers);
                WriteMesh(railingsMesh, railingsFilter.transform, railingsBuffers);
                UpdateWireframeOverlays();

                generatedArea = CalculateArea(upperBuffers) + CalculateArea(lowerBuffers) + CalculateArea(deckBuffers);
                generatedVolume = CalculateVolume(stations);
                volumeDisplay = generatedVolume.ToString("N1") + " m^3";
                areaDisplay = generatedArea.ToString("N1") + " m^2";

                part.buoyancy = adjustedBuoyancy;

                DisableReferenceColliders();
                GenerateColliders(stations);
                UpdateDeckNodes();
                UpdateCenterOfMass();
                NotifyGeometryChanged(notifyEditor);
            }
            catch (Exception ex)
            {
                Debug.LogError("[SunkWorks] Failed to generate procedural hull on " + part.name + ": " + ex);
            }
            finally
            {
                isRebuilding = false;
            }
        }

        void ConstrainParameters()
        {
            hullLength = Mathf.Max(4f, hullLength);
            beam = Mathf.Max(2f, beam);
            hullDepth = Mathf.Max(0.75f, hullDepth);
            bowLength = Mathf.Clamp(bowLength, 0.5f, hullLength * 0.75f);
            bowRake = Mathf.Clamp(bowRake, 0f, bowLength * 0.8f);
            sternTaperLength = Mathf.Clamp(sternTaperLength, 0f, hullLength * 0.6f);
            float combinedTaperLength = bowLength + sternTaperLength;
            float maximumCombinedTaperLength = hullLength * 0.9f;
            if (combinedTaperLength > maximumCombinedTaperLength)
            {
                float taperScale = maximumCombinedTaperLength / combinedTaperLength;
                bowLength *= taperScale;
                sternTaperLength *= taperScale;
            }
            sternBeamRatio = Mathf.Clamp(sternBeamRatio, 0.25f, 1f);
            sternFullness = Mathf.Clamp01(sternFullness);
            aftRunLength = Mathf.Clamp(aftRunLength, 0f, hullLength * 0.6f);
            aftRise = Mathf.Clamp(aftRise, 0f, hullDepth - kMinimumUpperSide);
            lowerSideHeight = Mathf.Max(0f, lowerSideHeight);

            float maximumRadiusByBeam = Mathf.Max(kMinimumDimension, beam * 0.5f - 0.1f);
            chineRadius = Mathf.Clamp(chineRadius, kMinimumDimension, maximumRadiusByBeam);
            longitudinalSections = Mathf.Clamp(Mathf.Round(longitudinalSections), 8f, 64f);
            chineSegments = Mathf.Clamp(Mathf.Round(chineSegments), 2f, 12f);
            bowFullness = Mathf.Clamp01(bowFullness);
            railingHeight = Mathf.Clamp(railingHeight, 0.1f, 1f);
            railingThickness = Mathf.Clamp(railingThickness, 0.02f, 0.2f);
            textureDensityU = Mathf.Max(1f, textureDensityU);
            textureDensityV = Mathf.Max(1f, textureDensityV);
        }

        List<HullStation> GenerateStations()
        {
            int sectionCount = Mathf.RoundToInt(longitudinalSections);
            List<float> stationParameters = new List<float>(sectionCount + 4);
            for (int index = 0; index <= sectionCount; index++)
            {
                // Cosine spacing supplies substantially more geometry at the bow and
                // stern without wasting the same density through the parallel midbody.
                float angle = Mathf.PI * index / sectionCount;
                float longitudinalT = 0.5f * (1f - Mathf.Cos(angle));
                stationParameters.Add(longitudinalT);
            }

            AddStationParameter(stationParameters, bowLength / hullLength);
            AddStationParameter(stationParameters, 1f - sternTaperLength / hullLength);
            AddStationParameter(stationParameters, 1f - aftRunLength / hullLength);
            stationParameters.Sort();

            List<HullStation> stations = new List<HullStation>(stationParameters.Count + 1);
            for (int index = 0; index < stationParameters.Count; index++)
            {
                float longitudinalT = stationParameters[index];
                float longitudinalPosition = Mathf.Lerp(-hullLength * 0.5f, hullLength * 0.5f, longitudinalT);
                float bowSectionEnd = bowLength / hullLength;
                if (Mathf.Abs(longitudinalT - bowSectionEnd) < 0.0001f)
                {
                    // Two copies of the full-width bow station form the transition.
                    // The forward copy finishes the uniformly raked bow; the second
                    // copy starts the vertical midbody. They coincide at the paint
                    // boundary, making each upper side's connecting quad collapse to
                    // the requested transition triangle. Below that shared edge, the
                    // lower transition strip connects the continued rake to the vertical
                    // midbody without changing either region's station slope.
                    stations.Add(GenerateStation(longitudinalT, longitudinalPosition, true));
                }
                stations.Add(GenerateStation(longitudinalT, longitudinalPosition, false));
            }
            return stations;
        }

        static void AddStationParameter(List<float> stationParameters, float value)
        {
            value = Mathf.Clamp01(value);
            for (int index = 0; index < stationParameters.Count; index++)
            {
                if (Mathf.Abs(stationParameters[index] - value) < 0.0001f)
                    return;
            }
            stationParameters.Add(value);
        }

        HullStation GenerateStation(float longitudinalT, float longitudinalPosition,
            bool forceRakedBowTransition = false)
        {
            float distanceFromBow = longitudinalT * hullLength;
            float bowT = bowLength <= 0f ? 1f : Mathf.Clamp01(distanceFromBow / bowLength);
            float smoothedBow = bowT * bowT * (3f - 2f * bowT);
            float bowExponent = Mathf.Lerp(2.4f, 0.55f, bowFullness);
            float bowWidthScale = Mathf.Pow(smoothedBow, bowExponent);

            float distanceFromStern = (1f - longitudinalT) * hullLength;
            float sternT = sternTaperLength <= 0f ? 1f : Mathf.Clamp01(distanceFromStern / sternTaperLength);
            float smoothedStern = sternT * sternT * (3f - 2f * sternT);
            float sternExponent = Mathf.Lerp(2.1f, 0.55f, sternFullness);
            float sternWidthScale = Mathf.Lerp(sternBeamRatio, 1f, Mathf.Pow(smoothedStern, sternExponent));
            float widthScale = bowWidthScale * sternWidthScale;

            float halfBeam = beam * 0.5f * widthScale;
            float runT = aftRunLength <= 0f ? 0f : Mathf.Clamp01(1f - distanceFromStern / aftRunLength);
            runT = runT * runT * (3f - 2f * runT);
            // The upper edge of the antifouling mesh is one horizontal plane for the
            // entire vessel. Bow closure and aft rise may approach it, but never cross it.
            float paintBoundaryY = GetPaintBoundaryY();
            float desiredBottomY = -hullDepth + aftRise * runT;
            float bottomY = Mathf.Min(desiredBottomY, paintBoundaryY);
            float availableLowerDepth = Mathf.Max(0f, paintBoundaryY - bottomY);

            // Preserve the configured chine/lower-side proportions while limiting their
            // combined height to one third of the current hull depth. The effective radius
            // is reduced further only where the bow or rising aft run cannot contain it.
            float effectiveChineRadius;
            float effectiveLowerSideHeight;
            GetEffectiveLowerHullDimensions(out effectiveChineRadius, out effectiveLowerSideHeight);
            float radius = Mathf.Min(effectiveChineRadius, halfBeam * 0.48f);
            radius = Mathf.Min(radius, availableLowerDepth);

            float flareOffset = Mathf.Tan(sideFlare * Mathf.Deg2Rad) * -paintBoundaryY;
            float gunwaleHalfBeam = halfBeam + flareOffset * widthScale;
            float circleCenterX = Mathf.Max(0f, halfBeam - radius);
            int arcSegments = Mathf.RoundToInt(chineSegments);

            HullStation station = new HullStation();
            station.longitudinalT = longitudinalT;
            bool isRakedBowStation = forceRakedBowTransition || distanceFromBow < bowLength - 0.0001f;
            // Every bow station receives the same affine shear, so its upper side,
            // lower side, and railing all continue along one rake slope. Move that
            // lattice forward by the configured rake at deck level; the displacement
            // reaches zero at the paint boundary and continues linearly below it.
            // Stations at and behind the second boundary copy receive neither operation
            // and therefore remain vertical through their full depth.
            station.longitudinalPosition = isRakedBowStation
                ? longitudinalPosition - bowRake
                : longitudinalPosition;
            station.bowRakeOffset = isRakedBowStation ? bowRake : 0f;
            station.referenceDepth = isRakedBowStation
                ? Mathf.Max(kMinimumDimension, -paintBoundaryY)
                : hullDepth;
            station.isRakedBowTransition = forceRakedBowTransition;
            station.lowerProfile.Add(new Vector2(-halfBeam, paintBoundaryY));
            station.lowerProfile.Add(new Vector2(-halfBeam, bottomY + radius));

            for (int index = 1; index <= arcSegments; index++)
            {
                float angle = Mathf.Lerp(0f, -Mathf.PI * 0.5f, index / (float)arcSegments);
                station.lowerProfile.Add(new Vector2(-(circleCenterX + Mathf.Cos(angle) * radius), bottomY + radius + Mathf.Sin(angle) * radius));
            }

            station.lowerProfile.Add(new Vector2(0f, bottomY));

            for (int index = 0; index <= arcSegments; index++)
            {
                float angle = Mathf.Lerp(-Mathf.PI * 0.5f, 0f, index / (float)arcSegments);
                station.lowerProfile.Add(new Vector2(circleCenterX + Mathf.Cos(angle) * radius, bottomY + radius + Mathf.Sin(angle) * radius));
            }

            station.lowerProfile.Add(new Vector2(halfBeam, paintBoundaryY));
            station.portGunwale = new Vector2(-gunwaleHalfBeam, 0f);
            station.starboardGunwale = new Vector2(gunwaleHalfBeam, 0f);
            station.portPaintBoundary = new Vector2(-halfBeam, paintBoundaryY);
            station.starboardPaintBoundary = new Vector2(halfBeam, paintBoundaryY);
            return station;
        }

        float GetPaintBoundaryY()
        {
            float effectiveChineRadius;
            float effectiveLowerSideHeight;
            GetEffectiveLowerHullDimensions(out effectiveChineRadius, out effectiveLowerSideHeight);
            return Mathf.Min(-kMinimumUpperSide, -hullDepth + effectiveChineRadius + effectiveLowerSideHeight);
        }

        void GetEffectiveLowerHullDimensions(out float effectiveChineRadius, out float effectiveLowerSideHeight)
        {
            float requestedLowerHullDepth = chineRadius + lowerSideHeight;
            float maximumLowerHullDepth = hullDepth * kMaximumLowerHullDepthRatio;
            float depthScale = requestedLowerHullDepth > maximumLowerHullDepth
                ? maximumLowerHullDepth / requestedLowerHullDepth
                : 1f;

            effectiveChineRadius = chineRadius * depthScale;
            effectiveLowerSideHeight = lowerSideHeight * depthScale;
        }

        MeshBuffers BuildLowerHull(List<HullStation> stations, TextureTiling tiling)
        {
            MeshBuffers buffers = new MeshBuffers();
            List<HullStation> lowerStations = BuildLowerHullStations(stations);
            int profileCount = lowerStations[0].lowerProfile.Count;
            for (int stationIndex = 0; stationIndex < lowerStations.Count; stationIndex++)
            {
                HullStation station = lowerStations[stationIndex];
                float[] surfaceDistances = GetSignedProfileSurfaceDistances(station.lowerProfile);
                for (int pointIndex = 0; pointIndex < profileCount; pointIndex++)
                {
                    Vector2 point = station.lowerProfile[pointIndex];
                    buffers.vertices.Add(ToPartLocal(point.x, point.y,
                        station.GetLongitudinalPosition(point.y)));
                    buffers.uv.Add(GetSideUV(station, surfaceDistances[pointIndex], tiling));
                }
            }

            AddLowerHullLoftTriangles(buffers.triangles, lowerStations, profileCount);
            AddProfileCap(buffers, lowerStations[0], true, tiling);
            AddProfileCap(buffers, lowerStations[lowerStations.Count - 1], false, tiling);
            return buffers;
        }

        List<HullStation> BuildLowerHullStations(List<HullStation> stations)
        {
            List<HullStation> lowerStations = new List<HullStation>(stations.Count);
            float transitionEnd = float.NegativeInfinity;
            for (int index = 0; index < stations.Count; index++)
            {
                HullStation station = stations[index];
                if (station.isRakedBowTransition)
                {
                    lowerStations.Add(station);

                    // Keep the raked row on the exact same paint-boundary curve as
                    // the upper hull. End the lower transition where that row reaches
                    // the keel, then resume with a vertical full-width station there.
                    // Midbody rows inside this interval are omitted so no surfaces
                    // overlap and z-fight beneath the transition triangle.
                    int bottomPointIndex = (station.lowerProfile.Count - 1) / 2;
                    float bottomY = station.lowerProfile[bottomPointIndex].y;
                    transitionEnd = station.GetLongitudinalPosition(bottomY);
                    float transitionT = Mathf.Clamp01(transitionEnd / hullLength + 0.5f);
                    lowerStations.Add(GenerateStation(transitionT, transitionEnd, false));
                    continue;
                }

                float physicalPosition = station.GetLongitudinalPosition(0f);
                if (!float.IsNegativeInfinity(transitionEnd) &&
                    physicalPosition <= transitionEnd + 0.0001f)
                    continue;

                lowerStations.Add(station);
            }
            return lowerStations;
        }

        void AddLowerHullLoftTriangles(List<int> triangles, List<HullStation> stations,
            int profileCount)
        {
            int bottomPointIndex = (profileCount - 1) / 2;
            for (int stationIndex = 0; stationIndex < stations.Count - 1; stationIndex++)
            {
                HullStation currentStation = stations[stationIndex];
                HullStation nextStation = stations[stationIndex + 1];
                float currentBottom = currentStation.GetLongitudinalPosition(
                    currentStation.lowerProfile[bottomPointIndex].y);
                float nextBottom = nextStation.GetLongitudinalPosition(
                    nextStation.lowerProfile[bottomPointIndex].y);

                // Preserve outward winding if an extreme parameter combination makes
                // a neighboring pair reverse longitudinal order at the keel.
                bool reverse = nextBottom < currentBottom;
                int currentOffset = stationIndex * profileCount;
                int nextOffset = currentOffset + profileCount;
                for (int pointIndex = 0; pointIndex < profileCount - 1; pointIndex++)
                {
                    AddQuad(triangles,
                        currentOffset + pointIndex,
                        currentOffset + pointIndex + 1,
                        nextOffset + pointIndex,
                        nextOffset + pointIndex + 1,
                        reverse);
                }
            }
        }

        MeshBuffers BuildUpperHull(List<HullStation> stations, TextureTiling tiling)
        {
            MeshBuffers buffers = new MeshBuffers();
            for (int index = 0; index < stations.Count; index++)
            {
                HullStation station = stations[index];
                Vector3 portGunwale = ToPartLocal(station.portGunwale.x, station.portGunwale.y,
                    station.GetLongitudinalPosition(station.portGunwale.y));
                Vector3 portBoundary = ToPartLocal(station.portPaintBoundary.x, station.portPaintBoundary.y,
                    station.GetLongitudinalPosition(station.portPaintBoundary.y));
                Vector3 starboardBoundary = ToPartLocal(station.starboardPaintBoundary.x, station.starboardPaintBoundary.y,
                    station.GetLongitudinalPosition(station.starboardPaintBoundary.y));
                Vector3 starboardGunwale = ToPartLocal(station.starboardGunwale.x, station.starboardGunwale.y,
                    station.GetLongitudinalPosition(station.starboardGunwale.y));
                buffers.vertices.Add(portGunwale);
                buffers.vertices.Add(portBoundary);
                buffers.vertices.Add(starboardBoundary);
                buffers.vertices.Add(starboardGunwale);

                // Map the side from the unraked station lattice rather than from
                // the final vertex positions. The rake changes the mesh's physical
                // longitude with height, but must not rotate the texture plane.
                // Consequently, every station is a vertical UV column and every
                // height is a horizontal UV row at the bow, amidships, and stern.
                float uvLongitudinal = station.longitudinalPosition;
                buffers.uv.Add(GetRectangularSideUV(uvLongitudinal, station.portGunwale.y, tiling));
                buffers.uv.Add(GetRectangularSideUV(uvLongitudinal, station.portPaintBoundary.y, tiling));
                buffers.uv.Add(GetRectangularSideUV(uvLongitudinal, station.starboardPaintBoundary.y, tiling));
                buffers.uv.Add(GetRectangularSideUV(uvLongitudinal, station.starboardGunwale.y, tiling));
            }

            for (int stationIndex = 0; stationIndex < stations.Count - 1; stationIndex++)
            {
                int current = stationIndex * 4;
                int next = current + 4;
                AddQuad(buffers.triangles, current, current + 1, next, next + 1, false);
                AddQuad(buffers.triangles, current + 2, current + 3, next + 2, next + 3, false);
            }

            AddUpperTransom(buffers, stations[0], true, tiling);
            AddUpperTransom(buffers, stations[stations.Count - 1], false, tiling);
            return buffers;
        }

        MeshBuffers BuildDeck(List<HullStation> stations, TextureTiling tiling)
        {
            MeshBuffers buffers = new MeshBuffers();
            for (int index = 0; index < stations.Count; index++)
            {
                HullStation station = stations[index];
                float deckLongitudinalPosition = station.GetLongitudinalPosition(0f);
                buffers.vertices.Add(ToPartLocal(station.portGunwale.x, 0f, deckLongitudinalPosition));
                buffers.vertices.Add(ToPartLocal(0f, 0f, deckLongitudinalPosition));
                buffers.vertices.Add(ToPartLocal(station.starboardGunwale.x, 0f, deckLongitudinalPosition));

                // Project the entire deck onto one rectangular longitudinal/beam
                // plane. Narrow bow and stern stations therefore sample only their
                // actual portion of the texture instead of stretching 0..1 across
                // every station and producing a chevron-shaped grain distortion.
                float longitudinalUV = (deckLongitudinalPosition + hullLength * 0.5f) * tiling.uPerMeter;
                buffers.uv.Add(new Vector2(longitudinalUV, station.portGunwale.x * tiling.vPerMeter));
                buffers.uv.Add(new Vector2(longitudinalUV, 0f));
                buffers.uv.Add(new Vector2(longitudinalUV, station.starboardGunwale.x * tiling.vPerMeter));
            }

            AddLoftTriangles(buffers.triangles, stations.Count, 3, true);
            // The solid stern railing occupies the deck edge and hides the joint.
            // Emitting the deck-material skirt there would leave a visible strip
            // between the railing and upper transom.
            if (!railingsEnabled || !sternRailings)
                AddDeckTransomSkirt(buffers, stations[stations.Count - 1], tiling);
            return buffers;
        }

        void AddDeckTransomSkirt(MeshBuffers buffers, HullStation stern, TextureTiling tiling)
        {
            // The deck is otherwise a zero-thickness plane. A short overlapping aft
            // fascia prevents a visible rasterization seam between it and the upper
            // transom when the two materials are viewed from near deck level.
            const float skirtDepth = 0.02f;
            int first = buffers.vertices.Count;
            Vector2[] points =
            {
                new Vector2(stern.portGunwale.x, -skirtDepth), stern.portGunwale,
                stern.starboardGunwale, new Vector2(stern.starboardGunwale.x, -skirtDepth)
            };
            for (int index = 0; index < points.Length; index++)
            {
                Vector2 point = points[index];
                buffers.vertices.Add(ToPartLocal(point.x, point.y, stern.GetLongitudinalPosition(point.y)));
                buffers.uv.Add(new Vector2(point.x * tiling.uPerMeter, point.y * tiling.vPerMeter));
            }
            buffers.triangles.Add(first); buffers.triangles.Add(first + 2); buffers.triangles.Add(first + 1);
            buffers.triangles.Add(first); buffers.triangles.Add(first + 3); buffers.triangles.Add(first + 2);
        }

        MeshBuffers BuildRailings(List<HullStation> stations, TextureTiling tiling)
        {
            MeshBuffers buffers = new MeshBuffers();
            if (!railingsEnabled || stations.Count < 2)
                return buffers;

            RailingPerimeter perimeter = BuildRailingPerimeter(stations);
            AddExtrudedRailingFaces(buffers, perimeter, tiling);
            return buffers;
        }

        RailingPerimeter BuildRailingPerimeter(List<HullStation> stations)
        {
            RailingPerimeter perimeter = new RailingPerimeter();
            HullStation bow = stations[0];
            perimeter.points.Add(CreateRailingPerimeterPoint(bow, 0f, RailingEdgeSide.Starboard));

            for (int index = 1; index < stations.Count; index++)
            {
                HullStation station = stations[index];
                RailingEdgeSide outgoingSide = index == stations.Count - 1
                    ? RailingEdgeSide.Stern
                    : RailingEdgeSide.Starboard;
                perimeter.points.Add(CreateRailingPerimeterPoint(station, station.starboardGunwale.x, outgoingSide));
            }
            for (int index = stations.Count - 1; index >= 1; index--)
            {
                HullStation station = stations[index];
                perimeter.points.Add(CreateRailingPerimeterPoint(station, station.portGunwale.x, RailingEdgeSide.Port));
            }

            for (int index = 0; index < perimeter.points.Count; index++)
            {
                int previous = (index - 1 + perimeter.points.Count) % perimeter.points.Count;
                int next = (index + 1) % perimeter.points.Count;
                perimeter.points[index].inner = CalculateInsetVertex(
                    perimeter.points[previous].outer,
                    perimeter.points[index].outer,
                    perimeter.points[next].outer,
                    railingThickness);

                // Near the stem, cosine-spaced deck stations can be narrower than
                // the requested inset. A conventional polygon inset then crosses
                // the center plane and overlaps the opposite railing half. Keep
                // each inner vertex on its own side; when the available half-beam
                // is exhausted, it terminates exactly on the straight center seam.
                float outerWidth = perimeter.points[index].outer.x;
                Vector2 inner = perimeter.points[index].inner;
                if (outerWidth > 0f)
                    inner.x = Mathf.Clamp(inner.x, 0f, outerWidth);
                else if (outerWidth < 0f)
                    inner.x = Mathf.Clamp(inner.x, outerWidth, 0f);
                else
                    inner.x = 0f;
                perimeter.points[index].inner = inner;
            }

            // The perimeter is symmetric and begins at the stem. Derive the
            // canonical inner apex from the first two usable starboard inset
            // points. The acute-corner miter limit can otherwise put the apex aft
            // of its neighboring point and fold the cap into a small M shape.
            Vector2 bowInner = perimeter.points[0].inner;
            bowInner.x = 0f;
            int firstInsetIndex = -1;
            int secondInsetIndex = -1;
            int starboardEnd = (perimeter.points.Count + 1) / 2;
            for (int index = 1; index < starboardEnd; index++)
            {
                if (perimeter.points[index].inner.x <= 0.00001f)
                    continue;
                if (firstInsetIndex < 0)
                    firstInsetIndex = index;
                else
                {
                    secondInsetIndex = index;
                    break;
                }
            }
            if (firstInsetIndex >= 0)
            {
                Vector2 firstInset = perimeter.points[firstInsetIndex].inner;
                float apexLongitudinal = Mathf.Min(bowInner.y, firstInset.y);
                if (secondInsetIndex >= 0)
                {
                    Vector2 secondInset = perimeter.points[secondInsetIndex].inner;
                    float widthDelta = secondInset.x - firstInset.x;
                    if (Mathf.Abs(widthDelta) > 0.00001f)
                    {
                        float lineT = -firstInset.x / widthDelta;
                        apexLongitudinal = firstInset.y + (secondInset.y - firstInset.y) * lineT;
                    }
                }
                bowInner.y = Mathf.Clamp(apexLongitudinal,
                    perimeter.points[0].outer.y, firstInset.y);
            }
            perimeter.points[0].inner = bowInner;

            return perimeter;
        }

        RailingPerimeterPoint CreateRailingPerimeterPoint(HullStation station, float width, RailingEdgeSide outgoingSide)
        {
            return new RailingPerimeterPoint
            {
                outer = new Vector2(width, station.GetLongitudinalPosition(0f)),
                longitudinalT = station.longitudinalT,
                bowRakeOffset = station.bowRakeOffset,
                referenceDepth = station.referenceDepth,
                outgoingSide = outgoingSide
            };
        }

        static Vector2 CalculateInsetVertex(Vector2 previous, Vector2 current, Vector2 next, float inset)
        {
            Vector2 previousDirection = (current - previous).normalized;
            Vector2 nextDirection = (next - current).normalized;
            Vector2 previousInward = new Vector2(-previousDirection.y, previousDirection.x);
            Vector2 nextInward = new Vector2(-nextDirection.y, nextDirection.x);
            Vector2 previousLine = current + previousInward * inset;
            Vector2 nextLine = current + nextInward * inset;
            float denominator = Cross2D(previousDirection, nextDirection);

            Vector2 insetVertex;
            if (Mathf.Abs(denominator) < 0.00001f)
            {
                Vector2 averageInward = previousInward + nextInward;
                insetVertex = current + (averageInward.sqrMagnitude > 0f ? averageInward.normalized : previousInward) * inset;
            }
            else
            {
                float distanceAlongPrevious = Cross2D(nextLine - previousLine, nextDirection) / denominator;
                insetVertex = previousLine + previousDirection * distanceAlongPrevious;
            }

            // Prevent an extremely acute corner from producing a long miter spike.
            Vector2 offset = insetVertex - current;
            float maximumMiter = inset * 6f;
            if (offset.magnitude > maximumMiter)
                insetVertex = current + offset.normalized * maximumMiter;
            return insetVertex;
        }

        static float Cross2D(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }

        void AddExtrudedRailingFaces(MeshBuffers buffers, RailingPerimeter perimeter, TextureTiling tiling)
        {
            Vector3 deckUp = downAxis.sqrMagnitude > 0f ? -downAxis.normalized : Vector3.back;
            int pointCount = perimeter.points.Count;
            bool[] enabledEdges = new bool[pointCount];
            for (int index = 0; index < pointCount; index++)
                enabledEdges[index] = IsRailingEdgeEnabled(perimeter.points[index], perimeter.points[(index + 1) % pointCount]);

            // The curved port, starboard, and bow walls share vertices so Unity's
            // recalculated normals form one smoothing group along the hull outline.
            // The transom is deliberately emitted separately to retain its hard
            // corners where it meets the side walls.
            int[] outerBottom = new int[pointCount];
            int[] outerTop = new int[pointCount];
            int[] innerBottom = new int[pointCount];
            int[] innerTop = new int[pointCount];
            int[] topOuter = new int[pointCount];
            int[] topInner = new int[pointCount];
            int centerlineInnerBottom = -1;
            int centerlineInnerTop = -1;
            int centerlineTopInner = -1;
            for (int index = 0; index < pointCount; index++)
            {
                RailingPerimeterPoint point = perimeter.points[index];
                Vector3 outerNormal = GetSmoothRailingWallNormal(perimeter, index, false);
                Vector3 innerNormal = GetSmoothRailingWallNormal(perimeter, index, true);
                Vector3 outerBottomVertex = GetRailingPerimeterVertex(point.outer, point, 0f);
                Vector3 outerTopVertex = GetRailingPerimeterVertex(point.outer, point, railingHeight);
                Vector2 outerBottomUV = GetRailingWallUV(outerBottomVertex, 0f, tiling);
                Vector2 outerTopUV = GetRailingWallUV(outerTopVertex, railingHeight, tiling);
                outerBottom[index] = AddRailingVertex(buffers, outerBottomVertex, outerNormal,
                    outerBottomUV.x, outerBottomUV.y);
                outerTop[index] = AddRailingVertex(buffers, outerTopVertex, outerNormal,
                    outerTopUV.x, outerTopUV.y);
                bool innerOnCenterline = Mathf.Abs(point.inner.x) < 0.00001f;
                if (innerOnCenterline && centerlineInnerBottom >= 0)
                {
                    innerBottom[index] = centerlineInnerBottom;
                    innerTop[index] = centerlineInnerTop;
                }
                else
                {
                    Vector3 innerBottomVertex = GetRailingInnerPerimeterVertex(perimeter, index, 0f);
                    Vector3 innerTopVertex = GetRailingInnerPerimeterVertex(perimeter, index, railingHeight);
                    Vector2 innerBottomUV = GetRailingWallUV(innerBottomVertex, 0f, tiling);
                    Vector2 innerTopUV = GetRailingWallUV(innerTopVertex, railingHeight, tiling);
                    innerBottom[index] = AddRailingVertex(buffers,
                        innerBottomVertex, innerNormal, innerBottomUV.x, innerBottomUV.y);
                    innerTop[index] = AddRailingVertex(buffers,
                        innerTopVertex, innerNormal, innerTopUV.x, innerTopUV.y);
                    if (innerOnCenterline)
                    {
                        centerlineInnerBottom = innerBottom[index];
                        centerlineInnerTop = innerTop[index];
                    }
                }
                // Keep a separate hard-edged smoothing group for the horizontal
                // cap, but share its vertices around the perimeter. In particular,
                // both bow halves now terminate at the exact same outer and inner
                // centerline vertices, producing a single clean miter.
                Vector2 topOuterUV = GetRailingTopUV(outerTopVertex, tiling);
                topOuter[index] = AddRailingVertex(buffers, outerTopVertex, deckUp,
                    topOuterUV.x, topOuterUV.y);

                if (innerOnCenterline && centerlineTopInner >= 0)
                {
                    topInner[index] = centerlineTopInner;
                }
                else
                {
                    Vector3 topInnerVertex = GetRailingInnerPerimeterVertex(perimeter, index, railingHeight);
                    Vector2 topInnerUV = GetRailingTopUV(topInnerVertex, tiling);
                    topInner[index] = AddRailingVertex(buffers,
                        topInnerVertex, deckUp, topInnerUV.x, topInnerUV.y);
                    if (innerOnCenterline)
                        centerlineTopInner = topInner[index];
                }
            }

            // The stem is a hard crease. Give the port wall its own copies of the
            // bow vertices so its normals do not get averaged with starboard.
            RailingPerimeterPoint bowPoint = perimeter.points[0];
            Vector3 bowPortNormal = GetBowPortRailingWallNormal(perimeter, false);
            Vector3 outerBottomBowPortVertex = GetRailingPerimeterVertex(bowPoint.outer, bowPoint, 0f);
            Vector3 outerTopBowPortVertex = GetRailingPerimeterVertex(bowPoint.outer, bowPoint, railingHeight);
            Vector2 outerBottomBowPortUV = GetRailingWallUV(outerBottomBowPortVertex, 0f, tiling);
            Vector2 outerTopBowPortUV = GetRailingWallUV(outerTopBowPortVertex, railingHeight, tiling);
            int outerBottomBowPort = AddRailingVertex(buffers,
                outerBottomBowPortVertex, bowPortNormal, outerBottomBowPortUV.x, outerBottomBowPortUV.y);
            int outerTopBowPort = AddRailingVertex(buffers,
                outerTopBowPortVertex, bowPortNormal, outerTopBowPortUV.x, outerTopBowPortUV.y);

            for (int index = 0; index < pointCount; index++)
            {
                if (!enabledEdges[index])
                    continue;

                int nextIndex = (index + 1) % pointCount;
                RailingPerimeterPoint start = perimeter.points[index];
                RailingPerimeterPoint end = perimeter.points[nextIndex];
                Vector3 outerBottomStart = GetRailingPerimeterVertex(start.outer, start, 0f);
                Vector3 outerBottomEnd = GetRailingPerimeterVertex(end.outer, end, 0f);
                Vector3 innerBottomStart = GetRailingInnerPerimeterVertex(perimeter, index, 0f);
                Vector3 innerBottomEnd = GetRailingInnerPerimeterVertex(perimeter, nextIndex, 0f);
                Vector3 outerTopStart = GetRailingPerimeterVertex(start.outer, start, railingHeight);
                Vector3 outerTopEnd = GetRailingPerimeterVertex(end.outer, end, railingHeight);
                Vector3 innerTopStart = GetRailingInnerPerimeterVertex(perimeter, index, railingHeight);
                Vector3 innerTopEnd = GetRailingInnerPerimeterVertex(perimeter, nextIndex, railingHeight);

                Vector3 inward = ((innerBottomStart - outerBottomStart) +
                    (innerBottomEnd - outerBottomEnd)).normalized;
                Vector3 edgeDirection = (outerBottomEnd - outerBottomStart).normalized;
                if (start.outgoingSide == RailingEdgeSide.Stern)
                {
                    AddRailingFace(buffers, outerBottomStart, outerBottomEnd, outerTopEnd, outerTopStart, -inward,
                        GetRailingTransomUV(outerBottomStart, tiling), GetRailingTransomUV(outerBottomEnd, tiling),
                        GetRailingTransomUV(outerTopEnd, tiling), GetRailingTransomUV(outerTopStart, tiling));
                    AddRailingFace(buffers, innerBottomStart, innerTopStart, innerTopEnd, innerBottomEnd, inward,
                        GetRailingTransomUV(innerBottomStart, tiling), GetRailingTransomUV(innerTopStart, tiling),
                        GetRailingTransomUV(innerTopEnd, tiling), GetRailingTransomUV(innerBottomEnd, tiling));
                }
                else
                {
                    int edgeOuterBottomEnd = nextIndex == 0 && start.outgoingSide == RailingEdgeSide.Port
                        ? outerBottomBowPort
                        : outerBottom[nextIndex];
                    int edgeOuterTopEnd = nextIndex == 0 && start.outgoingSide == RailingEdgeSide.Port
                        ? outerTopBowPort
                        : outerTop[nextIndex];
                    AddRailingIndexedQuad(buffers,
                        outerBottom[index], edgeOuterBottomEnd, edgeOuterTopEnd, outerTop[index],
                        outerBottomStart, outerBottomEnd, outerTopEnd, -inward);

                    // Centerline inner faces from the two bow halves would occupy
                    // the same plane with opposite winding. They are internal to
                    // the solid rim, so omit them to avoid z-fighting and bad normal
                    // averaging at the welded seam.
                    bool centerlineInnerEdge = Mathf.Abs(start.inner.x) < 0.00001f &&
                        Mathf.Abs(end.inner.x) < 0.00001f;
                    if (!centerlineInnerEdge)
                    {
                        AddRailingIndexedQuad(buffers,
                            innerBottom[index], innerTop[index], innerTop[nextIndex], innerBottom[nextIndex],
                            innerBottomStart, innerTopStart, innerTopEnd, inward);
                    }
                }
                AddRailingIndexedQuad(buffers,
                    topOuter[index], topOuter[nextIndex], topInner[nextIndex], topInner[index],
                    outerTopStart, outerTopEnd, innerTopEnd, deckUp);

                // Do not add a bottom face: it would be coplanar with the deck and
                // cause z-fighting. The deck itself closes the railing from below.

                int previousEdge = (index - 1 + pointCount) % pointCount;
                if (!enabledEdges[previousEdge])
                    AddRailingFace(buffers, outerBottomStart, outerTopStart, innerTopStart, innerBottomStart, -edgeDirection,
                        GetRailingTransomUV(outerBottomStart, tiling), GetRailingTransomUV(outerTopStart, tiling),
                        GetRailingTransomUV(innerTopStart, tiling), GetRailingTransomUV(innerBottomStart, tiling));
                if (!enabledEdges[nextIndex])
                    AddRailingFace(buffers, outerBottomEnd, innerBottomEnd, innerTopEnd, outerTopEnd, edgeDirection,
                        GetRailingTransomUV(outerBottomEnd, tiling), GetRailingTransomUV(innerBottomEnd, tiling),
                        GetRailingTransomUV(innerTopEnd, tiling), GetRailingTransomUV(outerTopEnd, tiling));
            }
        }

        Vector3 GetSmoothRailingWallNormal(RailingPerimeter perimeter, int index, bool innerSurface)
        {
            int pointCount = perimeter.points.Count;
            int previousIndex = (index - 1 + pointCount) % pointCount;
            int nextIndex = (index + 1) % pointCount;
            RailingPerimeterPoint previous = perimeter.points[previousIndex];
            RailingPerimeterPoint current = perimeter.points[index];
            RailingPerimeterPoint next = perimeter.points[nextIndex];
            Vector3 previousBottom = innerSurface
                ? GetRailingInnerPerimeterVertex(perimeter, previousIndex, 0f)
                : GetRailingPerimeterVertex(previous.outer, previous, 0f);
            Vector3 nextBottom = innerSurface
                ? GetRailingInnerPerimeterVertex(perimeter, nextIndex, 0f)
                : GetRailingPerimeterVertex(next.outer, next, 0f);
            Vector3 currentBottom = innerSurface
                ? GetRailingInnerPerimeterVertex(perimeter, index, 0f)
                : GetRailingPerimeterVertex(current.outer, current, 0f);
            Vector3 currentTop = innerSurface
                ? GetRailingInnerPerimeterVertex(perimeter, index, railingHeight)
                : GetRailingPerimeterVertex(current.outer, current, railingHeight);
            Vector3 tangent;
            if (index == 0)
                tangent = nextBottom - currentBottom;
            else if (current.outgoingSide == RailingEdgeSide.Stern)
                tangent = currentBottom - previousBottom;
            else if (previous.outgoingSide == RailingEdgeSide.Stern)
                tangent = nextBottom - currentBottom;
            else
                tangent = nextBottom - previousBottom;
            Vector3 verticalEdge = currentTop - currentBottom;
            Vector3 normal = Vector3.Cross(tangent, verticalEdge).normalized;

            Vector3 outerBottom = GetRailingPerimeterVertex(current.outer, current, 0f);
            Vector3 innerBottom = GetRailingInnerPerimeterVertex(perimeter, index, 0f);
            Vector3 desired = innerSurface ? innerBottom - outerBottom : outerBottom - innerBottom;
            if (desired.sqrMagnitude < 0.000001f)
                desired = normal;
            if (normal.sqrMagnitude < 0.000001f)
                normal = desired.normalized;
            else if (Vector3.Dot(normal, desired) < 0f)
                normal = -normal;
            return normal.normalized;
        }

        Vector3 GetBowPortRailingWallNormal(RailingPerimeter perimeter, bool innerSurface)
        {
            int bowIndex = 0;
            int portIndex = perimeter.points.Count - 1;
            RailingPerimeterPoint bow = perimeter.points[bowIndex];
            RailingPerimeterPoint port = perimeter.points[portIndex];
            Vector3 currentBottom = innerSurface
                ? GetRailingInnerPerimeterVertex(perimeter, bowIndex, 0f)
                : GetRailingPerimeterVertex(bow.outer, bow, 0f);
            Vector3 currentTop = innerSurface
                ? GetRailingInnerPerimeterVertex(perimeter, bowIndex, railingHeight)
                : GetRailingPerimeterVertex(bow.outer, bow, railingHeight);
            Vector3 previousBottom = innerSurface
                ? GetRailingInnerPerimeterVertex(perimeter, portIndex, 0f)
                : GetRailingPerimeterVertex(port.outer, port, 0f);
            Vector3 outerBottom = GetRailingPerimeterVertex(bow.outer, bow, 0f);
            Vector3 innerBottom = GetRailingInnerPerimeterVertex(perimeter, bowIndex, 0f);
            Vector3 desired = innerSurface ? innerBottom - outerBottom : outerBottom - innerBottom;
            Vector3 normal = Vector3.Cross(currentBottom - previousBottom, currentTop - currentBottom).normalized;
            if (normal.sqrMagnitude < 0.000001f)
                normal = desired.normalized;
            else if (Vector3.Dot(normal, desired) < 0f)
                normal = -normal;
            return normal.normalized;
        }

        static int AddRailingVertex(MeshBuffers buffers, Vector3 vertex, Vector3 normal, float u, float v)
        {
            int index = buffers.vertices.Count;
            buffers.vertices.Add(vertex);
            buffers.normals.Add(normal.normalized);
            buffers.uv.Add(new Vector2(u, v));
            return index;
        }

        static void AddRailingIndexedQuad(MeshBuffers buffers, int a, int b, int c, int d,
            Vector3 positionA, Vector3 positionB, Vector3 positionC, Vector3 desiredNormal)
        {
            bool reverse = Vector3.Dot(Vector3.Cross(positionB - positionA, positionC - positionA), desiredNormal) < 0f;
            if (!reverse)
            {
                buffers.triangles.Add(a); buffers.triangles.Add(b); buffers.triangles.Add(c);
                buffers.triangles.Add(a); buffers.triangles.Add(c); buffers.triangles.Add(d);
            }
            else
            {
                buffers.triangles.Add(a); buffers.triangles.Add(c); buffers.triangles.Add(b);
                buffers.triangles.Add(a); buffers.triangles.Add(d); buffers.triangles.Add(c);
            }
        }

        bool IsRailingEdgeEnabled(RailingPerimeterPoint start, RailingPerimeterPoint end)
        {
            if (start.outgoingSide == RailingEdgeSide.Stern)
                return sternRailings;
            float bowSectionEnd = Mathf.Clamp01(bowLength / hullLength);
            if ((start.longitudinalT + end.longitudinalT) * 0.5f < bowSectionEnd)
                return bowRailings;
            bool profileEdgeIsPort = start.outgoingSide == RailingEdgeSide.Port;
            Vector3 widthDirection = widthAxis.sqrMagnitude > 0f ? widthAxis.normalized : Vector3.right;
            Vector3 lengthDirection = lengthAxis.sqrMagnitude > 0f ? lengthAxis.normalized : Vector3.up;
            Vector3 downDirection = downAxis.sqrMagnitude > 0f ? downAxis.normalized : Vector3.forward;
            Vector3 physicalStarboard = Vector3.Cross(downDirection, lengthDirection).normalized;

            // The profile is authored with positive width on its starboard side,
            // but a configured negative width axis mirrors that profile in model
            // space. Account for the axis handedness so the PAW labels always refer
            // to the physical port and starboard sides seen by the player.
            bool positiveProfileWidthIsStarboard = Vector3.Dot(widthDirection, physicalStarboard) >= 0f;
            bool physicalEdgeIsPort = positiveProfileWidthIsStarboard
                ? profileEdgeIsPort
                : !profileEdgeIsPort;
            return physicalEdgeIsPort ? portRailings : starboardRailings;
        }

        Vector3 GetRailingPerimeterVertex(Vector2 planPosition, RailingPerimeterPoint point, float height)
        {
            // Continue the side-wall line upward instead of introducing a new,
            // shallower railing angle. Vertical midbody points have zero rake offset.
            float heightFraction = point.referenceDepth <= 0f ? 0f : height / point.referenceDepth;
            float longitudinal = planPosition.y - point.bowRakeOffset * heightFraction;
            return ToPartLocal(planPosition.x, height, longitudinal);
        }

        Vector3 GetRailingInnerPerimeterVertex(RailingPerimeter perimeter, int index, float height)
        {
            RailingPerimeterPoint point = perimeter.points[index];
			if (Mathf.Abs(point.inner.x) >= 0.00001f)
                return GetRailingPerimeterVertex(point.inner, point, height);

            // Once an inset bow station reaches the centerline, it is no longer a
            // separate port or starboard corner. Collapse every such station onto
            // the bow's single inner apex, including its rake metadata. Reusing
            // only x = 0 would leave different longitudinal/rake positions and
            // open a narrow V-shaped slit as the railing is extruded upward.
            RailingPerimeterPoint bow = perimeter.points[0];
            Vector2 innerApex = bow.inner;
            innerApex.x = 0f;
            return GetRailingPerimeterVertex(innerApex, bow, height);
        }

        Vector2 GetRailingWallUV(Vector3 vertex, float height,
            TextureTiling tiling)
        {
            // Project every railing-wall vertex onto one continuous side-view UV
            // plane. Both triangles of the raked-to-vertical transition therefore
            // agree along their shared diagonal instead of bending a texture line.
            Vector3 offset = vertex - ToPartLocal(0f, 0f, 0f);
            Vector3 lengthDirection = lengthAxis.sqrMagnitude > 0f
                ? lengthAxis.normalized
                : Vector3.up;
            float longitudinal = Vector3.Dot(offset, lengthDirection);
            return GetRectangularSideUV(longitudinal, height, tiling);
        }

        Vector2 GetRectangularSideUV(float longitudinalPosition, float verticalPosition,
            TextureTiling tiling)
        {
            // This is the flattened side-view plane. It intentionally accepts the
            // procedural coordinates that existed before rake or other geometric
            // displacement was applied; deriving these values from a final Vector3
            // would reintroduce diagonal texture rows on inclined faces.
            float longitudinal = longitudinalPosition + hullLength * 0.5f;
            float vertical = verticalPosition + hullDepth;
            return new Vector2(longitudinal * tiling.uPerMeter, vertical * tiling.vPerMeter);
        }

        Vector2 GetRailingTransomUV(Vector3 vertex, TextureTiling tiling)
        {
            Vector3 offset = vertex - ToPartLocal(0f, 0f, 0f);
            Vector3 widthDirection = widthAxis.sqrMagnitude > 0f ? widthAxis.normalized : Vector3.right;
            Vector3 upDirection = downAxis.sqrMagnitude > 0f ? -downAxis.normalized : Vector3.back;
            float transverse = Vector3.Dot(offset, widthDirection) + beam * 0.5f;
            float height = Vector3.Dot(offset, upDirection);
            return new Vector2(transverse * tiling.uPerMeter, height * tiling.vPerMeter);
        }

        Vector2 GetRailingTopUV(Vector3 vertex, TextureTiling tiling)
        {
            Vector3 offset = vertex - ToPartLocal(0f, 0f, 0f);
            Vector3 widthDirection = widthAxis.sqrMagnitude > 0f ? widthAxis.normalized : Vector3.right;
            Vector3 lengthDirection = lengthAxis.sqrMagnitude > 0f ? lengthAxis.normalized : Vector3.up;
            float longitudinal = Vector3.Dot(offset, lengthDirection) + hullLength * 0.5f;
            float transverse = Vector3.Dot(offset, widthDirection) + beam * 0.5f;
            return new Vector2(longitudinal * tiling.uPerMeter, transverse * tiling.vPerMeter);
        }

        static void AddRailingFace(MeshBuffers buffers, Vector3 a, Vector3 b, Vector3 c, Vector3 d,
            Vector3 desiredNormal, Vector2 uvA, Vector2 uvB, Vector2 uvC, Vector2 uvD)
        {
            int first = buffers.vertices.Count;
            buffers.vertices.Add(a);
            buffers.vertices.Add(b);
            buffers.vertices.Add(c);
            buffers.vertices.Add(d);
            Vector3 faceNormal = desiredNormal.normalized;
            buffers.normals.Add(faceNormal);
            buffers.normals.Add(faceNormal);
            buffers.normals.Add(faceNormal);
            buffers.normals.Add(faceNormal);
            buffers.uv.Add(uvA);
            buffers.uv.Add(uvB);
            buffers.uv.Add(uvC);
            buffers.uv.Add(uvD);

            bool reverse = Vector3.Dot(Vector3.Cross(b - a, c - a), desiredNormal) < 0f;
            if (!reverse)
            {
                buffers.triangles.Add(first); buffers.triangles.Add(first + 1); buffers.triangles.Add(first + 2);
                buffers.triangles.Add(first); buffers.triangles.Add(first + 2); buffers.triangles.Add(first + 3);
            }
            else
            {
                buffers.triangles.Add(first); buffers.triangles.Add(first + 2); buffers.triangles.Add(first + 1);
                buffers.triangles.Add(first); buffers.triangles.Add(first + 3); buffers.triangles.Add(first + 2);
            }
        }

        static void AddLoftTriangles(List<int> triangles, int stationCount, int profileCount, bool reverse)
        {
            for (int stationIndex = 0; stationIndex < stationCount - 1; stationIndex++)
            {
                int currentOffset = stationIndex * profileCount;
                int nextOffset = currentOffset + profileCount;
                for (int pointIndex = 0; pointIndex < profileCount - 1; pointIndex++)
                    AddQuad(triangles, currentOffset + pointIndex, currentOffset + pointIndex + 1,
                        nextOffset + pointIndex, nextOffset + pointIndex + 1, reverse);
            }
        }

        static void AddQuad(List<int> triangles, int currentA, int currentB, int nextA, int nextB, bool reverse)
        {
            if (!reverse)
            {
                triangles.Add(currentA); triangles.Add(currentB); triangles.Add(nextA);
                triangles.Add(currentB); triangles.Add(nextB); triangles.Add(nextA);
            }
            else
            {
                triangles.Add(currentA); triangles.Add(nextA); triangles.Add(currentB);
                triangles.Add(currentB); triangles.Add(nextA); triangles.Add(nextB);
            }
        }

        void AddProfileCap(MeshBuffers buffers, HullStation station, bool bow, TextureTiling tiling)
        {
            int first = buffers.vertices.Count;
            Vector3 center = Vector3.zero;
            Vector2 centerUV = Vector2.zero;
            float[] surfaceDistances = bow ? GetSignedProfileSurfaceDistances(station.lowerProfile) : null;
            for (int index = 0; index < station.lowerProfile.Count; index++)
            {
                Vector2 point = station.lowerProfile[index];
                Vector3 vertex = ToPartLocal(point.x, point.y, station.GetLongitudinalPosition(point.y));
                buffers.vertices.Add(vertex);
                Vector2 uv = bow
                    ? GetSideUV(station, surfaceDistances[index], tiling)
                    : new Vector2(point.x * tiling.uPerMeter, point.y * tiling.vPerMeter);
                buffers.uv.Add(uv);
                center += vertex;
                centerUV += uv;
            }
            center /= station.lowerProfile.Count;
            centerUV /= station.lowerProfile.Count;
            int centerIndex = buffers.vertices.Count;
            buffers.vertices.Add(center);
            buffers.uv.Add(centerUV);

            for (int index = 0; index < station.lowerProfile.Count - 1; index++)
            {
                if (bow)
                {
                    buffers.triangles.Add(centerIndex); buffers.triangles.Add(first + index + 1); buffers.triangles.Add(first + index);
                }
                else
                {
                    buffers.triangles.Add(centerIndex); buffers.triangles.Add(first + index); buffers.triangles.Add(first + index + 1);
                }
            }
            int last = first + station.lowerProfile.Count - 1;
            if (bow)
            {
                buffers.triangles.Add(centerIndex); buffers.triangles.Add(first); buffers.triangles.Add(last);
            }
            else
            {
                buffers.triangles.Add(centerIndex); buffers.triangles.Add(last); buffers.triangles.Add(first);
            }
        }

        void AddUpperTransom(MeshBuffers buffers, HullStation station, bool bow, TextureTiling tiling)
        {
            int first = buffers.vertices.Count;
            Vector2[] points =
            {
                station.portPaintBoundary, station.portGunwale,
                station.starboardGunwale, station.starboardPaintBoundary
            };
            for (int index = 0; index < points.Length; index++)
            {
                buffers.vertices.Add(ToPartLocal(points[index].x, points[index].y, station.GetLongitudinalPosition(points[index].y)));
                // End caps use a transverse/vertical planar projection. A plate
                // texture consequently remains square when viewed from the bow or
                // stern instead of inheriting either side wall's rake.
                buffers.uv.Add(new Vector2((points[index].x + beam * 0.5f) * tiling.uPerMeter,
                    (points[index].y + hullDepth) * tiling.vPerMeter));
            }
            if (bow)
            {
                buffers.triangles.Add(first); buffers.triangles.Add(first + 1); buffers.triangles.Add(first + 2);
                buffers.triangles.Add(first); buffers.triangles.Add(first + 2); buffers.triangles.Add(first + 3);
            }
            else
            {
                buffers.triangles.Add(first); buffers.triangles.Add(first + 2); buffers.triangles.Add(first + 1);
                buffers.triangles.Add(first); buffers.triangles.Add(first + 3); buffers.triangles.Add(first + 2);
            }
        }

        Vector2 GetSideUV(HullStation station, float signedSurfaceDistance, TextureTiling tiling)
        {
            // Flatten the lower side onto the same unsheared station lattice used by
            // the upper hull. Rake changes geometry, not a UV column's U coordinate.
            float longitudinalDistance = station.longitudinalPosition + hullLength * 0.5f;
            return new Vector2(longitudinalDistance * tiling.uPerMeter,
                signedSurfaceDistance * tiling.vPerMeter);
        }

        static float[] GetSignedProfileSurfaceDistances(List<Vector2> profile)
        {
            float[] distances = new float[profile.Count];
            // Lower profiles are built symmetrically around their bottom-center
            // vertex. Using the actual midpoint keeps the UV seam there even at
            // the bow, where every profile point can collapse onto x = 0.
            int centerIndex = (profile.Count - 1) / 2;

            for (int index = centerIndex - 1; index >= 0; index--)
                distances[index] = distances[index + 1] - Vector2.Distance(profile[index], profile[index + 1]);
            for (int index = centerIndex + 1; index < profile.Count; index++)
                distances[index] = distances[index - 1] + Vector2.Distance(profile[index - 1], profile[index]);
            return distances;
        }

        TextureTiling GetTextureTiling(MeshFilter filter)
        {
            Renderer renderer = filter == null ? null : filter.GetComponent<Renderer>();
            Material material = renderer == null ? null : renderer.sharedMaterial;
            Texture texture = material == null ? null : material.mainTexture;
            if (texture == null || texture.width <= 0 || texture.height <= 0)
                return new TextureTiling(1f, 1f);

            return new TextureTiling(textureDensityU / texture.width,
                textureDensityV / texture.height);
        }

        Vector3 ToPartLocal(float width, float vertical, float longitudinal)
        {
            Vector3 anchor = deckAnchor == null ? Vector3.zero : part.transform.InverseTransformPoint(deckAnchor.position);
            Vector3 widthDirection = widthAxis.sqrMagnitude > 0f ? widthAxis.normalized : Vector3.right;
            Vector3 lengthDirection = lengthAxis.sqrMagnitude > 0f ? lengthAxis.normalized : Vector3.up;
            Vector3 upDirection = downAxis.sqrMagnitude > 0f ? -downAxis.normalized : Vector3.back;
            return anchor + widthDirection * width + upDirection * vertical + lengthDirection * longitudinal;
        }

        void WriteMesh(Mesh mesh, Transform meshTransform, MeshBuffers buffers)
        {
            List<Vector3> localVertices = new List<Vector3>(buffers.vertices.Count);
            for (int index = 0; index < buffers.vertices.Count; index++)
            {
                Vector3 world = part.transform.TransformPoint(buffers.vertices[index]);
                localVertices.Add(meshTransform.InverseTransformPoint(world));
            }

            mesh.Clear();
            if (localVertices.Count > 65535)
                mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(localVertices);
            mesh.SetUVs(0, buffers.uv);
            mesh.SetTriangles(buffers.triangles, 0, true);
            if (buffers.normals.Count == buffers.vertices.Count)
            {
                List<Vector3> localNormals = new List<Vector3>(buffers.normals.Count);
                for (int index = 0; index < buffers.normals.Count; index++)
                {
                    Vector3 worldNormal = part.transform.TransformDirection(buffers.normals[index]);
                    localNormals.Add(meshTransform.InverseTransformDirection(worldNormal).normalized);
                }
                mesh.SetNormals(localNormals);
            }
            else
            {
                mesh.RecalculateNormals();
            }
            mesh.RecalculateBounds();
        }

        void UpdateWireframeOverlays()
        {
            ClearWireframeOverlays();
            if (!debugMode || !showWireframe || !HighLogic.LoadedSceneIsEditor)
                return;

            if (!CreateWireframeMaterial())
                return;

            CreateWireframeOverlay(upperHullFilter, upperHullMesh, "UpperHullWireframe");
            CreateWireframeOverlay(lowerHullFilter, lowerHullMesh, "LowerHullWireframe");
            CreateWireframeOverlay(deckFilter, deckMesh, "DeckWireframe");
            CreateWireframeOverlay(railingsFilter, railingsMesh, "RailingsWireframe");
        }

        bool CreateWireframeMaterial()
        {
            if (wireframeMaterial != null)
                return true;

            Shader shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            if (shader == null)
            {
                Debug.LogError("[SunkWorks] Procedural hull wireframe could not find an unlit color shader.");
                return false;
            }

            wireframeMaterial = new Material(shader);
            wireframeMaterial.name = part.name + "_ProceduralHullWireframe";
            wireframeMaterial.hideFlags = HideFlags.HideAndDontSave;
            wireframeMaterial.color = Color.white;
            wireframeMaterial.SetColor("_Color", Color.white);
            wireframeMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            wireframeMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            wireframeMaterial.SetInt("_Cull", (int)CullMode.Off);
            wireframeMaterial.SetInt("_ZWrite", 0);
            wireframeMaterial.SetInt("_ZTest", (int)CompareFunction.LessEqual);
            wireframeMaterial.renderQueue = 5000;
            return true;
        }

        void CreateWireframeOverlay(MeshFilter sourceFilter, Mesh sourceMesh, string objectName)
        {
            if (sourceFilter == null || sourceMesh == null || sourceMesh.vertexCount == 0)
                return;

            int[] triangles = sourceMesh.triangles;
            if (triangles == null || triangles.Length < 3)
                return;

            HashSet<ulong> edges = new HashSet<ulong>();
            List<int> lineIndices = new List<int>(triangles.Length * 2);
            for (int index = 0; index + 2 < triangles.Length; index += 3)
            {
                AddWireframeEdge(edges, lineIndices, triangles[index], triangles[index + 1]);
                AddWireframeEdge(edges, lineIndices, triangles[index + 1], triangles[index + 2]);
                AddWireframeEdge(edges, lineIndices, triangles[index + 2], triangles[index]);
            }

            Mesh wireMesh = new Mesh();
            wireMesh.name = part.name + "_" + objectName;
            wireMesh.hideFlags = HideFlags.HideAndDontSave;
            wireMesh.indexFormat = sourceMesh.indexFormat;
            wireMesh.vertices = sourceMesh.vertices;
            wireMesh.SetIndices(lineIndices.ToArray(), MeshTopology.Lines, 0, false);
            wireMesh.bounds = sourceMesh.bounds;
            wireframeMeshes.Add(wireMesh);

            GameObject wireObject = new GameObject(objectName);
            wireObject.hideFlags = HideFlags.HideAndDontSave;
            wireObject.layer = sourceFilter.gameObject.layer;
            wireObject.transform.SetParent(sourceFilter.transform, false);
            MeshFilter wireFilter = wireObject.AddComponent<MeshFilter>();
            wireFilter.sharedMesh = wireMesh;
            MeshRenderer wireRenderer = wireObject.AddComponent<MeshRenderer>();
            wireRenderer.sharedMaterial = wireframeMaterial;
            wireRenderer.shadowCastingMode = ShadowCastingMode.Off;
            wireRenderer.receiveShadows = false;
            wireframeObjects.Add(wireObject);
        }

        static void AddWireframeEdge(HashSet<ulong> edges, List<int> lineIndices, int a, int b)
        {
            int minimum = Mathf.Min(a, b);
            int maximum = Mathf.Max(a, b);
            ulong key = ((ulong)(uint)minimum << 32) | (uint)maximum;
            if (!edges.Add(key))
                return;

            lineIndices.Add(a);
            lineIndices.Add(b);
        }

        void ClearWireframeOverlays()
        {
            for (int index = 0; index < wireframeObjects.Count; index++)
            {
                if (wireframeObjects[index] != null)
                    UnityEngine.Object.Destroy(wireframeObjects[index]);
            }
            wireframeObjects.Clear();

            for (int index = 0; index < wireframeMeshes.Count; index++)
                DestroyRuntimeMesh(wireframeMeshes[index]);
            wireframeMeshes.Clear();
        }

        void GenerateColliders(List<HullStation> renderStations)
        {
            ClearGeneratedColliders();
            int pieceCount = Mathf.Clamp(colliderSections, 2, 12);
            for (int pieceIndex = 0; pieceIndex < pieceCount; pieceIndex++)
            {
                float fromT = pieceIndex / (float)pieceCount;
                float toT = (pieceIndex + 1) / (float)pieceCount;
                HullStation from = GenerateStation(fromT, Mathf.Lerp(-hullLength * 0.5f, hullLength * 0.5f, fromT));
                HullStation to = GenerateStation(toT, Mathf.Lerp(-hullLength * 0.5f, hullLength * 0.5f, toT));
                Mesh colliderMesh = BuildColliderMesh(from, to, pieceIndex);

                GameObject colliderObject = new GameObject("ProceduralHullCollider_" + pieceIndex);
                colliderObject.transform.SetParent(colliderHolder, false);
                MeshCollider meshCollider = colliderObject.AddComponent<MeshCollider>();
                meshCollider.convex = true;
                meshCollider.sharedMesh = colliderMesh;
                colliderMeshes.Add(colliderMesh);
            }
            GenerateRailingColliders(renderStations);
        }

        void GenerateRailingColliders(List<HullStation> renderStations)
        {
            if (!railingsEnabled || renderStations.Count < 2)
                return;

            // Safety collision uses a much coarser station set than the visual rails.
            // Every piece is a primitive BoxCollider rather than a detailed mesh.
            int sectionCount = Mathf.Clamp(colliderSections, 2, 12);
            List<float> stationParameters = new List<float>(sectionCount + 2);
            for (int index = 0; index <= sectionCount; index++)
            {
                float angle = Mathf.PI * index / sectionCount;
                stationParameters.Add(0.5f * (1f - Mathf.Cos(angle)));
            }
            AddStationParameter(stationParameters, bowLength / hullLength);
            stationParameters.Sort();

            List<HullStation> stations = new List<HullStation>(stationParameters.Count);
            for (int index = 0; index < stationParameters.Count; index++)
            {
                float longitudinalT = stationParameters[index];
                stations.Add(GenerateStation(longitudinalT,
                    Mathf.Lerp(-hullLength * 0.5f, hullLength * 0.5f, longitudinalT)));
            }

            RailingPerimeter perimeter = BuildRailingPerimeter(stations);
            int colliderIndex = 0;
            for (int index = 0; index < perimeter.points.Count; index++)
            {
                int nextIndex = (index + 1) % perimeter.points.Count;
                RailingPerimeterPoint start = perimeter.points[index];
                RailingPerimeterPoint end = perimeter.points[nextIndex];
                if (!IsRailingEdgeEnabled(start, end))
                    continue;

                Vector2 startCenter = (start.outer + start.inner) * 0.5f;
                Vector2 endCenter = (end.outer + end.inner) * 0.5f;
                AddRailingBoxCollider(
                    GetRailingPerimeterVertex(startCenter, start, 0f),
                    GetRailingPerimeterVertex(endCenter, end, 0f),
                    GetRailingPerimeterVertex(startCenter, start, railingHeight),
                    GetRailingPerimeterVertex(endCenter, end, railingHeight),
                    colliderIndex++);
            }
        }

        void AddRailingBoxCollider(Vector3 startBottom, Vector3 endBottom,
            Vector3 startTop, Vector3 endTop, int colliderIndex)
        {
            Vector3 startCenter = (startBottom + startTop) * 0.5f;
            Vector3 endCenter = (endBottom + endTop) * 0.5f;
            Vector3 longitudinal = endCenter - startCenter;
            Vector3 vertical = ((startTop - startBottom) + (endTop - endBottom)) * 0.5f;
            if (longitudinal.sqrMagnitude < 0.000001f || vertical.sqrMagnitude < 0.000001f)
                return;

            Vector3 up = vertical.normalized;
            Vector3 forward = Vector3.ProjectOnPlane(longitudinal, up).normalized;
            if (forward.sqrMagnitude < 0.000001f)
                return;

            Vector3 center = (startBottom + endBottom + startTop + endTop) * 0.25f;
            Vector3 worldCenter = part.transform.TransformPoint(center);
            Vector3 worldForward = part.transform.TransformDirection(forward);
            Vector3 worldUp = part.transform.TransformDirection(up);

            GameObject colliderObject = new GameObject("ProceduralRailingCollider_" + colliderIndex);
            colliderObject.transform.SetParent(colliderHolder, false);
            colliderObject.transform.position = worldCenter;
            colliderObject.transform.rotation = Quaternion.LookRotation(worldForward, worldUp);
            BoxCollider boxCollider = colliderObject.AddComponent<BoxCollider>();
            boxCollider.center = Vector3.zero;
            boxCollider.size = new Vector3(
                Mathf.Max(0.05f, railingThickness),
                vertical.magnitude,
                longitudinal.magnitude + railingThickness);
        }

        Mesh BuildColliderMesh(HullStation from, HullStation to, int pieceIndex)
        {
            List<Vector2> fromProfile = BuildClosedColliderProfile(from);
            List<Vector2> toProfile = BuildClosedColliderProfile(to);
            int profileCount = fromProfile.Count;
            MeshBuffers buffers = new MeshBuffers();

            for (int end = 0; end < 2; end++)
            {
                HullStation station = end == 0 ? from : to;
                List<Vector2> profile = end == 0 ? fromProfile : toProfile;
                for (int index = 0; index < profileCount; index++)
                {
                    Vector2 point = profile[index];
                    Vector3 partLocal = ToPartLocal(point.x, point.y, station.GetLongitudinalPosition(point.y));
                    Vector3 world = part.transform.TransformPoint(partLocal);
                    buffers.vertices.Add(colliderHolder.InverseTransformPoint(world));
                    buffers.uv.Add(Vector2.zero);
                }
            }

            for (int index = 0; index < profileCount; index++)
            {
                int next = (index + 1) % profileCount;
                AddQuad(buffers.triangles, index, next, profileCount + index, profileCount + next, false);
            }
            AddColliderEndCap(buffers.triangles, 0, profileCount, true);
            AddColliderEndCap(buffers.triangles, profileCount, profileCount, false);

            Mesh mesh = CreateRuntimeMesh(part.name + "_HullCollider_" + pieceIndex);
            mesh.SetVertices(buffers.vertices);
            mesh.SetTriangles(buffers.triangles, 0, true);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static List<Vector2> BuildClosedColliderProfile(HullStation station)
        {
            List<Vector2> profile = new List<Vector2>();
            profile.Add(station.portGunwale);
            for (int index = 0; index < station.lowerProfile.Count; index++)
                profile.Add(station.lowerProfile[index]);
            profile.Add(station.starboardGunwale);
            return profile;
        }

        static void AddColliderEndCap(List<int> triangles, int offset, int count, bool reverse)
        {
            for (int index = 1; index < count - 1; index++)
            {
                if (reverse)
                {
                    triangles.Add(offset); triangles.Add(offset + index + 1); triangles.Add(offset + index);
                }
                else
                {
                    triangles.Add(offset); triangles.Add(offset + index); triangles.Add(offset + index + 1);
                }
            }
        }

        void ClearGeneratedColliders()
        {
            for (int index = colliderHolder.childCount - 1; index >= 0; index--)
            {
                Transform child = colliderHolder.GetChild(index);
                if (child.name.StartsWith("ProceduralHullCollider_", StringComparison.Ordinal) ||
                    child.name.StartsWith("ProceduralRailingCollider_", StringComparison.Ordinal))
                    UnityEngine.Object.Destroy(child.gameObject);
            }
            for (int index = 0; index < colliderMeshes.Count; index++)
                DestroyRuntimeMesh(colliderMeshes[index]);
            colliderMeshes.Clear();
        }

        void DisableReferenceColliders()
        {
            Transform reference = part.FindModelTransform(referenceColliderTransform);
            if (reference != null)
            {
                Collider[] colliders = reference.GetComponentsInChildren<Collider>(true);
                for (int index = 0; index < colliders.Length; index++)
                    colliders[index].enabled = false;
                Renderer[] renderers = reference.GetComponentsInChildren<Renderer>(true);
                for (int index = 0; index < renderers.Length; index++)
                    renderers[index].enabled = false;
            }

            Collider holderCollider = colliderHolder.GetComponent<Collider>();
            if (holderCollider != null)
                holderCollider.enabled = false;
            Renderer holderRenderer = colliderHolder.GetComponent<Renderer>();
            if (holderRenderer != null)
                holderRenderer.enabled = false;

            // Modeling helpers are sometimes exported as small visible primitives.
            // Keep the transform as the deck origin, but never render or collide with it.
            if (deckAnchor != null && deckAnchor != deckFilter.transform)
            {
                Collider[] anchorColliders = deckAnchor.GetComponentsInChildren<Collider>(true);
                for (int index = 0; index < anchorColliders.Length; index++)
                    anchorColliders[index].enabled = false;
                Renderer[] anchorRenderers = deckAnchor.GetComponentsInChildren<Renderer>(true);
                for (int index = 0; index < anchorRenderers.Length; index++)
                    anchorRenderers[index].enabled = false;
            }
        }

        void UpdateDeckNodes()
        {
            Vector3 anchor = deckAnchor == null ? Vector3.zero : part.transform.InverseTransformPoint(deckAnchor.position);
            Vector3 outward = downAxis.sqrMagnitude > 0f ? -downAxis.normalized : Vector3.back;
            AttachNode deckNode = part.FindAttachNode("deck");
            if (deckNode != null)
            {
                deckNode.position = anchor;
                deckNode.originalPosition = anchor;
                deckNode.orientation = outward;
                deckNode.originalOrientation = outward;
                deckNode.size = Mathf.Clamp(Mathf.RoundToInt(beam / 1.25f), 1, 4);
            }
            if (part.srfAttachNode != null)
            {
                part.srfAttachNode.position = anchor;
                part.srfAttachNode.originalPosition = anchor;
                part.srfAttachNode.orientation = outward;
                part.srfAttachNode.originalOrientation = outward;
            }
        }

        void UpdateCenterOfMass()
        {
            part.CoMOffset = ToPartLocal(0f, GetPaintBoundaryY(), 0f);
        }

        void NotifyGeometryChanged(bool notifyEditor)
        {
            part.ResetModelMeshRenderersCache();
            part.ResetModelRenderersCache();
            part.SendEvent("OnPartModelChanged", null, 0);
            part.SendEvent("OnPartColliderChanged", null, 0);

            if ((HighLogic.LoadedSceneIsFlight || updateDragCubesInEditor) && DragCubeSystem.Instance != null)
            {
                DragCube cube = DragCubeSystem.Instance.RenderProceduralDragCube(part);
                bool procedural = part.DragCubes.Procedural;
                part.DragCubes.ClearCubes();
                part.DragCubes.Cubes.Add(cube);
                part.DragCubes.Procedural = procedural;
                part.DragCubes.ResetCubeWeights();
                part.DragCubes.ForceUpdate(true, true, false);
            }

            if (notifyEditor && HighLogic.LoadedSceneIsEditor && EditorLogic.fetch != null && EditorLogic.fetch.ship != null)
            {
                if (part.variants != null && part.variants.SelectedVariant != null)
                    GameEvents.onEditorVariantApplied.Fire(part, part.variants.SelectedVariant);

                GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
                MonoUtilities.RefreshPartContextWindow(part);
            }
        }

        static float CalculateArea(MeshBuffers buffers)
        {
            float area = 0f;
            for (int index = 0; index < buffers.triangles.Count; index += 3)
            {
                Vector3 a = buffers.vertices[buffers.triangles[index]];
                Vector3 b = buffers.vertices[buffers.triangles[index + 1]];
                Vector3 c = buffers.vertices[buffers.triangles[index + 2]];
                area += Vector3.Cross(b - a, c - a).magnitude * 0.5f;
            }
            return area;
        }

        static float CalculateVolume(List<HullStation> stations)
        {
            float volume = 0f;
            for (int index = 0; index < stations.Count - 1; index++)
            {
                float areaA = CalculateStationArea(stations[index]);
                float areaB = CalculateStationArea(stations[index + 1]);
                // Measure at each station's rake reference line. The bow lattice is
                // shifted forward at deck level, so its raw coordinate is not the
                // physical longitudinal spacing used for the volume approximation.
                float length = stations[index + 1].GetLongitudinalPosition(-stations[index + 1].referenceDepth) -
                    stations[index].GetLongitudinalPosition(-stations[index].referenceDepth);
                volume += (areaA + areaB) * 0.5f * length;
            }
            return Mathf.Abs(volume);
        }

        static float CalculateStationArea(HullStation station)
        {
            List<Vector2> profile = BuildClosedColliderProfile(station);
            float twiceArea = 0f;
            for (int index = 0; index < profile.Count; index++)
            {
                Vector2 current = profile[index];
                Vector2 next = profile[(index + 1) % profile.Count];
                twiceArea += current.x * next.y - next.x * current.y;
            }
            return Mathf.Abs(twiceArea) * 0.5f;
        }

        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit)
        {
            return generatedArea * massPerSquareMeter;
        }

        public ModifierChangeWhen GetModuleMassChangeWhen()
        {
            return ModifierChangeWhen.FIXED;
        }

        public float GetModuleCost(float defaultCost, ModifierStagingSituation sit)
        {
            return generatedArea * costPerSquareMeter;
        }

        public ModifierChangeWhen GetModuleCostChangeWhen()
        {
            return ModifierChangeWhen.FIXED;
        }

        enum RailingEdgeSide
        {
            Port,
            Starboard,
            Stern
        }

        sealed class RailingPerimeter
        {
            public readonly List<RailingPerimeterPoint> points = new List<RailingPerimeterPoint>();
        }

        sealed class RailingPerimeterPoint
        {
            public Vector2 outer;
            public Vector2 inner;
            public float longitudinalT;
            public float bowRakeOffset;
            public float referenceDepth;
            public RailingEdgeSide outgoingSide;
        }

        sealed class HullStation
        {
            public float longitudinalT;
            public float longitudinalPosition;
            public float bowRakeOffset;
            public float referenceDepth;
            public bool isRakedBowTransition;
            public readonly List<Vector2> lowerProfile = new List<Vector2>();
            public Vector2 portGunwale;
            public Vector2 starboardGunwale;
            public Vector2 portPaintBoundary;
            public Vector2 starboardPaintBoundary;

            public float GetLongitudinalPosition(float verticalPosition)
            {
                // Do not clamp at the paint boundary: the lower bow must continue
                // along exactly the same affine rake line as the upper bow.
                float depthFraction = referenceDepth <= 0f
                    ? 0f
                    : Mathf.Max(0f, -verticalPosition / referenceDepth);
                return longitudinalPosition + bowRakeOffset * depthFraction;
            }

        }

        struct TextureTiling
        {
            public readonly float uPerMeter;
            public readonly float vPerMeter;

            public TextureTiling(float uPerMeter, float vPerMeter)
            {
                this.uPerMeter = uPerMeter;
                this.vPerMeter = vPerMeter;
            }
        }

        sealed class MeshBuffers
        {
            public readonly List<Vector3> vertices = new List<Vector3>();
            public readonly List<Vector3> normals = new List<Vector3>();
            public readonly List<Vector2> uv = new List<Vector2>();
            public readonly List<int> triangles = new List<int>();
        }
    }
}
