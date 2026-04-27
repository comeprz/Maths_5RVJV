using System.Collections.Generic;
using UnityEngine;

public class HullRenderer : MonoBehaviour
{
    [Header("Hull")]
    public LineRenderer lineRenderer;

    [Header("Edges rendering")]
    public Material edgeMaterial;
    public float edgeWidth = 0.04f;
    public float triangulationZOffset = -0.05f;

    private readonly List<GameObject> edgeObjects = new List<GameObject>();

    public void DrawHull(List<Vector2> hull)
    {
        ClearEdges();

        if (hull == null || hull.Count < 2)
        {
            lineRenderer.positionCount = 0;
            return;
        }

        lineRenderer.positionCount = hull.Count + 1;

        for (int i = 0; i < hull.Count; i++)
        {
            lineRenderer.SetPosition(i, new Vector3(hull[i].x, hull[i].y, 0f));
        }

        Vector2 firstPoint = hull[0];
        lineRenderer.SetPosition(hull.Count, new Vector3(firstPoint.x, firstPoint.y, 0f));
    }

    public void DrawEdges(List<(Vector2, Vector2)> edges)
    {
        ClearHull();
        ClearEdges();

        foreach (var (a, b) in edges)
        {
            GameObject edgeObj = new GameObject("TriangulationEdge");
            edgeObj.transform.SetParent(transform);

            LineRenderer edgeLine = edgeObj.AddComponent<LineRenderer>();

            edgeLine.useWorldSpace = true;
            edgeLine.positionCount = 2;

            edgeLine.startWidth = edgeWidth;
            edgeLine.endWidth = edgeWidth;

            edgeLine.material = edgeMaterial != null ? edgeMaterial : lineRenderer.material;

            edgeLine.SetPosition(0, new Vector3(a.x, a.y, triangulationZOffset));
            edgeLine.SetPosition(1, new Vector3(b.x, b.y, triangulationZOffset));

            edgeObjects.Add(edgeObj);
        }

        Debug.Log($"Affichage triangulation : {edges.Count} arêtes dessinées");
    }

    public void ClearHull()
    {
        lineRenderer.positionCount = 0;
    }

    public void ClearEdges()
    {
        foreach (GameObject obj in edgeObjects)
        {
            Destroy(obj);
        }

        edgeObjects.Clear();
    }

    public void ClearAll()
    {
        ClearHull();
        ClearEdges();
    }
}