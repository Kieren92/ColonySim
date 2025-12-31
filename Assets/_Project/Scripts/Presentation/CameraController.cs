using UnityEngine;

/// <summary>
/// Improved isometric camera with angled zoom and cursor-based zoom.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("How fast the camera moves when using keyboard/mouse")]
    [SerializeField] private float moveSpeed = 20f;

    [Tooltip("How fast the camera moves when at edge of screen")]
    [SerializeField] private float edgeScrollSpeed = 15f;

    [Tooltip("Pixel distance from screen edge to trigger scrolling")]
    [SerializeField] private float edgeScrollBorder = 20f;

    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 10f;
    [SerializeField] private float minZoom = 10f;  // Closest to ground
    [SerializeField] private float maxZoom = 50f;  // Furthest from ground

    [Tooltip("How much to move towards cursor when zooming in (0-1)")]
    [SerializeField] private float zoomToCursorAmount = 0.3f;

    [Header("Boundaries")]
    [Tooltip("How far the camera can move in X direction")]
    [SerializeField] private float boundaryX = 50f;

    [Tooltip("How far the camera can move in Z direction")]
    [SerializeField] private float boundaryZ = 50f;

    private Camera mainCamera;
    private Vector3 targetPosition;

    /// <summary>
    /// Initialize camera reference and set starting position.
    /// </summary>
    private void Start()
    {
        // Get camera - either from Camera.main or find it as child
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = GetComponentInChildren<Camera>();
        }

        if (mainCamera == null)
        {
            Debug.LogError("CameraController: No camera found! Make sure Main Camera is a child of CameraRig.");
        }

        targetPosition = transform.position;
    }

    /// <summary>
    /// Update is called once per frame - this is where we handle all input.
    /// </summary>
    private void Update()
    {
        HandleKeyboardMovement();
        HandleMouseEdgeMovement();
        HandleZoom();

        // Smoothly move camera to target position
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 5f);
    }

    /// <summary>
    /// Handles WASD/Arrow key movement.
    /// </summary>
    private void HandleKeyboardMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 direction = new Vector3(horizontal, 0, vertical).normalized;

        targetPosition += direction * moveSpeed * Time.deltaTime;

        // Clamp to boundaries
        targetPosition.x = Mathf.Clamp(targetPosition.x, -boundaryX, boundaryX);
        targetPosition.z = Mathf.Clamp(targetPosition.z, -boundaryZ, boundaryZ);
    }

    /// <summary>
    /// Handles movement when mouse is at screen edge.
    /// </summary>
    private void HandleMouseEdgeMovement()
    {
        Vector3 mousePos = Input.mousePosition;
        Vector3 direction = Vector3.zero;

        if (mousePos.x < edgeScrollBorder)
            direction.x = -1;
        else if (mousePos.x > Screen.width - edgeScrollBorder)
            direction.x = 1;

        if (mousePos.y < edgeScrollBorder)
            direction.z = -1;
        else if (mousePos.y > Screen.height - edgeScrollBorder)
            direction.z = 1;

        if (direction != Vector3.zero)
        {
            targetPosition += direction.normalized * edgeScrollSpeed * Time.deltaTime;
            targetPosition.x = Mathf.Clamp(targetPosition.x, -boundaryX, boundaryX);
            targetPosition.z = Mathf.Clamp(targetPosition.z, -boundaryZ, boundaryZ);
        }
    }

    /// <summary>
    /// Improved zoom with angled movement and cursor targeting.
    /// </summary>
    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0)
        {
            // Get the child camera's forward direction for proper zoom angle
            Transform cameraTransform = mainCamera != null ? mainCamera.transform : transform;
            Vector3 zoomDirection = cameraTransform.forward;

            // Scroll UP (positive) = zoom IN (forward)
            // Scroll DOWN (negative) = zoom OUT (backward)
            float zoomAmount = scroll * zoomSpeed;
            Vector3 zoomMovement = zoomDirection * zoomAmount;

            // If zooming IN (scroll positive), add cursor movement
            if (scroll > 0 && zoomToCursorAmount > 0 && mainCamera != null)
            {
                Vector3 cursorMovement = GetMovementTowardsCursor();
                zoomMovement += cursorMovement * zoomToCursorAmount * zoomAmount;
            }

            // Apply movement
            Vector3 newPosition = targetPosition + zoomMovement;

            // Clamp to boundaries
            newPosition.y = Mathf.Clamp(newPosition.y, minZoom, maxZoom);
            newPosition.x = Mathf.Clamp(newPosition.x, -boundaryX, boundaryX);
            newPosition.z = Mathf.Clamp(newPosition.z, -boundaryZ, boundaryZ);

            targetPosition = newPosition;
        }
    }

    /// <summary>
    /// Calculate movement direction towards cursor position in world.
    /// </summary>
    private Vector3 GetMovementTowardsCursor()
    {
        if (mainCamera == null) return Vector3.zero;

        // Raycast from cursor to ground
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        // Create a horizontal plane at y=0 (ground level)
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        float distance;
        if (groundPlane.Raycast(ray, out distance))
        {
            // Get world point where cursor is pointing
            Vector3 cursorWorldPoint = ray.GetPoint(distance);

            // Direction from camera to cursor (only XZ, no Y)
            Vector3 directionToCursor = cursorWorldPoint - transform.position;
            directionToCursor.y = 0; // Flatten to horizontal

            return directionToCursor.normalized;
        }

        return Vector3.zero;
    }

    /// <summary>
    /// Debug visualization - shows camera boundaries in Scene view.
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3(0, 0, 0);
        Vector3 size = new Vector3(boundaryX * 2, 0, boundaryZ * 2);
        Gizmos.DrawWireCube(center, size);
    }
}