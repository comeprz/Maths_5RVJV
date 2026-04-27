using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PointManager : MonoBehaviour
{
    public Camera mainCamera;

    public List<Vector2> Points { get; private set; } = new List<Vector2>();

    public event Action OnPointsChanged;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            AddPointFromMouse();
        }
    }

    void AddPointFromMouse()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = -mainCamera.transform.position.z;

        Vector3 worldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        Vector2 point = new Vector2(worldPos.x, worldPos.y);

        Points.Add(point);
        OnPointsChanged?.Invoke();
    }

    public void ClearPoints()
    {
        Points.Clear();
        OnPointsChanged?.Invoke();
    }

    public void GenerateRandomPoints(int count)
    {
        Points.Clear();

        for (int i = 0; i < count; i++)
        {
            float x = UnityEngine.Random.Range(-5f, 5f);
            float y = UnityEngine.Random.Range(-3f, 3f);
            Points.Add(new Vector2(x, y));
        }

        OnPointsChanged?.Invoke();
    }
}