using System.Collections.Generic;
using UnityEngine;

public class HullRenderer : MonoBehaviour
{
    public LineRenderer lineRenderer;

    public void DrawHull(List<Vector2> hull)
    {
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

    public void ClearHull()
    {
        lineRenderer.positionCount = 0;
    }
}