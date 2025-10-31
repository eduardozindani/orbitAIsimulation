using UnityEngine;

/// <summary>
/// Chase camera for Mission Spaces - follows target (ISS) from behind.
/// Camera maintains a fixed offset from the target, perpendicular to its orbit trajectory.
/// Creates a "following along" perspective where you see the target with Earth in background.
/// User cannot move position with WASD/arrows, but can rotate view with mouse.
/// </summary>
public class FixedObserverCamera : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("What to follow (e.g., ISS_Satellite)")]
    public Transform target;

    [Header("Chase Distance")]
    [Tooltip("Distance behind target (perpendicular to orbit)")]
    public float chaseDistance = 2f;

    [Tooltip("Height offset above orbital plane")]
    public float heightOffset = 0.3f;

    [Header("Look Around (Mouse & Arrows)")]
    [Tooltip("Allow mouse drag to rotate view")]
    public bool allowMouseLook = true;

    [Tooltip("Mouse sensitivity for looking around")]
    public float mouseSensitivity = 2f;

    [Tooltip("Allow arrow keys to rotate view")]
    public bool allowArrowKeys = true;

    [Tooltip("Arrow key rotation speed (degrees per second)")]
    public float arrowKeySpeed = 60f;

    [Header("Camera Smoothing")]
    [Tooltip("How smoothly camera follows target (0=instant, 1=very smooth)")]
    public float followSmoothness = 0.1f;

    [Header("Limits")]
    public float minPitch = -80f;
    public float maxPitch = 80f;

    private float userYaw = 0f;   // User's look-around rotation
    private float userPitch = 0f;
    private Transform cam;
    private bool isMouseLooking = false;
    private Vector3 lastTargetPosition;

    void Start()
    {
        // Find the camera
        cam = GetComponentInChildren<Camera>().transform;
        if (cam == null)
        {
            Debug.LogError("[FixedObserverCamera] No camera found in children!");
            return;
        }

        if (target == null)
        {
            Debug.LogError("[FixedObserverCamera] No target assigned! Assign ISS_Satellite.");
            return;
        }

        // Initialize last position
        lastTargetPosition = target.position;

        // Initialize look-around rotation
        userYaw = 0f;
        userPitch = 0f;
    }

    void Update()
    {
        if (cam == null || target == null) return;

        // Handle mouse look (right-click drag to look around)
        if (allowMouseLook)
        {
            if (Input.GetMouseButtonDown(1)) // Right mouse button pressed
            {
                isMouseLooking = true;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (Input.GetMouseButtonUp(1)) // Right mouse button released
            {
                isMouseLooking = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (isMouseLooking)
            {
                float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
                float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

                userYaw += mouseX;
                userPitch -= mouseY; // Inverted for natural feel
                userPitch = Mathf.Clamp(userPitch, minPitch, maxPitch);
            }
        }

        // Handle arrow key look (always active, no button press needed)
        if (allowArrowKeys)
        {
            float horizontal = 0f;
            float vertical = 0f;

            if (Input.GetKey(KeyCode.LeftArrow))  horizontal -= 1f;
            if (Input.GetKey(KeyCode.RightArrow)) horizontal += 1f;
            if (Input.GetKey(KeyCode.UpArrow))    vertical += 1f;
            if (Input.GetKey(KeyCode.DownArrow))  vertical -= 1f;

            if (horizontal != 0f || vertical != 0f)
            {
                float arrowX = horizontal * arrowKeySpeed * Time.deltaTime;
                float arrowY = vertical * arrowKeySpeed * Time.deltaTime;

                userYaw += arrowX;
                userPitch += arrowY;
                userPitch = Mathf.Clamp(userPitch, minPitch, maxPitch);
            }
        }

        UpdateCameraPosition();
    }

    void UpdateCameraPosition()
    {
        Vector3 targetPos = target.position;
        Vector3 planetCenter = transform.position; // CameraRig is at planet center

        // Vector from planet center to ISS (radial direction - pointing away from Earth)
        Vector3 radialDirection = (targetPos - planetCenter).normalized;

        // Camera position: BEHIND ISS, further away from Earth than ISS
        // Position camera along the radial line, beyond the ISS
        Vector3 desiredPosition = targetPos + (radialDirection * chaseDistance);

        // Add slight perpendicular offset for better ISS visibility
        Vector3 targetVelocity = (targetPos - lastTargetPosition);
        if (targetVelocity.magnitude > 0.001f)
        {
            targetVelocity.Normalize();
            Vector3 sideOffset = Vector3.Cross(targetVelocity, radialDirection).normalized;
            desiredPosition += sideOffset * heightOffset; // Side offset for seeing ISS better
        }

        // Smooth follow (moves with ISS orbit)
        cam.position = Vector3.Lerp(cam.position, desiredPosition, 1f - followSmoothness);

        // Camera looks at EARTH CENTER
        // This creates the view: Camera → ISS (in front) → Earth (behind ISS)
        Vector3 lookDirection = (planetCenter - cam.position).normalized;

        // Apply user's look-around rotation
        Quaternion userRotation = Quaternion.Euler(userPitch, userYaw, 0f);
        lookDirection = userRotation * lookDirection;

        cam.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);

        // Store position for next frame
        lastTargetPosition = targetPos;
    }

    /// <summary>
    /// Reset look-around to default (looking straight at target)
    /// </summary>
    public void ResetView()
    {
        userYaw = 0f;
        userPitch = 0f;
    }
}
