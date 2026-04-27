using UnityEngine;
using System.Collections.Generic;

public class Graham : MonoBehaviour
{
    private List<Vector2> arrayPoint = new List<Vector2>();
    private List<(Vector2, Vector2)> linePoint = new List<(Vector2, Vector2)>();

    public List<Vector2> ComputeHull(List<Vector2> points)
    {
        arrayPoint = new List<Vector2>(points);
        return StartGraham();
    }

    Vector2 GetBarycentre()
    {
        float sumX = 0;
        float sumY = 0;

        for (int i = 0; i < arrayPoint.Count; i++)
        {
            sumX += arrayPoint[i].x;
            sumY += arrayPoint[i].y;
        }

        sumX /= arrayPoint.Count;
        sumY /= arrayPoint.Count;

        return new Vector2(sumX, sumY);
    }

    List<Vector2> TriePoint(List<Vector2> trie, Vector2 center)
    {
        Vector2 firstVector = center - Vector2.right;

        List<(float angle, Vector2 point)> listAngle = new List<(float, Vector2)>();

        foreach (Vector2 vector in trie)
        {
            Vector2 secondVector = center - vector;
            float angle = Vector2.SignedAngle(firstVector, secondVector);
            listAngle.Add((angle, vector));
        }

        listAngle.Sort((a, b) => a.angle.CompareTo(b.angle));

        List<Vector2> result = new List<Vector2>();

        foreach (var item in listAngle)
        {
            result.Add(item.point);
        }

        return result;
    }

    bool EstConvexe(Vector2 prev, Vector2 current, Vector2 next)
    {
        Vector2 a = current - prev;
        Vector2 b = next - current;

        float cross = a.x * b.y - a.y * b.x;

        return cross > 0;
    }

    List<Vector2> StartGraham()
    {
        linePoint.Clear();

        if (arrayPoint.Count < 3)
        {
            Debug.LogWarning("Il faut au moins 3 points pour Graham.");
            return new List<Vector2>();
        }

        Vector2 barycentre = GetBarycentre();
        List<Vector2> L = TriePoint(arrayPoint, barycentre);

        bool hasRemovedPoint = true;
        int security = 0;

        while (hasRemovedPoint && L.Count >= 3)
        {
            hasRemovedPoint = false;

            for (int index = 0; index < L.Count; index++)
            {
                int prev = (index - 1 + L.Count) % L.Count;
                int next = (index + 1) % L.Count;

                if (!EstConvexe(L[prev], L[index], L[next]))
                {
                    L.RemoveAt(index);
                    hasRemovedPoint = true;
                    break;
                }
            }

            security++;

            if (security > 10000)
            {
                Debug.LogWarning("Sécurité Graham : boucle arrêtée.");
                break;
            }
        }

        for (int i = 0; i < L.Count - 1; i++)
        {
            linePoint.Add((L[i], L[i + 1]));
        }

        linePoint.Add((L[L.Count - 1], L[0]));

        Debug.Log($"Graham : {L.Count} points dans l'enveloppe.");

        return L;
    }

    public List<(Vector2, Vector2)> GetLinePoint()
    {
        return linePoint;
    }
}