using UnityEngine;

public class CameraHandler : MonoBehaviour
{
    [Header("Target & Offset")]
    public Transform cameraTarget;
    public Vector3 startOffset = new Vector3(0, 5, -10); // Starting behind/above player
    public float smoothSpeed = 0.25f;

    [Header("Drag & Scroll Settings")]
    public float dragSpeed = 0.5f;
    public float scrollSpeed = 5f;
    public float minZoom = 3f;
    public float maxZoom = 20f;

    private Vector3 velocity = Vector3.zero;
    private Vector3 initialPosition;

    private void Start()
    {
        if (cameraTarget == null)
        {
            Debug.LogError("Camera target not assigned!");
            return;
        }

        // Initial camera position relative to player
        initialPosition = cameraTarget.position + startOffset;
        transform.position = initialPosition;
    }

    private void Update()
    {
        HandleDrag();
        HandleScroll();
        HandleReset();
    }

    // ==========================
    // Drag camera in X/Y plane
    // ==========================
    private void HandleDrag()
    {
        if (Input.GetMouseButton(0)) // Left-click hold
        {
            float moveX = -Input.GetAxis("Mouse X") * dragSpeed;
            float moveY = -Input.GetAxis("Mouse Y") * dragSpeed;

            // Move camera horizontally and vertically
            transform.position += new Vector3(moveX, moveY, 0);
        }
    }

    // ==========================
    // Scroll zoom
    // ==========================
    private void HandleScroll()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            Vector3 zoomDir = transform.forward * scroll * scrollSpeed;
            transform.position += zoomDir;

            // Clamp zoom distance from player
            float distance = Vector3.Distance(transform.position, cameraTarget.position);
            if (distance < minZoom)
            {
                transform.position = cameraTarget.position + (transform.position - cameraTarget.position).normalized * minZoom;
            }
            else if (distance > maxZoom)
            {
                transform.position = cameraTarget.position + (transform.position - cameraTarget.position).normalized * maxZoom;
            }
        }
    }

    // ==========================
    // Reset camera to starting position
    // ==========================
    private void HandleReset()
    {
        if (Input.GetMouseButtonDown(2)) // Middle click
        {
            transform.position = initialPosition;
        }
    }
}
