using UnityEngine;

// XR Input support for Quest controllers
#if UNITY_ANDROID || UNITY_STANDALONE
using UnityEngine.XR;
#endif

/// <summary>
/// Simple spherical camera controller: starts at a given pitch and radius,
/// then allows arrow-key orbit and mouse-wheel zoom.
/// Attach to CameraRig (at planet center), drag Main Camera into "cam".
/// </summary>
public class CameraSphereController : MonoBehaviour
{
    [Header("Child Camera")]
    public Transform cam;                // drag your Main Camera here

    [Header("Initial View")]
    [Tooltip("Starting yaw angle in degrees")]
    public float initialYaw = 0f;
    [Tooltip("Starting pitch angle in degrees")]
    public float initialPitch = 20f;
    [Tooltip("Starting distance from center")]
    public float startRadius = 10f;

    [Header("Movement Speeds")]
    public float yawSpeed   = 80f;       // left/right arrows
    public float pitchSpeed = 80f;       // up/down arrows
    public float zoomSpeed  = 5f;        // mouse wheel

    [Header("Limits")]
    public float minPitch = -80f;
    public float maxPitch =  80f;
    public float minRadius = 3f;
    public float maxRadius = 30f;

    float yaw;
    float pitch;
    float radius;

    [HideInInspector]
    public bool allowExternalRadiusControl = false;

    void Start()
    {
        if (cam == null)
            cam = GetComponentInChildren<Camera>().transform;

        // set initial orientation and distance
        yaw    = initialYaw;
        pitch  = Mathf.Clamp(initialPitch, minPitch, maxPitch);

        // Only set initial radius if not being controlled externally (for intro)
        if (!allowExternalRadiusControl)
        {
            radius = Mathf.Clamp(startRadius, minRadius, maxRadius);
        }

        UpdateCamera();
    }

    void Update()
    {
        // Only handle user input if not being controlled externally
        if (!allowExternalRadiusControl)
        {
            // Get input from keyboard OR Quest controllers
            float h = GetHorizontalInput();  // Yaw (left/right spin)
            float v = GetVerticalInput();    // Pitch (up/down to poles)

            // Use unscaledDeltaTime so camera controls are NOT affected by Time.timeScale
            yaw   += h * yawSpeed   * Time.unscaledDeltaTime;
            pitch += v * pitchSpeed * Time.unscaledDeltaTime;
            pitch  = Mathf.Clamp(pitch, minPitch, maxPitch);

            // Zoom from mouse wheel OR left thumbstick
            float zoomDelta = GetZoomInput();
            radius = Mathf.Clamp(radius - zoomDelta, minRadius, maxRadius);
        }

        // Always update camera position (even during external control)
        UpdateCamera();
    }

    void UpdateCamera()
    {
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pos = transform.position + rot * Vector3.back * radius;
        cam.position = pos;
        cam.LookAt(transform.position, Vector3.up);
    }

    // ---------------- External Control (for intro cutscene) ----------------

    /// <summary>
    /// Set camera radius externally (used by ExperienceManager during intro)
    /// </summary>
    public void SetRadius(float newRadius)
    {
        radius = newRadius;
    }

    /// <summary>
    /// Get the target "normal" viewing distance
    /// </summary>
    public float GetTargetRadius()
    {
        return startRadius;
    }

    // ---------------- Input Methods (Desktop + VR) ----------------

    private const float THUMBSTICK_DEADZONE = 0.15f;

    /// <summary>
    /// Get horizontal input for yaw rotation (left/right).
    /// Checks keyboard arrow keys OR Quest right thumbstick X-axis.
    /// </summary>
    private float GetHorizontalInput()
    {
        // Desktop keyboard input
        float keyboard = Input.GetKey(KeyCode.RightArrow) ?  1f :
                         Input.GetKey(KeyCode.LeftArrow)  ? -1f : 0f;

        // If keyboard input detected, use it (takes priority)
        if (Mathf.Abs(keyboard) > 0.01f)
            return keyboard;

        #if UNITY_ANDROID || UNITY_STANDALONE
        // Quest controller - right thumbstick X-axis
        if (UnityEngine.XR.XRSettings.isDeviceActive)
        {
            InputDevice rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (rightController.isValid)
            {
                Vector2 thumbstick;
                if (rightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out thumbstick))
                {
                    // Apply deadzone to prevent drift
                    if (Mathf.Abs(thumbstick.x) > THUMBSTICK_DEADZONE)
                    {
                        return -thumbstick.x;  // Inverted: left → left, right → right
                    }
                }
            }
        }
        #endif

        return 0f;
    }

    /// <summary>
    /// Get vertical input for pitch rotation (up/down).
    /// Checks keyboard arrow keys OR Quest right thumbstick Y-axis.
    /// </summary>
    private float GetVerticalInput()
    {
        // Desktop keyboard input
        float keyboard = Input.GetKey(KeyCode.UpArrow)   ?  1f :
                         Input.GetKey(KeyCode.DownArrow) ? -1f : 0f;

        // If keyboard input detected, use it (takes priority)
        if (Mathf.Abs(keyboard) > 0.01f)
            return keyboard;

        #if UNITY_ANDROID || UNITY_STANDALONE
        // Quest controller - right thumbstick Y-axis
        if (UnityEngine.XR.XRSettings.isDeviceActive)
        {
            InputDevice rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (rightController.isValid)
            {
                Vector2 thumbstick;
                if (rightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out thumbstick))
                {
                    // Apply deadzone to prevent drift
                    if (Mathf.Abs(thumbstick.y) > THUMBSTICK_DEADZONE)
                    {
                        return thumbstick.y;
                    }
                }
            }
        }
        #endif

        return 0f;
    }

    /// <summary>
    /// Get zoom input (in/out).
    /// Checks mouse scroll wheel OR Quest left thumbstick Y-axis.
    /// Returns zoom delta (positive = zoom in, negative = zoom out).
    /// </summary>
    private float GetZoomInput()
    {
        // Desktop mouse scroll wheel (discrete ticks)
        float scroll = Input.mouseScrollDelta.y;

        // If scroll detected, use it (takes priority)
        if (Mathf.Abs(scroll) > 0.01f)
            return scroll * zoomSpeed; // Already scaled by zoomSpeed

        #if UNITY_ANDROID || UNITY_STANDALONE
        // Quest controller - left thumbstick Y-axis (continuous)
        if (UnityEngine.XR.XRSettings.isDeviceActive)
        {
            InputDevice leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            if (leftController.isValid)
            {
                Vector2 thumbstick;
                if (leftController.TryGetFeatureValue(CommonUsages.primary2DAxis, out thumbstick))
                {
                    // Apply deadzone to prevent drift
                    if (Mathf.Abs(thumbstick.y) > THUMBSTICK_DEADZONE)
                    {
                        // Continuous input: scale by zoomSpeed and deltaTime
                        return thumbstick.y * zoomSpeed * Time.unscaledDeltaTime;
                    }
                }
            }
        }
        #endif

        return 0f;
    }
}
