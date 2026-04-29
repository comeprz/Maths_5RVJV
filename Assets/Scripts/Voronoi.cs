using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Voronoi : MonoBehaviour
{
    [SerializeField]
    public List<Vector2> arrayPoint = new List<Vector2>();

    // Résultats du Voronoï
    public List<Vector2> voronoiVertices = new List<Vector2>();
    public List<(Vector2, Vector2)> voronoiEdges = new List<(Vector2, Vector2)>();
    public List<List<(Vector2, Vector2)>> voronoiCells = new List<List<(Vector2, Vector2)>>();

    // Cas dégénéré : droites parallèles (médiatrices)
    // Chaque droite est stockée comme (point, direction)
    public List<(Vector2 origin, Vector2 direction)> degenerateLines = new List<(Vector2, Vector2)>();
    public bool isDegenerate = false;

    private Delaunay delaunay;

    // Mapping triangle index -> circumcentre (= sommet de Voronoï)
    private Dictionary<int, Vector2> circumcenters = new Dictionary<int, Vector2>();

    // Pour chaque arête de Delaunay, les indices des triangles incidents
    private Dictionary<(Vector2, Vector2), List<int>> edgeToTriangles =
        new Dictionary<(Vector2, Vector2), List<int>>();

    // Pour chaque sommet de Delaunay, les arêtes duales A* incidentes
    private Dictionary<Vector2, List<(Vector2, Vector2)>> vertexToDualEdges =
        new Dictionary<Vector2, List<(Vector2, Vector2)>>();

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
        vertexToDualEdges.Clear();
        isDegenerate = false;

        if (arrayPoint.Count < 2)
        {
            Debug.LogWarning("Il faut au moins 2 points pour un diagramme de Voronoï.");
            return;
        }

        // ──────────────────────────────────────────────────────
        //  Étape 1 : Triangulation de Delaunay T de S
        // ──────────────────────────────────────────────────────
        delaunay = GetComponent<Delaunay>();
        if (delaunay == null)
            delaunay = gameObject.AddComponent<Delaunay>();

        delaunay.RunDelaunayFromPoints(arrayPoint);

        // ──────────────────────────────────────────────────────
        //  Étape 2a : Si T dégénérée (points colinéaires)
        //  => V est composé de n-1 droites parallèles,
        //     chaque droite étant la médiatrice d'une arête [Pi, Pj]
        // ──────────────────────────────────────────────────────
        if (delaunay.triangles.Count == 0)
        {
            isDegenerate = true;
            BuildDegenerateVoronoi();
            Debug.Log($"Voronoï dégénéré : {degenerateLines.Count} droites parallèles.");
            return;
        }

        // ──────────────────────────────────────────────────────
        //  Étape 2b : Cas général
        // ──────────────────────────────────────────────────────

        // Pour chaque triangle T de T, déterminer le centre C_T
        // du cercle circonscrit à T
        ComputeCircumcenters();

        // Pour chaque arête A de T, déterminer l'arête A* correspondante
        BuildEdgeToTrianglesMap();
        BuildVoronoiEdgesFromDuality();

        // Pour chaque sommet S de T, déterminer la région R
        // correspondante comme la liste des arêtes A* duales
        // des arêtes A incidentes à S
        BuildVoronoiCells();

        Debug.Log($"Voronoï : {voronoiVertices.Count} sommets, {voronoiEdges.Count} arêtes, " +
                  $"{voronoiCells.Count} cellules");
    }

    // ═════════════════════════════════════════════════════════
    //  CAS DÉGÉNÉRÉ : tous les points sont colinéaires
    //  Le diagramme de Voronoï V est composé de n-1 droites
    //  parallèles, chaque droite étant la médiatrice d'une
    //  arête [Pi, Pj] consécutive.
    // ═════════════════════════════════════════════════════════

    void BuildDegenerateVoronoi()
    {
        // Les points triés par Delaunay (qui sont colinéaires)
        List<Vector2> sorted = delaunay.sortedPoints;

        for (int i = 0; i < sorted.Count - 1; i++)
        {
            Vector2 a = sorted[i];
            Vector2 b = sorted[i + 1];

            // Milieu de [a, b]
            Vector2 mid = (a + b) * 0.5f;

            // Direction de l'arête
            Vector2 edgeDir = (b - a).normalized;

            // Perpendiculaire = direction de la médiatrice
            Vector2 perpDir = new Vector2(-edgeDir.y, edgeDir.x);

            degenerateLines.Add((mid, perpDir));

            // On stocke aussi comme arête Voronoï (segment long pour le rendu)
            float extent = ComputeExtent() * 2f;
            Vector2 p1 = mid - perpDir * extent;
            Vector2 p2 = mid + perpDir * extent;
            voronoiEdges.Add((p1, p2));
        }
    }

    // ═════════════════════════════════════════════════════════
    //  Pour chaque triangle T de T :
    //  déterminer le centre C_T du cercle circonscrit à T
    // ═════════════════════════════════════════════════════════

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

    // ═════════════════════════════════════════════════════════
    //  Pour chaque arête A de T :
    //  déterminer l'arête A* correspondante
    //
    //  - Arête interne (2 triangles incidents) :
    //    A* est bornée, reliant les deux circumcentres
    //
    //  - Arête externe (1 triangle incident) :
    //    A* est une demi-droite dont le sommet est le centre
    //    du cercle circonscrit au triangle incident à A
    //    et passant par le milieu de l'arête A
    //    (cf. cours p.41)
    // ═════════════════════════════════════════════════════════

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

    void BuildVoronoiEdgesFromDuality()
    {
        foreach (var kvp in edgeToTriangles)
        {
            Vector2 a = kvp.Key.Item1;
            Vector2 b = kvp.Key.Item2;
            List<int> triIndices = kvp.Value;

            (Vector2, Vector2) dualEdge;

            if (triIndices.Count == 2)
            {
                // ── Arête interne de T ──
                // A* est une arête bornée dont les deux sommets sont
                // les centres des cercles circonscrits aux triangles
                // incidents à A.
                if (!circumcenters.ContainsKey(triIndices[0]) ||
                    !circumcenters.ContainsKey(triIndices[1]))
                    continue;

                Vector2 c1 = circumcenters[triIndices[0]];
                Vector2 c2 = circumcenters[triIndices[1]];

                dualEdge = (c1, c2);
            }
            else if (triIndices.Count == 1)
            {
                // ── Arête externe de T ──
                // A* est une arête bornée dont le sommet est le centre
                // du cercle circonscrit au triangle incident à A
                // et passant par le milieu de l'arête A.
                // On prolonge dans la direction circumcentre -> milieu.
                if (!circumcenters.ContainsKey(triIndices[0]))
                    continue;

                Vector2 center = circumcenters[triIndices[0]];
                Vector2 mid = (a + b) * 0.5f;

                // Direction : du circumcentre vers le milieu de l'arête
                Vector2 dir = (mid - center);

                if (dir.sqrMagnitude < EPSILON * EPSILON)
                {
                    // Cas rare : circumcentre = milieu
                    // On prend la perpendiculaire à l'arête
                    Vector2 edgeDir = (b - a).normalized;
                    dir = new Vector2(-edgeDir.y, edgeDir.x);
                }
                else
                {
                    dir = dir.normalized;
                }

                float rayLength = ComputeExtent() * 2f;
                Vector2 rayEnd = center + dir * rayLength;

                dualEdge = (center, rayEnd);
            }
            else
            {
                continue;
            }

            voronoiEdges.Add(dualEdge);

            // Enregistrer cette arête duale pour les deux sommets
            // de l'arête Delaunay (pour construire les cellules)
            RegisterDualEdgeForVertex(a, dualEdge);
            RegisterDualEdgeForVertex(b, dualEdge);
        }
    }

    void RegisterDualEdgeForVertex(Vector2 vertex, (Vector2, Vector2) dualEdge)
    {
        Vector2 foundKey = Vector2.zero;
        bool found = false;

        foreach (var key in vertexToDualEdges.Keys)
        {
            if (SamePoint(key, vertex))
            {
                foundKey = key;
                found = true;
                break;
            }
        }

        if (!found)
        {
            vertexToDualEdges[vertex] = new List<(Vector2, Vector2)>();
            foundKey = vertex;
        }

        vertexToDualEdges[foundKey].Add(dualEdge);
    }

    // ═════════════════════════════════════════════════════════
    //  Pour chaque sommet S de T :
    //  déterminer la région R correspondante comme la liste
    //  des arêtes A* duales des arêtes A incidentes à S
    //  (cf. cours p.41)
    // ═════════════════════════════════════════════════════════

    void BuildVoronoiCells()
    {
        foreach (Vector2 site in arrayPoint)
        {
            List<(Vector2, Vector2)> dualEdges = null;

            foreach (var kvp in vertexToDualEdges)
            {
                if (SamePoint(kvp.Key, site))
                {
                    dualEdges = kvp.Value;
                    break;
                }
            }

            if (dualEdges == null || dualEdges.Count == 0)
                continue;

            // Trier les arêtes duales en anneau autour du site
            List<(Vector2, Vector2)> sortedDualEdges = SortDualEdgesAroundSite(dualEdges, site);
            voronoiCells.Add(sortedDualEdges);
        }
    }

    /// <summary>
    /// Trie les arêtes duales par angle autour du site pour former la cellule.
    /// On utilise le milieu de chaque arête duale comme point de référence angulaire.
    /// </summary>
    List<(Vector2, Vector2)> SortDualEdgesAroundSite(
        List<(Vector2, Vector2)> dualEdges, Vector2 site)
    {
        return dualEdges
            .OrderBy(e =>
            {
                Vector2 mid = (e.Item1 + e.Item2) * 0.5f;
                return Mathf.Atan2(mid.y - site.y, mid.x - site.x);
            })
            .ToList();
    }

    // ═════════════════════════════════════════════════════════
    //  Utilitaires (même style que Delaunay)
    // ═════════════════════════════════════════════════════════

    (Vector2, Vector2) NormalizeEdgeKey(Vector2 a, Vector2 b)
    {
        if (a.x < b.x || (Mathf.Abs(a.x - b.x) < EPSILON && a.y < b.y))
            return (a, b);
        return (b, a);
    }

    /// <summary>
    /// Calcule l'étendue maximale des points (pour dimensionner les rayons semi-infinis).
    /// </summary>
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