using System.Collections.Generic;
using UnityEngine;

public class marcheDeJarvisScript : MonoBehaviour
{
    [SerializeField]
    List<Vector2> arrayPoint = new List<Vector2>();

    [Header("Visualisation")]
    [SerializeField] float pointRadius    = 0.15f;
    [SerializeField] Color pointColor     = Color.white;
    [SerializeField] Color hullColor      = Color.cyan;
    [SerializeField] Color firstPointColor = Color.yellow;

    List<(Vector2, Vector2)> linkedPoint = new List<(Vector2, Vector2)>();

    void Start()
    {
        StartJarvis();
    }
    
    (Vector2, int) GetFirstPoint(List<Vector2> points)
    {
        var highestX = float.MaxValue;
        var highestY = float.MaxValue;
        var index    = 0;

        for (var i = 0; i < points.Count; i++)
        {
            if (points[i].x < highestX)
            {
                highestX = points[i].x;
                highestY = points[i].y;
                index    = i;
            }
            else if (points[i].x == highestX)
            {
                if (!(highestY > points[i].y)) continue;
                highestX = points[i].x;
                highestY = points[i].y;
                index    = i;
            }
        }
        return (new Vector2(highestX, highestY), index);
    }

    float CalculateAngle(Vector2 direction, Vector2 toCandidate)
    {
        return Vector2.SignedAngle(direction, toCandidate);
    }

    void StartJarvis()
    {
        linkedPoint.Clear();

        if (arrayPoint.Count < 3)
        {
            Debug.LogWarning("Il faut au moins 3 points pour la marche de Jarvis.");
            return;
        }

        var (coordFirstPoint, _) = GetFirstPoint(arrayPoint);
        var currentPoint = coordFirstPoint;
        var direction    = Vector2.down;

        while (true)
        {
            var smallestAngle = float.MaxValue;
            var bestPoint     = Vector2.zero;

            for (var i = 0; i < arrayPoint.Count; i++)
            {
                var candidate = arrayPoint[i];
                if (candidate == currentPoint) continue;

                var toCandidate = candidate - currentPoint;
                var angle       = CalculateAngle(direction, toCandidate);

                if (angle < 0) angle += 360f;

                if (!(angle < smallestAngle)) continue;
                smallestAngle = angle;
                bestPoint     = candidate;
            }

            linkedPoint.Add((currentPoint, bestPoint));

            if (bestPoint == coordFirstPoint) break;

            direction    = bestPoint - currentPoint;
            currentPoint = bestPoint;
        }
    }
    
    void OnDrawGizmos()
    {
        if (arrayPoint == null || arrayPoint.Count == 0) return;

        if (linkedPoint == null || linkedPoint.Count == 0)
        {
            linkedPoint = new List<(Vector2, Vector2)>();
            StartJarvis();
        }

        Vector3 offset = transform.position;

        foreach (var p in arrayPoint)
        {
            Gizmos.color = pointColor;
            Gizmos.DrawSphere(offset + (Vector3)p, pointRadius);
        }

        var (fp, _) = GetFirstPoint(arrayPoint);
        Gizmos.color = firstPointColor;
        Gizmos.DrawSphere(offset + (Vector3)fp, pointRadius * 1.5f);

        Gizmos.color = hullColor;
        foreach (var (a, b) in linkedPoint)
        {
            Gizmos.DrawLine(offset + (Vector3)a, offset + (Vector3)b);
            DrawArrow(offset + (Vector3)a, offset + (Vector3)b);
        }
    }
    
    void DrawArrow(Vector2 from, Vector2 to)
    {
        var mid       = (from + to) * 0.5f;
        var dir       = (to - from).normalized;
        var perpLeft  = new Vector2(-dir.y,  dir.x) * 0.15f;
        var perpRight = new Vector2( dir.y, -dir.x) * 0.15f;
        var tip       = mid + dir * 0.2f;

        Gizmos.DrawLine((Vector3)tip, (Vector3)(mid + perpLeft));
        Gizmos.DrawLine((Vector3)tip, (Vector3)(mid + perpRight));
    }
}