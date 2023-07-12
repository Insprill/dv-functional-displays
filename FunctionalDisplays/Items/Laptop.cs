using UnityEngine;
using UnityEngine.Rendering;

namespace FunctionalDisplays.Items;

public class Laptop : DisplayableItem
{
    private const float WIDTH = 0.3557966f;
    private const float HEIGHT = 0.2269155f;
    private static readonly Vector3 LOCAL_POSITION = new(0.0000000149f, 0.1396f, 0.1835f);

    private readonly Mesh mesh = MeshBuilder.BuildQuad(WIDTH, HEIGHT);

    protected override string PrefabName => "LaptopOpen";

    public override void Setup(Transform transform)
    {
        Vector3 rotation = transform.Find("LaptopCap").localEulerAngles;
        rotation.z = 180f; // Fix texture being upside down

        GameObject displayObject = new("Display") {
            layer = transform.gameObject.layer,
            transform = {
                parent = transform,
                localPosition = LOCAL_POSITION,
                localEulerAngles = rotation
            }
        };

        MeshFilter filter = displayObject.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;

        MeshRenderer renderer = displayObject.AddComponent<MeshRenderer>();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.sharedMaterial = ScreenUpdater.GetMaterial();

        LODGroup lodGroup = transform.GetComponent<LODGroup>();
        LOD[] lods = lodGroup.GetLODs();
        for (int i = 0; i < lods.Length; i++)
        {
            Renderer[] renderers = lods[i].renderers;
            Renderer[] newRenderers = new Renderer[renderers.Length + 1];
            for (int j = 0; j < renderers.Length; j++)
                newRenderers[j] = renderers[j];
            newRenderers[renderers.Length] = renderer;
            lods[i].renderers = newRenderers;
        }

        lodGroup.SetLODs(lods);
    }
}
