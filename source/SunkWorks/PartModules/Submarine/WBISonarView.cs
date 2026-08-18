using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SunkWorks.Submarine
{
    /// <summary>
    /// Draws a high-visibility wireframe over the active body's live terrain meshes.
    /// Stock PQS meshes are used normally; when Parallax is loaded, the renderer uses
    /// its active subdivided child mesh so the wireframe follows the enhanced terrain.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class WBISonarView : MonoBehaviour
    {
        const string kLegacyParallaxAssemblyName = "Parallax";
        const string kParallaxContinuedAssemblyName = "ParallaxContinued";
        const string kInternalColorShader = "Hidden/Internal-Colored";
        const string kFallbackColorShader = "Unlit/Color";
        const float kMinimumFadeSpan = 1f;
        const int kCachePruneInterval = 300;
        const int kCacheRetainFrames = 600;

        Material wireMaterial;
        MaterialPropertyBlock wireProperties;
        CommandBuffer wireCommandBuffer;
        Camera commandCamera;
        readonly Dictionary<int, WireMeshEntry> wireMeshCache =
            new Dictionary<int, WireMeshEntry>();
        readonly List<int> staleWireMeshIds = new List<int>();
        WBISonarRanger activeSonar;
        Vessel activeVessel;
        PQS activePqs;
        bool parallaxLoaded;
        bool successfulDrawReported;

        /// <summary>Creates the runtime material and subscribes to camera rendering.</summary>
        public void Start()
        {
            parallaxLoaded = isAssemblyLoaded(kLegacyParallaxAssemblyName) ||
                isAssemblyLoaded(kParallaxContinuedAssemblyName);
            createMaterial();
            wireProperties = new MaterialPropertyBlock();
            Camera.onPreRender += onCameraPreRender;

            Debug.Log("[SunkWorks] Sonar View initialized with " +
                (parallaxLoaded ? "Parallax" : "stock PQS") + " terrain support.");
        }

        /// <summary>Finds the enabled Sonar View belonging to the active vessel.</summary>
        public void Update()
        {
            if (Time.frameCount % kCachePruneInterval == 0)
                pruneWireMeshCache();

            WBISonarRanger previousSonar = activeSonar;
            activeSonar = null;
            activeVessel = FlightGlobals.ActiveVessel;
            activePqs = null;

            if (wireMaterial == null || activeVessel == null || MapView.MapIsEnabled ||
                !isVesselUnderwater(activeVessel))
                return;

            activePqs = activeVessel.mainBody != null
                ? activeVessel.mainBody.pqsController
                : null;
            if (activePqs == null || !activePqs.isActive)
                return;

            for (int partIndex = 0; partIndex < activeVessel.parts.Count; partIndex++)
            {
                Part vesselPart = activeVessel.parts[partIndex];
                for (int moduleIndex = 0; moduleIndex < vesselPart.Modules.Count; moduleIndex++)
                {
                    WBISonarRanger sonar = vesselPart.Modules[moduleIndex] as WBISonarRanger;
                    if (sonar != null && sonar.isEnabled && sonar.sonarViewActive)
                    {
                        activeSonar = sonar;
                        if (activeSonar != previousSonar)
                            successfulDrawReported = false;
                        return;
                    }
                }
            }
        }

        /// <summary>Unsubscribes and destroys resources when leaving flight.</summary>
        public void OnDestroy()
        {
            Camera.onPreRender -= onCameraPreRender;
            detachCommandBuffer();
            if (wireMaterial != null)
                Destroy(wireMaterial);

            foreach (WireMeshEntry entry in wireMeshCache.Values)
            {
                if (entry.wireMesh != null)
                    Destroy(entry.wireMesh);
            }
            wireMeshCache.Clear();
        }

        /// <summary>
        /// Attaches the final-camera command buffer after normal scene updates.
        /// Its contents are populated at render time, after KSP has finished moving
        /// recycled PQS tiles for the current floating-origin frame.
        /// </summary>
        public void LateUpdate()
        {
            if (activeSonar == null || activeVessel == null || activePqs == null ||
                MapView.MapIsEnabled || FlightCamera.fetch == null ||
                FlightCamera.fetch.mainCamera == null)
            {
                detachCommandBuffer();
                return;
            }

            Camera camera = FlightCamera.fetch.mainCamera;
            ensureCommandBuffer(camera);
        }

        void onCameraPreRender(Camera camera)
        {
            if (camera == null || camera != commandCamera || wireCommandBuffer == null ||
                activeSonar == null || activeVessel == null || activePqs == null)
                return;

            wireCommandBuffer.Clear();

            PQ[] quads = activePqs.quads;
            if (quads == null)
                return;

            Vector3 origin = activeVessel.ReferenceTransform != null
                ? activeVessel.ReferenceTransform.position
                : activeVessel.transform.position;
            float range = Mathf.Clamp(activeSonar.sonarViewRange, 100f,
                activeSonar.sonarViewMaxRange);
            float rangeSquared = range * range;
            float fadeStart = range * activeSonar.sonarViewFadeStart;
            float fadeSpan = Mathf.Max(kMinimumFadeSpan, range - fadeStart);
            Color baseColor = activeSonar.SonarViewColor;
            Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(camera);

            try
            {
                int drawnMeshes = 0;
                for (int index = 0; index < quads.Length; index++)
                    drawnMeshes += drawQuadTree(quads[index], camera, wireCommandBuffer,
                        frustumPlanes, origin, range, rangeSquared, fadeSpan, baseColor);

                if (drawnMeshes > 0 && !successfulDrawReported)
                {
                    successfulDrawReported = true;
                    Debug.Log("[SunkWorks] Sonar View rendered " + drawnMeshes +
                        " terrain mesh(es) through camera " + camera.name + ".");
                }
            }
            catch (Exception exception)
            {
                Debug.LogError("[SunkWorks] Sonar View render failure: " + exception);
                activeSonar = null;
            }
        }

        int drawQuadTree(PQ quad, Camera camera, CommandBuffer commandBuffer,
            Plane[] frustumPlanes, Vector3 origin, float range, float rangeSquared,
            float fadeSpan, Color baseColor)
        {
            if (quad == null || !quad.isActive)
                return 0;

            // PQS.quads contains only the six root faces of the planet. Walk the
            // active subdivision tree to reach the leaf meshes KSP actually renders.
            if (quad.isSubdivided && quad.subNodes != null)
            {
                int childDrawCount = 0;
                for (int childIndex = 0; childIndex < quad.subNodes.Length; childIndex++)
                    childDrawCount += drawQuadTree(quad.subNodes[childIndex], camera,
                        commandBuffer, frustumPlanes, origin, range, rangeSquared,
                        fadeSpan, baseColor);
                return childDrawCount;
            }

            if (!quad.isBuilt || !quad.isVisible)
                return 0;

            MeshFilter meshFilter;
            MeshRenderer meshRenderer;
            getRenderedTerrainMesh(quad, out meshFilter, out meshRenderer);
            if (meshFilter == null || meshFilter.sharedMesh == null ||
                meshRenderer == null || !meshRenderer.enabled)
                return 0;

            int layerMask = 1 << meshRenderer.gameObject.layer;
            if ((camera.cullingMask & layerMask) == 0 ||
                !GeometryUtility.TestPlanesAABB(frustumPlanes, meshRenderer.bounds))
                return 0;

            float distanceSquared = meshRenderer.bounds.SqrDistance(origin);
            if (distanceSquared > rangeSquared)
                return 0;

            float distance = Mathf.Sqrt(distanceSquared);
            Color fadedColor = baseColor;
            fadedColor.a *= Mathf.Clamp01((range - distance) / fadeSpan);
            if (fadedColor.a <= 0.001f)
                return 0;

            Mesh mesh = getWireMesh(meshFilter.sharedMesh);
            if (mesh == null)
                return 0;

            Matrix4x4 matrix = meshFilter.transform.localToWorldMatrix;
            wireProperties.Clear();
            wireProperties.SetColor("_Color", fadedColor);
            commandBuffer.DrawMesh(mesh, matrix, wireMaterial, 0, 0, wireProperties);

            return 1;
        }

        Mesh getWireMesh(Mesh sourceMesh)
        {
            if (sourceMesh == null || !sourceMesh.isReadable)
                return null;

            int sourceId = sourceMesh.GetInstanceID();
            int sourceIndexCount = getTotalIndexCount(sourceMesh);
            WireMeshEntry entry;
            if (wireMeshCache.TryGetValue(sourceId, out entry) &&
                entry.sourceMesh == sourceMesh &&
                entry.vertexCount == sourceMesh.vertexCount &&
                entry.indexCount == sourceIndexCount)
            {
                // A stable Mesh instance does not mean stable terrain vertices.
                // KSP moves and repopulates PQS tiles without replacing the object.
                entry.wireMesh.vertices = sourceMesh.vertices;
                entry.wireMesh.bounds = sourceMesh.bounds;
                entry.lastUsedFrame = Time.frameCount;
                return entry.wireMesh;
            }

            if (entry != null && entry.wireMesh != null)
                Destroy(entry.wireMesh);

            List<int> lineIndices = new List<int>(sourceIndexCount * 2);
            for (int subMesh = 0; subMesh < sourceMesh.subMeshCount; subMesh++)
            {
                int[] sourceIndices = sourceMesh.GetIndices(subMesh);
                MeshTopology topology = sourceMesh.GetTopology(subMesh);
                if (topology == MeshTopology.Triangles)
                {
                    for (int index = 0; index + 2 < sourceIndices.Length; index += 3)
                    {
                        int a = sourceIndices[index];
                        int b = sourceIndices[index + 1];
                        int c = sourceIndices[index + 2];
                        lineIndices.Add(a);
                        lineIndices.Add(b);
                        lineIndices.Add(b);
                        lineIndices.Add(c);
                        lineIndices.Add(c);
                        lineIndices.Add(a);
                    }
                }
                else if (topology == MeshTopology.Lines)
                {
                    lineIndices.AddRange(sourceIndices);
                }
            }

            if (lineIndices.Count == 0)
                return null;

            Mesh wireMesh = new Mesh();
            wireMesh.name = sourceMesh.name + " Sonar View Wireframe";
            wireMesh.hideFlags = HideFlags.HideAndDontSave;
            wireMesh.indexFormat = sourceMesh.indexFormat;
            wireMesh.vertices = sourceMesh.vertices;
            wireMesh.SetIndices(lineIndices.ToArray(), MeshTopology.Lines, 0, false);
            wireMesh.bounds = sourceMesh.bounds;
            // PQS and Parallax both recycle mesh instances and replace their vertex
            // positions as terrain tiles move. Keep this mesh writable so its live
            // positions can be refreshed before every camera render.
            wireMesh.UploadMeshData(false);

            entry = new WireMeshEntry
            {
                sourceMesh = sourceMesh,
                wireMesh = wireMesh,
                vertexCount = sourceMesh.vertexCount,
                indexCount = sourceIndexCount,
                lastUsedFrame = Time.frameCount
            };
            wireMeshCache[sourceId] = entry;
            return wireMesh;
        }

        static int getTotalIndexCount(Mesh mesh)
        {
            int indexCount = 0;
            for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                indexCount += (int)mesh.GetIndexCount(subMesh);
            return indexCount;
        }

        void pruneWireMeshCache()
        {
            staleWireMeshIds.Clear();
            foreach (KeyValuePair<int, WireMeshEntry> pair in wireMeshCache)
            {
                WireMeshEntry entry = pair.Value;
                if (entry.sourceMesh == null ||
                    Time.frameCount - entry.lastUsedFrame > kCacheRetainFrames)
                {
                    if (entry.wireMesh != null)
                        Destroy(entry.wireMesh);
                    staleWireMeshIds.Add(pair.Key);
                }
            }

            for (int index = 0; index < staleWireMeshIds.Count; index++)
                wireMeshCache.Remove(staleWireMeshIds[index]);
        }

        void ensureCommandBuffer(Camera camera)
        {
            if (commandCamera == camera && wireCommandBuffer != null)
                return;

            detachCommandBuffer();
            commandCamera = camera;
            wireCommandBuffer = new CommandBuffer();
            wireCommandBuffer.name = "SunkWorks Sonar View";
            commandCamera.AddCommandBuffer(CameraEvent.AfterEverything,
                wireCommandBuffer);
        }

        void detachCommandBuffer()
        {
            if (commandCamera != null && wireCommandBuffer != null)
                commandCamera.RemoveCommandBuffer(CameraEvent.AfterEverything,
                    wireCommandBuffer);
            if (wireCommandBuffer != null)
                wireCommandBuffer.Release();

            wireCommandBuffer = null;
            commandCamera = null;
        }

        void getRenderedTerrainMesh(PQ quad, out MeshFilter meshFilter,
            out MeshRenderer meshRenderer)
        {
            meshFilter = quad.meshFilter;
            meshRenderer = quad.meshRenderer;

            if (!parallaxLoaded)
                return;

            // Parallax parents its active, CPU-subdivided visual mesh directly to the
            // stock quad and makes the stock renderer transparent. Avoid reflection so
            // this remains compatible across Parallax releases.
            Transform quadTransform = quad.transform;
            for (int childIndex = 0; childIndex < quadTransform.childCount; childIndex++)
            {
                Transform child = quadTransform.GetChild(childIndex);
                if (!child.gameObject.activeInHierarchy)
                    continue;

                MeshFilter childFilter = child.GetComponent<MeshFilter>();
                MeshRenderer childRenderer = child.GetComponent<MeshRenderer>();
                if (childFilter != null && childFilter.sharedMesh != null &&
                    childRenderer != null && childRenderer.enabled)
                {
                    meshFilter = childFilter;
                    meshRenderer = childRenderer;
                    return;
                }
            }
        }

        void createMaterial()
        {
            Shader shader = Shader.Find(kInternalColorShader);
            if (shader == null)
                shader = Shader.Find(kFallbackColorShader);
            if (shader == null)
            {
                Debug.LogError("[SunkWorks] Sonar View could not find a wireframe shader.");
                return;
            }

            wireMaterial = new Material(shader);
            wireMaterial.hideFlags = HideFlags.HideAndDontSave;
            wireMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            wireMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            wireMaterial.SetInt("_Cull", (int)CullMode.Off);
            wireMaterial.SetInt("_ZWrite", 0);
            // Scatterer/EVE can replace or clear the camera depth target while the
            // underwater post-process runs. Sonar is deliberately an x-ray terrain
            // aid, so its final-pass lines must not depend on that transient depth.
            wireMaterial.SetInt("_ZTest", (int)CompareFunction.Always);
            wireMaterial.renderQueue = 5000;
        }

        static bool isVesselUnderwater(Vessel vessel)
        {
            return vessel.mainBody != null && vessel.mainBody.ocean &&
                vessel.ReferenceTransform != null &&
                vessel.mainBody.GetAltitude(vessel.ReferenceTransform.position) < 0.0;
        }

        static bool isAssemblyLoaded(string assemblyName)
        {
            for (int index = 0; index < AssemblyLoader.loadedAssemblies.Count; index++)
            {
                AssemblyLoader.LoadedAssembly loadedAssembly =
                    AssemblyLoader.loadedAssemblies[index];
                if (loadedAssembly != null && loadedAssembly.assembly != null &&
                    string.Equals(loadedAssembly.assembly.GetName().Name, assemblyName,
                        StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        class WireMeshEntry
        {
            public Mesh sourceMesh;
            public Mesh wireMesh;
            public int vertexCount;
            public int indexCount;
            public int lastUsedFrame;
        }
    }
}
