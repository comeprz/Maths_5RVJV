using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PointManager3D : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;
    public GameObject pointPrefab;
    public Transform pointsParent;

    [Header("Random generation")]
    public float spawnRadius = 3f;

    [Header("Manual placement")]
    public bool enableManualPlacement = true;
    public float currentHeight = 0f;
    public float heightStep = 0.25f;
    public float minHeight = -3f;
    public float maxHeight = 3f;

    [Header("Preview")]
    public GameObject previewPrefab;
    private GameObject previewObject;

    private readonly List<Vector3> points = new List<Vector3>();
    private readonly List<GameObject> pointObjects = new List<GameObject>();

    public List<Vector3> Points => points;

    public event Action OnPointsChanged;

    void Start()
    {
        CreatePreview();
        UpdatePreview();
    }

    void Update()
    {
        if (!enableManualPlacement)
            return;

        if (Mouse.current == null)
            return;

        HandleHeightInput();
        UpdatePreview();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (TryGetMouseWorldPosition(out Vector3 position))
            {
                AddPoint(position);
            }
        }
    }

    void HandleHeightInput()
    {
        Vector2 scroll = Mouse.current.scroll.ReadValue();

        if (Mathf.Abs(scroll.y) > 0.01f)
        {
            currentHeight += Mathf.Sign(scroll.y) * heightStep;
            currentHeight = Mathf.Clamp(currentHeight, minHeight, maxHeight);
        }

        if (Keyboard.current != null)
        {
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                currentHeight += heightStep;
                currentHeight = Mathf.Clamp(currentHeight, minHeight, maxHeight);
            }

            if (Keyboard.current.qKey.wasPressedThisFrame)
            {
                currentHeight -= heightStep;
                currentHeight = Mathf.Clamp(currentHeight, minHeight, maxHeight);
            }
        }
    }

    bool TryGetMouseWorldPosition(out Vector3 position)
    {
        position = Vector3.zero;

        if (mainCamera == null)
            return false;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);

        // Plan horizontal XZ à y = 0.
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (!groundPlane.Raycast(ray, out float distance))
            return false;

        Vector3 hitPoint = ray.GetPoint(distance);

        // On garde X et Z du clic, et on met Y avec la hauteur actuelle.
        position = new Vector3(hitPoint.x, currentHeight, hitPoint.z);

        return true;
    }

    void CreatePreview()
    {
        if (previewPrefab != null)
        {
            previewObject = Instantiate(previewPrefab);
        }
        else
        {
            previewObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            previewObject.transform.localScale = Vector3.one * 0.08f;

            Collider col = previewObject.GetComponent<Collider>();
            if (col != null)
                Destroy(col);
        }

        previewObject.name = "Point3D_Preview";
    }

    void UpdatePreview()
    {
        if (previewObject == null)
            return;

        if (TryGetMouseWorldPosition(out Vector3 position))
        {
            previewObject.SetActive(true);
            previewObject.transform.position = position;
        }
        else
        {
            previewObject.SetActive(false);
        }
    }

    public void ClearPoints()
    {
        foreach (GameObject go in pointObjects)
        {
            if (go != null)
                Destroy(go);
        }

        pointObjects.Clear();
        points.Clear();

        OnPointsChanged?.Invoke();
    }

    public void GenerateRandomPoints(int count)
    {
        ClearPoints();

        for (int i = 0; i < count; i++)
        {
            Vector3 p = UnityEngine.Random.insideUnitSphere * spawnRadius;
            AddPoint(p);
        }
    }

    public void AddPoint(Vector3 position)
    {
        points.Add(position);

        GameObject go;

        if (pointPrefab != null)
        {
            go = Instantiate(pointPrefab, position, Quaternion.identity, pointsParent);
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 0.08f;

            if (pointsParent != null)
                go.transform.SetParent(pointsParent);
        }

        pointObjects.Add(go);

        Debug.Log("Point 3D ajouté : " + position);

        OnPointsChanged?.Invoke();
    }
}