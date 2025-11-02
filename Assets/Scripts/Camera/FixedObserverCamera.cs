using UnityEngine;
using System.Reflection;

#if UNITY_ANDROID || UNITY_STANDALONE
using UnityEngine.XR;
#endif

/// <summary>
/// Observer camera for Mission Spaces (ISS, Voyager, Hubble).
/// In VR: Positions camera above target satellite, looking down at it with Earth in background.
/// Camera follows target's orbit, maintaining fixed relative position. Head tracking enabled.
/// In Desktop: Mouse/arrow keys can rotate view around target.
/// </summary>
public class FixedObserverCamera : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Satellite to observe (e.g., ISS_Satellite)")]
    public Transform target;

    [Header("VR Camera Position")]
    [Tooltip("Distance above target satellite along radial line (VR mode)")]
    public float heightAboveTarget = 0.25f;
    
    [Tooltip("If the model's pivot is not at its visual center, offset the visual center toward Earth by this amount (meters)")]
    public float centerOffsetTowardEarth = 0.0f;
    
    [Tooltip("Latitude adjustment (meters) moving 'down' along the meridian (toward south). Positive reduces latitude.")]
    public float latitudeOffset = 0.5f;  // Offset by ~half ISS size to center visual bulk

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

    private float userYaw = 0f;      // Desktop: User's look-around rotation (mouse/arrows)
    private float userPitch = 0f;    // Desktop: User's look-around rotation
    private Transform cam;            // Camera transform reference
    private bool isMouseLooking = false;
    private Vector3 lastTargetPosition;  // Used to track target movement
    private bool _isVR = false;
    private Transform _trackingSpace;    // VR: OVR TrackingSpace transform
    private bool _vrCamerasConfigured = false;
    private int _alignDebugCount = 0;

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

                    // Configure VR eye cameras once available
                    ConfigureVREyeCameras();
                }
            }
            
            // Fallback: use standard camera discovery
            if (cam == null)
            {
                cam = GetComponentInChildren<Camera>().transform;
            }
        }
        
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

        // Re-detect VR mode in case it changes
        #if UNITY_ANDROID || UNITY_STANDALONE
        _isVR = UnityEngine.XR.XRSettings.isDeviceActive;
        #endif

        // Handle rotation input (mouse/arrows for desktop, thumbstick for VR)
        if (!_isVR)
        {
            // Desktop: Mouse look (right-click drag)
            if (allowMouseLook)
            {
                if (Input.GetMouseButtonDown(1))
                {
                    isMouseLooking = true;
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }

                if (Input.GetMouseButtonUp(1))
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
                    userPitch -= mouseY;
                    userPitch = Mathf.Clamp(userPitch, minPitch, maxPitch);
                }
            }

            // Desktop: Arrow keys
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
        }
        else
        {
            // VR: Position fixed relative to target, only head tracking for view rotation
        }

        UpdateCameraPosition();
    }

    void UpdateCameraPosition()
    {
        Vector3 targetPos = target.position;
        Vector3 planetCenter = transform.position; // CameraRig is at planet center

        // Calculate radial direction from Earth center to target satellite
        Vector3 radialDirection = (targetPos - planetCenter).normalized;

        // Adjust for model pivot if needed: estimate visual center toward Earth
        Vector3 targetVisualCenter = targetPos - (radialDirection * centerOffsetTowardEarth);

        // Calculate local north direction (tangent to meridian at this point)
        Vector3 northDir = Vector3.ProjectOnPlane(Vector3.up, radialDirection);
        if (northDir.sqrMagnitude < 1e-6f)
        {
            // Near poles: derive a stable north from an orthogonal basis
            Vector3 eastDir = Vector3.Cross(radialDirection, Vector3.forward);
            if (eastDir.sqrMagnitude < 1e-6f) eastDir = Vector3.right;
            northDir = Vector3.Cross(eastDir.normalized, radialDirection).normalized;
        }
        else
        {
            northDir.Normalize();
        }

        // Calculate desired camera position: above visual center + latitude offset (toward south)
        Vector3 desiredPosition = targetVisualCenter
                                 + (radialDirection * heightAboveTarget)
                                 - (northDir * latitudeOffset);
        
        // Calculate look direction: toward Earth center (ISS between camera and Earth)
        Vector3 toEarth = (planetCenter - desiredPosition).normalized;

        if (!_isVR)
        {
            // Desktop: Control camera directly (same positioning logic as VR)
            cam.position = Vector3.Lerp(cam.position, desiredPosition, 1f - followSmoothness);

            // Apply user's look-around rotation (mouse/arrow keys)
            Quaternion userRotation = Quaternion.Euler(userPitch, userYaw, 0f);
            Vector3 rotatedLook = userRotation * toEarth;

            cam.rotation = Quaternion.LookRotation(rotatedLook, Vector3.up);
        }
        else
        {
            // VR: Keep CameraRig at identity rotation (OVR requirement)
            transform.rotation = Quaternion.identity;

            if (_trackingSpace == null)
            {
                _trackingSpace = FindTrackingSpace();
            }

            if (_trackingSpace != null)
            {
                // Calculate camera orientation: look at Earth, with radial direction as "up"
                // This ensures forward view is aligned with line from camera through ISS to Earth center
                Vector3 cameraUp = radialDirection;
                Quaternion lookAtEarth = Quaternion.LookRotation(toEarth, cameraUp);

                // Find eye anchor to get head's local offset within TrackingSpace
                Transform centerEye = _trackingSpace.Find("CenterEyeAnchor");
                if (centerEye == null)
                {
                    centerEye = _trackingSpace.Find("LeftEyeAnchor");
                }
                Vector3 headLocalOffset = centerEye != null ? centerEye.localPosition : Vector3.zero;
                
                // Convert head local offset to world space (relative to TrackingSpace rotation)
                Vector3 headWorldOffset = lookAtEarth * headLocalOffset;

                // Position TrackingSpace such that head (TrackingSpace + head offset) = desired position
                // This ensures your eyes are exactly at the calculated position above ISS
                Vector3 targetTrackingPos = desiredPosition - headWorldOffset;
                _trackingSpace.position = Vector3.Lerp(_trackingSpace.position, targetTrackingPos, 1f - followSmoothness);

                // Set TrackingSpace rotation - OVR applies head tracking as local rotations on eye anchors
                _trackingSpace.rotation = lookAtEarth;

                // Ensure VR eye cameras have a small near clip plane to avoid close-up clipping
                ConfigureVREyeCameras();

                // Alignment diagnostics (first few frames only)
                if (_alignDebugCount < 3)
                {
                    // Build stable local basis at the satellite location
                    Vector3 eastDir = Vector3.Cross(radialDirection, Vector3.up);
                    if (eastDir.sqrMagnitude < 1e-6f) eastDir = Vector3.Cross(radialDirection, Vector3.forward);
                    eastDir.Normalize();
                    Vector3 northDirDbg = Vector3.Cross(eastDir, radialDirection).normalized;

                    // Visual center after pivot correction
                    Vector3 targetVisualCenterDbg = targetPos - (radialDirection * centerOffsetTowardEarth);

                    // Compute head world position from current rig pose
                    Vector3 headWorld = _trackingSpace.position + (_trackingSpace.rotation * headLocalOffset);
                    Vector3 delta = headWorld - targetVisualCenterDbg;

                    float compRadial = Vector3.Dot(delta, radialDirection);
                    float compNorth  = Vector3.Dot(delta, northDirDbg);
                    float compEast   = Vector3.Dot(delta, eastDir);

                    UnityEngine.Debug.Log(
                        $"[ISS_ALIGN F{_alignDebugCount}] hAbove={heightAboveTarget:F3} latOff={latitudeOffset:F3} centerToward={centerOffsetTowardEarth:F3} | " +
                        $"rad={compRadial:F3} north={compNorth:F3} east={compEast:F3} | " +
                        $"head=({headWorld.x:F2},{headWorld.y:F2},{headWorld.z:F2}) visCtr=({targetVisualCenterDbg.x:F2},{targetVisualCenterDbg.y:F2},{targetVisualCenterDbg.z:F2})");

                    _alignDebugCount++;
                }
            }
            else
            {
                Debug.LogWarning("[FixedObserverCamera] VR mode but TrackingSpace not found!");
                cam.position = Vector3.Lerp(cam.position, desiredPosition, 1f - followSmoothness);
            }
        }

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
    /// Configure VR eye cameras (Left/Right/CenterEye) to use a small near clip
    /// plane so that close-up views of the ISS do not get clipped when the user
    /// tilts or leans.
    /// </summary>
    private void ConfigureVREyeCameras()
    {
        if (_vrCamerasConfigured || _trackingSpace == null) return;

        Camera[] eyeCameras = _trackingSpace.GetComponentsInChildren<Camera>(true);
        if (eyeCameras == null || eyeCameras.Length == 0) return;

        // Use conservative near clip for close-up inspection
        const float desiredNear = 0.01f; // 1 cm
        foreach (var c in eyeCameras)
        {
            if (c != null && c.nearClipPlane > desiredNear)
            {
                c.nearClipPlane = desiredNear;
            }
        }

        _vrCamerasConfigured = true;
    }
}
