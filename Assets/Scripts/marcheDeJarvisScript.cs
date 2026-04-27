using System.Collections.Generic;
using UnityEngine;

public class marcheDeJarvisScript : MonoBehaviour
{
    private List<Vector2> arrayPoint = new List<Vector2>();
    private List<(Vector2, Vector2)> linkedPoint = new List<(Vector2, Vector2)>();

    public List<Vector2> ComputeHull(List<Vector2> points)
    {
        arrayPoint = new List<Vector2>(points);

        StartJarvis();

        return ConvertLinkedPointsToHull();
    }

    (Vector2, int) GetFirstPoint(List<Vector2> points)
    {
        var highestX = float.MaxValue;
        var highestY = float.MaxValue;
        var index = 0;

        for (var i = 0; i < points.Count; i++)
        {
            if (points[i].x < highestX)
            {
                highestX = points[i].x;
                highestY = points[i].y;
                index = i;
            }
            else if (points[i].x == highestX)
            {
                if (!(highestY > points[i].y)) continue;

                highestX = points[i].x;
                highestY = points[i].y;
                index = i;
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
        var direction = Vector2.down;

        while (true)
        {
            var smallestAngle = float.MaxValue;
            var bestPoint = Vector2.zero;

            for (var i = 0; i < arrayPoint.Count; i++)
            {
                var candidate = arrayPoint[i];

                if (candidate == currentPoint)
                    continue;

                var toCandidate = candidate - currentPoint;
                var angle = CalculateAngle(direction, toCandidate);

                if (angle < 0)
                    angle += 360f;

                if (!(angle < smallestAngle))
                    continue;

                smallestAngle = angle;
                bestPoint = candidate;
            }

            linkedPoint.Add((currentPoint, bestPoint));

            if (bestPoint == coordFirstPoint)
                break;

            direction = bestPoint - currentPoint;
            currentPoint = bestPoint;
        }
    }

    List<Vector2> ConvertLinkedPointsToHull()
    {
        List<Vector2> hull = new List<Vector2>();

        foreach (var (a, _) in linkedPoint)
        {
            hull.Add(a);
        }

        return hull;
    }

    public List<(Vector2, Vector2)> GetLinkedPoints()
    {
        return linkedPoint;
    }
}