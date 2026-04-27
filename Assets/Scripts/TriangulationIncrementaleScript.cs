using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TriangulationIncrementale : MonoBehaviour
{
    [SerializeField]
    public List<Vector2> arrayPoint = new List<Vector2>();
    
    public List<Vector2> sortedPoints = new List<Vector2>();
    public List<(Vector2, Vector2)> edges = new List<(Vector2, Vector2)>();
    public List<(Vector2, Vector2, Vector2)> triangles = new List<(Vector2, Vector2, Vector2)>();
    
    private List<Vector2> hull = new List<Vector2>();

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
        sortedPoints.Clear();
        edges.Clear();
        triangles.Clear();
        hull.Clear();

        if (arrayPoint.Count < 2)
        {
            Debug.LogWarning("Il faut au moins 2 points.");
            return;
        }
        
        sortedPoints = arrayPoint
            .OrderBy(p => p.x)
            .ThenBy(p => p.y)
            .ToList();
        
        int k = InitializeColinearEdges();

        if (k >= sortedPoints.Count)
        {
            Debug.Log("Tous les points sont colinéaires — aucune triangulation 2D.");
            return;
        }

        BuildInitialTriangulation(k);

        for (int q = k + 1; q < sortedPoints.Count; q++)
        {
            AddPoint(sortedPoints[q]);
        }

        Debug.Log($"Triangulation : {triangles.Count} triangles, {edges.Count} arêtes, " +
                  $"hull = {hull.Count} sommets");
    }
    
    int InitializeColinearEdges()
    {
        int k = 1;
        while (k < sortedPoints.Count)
        {
            if (k == 1)
            {
                edges.Add((sortedPoints[0], sortedPoints[1]));
                k++;
                continue;
            }

            if (Colinear(sortedPoints[k - 2], sortedPoints[k - 1], sortedPoints[k]))
            {
                edges.Add((sortedPoints[k - 1], sortedPoints[k]));
                k++;
            }
            else
            {
                break;
            }
        }
        return k;
    }

    void BuildInitialTriangulation(int k)
    {
        Vector2 pk1 = sortedPoints[k]; // P_{k+1}
        
        for (int i = 0; i < k - 1; i++)
        {
            triangles.Add((sortedPoints[i], sortedPoints[i + 1], pk1));
        }
        
        for (int i = 0; i < k; i++)
        {
            edges.Add((sortedPoints[i], pk1));
        }
        
        hull.Clear();
        for (int i = 0; i < k; i++) hull.Add(sortedPoints[i]);
        hull.Add(pk1);
        
        if (SignedArea(hull) < 0f) hull.Reverse();
    }
    
    void AddPoint(Vector2 p)
    {
        List<int> visibleEdgeIndices = new List<int>();
        int n = hull.Count;

        for (int i = 0; i < n; i++)
        {
            Vector2 a = hull[i];
            Vector2 b = hull[(i + 1) % n];
            if (IsEdgeVisibleFrom(a, b, p))
                visibleEdgeIndices.Add(i);
        }

        if (visibleEdgeIndices.Count == 0)
        {
            Debug.LogWarning($"Point {p} à l'intérieur du hull - ignoré.");
            return;
        }

        // 3b) Pour chaque arête vue : créer un triangle [A, B, P]
        foreach (int idx in visibleEdgeIndices)
        {
            Vector2 a = hull[idx];
            Vector2 b = hull[(idx + 1) % n];
            triangles.Add((a, b, p));
        }
        
        HashSet<Vector2> visitedVerts = new HashSet<Vector2>();
        foreach (int idx in visibleEdgeIndices)
        {
            Vector2 a = hull[idx];
            Vector2 b = hull[(idx + 1) % n];
            if (visitedVerts.Add(a)) edges.Add((a, p));
            if (visitedVerts.Add(b)) edges.Add((b, p));
        }
        
        UpdateHull(p, visibleEdgeIndices);
    }
    
    void UpdateHull(Vector2 p, List<int> visibleEdgeIndices)
    {
        int n = hull.Count;
        
        bool[] visible = new bool[n];
        foreach (int i in visibleEdgeIndices) visible[i] = true;
        
        int chainStart = -1;
        for (int i = 0; i < n; i++)
        {
            int prev = (i - 1 + n) % n;
            if (visible[i] && !visible[prev])
            {
                chainStart = i;
                break;
            }
        }
        
        if (chainStart == -1)
        {
            Debug.LogWarning("Toutes les arêtes du hull sont vues — configuration inattendue.");
            return;
        }
        
        int chainEnd = chainStart;
        while (visible[(chainEnd + 1) % n] && (chainEnd + 1) % n != chainStart)
            chainEnd = (chainEnd + 1) % n;
        
        Vector2 firstVisibleStart = hull[chainStart];
        Vector2 lastVisibleEnd    = hull[(chainEnd + 1) % n];
        
        List<Vector2> newHull = new List<Vector2>();
        int idx = (chainEnd + 1) % n; // commence par lastVisibleEnd
        for (int step = 0; step < n; step++)
        {
            newHull.Add(hull[idx]);
            if (hull[idx] == firstVisibleStart) break;
            idx = (idx + 1) % n;
        }
        newHull.Add(p);

        hull = newHull;
    }
    
    static float Cross(Vector2 o, Vector2 a, Vector2 b)
    {
        return (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
    }
    
    static bool Colinear(Vector2 a, Vector2 b, Vector2 c, float epsilon = 1e-6f)
    {
        return Mathf.Abs(Cross(a, b, c)) < epsilon;
    }
    
    static bool IsEdgeVisibleFrom(Vector2 a, Vector2 b, Vector2 p)
    {
        return Cross(a, b, p) < 0f;
    }
    
    static float SignedArea(List<Vector2> poly)
    {
        float sum = 0f;
        int n = poly.Count;
        for (int i = 0; i < n; i++)
        {
            Vector2 a = poly[i];
            Vector2 b = poly[(i + 1) % n];
            sum += (b.x - a.x) * (b.y + a.y);
        }
        return -sum * 0.5f;
    }
}