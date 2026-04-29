using UnityEngine;
using UnityEngine.InputSystem;

// Caméra orbitale :
// - Clic-gauche maintenu + souris : tourner autour de target
// - Molette : zoomer
// - Clic-droit maintenu + souris : pan latéral
// - Touche F : reecentrer
public class OrbitCamera : MonoBehaviour
{
    public Transform target;
    public float distance = 8f;
    public float minDistance = 1f;
    public float maxDistance = 50f;

    public float rotateSpeed = 0.2f;
    public float zoomSpeed = 1f;
    public float panSpeed = 0.005f;

    public float minPitch = -89f;
    public float maxPitch = 89f;

    private float yaw;
    private float pitch = 20f;
    private Vector3 panOffset = Vector3.zero;

    void Start()
    {
        if (target == null)
        {
            var go = new GameObject("OrbitCameraTarget");
            target = go.transform;
        }

        var euler = transform.eulerAngles;
        yaw = euler.y;
        pitch = euler.x;
    }

    void LateUpdate()
    {
        if (target == null) return;

        var mouse = Mouse.current;
        var keyboard = Keyboard.current;
        if (mouse == null) return;
        
        var mouseDelta = mouse.delta.ReadValue();
        
        if (mouse.leftButton.isPressed)
        {
            yaw += mouseDelta.x * rotateSpeed;
            pitch -= mouseDelta.y * rotateSpeed;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        var scroll = mouse.scroll.ReadValue().y / 120f;
        if (scroll != 0f)
        {
            distance -= scroll * zoomSpeed;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }
        
        if (mouse.rightButton.isPressed)
        {
            panOffset += -transform.right * mouseDelta.x * panSpeed * distance;
            panOffset += -transform.up * mouseDelta.y * panSpeed * distance;
        }
        
        if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
        {
            panOffset = Vector3.zero;
        }
        
        var rotation = Quaternion.Euler(pitch, yaw, 0f);
        var pivot = target.position + panOffset;
        var offset = rotation * new Vector3(0f, 0f, -distance);

        transform.position = pivot + offset;
        transform.rotation = rotation;
    }
}