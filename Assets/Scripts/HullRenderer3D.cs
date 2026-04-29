using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class HullRenderer3D : MonoBehaviour
{
    public Material hullMaterial;
    public bool showWireframe = true;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh currentMesh;

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        if (hullMaterial != null) meshRenderer.material = hullMaterial;
    }

    public void DrawHull(List<Vector3> vertices, List<int> triangles)
    {
        ClearHull();

        currentMesh = new Mesh();
        currentMesh.name = "ConvexHull";

        currentMesh.vertices = vertices.ToArray();
        currentMesh.triangles = triangles.ToArray();

        currentMesh.RecalculateNormals();
        currentMesh.RecalculateBounds();

        meshFilter.mesh = currentMesh;
    }

    public void ClearHull()
    {
        if (currentMesh != null)
        {
            Destroy(currentMesh);
            currentMesh = null;
        }
        meshFilter.mesh = null;
    }

    public void ClearAll()
    {
        ClearHull();
    }

    void OnDrawGizmos()
    {
        if (!showWireframe) return;
        if (currentMesh == null) return;

        Gizmos.color = Color.cyan;
        var verts = currentMesh.vertices;
        var tris = currentMesh.triangles;

        for (var i = 0; i < tris.Length; i += 3)
        {
            var v0 = transform.TransformPoint(verts[tris[i]]);
            var v1 = transform.TransformPoint(verts[tris[i + 1]]);
            var v2 = transform.TransformPoint(verts[tris[i + 2]]);

            Gizmos.DrawLine(v0, v1);
            Gizmos.DrawLine(v1, v2);
            Gizmos.DrawLine(v2, v0);
        }
    }
}