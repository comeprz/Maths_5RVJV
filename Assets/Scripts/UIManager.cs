using System.Collections.Generic;
using Stopwatch = System.Diagnostics.Stopwatch;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public PointManager pointManager;
    public HullRenderer hullRenderer;

    public TMP_Text jarvisTimeText;
    public TMP_Text grahamTimeText;
    public TMP_Text triangulationTimeText;
    public TMP_Text delaunayTimeText;
    public TMP_Text voronoiTimeText;
    public marcheDeJarvisScript jarvisScript;
    public TriangulationIncrementale triangulationScript;
    public Graham grahamScript;
    public Delaunay delaunayScript;
    public Voronoi voronoiScript;

    public PointManager3D pointManager3D;
    public HullRenderer3D hullRenderer3D;
    public convexeIncremental3DScript convex3DScript;
    public TMP_Text convex3DTimeText;

    public void OnClearClicked()
    {
        pointManager.ClearPoints();
        hullRenderer.ClearAll();

        jarvisTimeText.text = "Jarvis : -";
        grahamTimeText.text = "Graham : -";
        triangulationTimeText.text = "Triangulation : -";
        delaunayTimeText.text = "Delaunay : -";
        voronoiTimeText.text = "Voronoï : -";
    }

    public void OnRandom10Clicked()
    {
        pointManager.GenerateRandomPoints(10);
        hullRenderer.ClearHull();
    }

    public void OnRandom100Clicked()
    {
        pointManager.GenerateRandomPoints(100);
        hullRenderer.ClearHull();
    }

    public void OnRandom1000Clicked()
    {
        pointManager.GenerateRandomPoints(1000);
        hullRenderer.ClearHull();
    }

    public void OnJarvisClicked()
    {
        if (pointManager.Points.Count < 3)
        {
            hullRenderer.ClearHull();
            jarvisTimeText.text = "Jarvis : pas assez de points";
            return;
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        List<Vector2> hull = jarvisScript.ComputeHull(pointManager.Points);

        stopwatch.Stop();

        hullRenderer.DrawHull(hull);

        jarvisTimeText.text = $"Jarvis : {stopwatch.Elapsed.TotalMilliseconds:F4} ms";
    }

    public void OnGrahamClicked()
    {
        if (pointManager.Points.Count < 3)
        {
            hullRenderer.ClearAll();
            grahamTimeText.text = "Graham : pas assez de points";
            return;
        }

        Stopwatch stopwatch = Stopwatch.StartNew();

        List<Vector2> hull = grahamScript.ComputeHull(pointManager.Points);

        stopwatch.Stop();

        hullRenderer.DrawHull(hull);

        grahamTimeText.text = $"Graham : {stopwatch.Elapsed.TotalMilliseconds:F4} ms";
    }

    public void OnTriangulationClicked()
    {
        if (pointManager.Points.Count < 3)
        {
            hullRenderer.ClearAll();
            triangulationTimeText.text = "Triangulation : pas assez de points";
            return;
        }

        Stopwatch stopwatch = Stopwatch.StartNew();

        triangulationScript.RunFromPoints(pointManager.Points);

        stopwatch.Stop();

        hullRenderer.DrawEdges(triangulationScript.edges);

        triangulationTimeText.text = $"Triangulation : {stopwatch.Elapsed.TotalMilliseconds:F4} ms";
    }

    public void OnDelaunayClicked()
    {
        if (pointManager.Points.Count < 3)
        {
            hullRenderer.ClearAll();
            delaunayTimeText.text = "Delaunay : pas assez de points";
            return;
        }

        Stopwatch stopwatch = Stopwatch.StartNew();

        delaunayScript.RunDelaunayFromPoints(pointManager.Points);

        stopwatch.Stop();

        hullRenderer.DrawEdges(delaunayScript.edges);

        delaunayTimeText.text = $"Delaunay : {stopwatch.Elapsed.TotalMilliseconds:F4} ms";
    }

    public void OnVoronoiClicked()
    {
        if (pointManager.Points.Count < 3)
        {
            hullRenderer.ClearAll();
            voronoiTimeText.text = "Voronoï : pas assez de points";
            return;
        }

        Stopwatch stopwatch = Stopwatch.StartNew();

        voronoiScript.RunFromPoints(pointManager.Points);

        stopwatch.Stop();

        List<(Vector2, Vector2)> allEdges = new List<(Vector2, Vector2)>();
        allEdges.AddRange(voronoiScript.voronoiEdges);

        hullRenderer.DrawEdges(allEdges);

        voronoiTimeText.text = $"Voronoï : {stopwatch.Elapsed.TotalMilliseconds:F4} ms";
    }

    public void OnClear3DClicked()
    {
        pointManager3D.ClearPoints();
        hullRenderer3D.ClearAll();
        convex3DTimeText.text = "Convex 3D : -";
    }

    public void OnRandom3D10Clicked()
    {
        pointManager3D.GenerateRandomPoints(10);
        hullRenderer3D.ClearHull();
    }

    public void OnRandom3D100Clicked()
    {
        pointManager3D.GenerateRandomPoints(100);
        hullRenderer3D.ClearHull();
    }

    public void OnRandom3D1000Clicked()
    {
        pointManager3D.GenerateRandomPoints(1000);
        hullRenderer3D.ClearHull();
    }

    public void OnConvex3DClicked()
    {
        if (pointManager3D.Points.Count < 4)
        {
            hullRenderer3D.ClearHull();
            convex3DTimeText.text = "Convex 3D : pas assez de points (min 4)";
            return;
        }

        Stopwatch stopwatch = Stopwatch.StartNew();

        List<int> triangles = convex3DScript.ComputeHull(pointManager3D.Points);

        stopwatch.Stop();

        hullRenderer3D.DrawHull(convex3DScript.GetVertices(), triangles);

        convex3DTimeText.text = $"Convex 3D : {stopwatch.Elapsed.TotalMilliseconds:F4} ms";
    }
}