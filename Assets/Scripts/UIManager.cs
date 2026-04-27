using System.Collections.Generic;
using System.Diagnostics;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public PointManager pointManager;
    public HullRenderer hullRenderer;

    public TMP_Text jarvisTimeText;
    public TMP_Text grahamTimeText;

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
        // Temporaire en attendant l'algo de ton collègue
        List<Vector2> hull = new List<Vector2>(pointManager.Points);

        hullRenderer.DrawHull(hull);
        jarvisTimeText.text = "Jarvis : non branché";
    }

    public void OnGrahamClicked()
    {
        // Temporaire en attendant l'algo de ton collègue
        List<Vector2> hull = new List<Vector2>(pointManager.Points);

        hullRenderer.DrawHull(hull);
        grahamTimeText.text = "Graham : non branché";
    }
}