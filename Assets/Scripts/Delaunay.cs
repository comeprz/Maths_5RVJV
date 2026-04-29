using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Delaunay : MonoBehaviour
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
        
        // Tri abscisses puis ordonnées
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

        if (triangles.Count > 0)
        {
            RebuildEdgesFromTriangles();
        }

        Debug.Log($"Triangulation : {triangles.Count} triangles, {edges.Count} arêtes, " +
                  $"hull = {hull.Count} sommets");
    }

    public void RunDelaunayFromPoints(List<Vector2> points)
    {
        arrayPoint = new List<Vector2>(points);

        // Triangulation
        Run();

        if (triangles.Count == 0)
        {
            Debug.LogWarning("Impossible de faire Delaunay : aucune triangulation de base.");
            return;
        }

        // Flipping
        FlipToDelaunay();

        // Reconstruction des arêtes
        RebuildEdgesFromTriangles();

        Debug.Log($"Delaunay : {triangles.Count} triangles, {edges.Count} arêtes");
    }
    
    // Début du nuage, vérifier que les points ne sont pas alignés
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

    // Crée le 1er triangle
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
        
        // Sens trigo
        if (SignedArea(hull) < 0f) hull.Reverse();
    }
    
    // Ajout des points dans la triangulation
    void AddPoint(Vector2 p)
    {
        List<int> visibleEdgeIndices = new List<int>();
        int n = hull.Count;

        // Parcours des arêtes
        for (int i = 0; i < n; i++)
        {
            Vector2 a = hull[i];
            Vector2 b = hull[(i + 1) % n];
            if (IsEdgeVisibleFrom(a, b, p))
                visibleEdgeIndices.Add(i);
        }

        // 3b) Pour chaque arête vue : créer un triangle [A, B, P]
        foreach (int idx in visibleEdgeIndices)
        {
            Vector2 a = hull[idx];
            Vector2 b = hull[(idx + 1) % n];
            triangles.Add((a, b, p));
        }
        
        // Évite d'ajouter la même arête plusieurs fois
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
    
    // Produit vectoriel
    static float Cross(Vector2 o, Vector2 a, Vector2 b)
    {
        return (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
    }
    
    // Vérifier la colinéarité
    static bool Colinear(Vector2 a, Vector2 b, Vector2 c, float epsilon = 1e-6f)
    {
        return Mathf.Abs(Cross(a, b, c)) < epsilon;
    }
    
    // A droite de l'arête => visible
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

    // Delaunay
    private const float EPSILON = 1e-5f;

    // Flip en fonction d'un nombre max d'itérations
    public void FlipToDelaunay()
    {
        int flipCount = 0;
        int iteration = 0;
        int maxIterations = 10000;

        bool hasFlipped = true;

        while (hasFlipped && iteration < maxIterations)
        {
            hasFlipped = TryFlipFirstIllegalEdge();

            if (hasFlipped)
                flipCount++;

            iteration++;
        }

        if (iteration >= maxIterations)
        {
            Debug.LogWarning("Delaunay : nombre maximal d'itérations atteint.");
        }

        Debug.Log($"Flipping Delaunay terminé : {flipCount} flips.");
    }

    // Flip en fonction des règles
    bool TryFlipFirstIllegalEdge()
    {
        for (int i = 0; i < triangles.Count; i++)
        {
            for (int j = i + 1; j < triangles.Count; j++)
            {
                Vector2 a, b, c, d;

                if (!GetAdjacentTriangles(triangles[i], triangles[j], out a, out b, out c, out d))
                    continue;

                // Sécurité : les deux triangles doivent être de part et d'autre de l'arête commune
                if (!AreOnOppositeSides(a, b, c, d))
                    continue;

                bool illegal = PointInCircumcircle(d, triangles[i]) ||
                            PointInCircumcircle(c, triangles[j]);

                if (!illegal)
                    continue;

                // Flip : arête a-b remplacée par nouvelle arête c-d
                triangles[i] = MakeCCWTriangle(c, d, a);
                triangles[j] = MakeCCWTriangle(d, c, b);

                return true;
            }
        }

        return false;
    }

    // Cherche si 2 triangles partagent la même arête
    bool GetAdjacentTriangles(
        (Vector2, Vector2, Vector2) t1,
        (Vector2, Vector2, Vector2) t2,
        out Vector2 a,
        out Vector2 b,
        out Vector2 c,
        out Vector2 d)
    {
        a = b = c = d = Vector2.zero;

        List<Vector2> vertices1 = new List<Vector2> { t1.Item1, t1.Item2, t1.Item3 };
        List<Vector2> vertices2 = new List<Vector2> { t2.Item1, t2.Item2, t2.Item3 };

        List<Vector2> shared = new List<Vector2>();

        foreach (Vector2 v1 in vertices1)
        {
            foreach (Vector2 v2 in vertices2)
            {
                if (SamePoint(v1, v2) && !ContainsPoint(shared, v1))
                {
                    shared.Add(v1);
                }
            }
        }

        // Nbr de points en commun != 2 => Non
        if (shared.Count != 2)
            return false;

        // Arête commune AB
        a = shared[0];
        b = shared[1];

        // C => 1er triangle, D => 2ème triangle
        c = GetOppositeVertex(t1, a, b);
        d = GetOppositeVertex(t2, a, b);

        return true;
    }

    // Sommet opposé
    Vector2 GetOppositeVertex((Vector2, Vector2, Vector2) triangle, Vector2 a, Vector2 b)
    {
        if (!SamePoint(triangle.Item1, a) && !SamePoint(triangle.Item1, b))
            return triangle.Item1;

        if (!SamePoint(triangle.Item2, a) && !SamePoint(triangle.Item2, b))
            return triangle.Item2;

        return triangle.Item3;
    }

    // Point dans le cercle circonscrit ?
    bool PointInCircumcircle(Vector2 p, (Vector2, Vector2, Vector2) triangle)
    {
        Vector2 center;
        float radiusSquared;

        if (!TryGetCircumcircle(triangle.Item1, triangle.Item2, triangle.Item3, out center, out radiusSquared))
            return false;

        float distanceSquared = (p - center).sqrMagnitude;

        return distanceSquared < radiusSquared - EPSILON;
    }

    bool TryGetCircumcircle(Vector2 a, Vector2 b, Vector2 c, out Vector2 center, out float radiusSquared)
    {
        center = Vector2.zero;
        radiusSquared = 0f;

        float ax = a.x;
        float ay = a.y;
        float bx = b.x;
        float by = b.y;
        float cx = c.x;
        float cy = c.y;

        // Déterminant principal
        float d = 2f * (ax * (by - cy) + bx * (cy - ay) + cx * (ay - by));

        // Déterminant nul = c'est mort
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

    // Bon sens trigo
    (Vector2, Vector2, Vector2) MakeCCWTriangle(Vector2 a, Vector2 b, Vector2 c)
    {
        if (Cross(a, b, c) < 0f)
            return (a, c, b);

        return (a, b, c);
    }

    // 2 sommets opposés
    bool AreOnOppositeSides(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        float sideC = Cross(a, b, c);
        float sideD = Cross(a, b, d);

        // Produit vectoriel négatif
        return sideC * sideD < -EPSILON;
    }

    void RebuildEdgesFromTriangles()
    {
        edges.Clear();

        foreach (var triangle in triangles)
        {
            AddUniqueEdge(triangle.Item1, triangle.Item2);
            AddUniqueEdge(triangle.Item2, triangle.Item3);
            AddUniqueEdge(triangle.Item3, triangle.Item1);
        }
    }

    void AddUniqueEdge(Vector2 a, Vector2 b)
    {
        if (SamePoint(a, b))
            return;

        foreach (var edge in edges)
        {
            if (SameEdge(edge.Item1, edge.Item2, a, b))
                return;
        }

        edges.Add((a, b));
    }

    // Même arête ?
    bool SameEdge(Vector2 a1, Vector2 b1, Vector2 a2, Vector2 b2)
    {
        return SamePoint(a1, a2) && SamePoint(b1, b2) ||
            SamePoint(a1, b2) && SamePoint(b1, a2);
    }

    // Même point ?
    bool SamePoint(Vector2 a, Vector2 b)
    {
        return (a - b).sqrMagnitude < EPSILON * EPSILON;
    }

    // Contient le point ?
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