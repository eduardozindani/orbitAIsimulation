using UnityEngine;
using System.Reflection;
using System.Collections;

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

    // VR support
    private Transform _trackingSpace;
    private bool _isVR = false;
    private bool _debugLoggedOnce = false; // Only log once per session

    void Start()
    {
        // Detect VR mode
        #if UNITY_ANDROID || UNITY_STANDALONE
        _isVR = UnityEngine.XR.XRSettings.isDeviceActive;
        #endif

        // Find camera reference
        if (cam == null)
        {
            if (_isVR)
            {
                // VR: Try to find OVR camera rig structure
                _trackingSpace = FindTrackingSpace();
                if (_trackingSpace != null)
                {
                    // Find camera in OVR rig (LeftEyeAnchor or RightEyeAnchor)
                    Camera ovrCamera = _trackingSpace.GetComponentInChildren<Camera>();
                    if (ovrCamera != null)
                    {
                        cam = ovrCamera.transform;
                    }
                }
            }
            
            // Fallback: use standard camera discovery
            if (cam == null)
            {
                cam = GetComponentInChildren<Camera>().transform;
            }
        }

        // set initial orientation and distance
        yaw    = initialYaw;
        pitch  = Mathf.Clamp(initialPitch, minPitch, maxPitch);

        // Only set initial radius if not being controlled externally (for intro)
        if (!allowExternalRadiusControl)
        {
            radius = Mathf.Clamp(startRadius, minRadius, maxRadius);
        }

        // Debug: Log initial state in VR mode
        #if UNITY_ANDROID || UNITY_STANDALONE
        if (_isVR)
        {
            Debug.Log($"[CameraSphereController] Start() - VR Mode Detected");
            Debug.Log($"[CameraSphereController] TrackingSpace found: {_trackingSpace != null}");
            if (_trackingSpace != null)
            {
                Debug.Log($"[CameraSphereController] TrackingSpace: {_trackingSpace.name}, active: {_trackingSpace.gameObject.activeSelf}");
                
                // Check eye cameras
                Transform leftEye = _trackingSpace.Find("LeftEyeAnchor");
                Transform rightEye = _trackingSpace.Find("RightEyeAnchor");
                Debug.Log($"[CameraSphereController] LeftEyeAnchor: {(leftEye != null ? leftEye.name + (leftEye.gameObject.activeSelf ? " (active)" : " (inactive)") : "NOT FOUND")}");
                Debug.Log($"[CameraSphereController] RightEyeAnchor: {(rightEye != null ? rightEye.name + (rightEye.gameObject.activeSelf ? " (active)" : " (inactive)") : "NOT FOUND")}");
                
                if (leftEye != null)
                {
                    Camera leftCam = leftEye.GetComponent<Camera>();
                    Debug.Log($"[CameraSphereController] LeftEye Camera: {(leftCam != null ? "EXISTS, enabled: " + leftCam.enabled : "MISSING")}");
                }
            }
            Debug.Log($"[CameraSphereController] cam reference: {(cam != null ? cam.name : "NULL")}");
            
            // Check OVRCameraRig component
            Component[] components = GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp.GetType().Name == "OVRCameraRig")
                {
                    MonoBehaviour mb = comp as MonoBehaviour;
                    bool isEnabled = mb != null ? mb.enabled : true;
                    Debug.Log($"[CameraSphereController] OVRCameraRig found, enabled: {isEnabled}");
                    break;
                }
            }
        }
        #endif

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
        // Re-detect VR mode in case it changes
        #if UNITY_ANDROID || UNITY_STANDALONE
        _isVR = UnityEngine.XR.XRSettings.isDeviceActive;
        #endif

        if (!_isVR)
        {
            // Desktop: existing behavior (position + LookAt)
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 pos = transform.position + rot * Vector3.back * radius;
            cam.position = pos;
            cam.LookAt(transform.position, Vector3.up);
        }
        else
        {
            // VR: Keep CameraRig at identity rotation (OVR requires this for head tracking)
            // Position and rotate TrackingSpace instead to achieve orbit effect
            transform.rotation = Quaternion.identity; // Critical: OVR needs CameraRig at identity

            // Position TrackingSpace relative to pivot (orbit radius)
            // OVRCameraRig will handle camera rotation via head tracking
            if (_trackingSpace == null)
            {
                _trackingSpace = FindTrackingSpace();
            }

            if (_trackingSpace != null)
            {
                // Calculate orbit position in world space
                Quaternion orbitRot = Quaternion.Euler(pitch, yaw, 0f);
                Vector3 orbitOffset = orbitRot * Vector3.back * radius;
                
                // Position TrackingSpace at orbit distance (world space)
                _trackingSpace.position = transform.position + orbitOffset;
                
                // Orient TrackingSpace to face Earth (provides natural "forward" direction)
                // OVR applies head tracking as LOCAL rotations on eye anchors relative to this
                Vector3 lookDir = transform.position - _trackingSpace.position;
                if (lookDir.sqrMagnitude > 0.01f)
                {
                    _trackingSpace.rotation = Quaternion.LookRotation(lookDir, Vector3.up);
                }
                
                // Debug: Log once to verify eye cameras and check if OVR is updating rotation
                if (!_debugLoggedOnce)
                {
                    Transform leftEye = _trackingSpace.Find("LeftEyeAnchor");
                    Transform rightEye = _trackingSpace.Find("RightEyeAnchor");
                    Transform centerEye = _trackingSpace.Find("CenterEyeAnchor");
                    
                    Debug.Log($"[CameraSphereController] VR Path: TrackingSpace found");
                    Debug.Log($"[CameraSphereController] TrackingSpace world pos: {_trackingSpace.position}, world rot: {_trackingSpace.rotation.eulerAngles}");
                    Debug.Log($"[CameraSphereController] CameraRig rotation: {transform.rotation.eulerAngles} (should be identity)");
                    Debug.Log($"[CameraSphereController] Orbit: pitch={pitch}, yaw={yaw}, radius={radius}");
                    
                    if (leftEye != null)
                    {
                        Camera leftCam = leftEye.GetComponent<Camera>();
                        Debug.Log($"[CameraSphereController] LeftEyeAnchor - local rot: {leftEye.localRotation.eulerAngles}, cam enabled: {(leftCam != null ? leftCam.enabled.ToString() : "NO CAMERA")}");
                    }
                    if (centerEye != null)
                    {
                        Camera centerCam = centerEye.GetComponent<Camera>();
                        Debug.Log($"[CameraSphereController] CenterEyeAnchor - local rot: {centerEye.localRotation.eulerAngles}, cam enabled: {(centerCam != null ? centerCam.enabled.ToString() : "NO CAMERA")}");
                    }
                    if (rightEye != null)
                    {
                        Camera rightCam = rightEye.GetComponent<Camera>();
                        Debug.Log($"[CameraSphereController] RightEyeAnchor - local rot: {rightEye.localRotation.eulerAngles}, cam enabled: {(rightCam != null ? rightCam.enabled.ToString() : "NO CAMERA")}");
                    }
                    
                    // Schedule a delayed check to see if eye rotation changes (OVR should update it)
                    StartCoroutine(CheckEyeCameraRotation(leftEye, centerEye));
                    
                    _debugLoggedOnce = true;
                }
                // DO NOT modify camera rotation - let OVR handle head tracking
            }
            else
            {
                // Fallback: if no TrackingSpace found, use standard camera positioning
                // But still don't force LookAt to allow head tracking
                if (!_debugLoggedOnce)
                {
                    Debug.LogWarning($"[CameraSphereController] VR Mode but TrackingSpace NOT FOUND - using fallback path with cam: {(cam != null ? cam.name : "NULL")}");
                    _debugLoggedOnce = true;
                }
                Quaternion rot2 = Quaternion.Euler(pitch, yaw, 0f);
                Vector3 pos = transform.position + rot2 * Vector3.back * radius;
                cam.position = pos;
                // Don't call LookAt in VR - let XR system handle rotation
            }
        }
    }

    /// <summary>
    /// Find OVR TrackingSpace transform (used for VR head tracking)
    /// </summary>
    private Transform FindTrackingSpace()
    {
        #if UNITY_ANDROID || UNITY_STANDALONE
        // Method 1: Find by name (OVRCameraRig creates TrackingSpace as child)
        Transform trackingSpace = transform.Find("TrackingSpace");
        if (trackingSpace != null)
        {
            return trackingSpace;
        }

        // Method 2: Find OVRCameraRig component and get its tracking space reference
        // Use reflection to avoid compile errors if OVR namespace not available
        Component[] components = GetComponents<Component>();
        Component ovrRig = null;
        foreach (var comp in components)
        {
            if (comp.GetType().Name == "OVRCameraRig")
            {
                ovrRig = comp;
                break;
            }
        }
        
        if (ovrRig != null)
        {
            // Try to get trackingSpace property via reflection
            var trackingSpaceProp = ovrRig.GetType().GetProperty("trackingSpace");
            if (trackingSpaceProp != null)
            {
                var trackingSpaceObj = trackingSpaceProp.GetValue(ovrRig);
                if (trackingSpaceObj is Transform ts)
                {
                    return ts;
                }
            }
            
            // Alternative: TrackingSpace might be a child
            trackingSpace = ovrRig.transform.Find("TrackingSpace");
            if (trackingSpace != null)
            {
                return trackingSpace;
            }
        }
        #endif

        return null;
    }

    /// <summary>
    /// Coroutine to check if eye camera rotation changes (verifies OVR head tracking is working)
    /// </summary>
    private IEnumerator CheckEyeCameraRotation(Transform leftEye, Transform centerEye)
    {
        if (leftEye == null && centerEye == null) yield break;
        
        Transform eyeToCheck = centerEye != null ? centerEye : leftEye;
        Quaternion initialRot = eyeToCheck.localRotation;
        
        // Wait 0.5 seconds
        yield return new WaitForSeconds(0.5f);
        
        Quaternion afterRot = eyeToCheck.localRotation;
        
        float angleDiff = Quaternion.Angle(initialRot, afterRot);
        Debug.Log($"[CameraSphereController] Eye rotation check (0.5s later): Initial={initialRot.eulerAngles}, After={afterRot.eulerAngles}, Change={angleDiff}°");
        
        if (angleDiff > 1f)
        {
            Debug.Log($"[CameraSphereController] ✓ OVR IS updating eye camera rotation (head tracking working)");
        }
        else
        {
            Debug.LogWarning($"[CameraSphereController] ✗ OVR is NOT updating eye camera rotation - rotation is static! This indicates OVR head tracking is blocked or not working.");
        }
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
