﻿using UnityEngine;

namespace FunctionalDisplays.Items;

public class Laptop : DisplayableItem
{
    private const float WIDTH = 0.3575f;
    private const float HEIGHT = 0.2275f;
    private static readonly Vector3 LOCAL_POSITION = new(0f, 0.14f, 0.1827f);
    private static readonly Quaternion LOCAL_ROTATION = Quaternion.Euler(new Vector3(18.637f, 0f, 180f));

    private readonly Mesh mesh = MeshBuilder.BuildQuad(WIDTH, HEIGHT);

    protected override string PrefabName => "LaptopOpen";

    public override void Setup(Transform transform)
    {
        GameObject displayObject = new("Display") {
            transform = {
                parent = transform,
                localPosition = LOCAL_POSITION,
                localRotation = LOCAL_ROTATION
            }
        };

        MeshFilter filter = displayObject.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;

        MeshRenderer renderer = displayObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = ScreenUpdater.GetMaterial();
    }
}
