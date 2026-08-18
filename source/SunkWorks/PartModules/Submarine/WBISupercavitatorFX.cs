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

        #endregion

        #region Housekeeping
        WBISupercavitator supercavitator;
        GameObject cavityObject;
        Mesh cavityMesh;
        MeshRenderer cavityRenderer;
        Material cavityMaterial;
        Material diagnosticRingMaterial;
        LineRenderer originRing;
        LineRenderer expansionEndRing;
        LineRenderer taperStartRing;
        Vector3[] vertices;
        int[] triangles;
        float lastLength = -1f;
        float lastCavitatorRadius = -1f;
        float lastTipRadius = -1f;
        float lastMaximumRadius = -1f;
        float lastExpansionFraction = -1f;
        float lastStraightLength = -1f;
        float lastOpacity = -1f;
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
            vertices = new Vector3[ringCount * radialSegments];
            triangles = new int[lengthSegments * radialSegments * 6];
            buildTriangles();
            cavityMesh.vertices = vertices;
            cavityMesh.triangles = triangles;
            updateMaterial();
            setVisualizationEnabled(false);
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
                int currentRing = ring * radialSegments;
                int nextRing = (ring + 1) * radialSegments;
                for (int side = 0; side < radialSegments; side++)
                {
                    int nextSide = (side + 1) % radialSegments;
                    int a = currentRing + side;
                    int b = currentRing + nextSide;
                    int c = nextRing + side;
                    int d = nextRing + nextSide;

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
            for (int ring = 0; ring <= lengthSegments; ring++)
            {
                float axialDistance = getAxialDistanceForRing(ring, cavity);
                float radius = cavity.RadiusAt(axialDistance);
                int ringStart = ring * radialSegments;

                for (int side = 0; side < radialSegments; side++)
                {
                    float angle = Mathf.PI * 2f * side / radialSegments;
                    vertices[ringStart + side] = new Vector3(
                        Mathf.Cos(angle) * radius,
                        Mathf.Sin(angle) * radius,
                        axialDistance);
                }
            }

            cavityMesh.vertices = vertices;
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
    }
    #endregion
}
