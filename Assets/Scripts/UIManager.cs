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
    public marcheDeJarvisScript jarvisScript;

    public void OnClearClicked()
    {
        pointManager.ClearPoints();
        hullRenderer.ClearHull();

        jarvisTimeText.text = "Jarvis : -";
        grahamTimeText.text = "Graham : -";
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
        Debug.Log("Bouton Jarvis cliqué");

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
        // Temporaire en attendant l'algo de ton collègue
        List<Vector2> hull = new List<Vector2>(pointManager.Points);

        hullRenderer.DrawHull(hull);
        grahamTimeText.text = "Graham : non branché";
    }
}