using System.Collections.Generic;
using UnityEngine;

public class PointManager3D : MonoBehaviour
{
    public GameObject pointPrefab;
    public Transform pointsParent;
    public float spawnRadius = 3f;

    private List<Vector3> points = new List<Vector3>();
    private List<GameObject> pointObjects = new List<GameObject>();

    public List<Vector3> Points => points;
    
    public void ClearPoints()
    {
        foreach (var go in pointObjects)
        {
            if (go != null) Destroy(go);
        }
        pointObjects.Clear();
        points.Clear();
    }

    public void GenerateRandomPoints(int count)
    {
        ClearPoints();

        for (var i = 0; i < count; i++)
        {
            var p = Random.insideUnitSphere * spawnRadius;
            AddPoint(p);
        }
    }

    public void AddPoint(Vector3 position)
    {
        points.Add(position);

        if (pointPrefab != null)
        {
            var go = Instantiate(pointPrefab, position, Quaternion.identity, pointsParent);
            pointObjects.Add(go);
        }
        else
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 0.05f;
            if (pointsParent != null) go.transform.SetParent(pointsParent);
            pointObjects.Add(go);
        }
    }
}