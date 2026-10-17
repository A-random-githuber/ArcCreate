using System.Collections.Generic;
using ArcCreate.Gameplay.Utility;
using UnityEngine;

namespace ArcCreate.Gameplay.Render
{
    public class RenderService : MonoBehaviour, IRenderService
    {
        [SerializeField] private Camera notesCamera;
        [SerializeField] private int layer;

        [Header("Meshes")]
        [SerializeField] private Mesh arctapMesh;
        [SerializeField] private Mesh arctapSfxMesh;
        [SerializeField] private Mesh arctapShadowMesh;
        [SerializeField] private Mesh tapMesh;
        [SerializeField] private Mesh holdMesh;
        [SerializeField] private Mesh connectionLineMesh;
        [SerializeField] private Mesh heightIndicatorMesh;
        [SerializeField] private Mesh arcCapMesh;
        [SerializeField] private Mesh arcCapControllerMesh;

        [Header("Materials")]
        [SerializeField] private Material baseArctapMaterial;
        [SerializeField] private Material baseTapMaterial;
        [SerializeField] private Material baseHoldMaterial;
        [SerializeField] private Material baseArcCapMaterial;
        [SerializeField] private Material arctapShadowMaterial;
        [SerializeField] private Material heightIndicatorMaterial;
        [SerializeField] private Material connectionLineMaterial;

        private static readonly int ColorShaderId = Shader.PropertyToID("_Color");
        private static readonly int PropertiesShaderId = Shader.PropertyToID("_Properties");
        private static readonly int TraceColorShaderId = Shader.PropertyToID("_TraceColorOverride");
        private readonly List<Material> generatedMaterials = new List<Material>();

        // Floor
        private readonly Dictionary<Texture, InstancedRendererPool> tapDrawers = new Dictionary<Texture, InstancedRendererPool>();
        private readonly Dictionary<Texture, InstancedRendererPool> holdDrawers = new Dictionary<Texture, InstancedRendererPool>();
        private InstancedRendererPool connectionLineDrawer;

        // Arc & trace
        private readonly List<ArcBodySegmentRenderer> arcSegmentRenderers = new List<ArcBodySegmentRenderer>();
        private readonly List<ArcBodySegmentRenderer> traceSegmentRenderers = new List<ArcBodySegmentRenderer>();
        private int activeArcSegmentRenderers;
        private int activeTraceSegmentRenderers;
        private Material arcBodyMaterial;
        private Material traceBodyMaterial;
        private InstancedRendererPool arcHeadDrawer;
        private readonly Dictionary<Texture, InstancedRendererPool> arcCapDrawers = new Dictionary<Texture, InstancedRendererPool>();
        private InstancedRendererPool arcShadowDrawer;
        private InstancedRendererPool traceHeadDrawer;
        private InstancedRendererPool traceShadowDrawer;
        private InstancedRendererPool heightIndicatorDrawer;

        // Arctap
        private readonly List<ArcTapDrawCall> queuedArcTapDrawCalls = new List<ArcTapDrawCall>();
        private readonly IComparer<ArcTapDrawCall> arcTapDrawCallComparer = new ArcTapDrawCallComparer();
        private readonly Dictionary<Texture, InstancedRendererPool> arctapDrawers = new Dictionary<Texture, InstancedRendererPool>();
        private InstancedRendererPool arctapShadowDrawer;

        public bool IsLoaded { get; private set; }

        public Mesh TapMesh => tapMesh;

        public Mesh HoldMesh => holdMesh;

        public Mesh ArcTapMesh => arctapMesh;

        public void DrawTap(Texture texture, Matrix4x4 matrix, Color color, bool selected)
        {
            if (!tapDrawers.ContainsKey(texture))
            {
                Material newTap = Instantiate(baseTapMaterial);
                newTap.mainTexture = texture;
                generatedMaterials.Add(newTap);
                tapDrawers.Add(texture, new InstancedRendererPool(
                    newTap,
                    tapMesh,
                    true));
            }

            tapDrawers[texture].RegisterInstance(matrix, color, new Vector4(selected ? 1 : 0, 0, 0, 0));
        }

        public void DrawHold(Texture texture, Matrix4x4 matrix, Color color, bool selected, float from, bool highlight)
        {
            if (!holdDrawers.ContainsKey(texture))
            {
                Material newHold = Instantiate(baseHoldMaterial);
                newHold.mainTexture = texture;
                generatedMaterials.Add(newHold);
                holdDrawers.Add(texture, new InstancedRendererPool(
                    newHold,
                    holdMesh,
                    true));
            }

            holdDrawers[texture].RegisterInstance(matrix, color, new Vector4(selected ? 1 : 0, from, highlight ? 1 : 0, 0));
        }

        public void DrawConnectionLine(Matrix4x4 matrix, Color color)
        {
            connectionLineDrawer.RegisterInstance(matrix, color);
        }

        public void DrawArcSegment(int colorId, bool highlight, Matrix4x4 matrix, Color color, bool selected, float redValue, float y, float depth)
        {
            (Color high, Color low) = Services.Skin.GetArcColor(colorId);
            color *= Color.Lerp(Color.Lerp(low, high, (y - 1) / 4.5f), Color.red, redValue);
            Vector4 properties = new Vector4(selected ? 1 : 0, highlight ? 1 : 0, 0, 0);
            ArcBodySegmentRenderer renderer = GetArcBodySegmentRenderer(false);
            renderer.Update(matrix, color, properties, Color.clear, colorId, y);
        }

        public void DrawTraceSegment(int colorId, Matrix4x4 matrix, Color color, Color traceColor, bool selected, float y, float depth)
        {
            Vector4 properties = new Vector4(selected ? 1 : 0, 0, 0, 0);
            ArcBodySegmentRenderer renderer = GetArcBodySegmentRenderer(true);
            renderer.Update(matrix, color, properties, traceColor, colorId, y);
        }

        public void DrawArcShadow(Matrix4x4 matrix, Color color, Vector4 cornerOffset)
        {
            arcShadowDrawer.RegisterInstance(matrix, color, cornerOffset);
        }

        public void DrawTraceShadow(Matrix4x4 matrix, Color color)
        {
            traceShadowDrawer.RegisterInstance(matrix, color);
        }

        public void DrawArcHead(int colorId, bool highlight, Matrix4x4 matrix, Color color, bool selected, float redValue, float y)
        {
            (Color high, Color low) = Services.Skin.GetArcColor(colorId);
            color *= Color.Lerp(Color.Lerp(low, high, (y - 1) / 4.5f), Color.red, redValue);
            Vector4 properties = new Vector4(selected ? 1 : 0, highlight ? 1 : 0, 0, 0);
            arcHeadDrawer.RegisterInstance(matrix, color, properties);
        }

        public void DrawTraceHead(Matrix4x4 matrix, Color color, Color traceColor, bool selected)
        {
            traceHeadDrawer.RegisterInstance(matrix, color, new Vector4(selected ? 1 : 0, 0, 0, 0), traceColor);
        }

        public void DrawArcCap(Texture texture, Matrix4x4 matrix, Color color, bool isController)
        {
            if (!arcCapDrawers.ContainsKey(texture))
            {
                Material newArcCap = Instantiate(baseArcCapMaterial);
                newArcCap.mainTexture = texture;
                generatedMaterials.Add(newArcCap);
                arcCapDrawers.Add(texture, new InstancedRendererPool(
                    newArcCap,
                    isController ? arcCapControllerMesh : arcCapMesh,
                    false));
            }

            arcCapDrawers[texture].RegisterInstance(matrix, color);
        }

        public void DrawHeightIndicator(Matrix4x4 matrix, Color color)
        {
            heightIndicatorDrawer.RegisterInstance(matrix, color);
        }

        public void DrawArcTap(bool sfx, Texture texture, Matrix4x4 matrix, Color color, bool selected, float depth)
        {
            queuedArcTapDrawCalls.Add(new ArcTapDrawCall
            {
                IsSfx = sfx,
                Texture = texture,
                Matrix = matrix,
                Color = color,
                Properties = new Vector4(selected ? 1 : 0, 0, 0, 0),
                Depth = depth
            });
        }

        public void DrawArcTapShadow(Matrix4x4 matrix, Color color)
        {
            arctapShadowDrawer.RegisterInstance(matrix, color);
        }

        public void UpdateRenderers()
        {
            FinishArcBodyFrame();
            if (!notesCamera.enabled || !notesCamera.gameObject.activeInHierarchy)
            {
                return;
            }

            foreach (var pair in holdDrawers)
            {
                pair.Value.Draw(notesCamera, layer);
            }

            connectionLineDrawer.Draw(notesCamera, layer);
            foreach (var pair in tapDrawers)
            {
                pair.Value.Draw(notesCamera, layer);
            }

            traceShadowDrawer.Draw(notesCamera, layer);
            arcShadowDrawer.Draw(notesCamera, layer);

            traceHeadDrawer.Draw(notesCamera, layer);
            arctapShadowDrawer.Draw(notesCamera, layer);

            heightIndicatorDrawer.Draw(notesCamera, layer);
            foreach (var pair in arcCapDrawers)
            {
                pair.Value.Draw(notesCamera, layer);
            }

            #region ArcTap

            queuedArcTapDrawCalls.Sort(arcTapDrawCallComparer);
            foreach (var call in queuedArcTapDrawCalls)
            {
                if (!arctapDrawers.ContainsKey(call.Texture))
                {
                    Material newArctap = Instantiate(baseArctapMaterial);
                    newArctap.mainTexture = call.Texture;
                    generatedMaterials.Add(newArctap);
                    arctapDrawers.Add(call.Texture, new InstancedRendererPool(
                        newArctap,
                        call.IsSfx ? arctapSfxMesh : arctapMesh,
                        true));
                }

                arctapDrawers[call.Texture].RegisterInstance(call.Matrix, call.Color, call.Properties);
                arctapDrawers[call.Texture].Draw(notesCamera, layer);
            }

            queuedArcTapDrawCalls.Clear();

            #endregion

            arcHeadDrawer.Draw(notesCamera, layer);
        }

        public void SetTraceMaterial(Material material)
        {
            DisposeArcBodySegmentRenderers(traceSegmentRenderers);
            activeTraceSegmentRenderers = 0;
            traceBodyMaterial = material;

            traceHeadDrawer?.Dispose();
            traceHeadDrawer = new InstancedRendererPool(
                material,
                ArcMeshGenerator.GetHeadMesh(true),
                true,
                true);

            UpdateLoadedState();
        }

        public void SetShadowMaterial(Material material)
        {
            arcShadowDrawer?.Dispose();
            arcShadowDrawer = new InstancedRendererPool(
                material,
                ArcMeshGenerator.GetShadowMesh(false),
                true);

            traceShadowDrawer?.Dispose();
            traceShadowDrawer = new InstancedRendererPool(
                material,
                ArcMeshGenerator.GetShadowMesh(true),
                false);

            UpdateLoadedState();
        }

        public void SetArcMaterial(Material material)
        {
            DisposeArcBodySegmentRenderers(arcSegmentRenderers);
            activeArcSegmentRenderers = 0;
            arcBodyMaterial = material;

            arcHeadDrawer?.Dispose();
            arcHeadDrawer = new InstancedRendererPool(
                material,
                ArcMeshGenerator.GetHeadMesh(false),
                true);

            UpdateLoadedState();
        }

        public void SetTextures(Texture heightIndicator, Texture arctapShadow)
        {
            heightIndicatorDrawer?.Dispose();
            arctapShadowDrawer?.Dispose();

            Material height = Instantiate(heightIndicatorMaterial);
            height.mainTexture = heightIndicator;
            generatedMaterials.Add(height);
            heightIndicatorDrawer = new InstancedRendererPool(
                height,
                heightIndicatorMesh,
                false);

            Material shadow = Instantiate(arctapShadowMaterial);
            shadow.mainTexture = arctapShadow;
            generatedMaterials.Add(shadow);
            arctapShadowDrawer = new InstancedRendererPool(
                shadow,
                arctapShadowMesh,
                false);

            UpdateLoadedState();
        }

        private ArcBodySegmentRenderer GetArcBodySegmentRenderer(bool isTrace)
        {
            List<ArcBodySegmentRenderer> renderers = isTrace ? traceSegmentRenderers : arcSegmentRenderers;
            int index = isTrace ? activeTraceSegmentRenderers++ : activeArcSegmentRenderers++;
            if (index >= renderers.Count)
            {
                Material material = isTrace ? traceBodyMaterial : arcBodyMaterial;
                float offset = isTrace ? Values.TraceMeshOffset : Values.ArcMeshOffset;
                renderers.Add(new ArcBodySegmentRenderer(transform, layer, material, offset, isTrace));
            }

            return renderers[index];
        }

        private void FinishArcBodyFrame()
        {
            DisableUnusedArcBodySegmentRenderers(arcSegmentRenderers, activeArcSegmentRenderers);
            DisableUnusedArcBodySegmentRenderers(traceSegmentRenderers, activeTraceSegmentRenderers);
            activeArcSegmentRenderers = 0;
            activeTraceSegmentRenderers = 0;
        }

        private static void DisableUnusedArcBodySegmentRenderers(List<ArcBodySegmentRenderer> renderers, int activeCount)
        {
            for (int i = activeCount; i < renderers.Count; i++)
            {
                renderers[i].Disable();
            }
        }

        private static void DisposeArcBodySegmentRenderers(List<ArcBodySegmentRenderer> renderers)
        {
            for (int i = 0; i < renderers.Count; i++)
            {
                renderers[i].Dispose();
            }

            renderers.Clear();
        }

        private void UpdateLoadedState()
        {
            IsLoaded = connectionLineDrawer != null
                    && arcBodyMaterial != null
                    && arcHeadDrawer != null
                    && traceBodyMaterial != null
                    && traceHeadDrawer != null
                    && arcShadowDrawer != null
                    && traceShadowDrawer != null
                    && connectionLineDrawer != null
                    && heightIndicatorDrawer != null
                    && arctapShadowDrawer != null;
        }

        private void Awake()
        {
            Shader.WarmupAllShaders();
            connectionLineDrawer = new InstancedRendererPool(
                connectionLineMaterial,
                connectionLineMesh,
                false);

            UpdateLoadedState();
        }

        private void OnDestroy()
        {
            foreach (Material material in generatedMaterials)
            {
                Destroy(material);
            }

            generatedMaterials.Clear();

            foreach (var pair in holdDrawers)
            {
                pair.Value.Dispose();
            }

            connectionLineDrawer.Dispose();
            foreach (var pair in tapDrawers)
            {
                pair.Value.Dispose();
            }

            traceShadowDrawer.Dispose();
            arcShadowDrawer.Dispose();

            DisposeArcBodySegmentRenderers(traceSegmentRenderers);
            traceHeadDrawer.Dispose();
            arctapShadowDrawer.Dispose();

            heightIndicatorDrawer.Dispose();
            foreach (var pair in arcCapDrawers)
            {
                pair.Value.Dispose();
            }

            DisposeArcBodySegmentRenderers(arcSegmentRenderers);

            foreach (var pair in arctapDrawers)
            {
                pair.Value.Dispose();
            }

            arcHeadDrawer.Dispose();
        }

        private sealed class ArcBodySegmentRenderer
        {
            private static readonly int[] LeftTriangles = { 0, 3, 2, 0, 2, 1 };
            private static readonly int[] RightTriangles = { 0, 1, 4, 0, 4, 5 };
            private static readonly Vector2[] Uv =
            {
                new Vector2(0, 0),
                new Vector2(0, 1),
                new Vector2(1, 1),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(1, 0),
            };

            private readonly GameObject gameObject;
            private readonly Mesh mesh;
            private readonly MeshRenderer meshRenderer;
            private readonly Material leftMaterial;
            private readonly Material rightMaterial;
            private readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            private readonly Vector3[] sourceVertices;
            private readonly Vector3[] vertices = new Vector3[6];
            private readonly Transform parent;

            public ArcBodySegmentRenderer(Transform parent, int layer, Material material, float offset, bool isTrace)
            {
                this.parent = parent;
                gameObject = new GameObject(isTrace ? "Trace body segment" : "Arc body segment");
                gameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
                gameObject.layer = layer;
                gameObject.transform.SetParent(parent, false);

                float offsetHalf = offset / 2;
                sourceVertices = new Vector3[]
                {
                    new Vector3(0, offsetHalf, 0),
                    new Vector3(0, offsetHalf, 1),
                    new Vector3(offset, -offsetHalf, 1),
                    new Vector3(offset, -offsetHalf, 0),
                    new Vector3(-offset, -offsetHalf, 1),
                    new Vector3(-offset, -offsetHalf, 0),
                };

                mesh = new Mesh
                {
                    name = isTrace ? "Trace body segment" : "Arc body segment",
                };
                mesh.MarkDynamic();
                mesh.vertices = vertices;
                mesh.uv = Uv;
                mesh.subMeshCount = 2;
                mesh.SetTriangles(LeftTriangles, 0, false);
                mesh.SetTriangles(RightTriangles, 1, false);
                mesh.RecalculateBounds();

                MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = mesh;
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
                leftMaterial = new Material(material);
                rightMaterial = new Material(material);
                meshRenderer.sharedMaterials = new Material[] { leftMaterial, rightMaterial };
            }

            public void Update(Matrix4x4 matrix, Color color, Vector4 properties, Color traceColor, int colorId, float endY)
            {
                Matrix4x4 localMatrix = parent.worldToLocalMatrix * matrix;
                for (int i = 0; i < vertices.Length; i++)
                {
                    vertices[i] = localMatrix.MultiplyPoint3x4(sourceVertices[i]);
                }

                mesh.vertices = vertices;
                mesh.RecalculateBounds();

                int renderQueue = 1002999 + (3 * colorId) + (4 * (int)(endY * 100));
                leftMaterial.renderQueue = renderQueue;
                rightMaterial.renderQueue = renderQueue + 500;

                propertyBlock.Clear();
                propertyBlock.SetColor(ColorShaderId, color);
                propertyBlock.SetVector(PropertiesShaderId, properties);
                propertyBlock.SetColor(TraceColorShaderId, traceColor);
                meshRenderer.SetPropertyBlock(propertyBlock, 0);
                meshRenderer.SetPropertyBlock(propertyBlock, 1);
                meshRenderer.enabled = true;
            }

            public void Disable()
            {
                meshRenderer.enabled = false;
            }

            public void Dispose()
            {
                Object.Destroy(leftMaterial);
                Object.Destroy(rightMaterial);
                Object.Destroy(mesh);
                Object.Destroy(gameObject);
            }
        }
    }
}
