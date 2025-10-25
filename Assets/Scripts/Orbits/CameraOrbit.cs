using UnityEngine;

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
            // input
            float h = Input.GetKey(KeyCode.RightArrow) ?  1f :
                      Input.GetKey(KeyCode.LeftArrow)  ? -1f : 0f;
            float v = Input.GetKey(KeyCode.UpArrow)    ?  1f :
                      Input.GetKey(KeyCode.DownArrow)  ? -1f : 0f;

            // Use unscaledDeltaTime so camera controls are NOT affected by Time.timeScale
            yaw   += h * yawSpeed   * Time.unscaledDeltaTime;
            pitch += v * pitchSpeed * Time.unscaledDeltaTime;
            pitch  = Mathf.Clamp(pitch, minPitch, maxPitch);

            // zoom
            float scroll = Input.mouseScrollDelta.y;
            radius = Mathf.Clamp(radius - scroll * zoomSpeed, minRadius, maxRadius);
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
}
