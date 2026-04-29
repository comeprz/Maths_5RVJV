using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class HullRenderer3D : MonoBehaviour
{
    public Material hullMaterial;

    [Header("Wireframe visible en Game View")]
    public bool showWireframe = true;
    public Material wireMaterial;
    public float wireWidth = 0.025f;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh currentMesh;

    private readonly List<GameObject> wireObjects = new List<GameObject>();

    private struct Edge
    {
        public int a;
        public int b;

        public Edge(int i, int j)
        {
            if (i < j)
            {
                a = i;
                b = j;
            }
            else
            {
                a = j;
                b = i;
            }
        }
    }

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        if (hullMaterial != null)
            meshRenderer.material = hullMaterial;
    }

    public void DrawHull(List<Vector3> vertices, List<int> triangles)
    {
        ClearHull();

        if (vertices == null || vertices.Count == 0 || triangles == null || triangles.Count < 3)
            return;

        currentMesh = new Mesh();
        currentMesh.name = "ConvexHull3D";

        if (vertices.Count > 65000)
            currentMesh.indexFormat = IndexFormat.UInt32;

        Vector3[] localVertices = new Vector3[vertices.Count];

        for (int i = 0; i < vertices.Count; i++)
        {
            localVertices[i] = transform.InverseTransformPoint(vertices[i]);
        }

        currentMesh.vertices = localVertices;
        currentMesh.triangles = triangles.ToArray();

        currentMesh.RecalculateNormals();
        currentMesh.RecalculateBounds();

        meshFilter.mesh = currentMesh;

        if (showWireframe)
            DrawWireframe(vertices, triangles);
    }

    void DrawWireframe(List<Vector3> vertices, List<int> triangles)
    {
        HashSet<Edge> uniqueEdges = new HashSet<Edge>();

        for (int i = 0; i < triangles.Count; i += 3)
        {
            int i0 = triangles[i];
            int i1 = triangles[i + 1];
            int i2 = triangles[i + 2];

            uniqueEdges.Add(new Edge(i0, i1));
            uniqueEdges.Add(new Edge(i1, i2));
            uniqueEdges.Add(new Edge(i2, i0));
        }

        foreach (Edge edge in uniqueEdges)
        {
            CreateWireLine(vertices[edge.a], vertices[edge.b]);
        }
    }

    void CreateWireLine(Vector3 a, Vector3 b)
    {
        GameObject lineObject = new GameObject("Hull3D_WireEdge");
        lineObject.transform.SetParent(transform);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();

        line.useWorldSpace = true;
        line.positionCount = 2;

        line.startWidth = wireWidth;
        line.endWidth = wireWidth;

        line.numCapVertices = 2;

        if (wireMaterial != null)
            line.material = wireMaterial;
        else if (hullMaterial != null)
            line.material = hullMaterial;

        line.SetPosition(0, a);
        line.SetPosition(1, b);

        wireObjects.Add(lineObject);
    }

    public void ClearHull()
    {
        ClearWireframe();

        if (currentMesh != null)
        {
            Destroy(currentMesh);
            currentMesh = null;
        }

        if (meshFilter != null)
            meshFilter.mesh = null;
    }

    void ClearWireframe()
    {
        foreach (GameObject obj in wireObjects)
        {
            if (obj != null)
                Destroy(obj);
        }

        wireObjects.Clear();
    }

    public void ClearAll()
    {
        ClearHull();
    }
}