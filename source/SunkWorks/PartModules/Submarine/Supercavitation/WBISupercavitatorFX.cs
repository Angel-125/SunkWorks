using UnityEngine;

namespace SunkWorks.Submarine
{
    /// <summary>
    /// Renders the supercavity as a translucent procedural mesh in flight and as a
    /// full-strength preview in the editor.
    /// </summary>
    [KSPModule("Supercavitator Visualizer")]
    public class WBISupercavitatorFX : PartModule
    {
        #region Constants
        const string kGroupName = "Supercavitation";
        #endregion

        #region Fields
        /// <summary>Shows or hides the cavity visualization.</summary>
        [KSPField(guiActive = true, guiActiveEditor = true, isPersistant = true,
            guiName = "#LOC_SUNKWORKS_showCavity",
            groupName = kGroupName,
            groupDisplayName = "#LOC_SUNKWORKS_supercavitationGroup")]
        [UI_Toggle(enabledText = "#LOC_SUNKWORKS_on", disabledText = "#LOC_SUNKWORKS_off")]
        public bool showCavity = true;

        /// <summary>Shows or hides the animated foam layer in flight.</summary>
        [KSPField(guiActive = true, isPersistant = true,
            guiName = "#LOC_SUNKWORKS_showCavityFoam",
            groupName = kGroupName,
            groupDisplayName = "#LOC_SUNKWORKS_supercavitationGroup")]
        [UI_Toggle(enabledText = "#LOC_SUNKWORKS_on", disabledText = "#LOC_SUNKWORKS_off")]
        public bool showFoamAnimation = true;

        /// <summary>Opacity of the translucent cavity shell.</summary>
        [KSPField(isPersistant = true,
            guiName = "#LOC_SUNKWORKS_cavityOpacity",
            groupName = kGroupName,
            groupDisplayName = "#LOC_SUNKWORKS_supercavitationGroup")]
        [UI_FloatRange(minValue = 0.02f, maxValue = 0.5f, stepIncrement = 0.01f)]
        public float cavityOpacity = 0.12f;

        /// <summary>
        /// Part EFFECTS group driven by the supercavitator's normalized cavity scale.
        /// </summary>
        [KSPField]
        public string runningEffect = string.Empty;

        /// <summary>RGB color encoded as a KSP ConfigNode color string.</summary>
        [KSPField]
        public string cavityColor = "0.15, 0.65, 1.0";

        /// <summary>Number of subdivisions along the cavity.</summary>
        [KSPField]
        public int lengthSegments = 32;

        /// <summary>Number of subdivisions around the cavity.</summary>
        [KSPField]
        public int radialSegments = 24;

        /// <summary>Width in metres of the cavity origin and expansion-end rings.</summary>
        [KSPField]
        public float diagnosticRingWidth = 0.04f;

        /// <summary>GameDatabase URL of the transparent, tileable foam texture.</summary>
        [KSPField]
        public string foamTextureURL = "WildBlueIndustries/SunkWorks/FX/SupercavityFoam";

        /// <summary>Length in metres represented by one repeat of the foam texture.</summary>
        [KSPField]
        public float foamRepeatLength = 3f;

        /// <summary>Maximum downstream speed of the animated foam in metres per second.</summary>
        [KSPField]
        public float foamFlowSpeed = 18f;

        /// <summary>Minimum downstream speed of the animated foam in metres per second.</summary>
        [KSPField]
        public float foamMinimumFlowSpeed = 8f;

        /// <summary>
        /// Multiple of fullCavitySpeed at which the foam reaches foamFlowSpeed.
        /// </summary>
        [KSPField]
        public float foamMaximumVesselSpeedMultiplier = 2.5f;

        /// <summary>RGB tint applied to the animated foam texture.</summary>
        [KSPField]
        public string foamColor = "0.8, 0.95, 1.0";

        /// <summary>Opacity multiplier applied to the animated foam texture.</summary>
        [KSPField]
        public float foamOpacity = 0.35f;

        #endregion

        #region Housekeeping
        WBISupercavitator supercavitator;
        GameObject cavityObject;
        Mesh cavityMesh;
        MeshRenderer cavityRenderer;
        Material cavityMaterial;
        GameObject foamObject;
        MeshRenderer foamRenderer;
        Material foamMaterial;
        Material diagnosticRingMaterial;
        LineRenderer originRing;
        LineRenderer expansionEndRing;
        LineRenderer taperStartRing;
        Vector3[] vertices;
        Vector2[] uvs;
        int[] triangles;
        int verticesPerRing;
        float lastLength = -1f;
        float lastCavitatorRadius = -1f;
        float lastTipRadius = -1f;
        float lastMaximumRadius = -1f;
        float lastExpansionFraction = -1f;
        float lastStraightLength = -1f;
        float lastOpacity = -1f;
        float foamTextureOffset;
        string lastColor = string.Empty;
        #endregion

        /// <summary>Initializes the optional cavity renderer.</summary>
        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            lengthSegments = Mathf.Clamp(lengthSegments, 4, 128);
            radialSegments = Mathf.Clamp(radialSegments, 6, 64);
            diagnosticRingWidth = Mathf.Clamp(diagnosticRingWidth, 0.005f, 0.25f);
            cavityOpacity = Mathf.Clamp(cavityOpacity, 0.02f, 0.5f);
            foamRepeatLength = Mathf.Max(0.1f, foamRepeatLength);
            foamMinimumFlowSpeed = Mathf.Max(0f, foamMinimumFlowSpeed);
            foamFlowSpeed = Mathf.Max(foamMinimumFlowSpeed, foamFlowSpeed);
            foamMaximumVesselSpeedMultiplier = Mathf.Max(1f,
                foamMaximumVesselSpeedMultiplier);
            foamOpacity = Mathf.Clamp01(foamOpacity);
            supercavitator = part.FindModuleImplementing<WBISupercavitator>();

            if (supercavitator == null)
            {
                Debug.LogWarning("[SunkWorks] WBISupercavitatorFX on " + part.partInfo.title +
                    " requires WBISupercavitator on the same part.");
                enabled = false;
                return;
            }

            updateOpacityFieldVisibility();
            setRunningEffect(0f);
            createCavityObject();
        }

        /// <summary>Updates the live cavity or editor preview.</summary>
        public void Update()
        {
            if (cavityRenderer == null || supercavitator == null)
                return;

            updateOpacityFieldVisibility();
            cavityOpacity = Mathf.Clamp(cavityOpacity, 0.02f, 0.5f);
            updateMaterial();

            Supercavity cavity = default(Supercavity);
            bool hasCavity = HighLogic.LoadedSceneIsEditor
                ? supercavitator.TryGetEditorPreviewSupercavity(out cavity)
                : HighLogic.LoadedSceneIsFlight &&
                  supercavitator.TryGetCurrentSupercavity(out cavity);

            setRunningEffect(HighLogic.LoadedSceneIsFlight
                ? supercavitator.CavityScale
                : 0f);

            if (!showCavity || !supercavitator.cavityEnabled || !hasCavity ||
                cavity.length <= 0f || cavity.axis.sqrMagnitude < 1e-6f)
            {
                setVisualizationEnabled(false);
                return;
            }

            // The procedural mesh is expressed directly in world metres. Use the live
            // model-transform position instead of the physics-tick snapshot stored in
            // cavity.origin; otherwise a fast vessel visibly outruns the unparented FX
            // between FixedUpdate and the rendered frame.
            cavityObject.transform.position = supercavitator.CavityTransform.position;
            cavityObject.transform.rotation = Quaternion.LookRotation(
                cavity.axis.normalized, supercavitator.CavityTransform.up);
            cavityObject.transform.localScale = Vector3.one;

            if (geometryChanged(cavity))
                updateMesh(cavity);

            if (HighLogic.LoadedSceneIsFlight && showFoamAnimation)
                updateFoamAnimation();
            setVisualizationEnabled(true);
        }

        /// <summary>Toggles the cavity visualization through an action group.</summary>
        [KSPAction("#LOC_SUNKWORKS_toggleCavityVisualization")]
        public void ToggleCavityVisualizationAction(KSPActionParam param)
        {
            showCavity = !showCavity;
        }

        /// <summary>Destroys runtime-created Unity objects.</summary>
        public void OnDestroy()
        {
            setRunningEffect(0f);
            if (cavityObject != null)
                Destroy(cavityObject);
            if (cavityMesh != null)
                Destroy(cavityMesh);
            if (cavityMaterial != null)
                Destroy(cavityMaterial);
            if (foamMaterial != null)
                Destroy(foamMaterial);
            if (diagnosticRingMaterial != null)
                Destroy(diagnosticRingMaterial);
        }

        #region Helpers
        void createCavityObject()
        {
            cavityObject = new GameObject("SunkWorks Supercavity");
            cavityObject.layer = part.gameObject.layer;
            cavityObject.transform.localScale = Vector3.one;

            MeshFilter meshFilter = cavityObject.AddComponent<MeshFilter>();
            cavityRenderer = cavityObject.AddComponent<MeshRenderer>();

            cavityMesh = new Mesh
            {
                name = "SunkWorks Supercavity Mesh"
            };
            cavityMesh.MarkDynamic();
            meshFilter.sharedMesh = cavityMesh;

            Shader shader = Shader.Find("KSP/Alpha/Translucent");
            if (shader == null)
                shader = Shader.Find("Legacy Shaders/Transparent/Diffuse");
            if (shader == null)
            {
                Debug.LogError("[SunkWorks] WBISupercavitatorFX could not find a transparent shader.");
                enabled = false;
                Destroy(cavityObject);
                return;
            }

            cavityMaterial = new Material(shader)
            {
                name = "SunkWorks Supercavity Material",
                renderQueue = 3000
            };
            if (cavityMaterial.HasProperty("_Cull"))
                cavityMaterial.SetFloat("_Cull", 0f);
            if (cavityMaterial.HasProperty("_ZWrite"))
                cavityMaterial.SetFloat("_ZWrite", 0f);
            cavityRenderer.sharedMaterial = cavityMaterial;
            cavityRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            cavityRenderer.receiveShadows = false;

            createFoamRenderer();

            diagnosticRingMaterial = new Material(shader)
            {
                name = "SunkWorks Supercavity Diagnostic Ring Material",
                renderQueue = 3001
            };
            if (diagnosticRingMaterial.HasProperty("_Cull"))
                diagnosticRingMaterial.SetFloat("_Cull", 0f);
            if (diagnosticRingMaterial.HasProperty("_ZWrite"))
                diagnosticRingMaterial.SetFloat("_ZWrite", 0f);
            originRing = createDiagnosticRing("Cavity Origin Ring");
            expansionEndRing = createDiagnosticRing("Cavity Expansion End Ring");
            taperStartRing = createDiagnosticRing("Cavity Taper Start Ring");

            int ringCount = lengthSegments + 1;
            verticesPerRing = radialSegments + 1;
            vertices = new Vector3[ringCount * verticesPerRing];
            uvs = new Vector2[vertices.Length];
            triangles = new int[lengthSegments * radialSegments * 6];
            buildTriangles();
            cavityMesh.vertices = vertices;
            cavityMesh.uv = uvs;
            cavityMesh.triangles = triangles;
            updateMaterial();
            setVisualizationEnabled(false);
        }

        void createFoamRenderer()
        {
            Texture2D foamTexture = GameDatabase.Instance.GetTexture(foamTextureURL, false);
            if (foamTexture == null)
            {
                Debug.LogWarning("[SunkWorks] WBISupercavitatorFX could not load foam texture: " +
                    foamTextureURL);
                return;
            }

            foamTexture.wrapMode = TextureWrapMode.Repeat;
            foamMaterial = new Material(cavityMaterial)
            {
                name = "SunkWorks Supercavity Foam Material",
                renderQueue = 3001
            };
            foamMaterial.SetTexture("_MainTex", foamTexture);

            Color color;
            if (!ConfigNode.CheckAndParseColor(foamColor, out color))
                color = new Color(0.8f, 0.95f, 1f);
            color.a = Mathf.Clamp01(foamOpacity);
            foamMaterial.color = color;
            if (foamMaterial.HasProperty("_Color"))
                foamMaterial.SetColor("_Color", color);

            foamObject = new GameObject("Supercavity Foam");
            foamObject.layer = part.gameObject.layer;
            foamObject.transform.SetParent(cavityObject.transform, false);

            MeshFilter foamMeshFilter = foamObject.AddComponent<MeshFilter>();
            foamMeshFilter.sharedMesh = cavityMesh;
            foamRenderer = foamObject.AddComponent<MeshRenderer>();
            foamRenderer.sharedMaterial = foamMaterial;
            foamRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            foamRenderer.receiveShadows = false;
        }

        void setRunningEffect(float power)
        {
            if (part != null && !string.IsNullOrEmpty(runningEffect))
                part.Effect(runningEffect, Mathf.Clamp01(power));
        }

        void updateOpacityFieldVisibility()
        {
            BaseField opacityField = Fields["cavityOpacity"];
            if (opacityField == null)
                return;

            opacityField.guiActive = supercavitator != null && supercavitator.debugMode;
            opacityField.guiActiveEditor = supercavitator != null && supercavitator.debugMode;
        }

        LineRenderer createDiagnosticRing(string objectName)
        {
            GameObject ringObject = new GameObject(objectName);
            ringObject.layer = part.gameObject.layer;
            ringObject.transform.SetParent(cavityObject.transform, false);

            LineRenderer lineRenderer = ringObject.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = false;
            lineRenderer.loop = true;
            lineRenderer.positionCount = radialSegments;
            lineRenderer.widthMultiplier = diagnosticRingWidth;
            lineRenderer.alignment = LineAlignment.View;
            lineRenderer.sharedMaterial = diagnosticRingMaterial;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
            return lineRenderer;
        }

        void buildTriangles()
        {
            int triangleIndex = 0;
            for (int ring = 0; ring < lengthSegments; ring++)
            {
                int currentRing = ring * verticesPerRing;
                int nextRing = (ring + 1) * verticesPerRing;
                for (int side = 0; side < radialSegments; side++)
                {
                    int a = currentRing + side;
                    int b = currentRing + side + 1;
                    int c = nextRing + side;
                    int d = nextRing + side + 1;

                    triangles[triangleIndex++] = a;
                    triangles[triangleIndex++] = c;
                    triangles[triangleIndex++] = b;
                    triangles[triangleIndex++] = b;
                    triangles[triangleIndex++] = c;
                    triangles[triangleIndex++] = d;
                }
            }
        }

        bool geometryChanged(Supercavity cavity)
        {
            return !Mathf.Approximately(lastLength, cavity.length) ||
                !Mathf.Approximately(lastCavitatorRadius, cavity.cavitatorRadius) ||
                !Mathf.Approximately(lastTipRadius, cavity.tipRadius) ||
                !Mathf.Approximately(lastMaximumRadius, cavity.maximumRadius) ||
                !Mathf.Approximately(lastExpansionFraction, cavity.expansionFraction) ||
                !Mathf.Approximately(lastStraightLength, cavity.straightLength);
        }

        void updateMesh(Supercavity cavity)
        {
            float repeatLength = Mathf.Max(0.1f, foamRepeatLength);
            for (int ring = 0; ring <= lengthSegments; ring++)
            {
                float axialDistance = getAxialDistanceForRing(ring, cavity);
                float radius = cavity.RadiusAt(axialDistance);
                int ringStart = ring * verticesPerRing;

                for (int side = 0; side <= radialSegments; side++)
                {
                    float u = (float)side / radialSegments;
                    float angle = Mathf.PI * 2f * u;
                    int vertexIndex = ringStart + side;
                    vertices[vertexIndex] = new Vector3(
                        Mathf.Cos(angle) * radius,
                        Mathf.Sin(angle) * radius,
                        axialDistance);
                    uvs[vertexIndex] = new Vector2(u, axialDistance / repeatLength);
                }
            }

            cavityMesh.vertices = vertices;
            cavityMesh.uv = uvs;
            cavityMesh.RecalculateNormals();
            cavityMesh.RecalculateBounds();
            updateDiagnosticRing(originRing, 0f, cavity.cavitatorRadius);
            updateDiagnosticRing(expansionEndRing,
                cavity.NoseLength,
                cavity.maximumRadius);
            updateDiagnosticRing(taperStartRing,
                cavity.TaperStartDistance,
                cavity.maximumRadius);
            lastLength = cavity.length;
            lastCavitatorRadius = cavity.cavitatorRadius;
            lastTipRadius = cavity.tipRadius;
            lastMaximumRadius = cavity.maximumRadius;
            lastExpansionFraction = cavity.expansionFraction;
            lastStraightLength = cavity.straightLength;
        }

        float getAxialDistanceForRing(int ring, Supercavity cavity)
        {
            // Place vertices at both profile transitions and reserve enough detail for
            // the rounded nose, straight body, and tapered tail.
            int noseSegments = Mathf.Clamp(lengthSegments / 4, 4, lengthSegments - 6);
            int straightSegments = cavity.straightLength > 0f
                ? Mathf.Clamp(lengthSegments / 8, 2, lengthSegments - noseSegments - 4)
                : 0;
            int taperSegments = lengthSegments - noseSegments - straightSegments;

            if (ring <= noseSegments)
                return cavity.NoseLength * ring / noseSegments;

            if (ring <= noseSegments + straightSegments)
            {
                float straightProgress = (float)(ring - noseSegments) / straightSegments;
                return Mathf.Lerp(cavity.NoseLength,
                    cavity.TaperStartDistance, straightProgress);
            }

            float taperProgress = (float)(ring - noseSegments - straightSegments) /
                taperSegments;
            return Mathf.Lerp(cavity.TaperStartDistance, cavity.length, taperProgress);
        }

        void updateDiagnosticRing(LineRenderer lineRenderer, float axialDistance, float radius)
        {
            if (lineRenderer == null)
                return;

            lineRenderer.widthMultiplier = diagnosticRingWidth;
            for (int side = 0; side < radialSegments; side++)
            {
                float angle = Mathf.PI * 2f * side / radialSegments;
                lineRenderer.SetPosition(side, new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius,
                    axialDistance));
            }
        }

        void setVisualizationEnabled(bool isEnabled)
        {
            if (cavityRenderer != null)
                cavityRenderer.enabled = isEnabled;
            if (foamRenderer != null)
                foamRenderer.enabled = isEnabled && HighLogic.LoadedSceneIsFlight &&
                    showFoamAnimation;
            bool showDiagnosticRings = isEnabled && supercavitator != null &&
                supercavitator.debugMode;
            if (originRing != null)
                originRing.enabled = showDiagnosticRings;
            if (expansionEndRing != null)
                expansionEndRing.enabled = showDiagnosticRings;
            if (taperStartRing != null)
                taperStartRing.enabled = showDiagnosticRings;
        }

        void updateMaterial()
        {
            if (cavityMaterial == null ||
                Mathf.Approximately(lastOpacity, cavityOpacity) && lastColor == cavityColor)
                return;

            Color color;
            if (!ConfigNode.CheckAndParseColor(cavityColor, out color))
                color = new Color(0.15f, 0.65f, 1f);
            color.a = cavityOpacity;
            cavityMaterial.color = color;
            if (cavityMaterial.HasProperty("_Color"))
                cavityMaterial.SetColor("_Color", color);

            Color ringColor = color;
            ringColor.a = Mathf.Clamp(cavityOpacity * 3f, 0.25f, 0.8f);
            if (diagnosticRingMaterial != null)
            {
                diagnosticRingMaterial.color = ringColor;
                if (diagnosticRingMaterial.HasProperty("_Color"))
                    diagnosticRingMaterial.SetColor("_Color", ringColor);
            }
            if (originRing != null)
            {
                originRing.startColor = ringColor;
                originRing.endColor = ringColor;
            }
            if (expansionEndRing != null)
            {
                expansionEndRing.startColor = ringColor;
                expansionEndRing.endColor = ringColor;
            }
            if (taperStartRing != null)
            {
                taperStartRing.startColor = ringColor;
                taperStartRing.endColor = ringColor;
            }
            lastOpacity = cavityOpacity;
            lastColor = cavityColor;
        }

        void updateFoamAnimation()
        {
            if (foamMaterial == null)
                return;

            float repeatLength = Mathf.Max(0.1f, foamRepeatLength);
            float sourceSpeed = HighLogic.LoadedSceneIsFlight
                ? supercavitator.DiagnosticSpeed
                : supercavitator.minimumCavitySpeed;
            float maximumVesselSpeed = Mathf.Max(
                supercavitator.minimumCavitySpeed + 0.01f,
                supercavitator.fullCavitySpeed * foamMaximumVesselSpeedMultiplier);
            float speedProgress = Mathf.InverseLerp(
                supercavitator.minimumCavitySpeed,
                maximumVesselSpeed,
                sourceSpeed);
            float currentFlowSpeed = Mathf.Lerp(
                foamMinimumFlowSpeed,
                foamFlowSpeed,
                speedProgress);

            foamTextureOffset = Mathf.Repeat(foamTextureOffset +
                currentFlowSpeed * Time.deltaTime / repeatLength, 1f);
            foamMaterial.SetTextureOffset("_MainTex",
                new Vector2(0f, -foamTextureOffset));
        }
    }
    #endregion
}
