using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PointManager : MonoBehaviour
{
    public Camera mainCamera;

    public List<Vector2> Points { get; private set; } = new List<Vector2>();

    public event Action OnPointsChanged;

    void Start()
    {
        Debug.Log("PointManager actif");
    }

    void Update()
    {
        if (Mouse.current == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Debug.Log("Clic détecté");

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                Debug.Log("Clic bloqué par UI");
                return;
            }

            AddPointFromMouse();
        }
    }

    void AddPointFromMouse()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();

        Vector3 mouseScreenPos = new Vector3(mousePosition.x, mousePosition.y, -mainCamera.transform.position.z);

        Vector3 worldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        Vector2 point = new Vector2(worldPos.x, worldPos.y);

        Debug.Log("Point ajouté : " + point);

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