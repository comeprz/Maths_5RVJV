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
    public marcheDeJarvisScript jarvisScript;
    public TriangulationIncrementale triangulationScript;
    public Graham grahamScript;

    public void OnClearClicked()
    {
        pointManager.ClearPoints();
        hullRenderer.ClearAll();

        jarvisTimeText.text = "Jarvis : -";
        grahamTimeText.text = "Graham : -";
        triangulationTimeText.text = "Triangulation : -";
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
}