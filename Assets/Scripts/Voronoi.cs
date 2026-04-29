using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Voronoi : MonoBehaviour
{
    [SerializeField]
    public List<Vector2> arrayPoint = new List<Vector2>();

    public List<Vector2> voronoiVertices = new List<Vector2>();
    public List<(Vector2, Vector2)> voronoiEdges = new List<(Vector2, Vector2)>();
    public List<VoronoiCell> voronoiCells = new List<VoronoiCell>();

    public bool isDegenerate = false;
    public List<(Vector2 origin, Vector2 direction)> degenerateLines = new List<(Vector2, Vector2)>();

    private Delaunay delaunay;
    private Dictionary<int, Vector2> circumcenters = new Dictionary<int, Vector2>();
    private Dictionary<(Vector2, Vector2), List<int>> edgeToTriangles =
        new Dictionary<(Vector2, Vector2), List<int>>();

    private const float EPSILON = 1e-5f;

    void Start()
    {
    }

    public void RunFromPoints(List<Vector2> points)
    {
        arrayPoint = new List<Vector2>(points);
        Run();
    }

    public void Run()
    {
        voronoiVertices.Clear();
        voronoiEdges.Clear();
        voronoiCells.Clear();
        degenerateLines.Clear();
        circumcenters.Clear();
        edgeToTriangles.Clear();
        isDegenerate = false;

        if (arrayPoint.Count < 2)
        {
            Debug.LogWarning("Il faut au moins 2 points pour un diagramme de Voronoï.");
            return;
        }

        delaunay = GetComponent<Delaunay>();
        if (delaunay == null)
            delaunay = gameObject.AddComponent<Delaunay>();

        delaunay.RunDelaunayFromPoints(arrayPoint);

        if (delaunay.triangles.Count == 0)
        {
            isDegenerate = true;
            BuildDegenerateVoronoi();
            return;
        }

        ComputeCircumcenters();
        BuildEdgeToTrianglesMap();
        List<DualEdge> dualEdges = BuildDualEdges();
        BuildVoronoiCells(dualEdges);
    }

    void BuildDegenerateVoronoi()
    {
        List<Vector2> sorted = delaunay.sortedPoints;

        for (int i = 0; i < sorted.Count - 1; i++)
        {
            Vector2 a = sorted[i];
            Vector2 b = sorted[i + 1];

            Vector2 mid = (a + b) * 0.5f;
            Vector2 edgeDir = (b - a).normalized;
            Vector2 perpDir = new Vector2(-edgeDir.y, edgeDir.x);

            degenerateLines.Add((mid, perpDir));

            float extent = ComputeExtent() * 2f;
            voronoiEdges.Add((mid - perpDir * extent, mid + perpDir * extent));
        }
    }

    void ComputeCircumcenters()
    {
        for (int i = 0; i < delaunay.triangles.Count; i++)
        {
            var tri = delaunay.triangles[i];
            Vector2 center;
            float radiusSq;

            if (TryGetCircumcircle(tri.Item1, tri.Item2, tri.Item3, out center, out radiusSq))
            {
                circumcenters[i] = center;

                if (!ContainsPoint(voronoiVertices, center))
                    voronoiVertices.Add(center);
            }
        }
    }

    void BuildEdgeToTrianglesMap()
    {
        for (int i = 0; i < delaunay.triangles.Count; i++)
        {
            var tri = delaunay.triangles[i];
            RegisterEdge(tri.Item1, tri.Item2, i);
            RegisterEdge(tri.Item2, tri.Item3, i);
            RegisterEdge(tri.Item3, tri.Item1, i);
        }
    }

    void RegisterEdge(Vector2 a, Vector2 b, int triangleIndex)
    {
        var key = NormalizeEdgeKey(a, b);

        if (!edgeToTriangles.ContainsKey(key))
            edgeToTriangles[key] = new List<int>();

        edgeToTriangles[key].Add(triangleIndex);
    }

    List<DualEdge> BuildDualEdges()
    {
        List<DualEdge> dualEdges = new List<DualEdge>();

        foreach (var kvp in edgeToTriangles)
        {
            Vector2 a = kvp.Key.Item1;
            Vector2 b = kvp.Key.Item2;
            List<int> triIndices = kvp.Value;

            Vector2 start, end;

            if (triIndices.Count == 2)
            {
                if (!circumcenters.ContainsKey(triIndices[0]) ||
                    !circumcenters.ContainsKey(triIndices[1]))
                    continue;

                start = circumcenters[triIndices[0]];
                end = circumcenters[triIndices[1]];
            }
            else if (triIndices.Count == 1)
            {
                if (!circumcenters.ContainsKey(triIndices[0]))
                    continue;

                Vector2 center = circumcenters[triIndices[0]];
                Vector2 mid = (a + b) * 0.5f;

                var tri = delaunay.triangles[triIndices[0]];
                Vector2 opposite = GetOppositeVertex(tri, a, b);

                Vector2 edgeDir = (b - a).normalized;
                Vector2 perp = new Vector2(-edgeDir.y, edgeDir.x);

                if (Vector2.Dot(perp, opposite - mid) > 0f)
                    perp = -perp;

                float rayLength = ComputeExtent() * 2f;

                start = center;
                end = center + perp * rayLength;
            }
            else
            {
                continue;
            }

            voronoiEdges.Add((start, end));
            dualEdges.Add(new DualEdge(a, b, start, end));
        }

        return dualEdges;
    }

    void BuildVoronoiCells(List<DualEdge> dualEdges)
    {
        foreach (Vector2 site in arrayPoint)
        {
            List<(Vector2, Vector2)> cellEdges = new List<(Vector2, Vector2)>();

            foreach (DualEdge de in dualEdges)
            {
                if (SamePoint(de.siteA, site) || SamePoint(de.siteB, site))
                {
                    cellEdges.Add((de.voronoiStart, de.voronoiEnd));
                }
            }

            if (cellEdges.Count == 0)
                continue;

            cellEdges = cellEdges
                .OrderBy(e =>
                {
                    Vector2 mid = (e.Item1 + e.Item2) * 0.5f;
                    return Mathf.Atan2(mid.y - site.y, mid.x - site.x);
                })
                .ToList();

            List<Vector2> cellVertices = ExtractCellVertices(cellEdges);
            voronoiCells.Add(new VoronoiCell(site, cellEdges, cellVertices));
        }
    }

    List<Vector2> ExtractCellVertices(List<(Vector2, Vector2)> sortedEdges)
    {
        List<Vector2> vertices = new List<Vector2>();

        foreach (var edge in sortedEdges)
        {
            if (!ContainsPoint(vertices, edge.Item1))
                vertices.Add(edge.Item1);
            if (!ContainsPoint(vertices, edge.Item2))
                vertices.Add(edge.Item2);
        }

        if (vertices.Count > 0)
        {
            Vector2 center = Vector2.zero;
            foreach (var v in vertices) center += v;
            center /= vertices.Count;

            vertices = vertices
                .OrderBy(v => Mathf.Atan2(v.y - center.y, v.x - center.x))
                .ToList();
        }

        return vertices;
    }

    (Vector2, Vector2) NormalizeEdgeKey(Vector2 a, Vector2 b)
    {
        if (a.x < b.x || (Mathf.Abs(a.x - b.x) < EPSILON && a.y < b.y))
            return (a, b);
        return (b, a);
    }

    float ComputeExtent()
    {
        if (arrayPoint.Count == 0) return 100f;

        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach (var p in arrayPoint)
        {
            if (p.x < minX) minX = p.x;
            if (p.x > maxX) maxX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.y > maxY) maxY = p.y;
        }

        return Mathf.Max(maxX - minX, maxY - minY);
    }

    Vector2 GetOppositeVertex((Vector2, Vector2, Vector2) triangle, Vector2 a, Vector2 b)
    {
        if (!SamePoint(triangle.Item1, a) && !SamePoint(triangle.Item1, b))
            return triangle.Item1;
        if (!SamePoint(triangle.Item2, a) && !SamePoint(triangle.Item2, b))
            return triangle.Item2;
        return triangle.Item3;
    }

    bool TryGetCircumcircle(Vector2 a, Vector2 b, Vector2 c,
        out Vector2 center, out float radiusSquared)
    {
        center = Vector2.zero;
        radiusSquared = 0f;

        float ax = a.x, ay = a.y;
        float bx = b.x, by = b.y;
        float cx = c.x, cy = c.y;

        float d = 2f * (ax * (by - cy) + bx * (cy - ay) + cx * (ay - by));

        if (Mathf.Abs(d) < EPSILON)
            return false;

        float aSquared = ax * ax + ay * ay;
        float bSquared = bx * bx + by * by;
        float cSquared = cx * cx + cy * cy;

        float ux = (aSquared * (by - cy) + bSquared * (cy - ay) + cSquared * (ay - by)) / d;
        float uy = (aSquared * (cx - bx) + bSquared * (ax - cx) + cSquared * (bx - ax)) / d;

        center = new Vector2(ux, uy);
        radiusSquared = (center - a).sqrMagnitude;

        return true;
    }

    bool SamePoint(Vector2 a, Vector2 b)
    {
        return (a - b).sqrMagnitude < EPSILON * EPSILON;
    }

    bool ContainsPoint(List<Vector2> points, Vector2 p)
    {
        foreach (Vector2 point in points)
        {
            if (SamePoint(point, p))
                return true;
        }
        return false;
    }
}

public struct DualEdge
{
    public Vector2 siteA;
    public Vector2 siteB;
    public Vector2 voronoiStart;
    public Vector2 voronoiEnd;

    public DualEdge(Vector2 siteA, Vector2 siteB, Vector2 voronoiStart, Vector2 voronoiEnd)
    {
        this.siteA = siteA;
        this.siteB = siteB;
        this.voronoiStart = voronoiStart;
        this.voronoiEnd = voronoiEnd;
    }
}

[System.Serializable]
public struct VoronoiCell
{
    public Vector2 site;
    public List<(Vector2, Vector2)> edges;
    public List<Vector2> vertices;

    public VoronoiCell(Vector2 site, List<(Vector2, Vector2)> edges, List<Vector2> vertices)
    {
        this.site = site;
        this.edges = edges;
        this.vertices = vertices;
    }
}