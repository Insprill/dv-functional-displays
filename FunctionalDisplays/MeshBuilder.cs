using UnityEngine;

namespace FunctionalDisplays;

public static class MeshBuilder
{
    public static Mesh BuildQuad(float width, float height)
    {
        Mesh mesh = new();

        // Setup vertices
        float halfHeight = height * 0.5f;
        float halfWidth = width * 0.5f;
        Vector3[] vertices = {
            new(halfWidth, -halfHeight, 0),
            new(halfWidth, halfHeight, 0),
            new(-halfWidth, -halfHeight, 0),
            new(-halfWidth, halfHeight, 0)
        };

        // Setup UVs
        Vector2[] uvs = {
            new(0, 0),
            new(0, 1),
            new(1, 0),
            new(1, 1)
        };

        // Setup triangles
        int[] triangles = {
            0, 2, 1,
            1, 2, 3
        };

        // Setup normals
        Vector3[] normals = new Vector3[vertices.Length];
        for (int i = 0; i < normals.Length; i++)
            normals[i] = Vector3.forward;

        // Create quad
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.normals = normals;

        // Upload to GPU and free from CPU memory
        mesh.UploadMeshData(true);

        return mesh;
    }
}
