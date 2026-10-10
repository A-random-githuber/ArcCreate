using UnityEngine;

public class InstancedRenderer
{
    public const int Population = 511;
    public static readonly int ColorShaderId = Shader.PropertyToID("_Color");
    public static readonly int PropertyShaderId = Shader.PropertyToID("_Properties");
    public static readonly int TraceColorShaderId = Shader.PropertyToID("_TraceColorOverride");

    private readonly Material material;
    private readonly Mesh mesh;
    private readonly Matrix4x4[] matrices = new Matrix4x4[Population];
    private readonly Vector4[] colors = new Vector4[Population];
    private readonly Vector4[] properties = new Vector4[Population];
    private readonly Vector4[] traceColors;
    private readonly bool useProperties;
    private readonly bool useTraceColors;
    private readonly MaterialPropertyBlock mpb;

    private int count = 0;

    public InstancedRenderer(Material material, Mesh mesh, bool useProperties, bool useTraceColors)
    {
        this.material = material;
        this.mesh = mesh;
        this.useProperties = useProperties;
        this.useTraceColors = useTraceColors;
        traceColors = useTraceColors ? new Vector4[Population] : null;
        mpb = new MaterialPropertyBlock();
    }

    public bool RegisterInstance(Matrix4x4 matrix, Color color, Vector4 property = default, Color traceColor = default)
    {
        if (count >= Population)
        {
            return false;
        }

        matrices[count] = matrix;
        colors[count] = color;
        properties[count] = property;
        if (useTraceColors)
        {
            traceColors[count] = traceColor;
        }
        count++;
        return true;
    }

    public void Draw(Camera camera, LayerMask layerMask)
    {
        if (count > 0)
        {
            mpb.SetVectorArray(ColorShaderId, colors);
            if (useProperties)
            {
                mpb.SetVectorArray(PropertyShaderId, properties);
            }

            if (useTraceColors)
            {
                mpb.SetVectorArray(TraceColorShaderId, traceColors);
            }

            Graphics.DrawMeshInstanced(
                mesh: mesh,
                submeshIndex: 0,
                material: material,
                matrices: matrices,
                count: count,
                properties: mpb,
                castShadows: UnityEngine.Rendering.ShadowCastingMode.Off,
                receiveShadows: false,
                layer: layerMask,
                camera: camera);
        }

        count = 0;
    }
}