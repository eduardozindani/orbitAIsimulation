using UnityEngine;
using System.Reflection;

#if UNITY_ANDROID || UNITY_STANDALONE
using UnityEngine.XR;
#endif

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
    public float chaseDistance = 1f;  // Increased from 0.2 for better ISS view

    [Tooltip("Height offset above orbital plane")]
    public float heightOffset = 0.5f;

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

    private float userYaw = 0f;   // User's look-around rotation (desktop mouse or VR thumbstick)
    private float userPitch = 0f;
    private Transform cam;
    private bool isMouseLooking = false;
    private Vector3 lastTargetPosition;
    private bool _isVR = false;
    private bool _debugLoggedOnce = false;
    private Transform _trackingSpace; // For VR head tracking
    private int _debugFrameCount = 0; // Log first few frames to diagnose
    
    // VR controller input settings
    private const float THUMBSTICK_DEADZONE = 0.15f;
    private const float VR_LOOK_SPEED = 60f; // Degrees per second for thumbstick rotation

    void Start()
    {
        // Detect VR mode
        #if UNITY_ANDROID || UNITY_STANDALONE
        _isVR = UnityEngine.XR.XRSettings.isDeviceActive;
        
        // VR: Ensure chase distance is reasonable (override serialized value if too small)
        if (_isVR && chaseDistance < 0.5f)
        {
            chaseDistance = 1.5f; // Good distance for viewing ISS in VR
            Debug.Log($"[FixedObserverCamera] VR mode: adjusted chase distance to {chaseDistance}");
        }
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
            // VR: No controller rotation - just head tracking (fixed view of ISS)
            // User can look around with head movement, but camera stays in fixed position relative to ISS
        }

        UpdateCameraPosition();
    }

    void UpdateCameraPosition()
    {
        Vector3 targetPos = target.position;
        Vector3 planetCenter = transform.position; // CameraRig is at planet center

        // Vector from planet center to ISS (radial direction - pointing away from Earth)
        Vector3 radialDirection = (targetPos - planetCenter).normalized;

        // VR: Position camera ABOVE ISS (further from Earth along same radial line)
        // This puts you at higher altitude, looking down at ISS with Earth in background
        float heightAboveISS = 1.0f; // Distance above ISS
        Vector3 desiredPosition = targetPos + (radialDirection * heightAboveISS);
        
        // Look toward Earth center (so ISS is between you and Earth)
        Vector3 toEarth = (planetCenter - desiredPosition).normalized;

        if (!_isVR)
        {
            // Desktop: Control camera directly
            cam.position = Vector3.Lerp(cam.position, desiredPosition, 1f - followSmoothness);

            // Apply user's look-around rotation
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
                // Position TrackingSpace ABOVE ISS (higher altitude, same orbit)
                _trackingSpace.position = Vector3.Lerp(_trackingSpace.position, desiredPosition, 1f - followSmoothness);

                // Orient TrackingSpace to look DOWN at Earth
                // Use radial direction as "up" (perpendicular to Earth's surface)
                // This makes your natural forward view aligned with ISS→Earth line
                Vector3 cameraUp = radialDirection; // Up = away from Earth (radial)
                Quaternion lookAtEarth = Quaternion.LookRotation(toEarth, cameraUp);
                _trackingSpace.rotation = lookAtEarth;
                
                // DIAGNOSTIC: Log first 3 frames
                if (_debugFrameCount < 3)
                {
                    float distToISS = Vector3.Distance(_trackingSpace.position, targetPos);
                    Vector3 toISS = (targetPos - _trackingSpace.position).normalized;
                    float dotISS = Vector3.Dot(_trackingSpace.forward, toISS);
                    float dotEarth = Vector3.Dot(_trackingSpace.forward, toEarth);
                    
                    UnityEngine.Debug.LogWarning(
                        $"[ISS_VR_DEBUG F{_debugFrameCount}] " +
                        $"ISS=({targetPos.x:F1},{targetPos.y:F1},{targetPos.z:F1}) distEarth={targetPos.magnitude:F1} | " +
                        $"TrackPos=({_trackingSpace.position.x:F1},{_trackingSpace.position.y:F1},{_trackingSpace.position.z:F1}) distEarth={_trackingSpace.position.magnitude:F1} | " +
                        $"Dist→ISS={distToISS:F2} | HeightAbove={(desiredPosition - targetPos).magnitude:F2} | " +
                        $"dot→Earth={dotEarth:F2} dot→ISS={dotISS:F2} | " +
                        $"LookingAt={(dotEarth > 0.5f ? "EARTH" : "SPACE")}");
                    
                    _debugFrameCount++;
                }
                
                // OVR applies head tracking as local rotations on top of this base orientation
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
}
