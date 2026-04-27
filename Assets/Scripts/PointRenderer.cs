using System.Collections.Generic;
using UnityEngine;

public class PointRenderer : MonoBehaviour
{
    public PointManager pointManager;
    public GameObject pointPrefab;
    public Transform pointParent;

    private readonly List<GameObject> pointObjects = new List<GameObject>();

    void Start()
    {
        pointManager.OnPointsChanged += RedrawPoints;
    }

    public void RedrawPoints()
    {
        foreach (GameObject obj in pointObjects)
        {
            Destroy(obj);
        }

        pointObjects.Clear();

        foreach (Vector2 point in pointManager.Points)
        {
            GameObject obj = Instantiate(pointPrefab, pointParent);
            obj.transform.position = new Vector3(point.x, point.y, 0f);
            pointObjects.Add(obj);
        }
    }
}