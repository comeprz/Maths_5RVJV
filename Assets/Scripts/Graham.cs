using UnityEngine;
using System.Collections.Generic;

public class Graham : MonoBehaviour
{

    List<Vector2> arrayPoint = new List<Vector2>();
    List<(Vector2, Vector2)> LinePoint = new List<(Vector2, Vector2)>();
    void Start()
    {
        StartGraham();
    }

    // Update is called once per frame
    void Update()
    {
        

    }

    Vector2 getBarycentre()
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
        foreach(Vector2 vector in trie) {
            Vector2 secondVector = center - vector;
            float angle = Vector2.SignedAngle(firstVector,secondVector);
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

    void StartGraham()
    {
        Vector2 Bcenter = getBarycentre();
        List<Vector2> L = TriePoint(arrayPoint, Bcenter);
        var Sinit = L[0];
        var pivot = Sinit;
        var avance = true;
        var index = 0;

        while (pivot != Sinit || avance == false)
        {
            int prev = (index - 1 + L.Count) % L.Count;
            int next = (index + 1) % L.Count;

            if (EstConvexe(L[prev], L[index], L[next]))
            {
                index = (index + 1) % L.Count;
                pivot = L[index];
                avance = true;
            }
            else
            {
                L.RemoveAt(index);
                index = (index - 1 + L.Count) % L.Count;
                pivot = L[index];
                avance = false;
            }
        }

        for (int i = 0; i < L.Count-1; i++)
        {
            LinePoint.Add((L[i], L[i + 1]));
        }
        LinePoint.Add((L[L.Count - 1], L[0]));
    }
}
