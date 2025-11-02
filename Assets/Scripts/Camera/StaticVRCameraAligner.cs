using UnityEngine;
using UnityEngine.XR;
using System.Reflection;

/// <summary>
/// StaticVRCameraAligner: Minimal VR head tracking for Hubble scene.
/// 
/// Preserves the exact desktop camera composition by aligning the OVRCameraRig's TrackingSpace
/// so the user's head (CenterEyeAnchor) sits precisely at the desktop camera's world transform.
/// 
/// Desktop behavior: Completely untouched.
/// VR behavior: One-shot alignment on Start; head tracking then works naturally via OVR.
/// 
/// Used exclusively in the Hubble scene. Does NOT share logic with ISS/Hub orbit systems.
/// </summary>
public class StaticVRCameraAligner : MonoBehaviour
{
    private bool _isVR = false;
    private Transform _trackingSpace = null;
    private Transform _centerEyeAnchor = null;
    private Camera _desktopCamera = null;
    private bool _aligned = false;

    void Start()
    {
        // Detect VR mode
        _isVR = XRSettings.isDeviceActive && XRSettings.loadedDeviceName.Length > 0;

        if (!_isVR)
        {
            // Desktop mode: do nothing, preserve existing behavior
            return;
        }

        // VR mode: perform one-shot alignment
        StartCoroutine(AlignVRCamera());
    }

    private System.Collections.IEnumerator AlignVRCamera()
    {
        // Find OVRCameraRig components via reflection
        Component ovrRigComp = GetComponent("OVRCameraRig");
        if (ovrRigComp == null)
        {
            Debug.LogWarning("[StaticVRCameraAligner] OVRCameraRig not found on CameraRig. VR head tracking disabled.");
            yield break;
        }

        // Find TrackingSpace
        _trackingSpace = FindTrackingSpace(ovrRigComp);
        if (_trackingSpace == null)
        {
            Debug.LogWarning("[StaticVRCameraAligner] TrackingSpace not found. Retrying next frame...");
            yield return new WaitForEndOfFrame();
            _trackingSpace = FindTrackingSpace(ovrRigComp);
            if (_trackingSpace == null)
            {
                Debug.LogError("[StaticVRCameraAligner] TrackingSpace not found after retry. VR alignment failed.");
                yield break;
            }
        }

        // Find CenterEyeAnchor
        _centerEyeAnchor = FindCenterEyeAnchor(ovrRigComp);
        if (_centerEyeAnchor == null)
        {
            // Fallback to LeftEyeAnchor if available
            _centerEyeAnchor = FindLeftEyeAnchor(ovrRigComp);
            if (_centerEyeAnchor == null)
            {
                Debug.LogError("[StaticVRCameraAligner] CenterEyeAnchor/LeftEyeAnchor not found. VR alignment failed.");
                yield break;
            }
        }

        // Find the desktop camera (the existing Main Camera that composes the current view)
        _desktopCamera = GetComponentInChildren<Camera>();
        if (_desktopCamera == null)
        {
            Debug.LogError("[StaticVRCameraAligner] Desktop camera not found as child of CameraRig. VR alignment failed.");
            yield break;
        }

        // Ensure CameraRig is at identity rotation (required for OVR)
        transform.rotation = Quaternion.identity;

        // Read the desktop camera's world transform as the desired head pose
        Vector3 desiredPosition = _desktopCamera.transform.position;
        Quaternion desiredRotation = _desktopCamera.transform.rotation;

        // Compute head-offset compensation
        // The CenterEyeAnchor has a local offset within the TrackingSpace hierarchy
        // We need to position TrackingSpace such that when OVR applies this offset,
        // the user's actual eye position lands exactly at desiredPosition
        Vector3 headLocal = _centerEyeAnchor.localPosition;
        Vector3 headWorldOffset = desiredRotation * headLocal;

        // Set TrackingSpace transform
        _trackingSpace.position = desiredPosition - headWorldOffset;
        _trackingSpace.rotation = desiredRotation;

        // Disable the desktop camera to avoid double rendering
        _desktopCamera.enabled = false;

        // Configure eye cameras' near clip plane for close-up safety
        ConfigureVREyeCameras();

        _aligned = true;

        // Optional one-shot diagnostic log
        Debug.Log($"[HUBBLE_VR_ALIGN] desiredPos={desiredPosition:F2} desiredRot={desiredRotation.eulerAngles:F1} headLocal={headLocal:F3} trackingSpace.finalPos={_trackingSpace.position:F2}");
    }

    private Transform FindTrackingSpace(Component ovrRigComp)
    {
        // Try reflection first
        PropertyInfo prop = ovrRigComp.GetType().GetProperty("trackingSpace", BindingFlags.Public | BindingFlags.Instance);
        if (prop != null)
        {
            Transform ts = prop.GetValue(ovrRigComp) as Transform;
            if (ts != null) return ts;
        }

        // Fallback: search by name
        Transform found = transform.Find("TrackingSpace");
        if (found != null) return found;

        // Deep search
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "TrackingSpace") return child;
        }

        return null;
    }

    private Transform FindCenterEyeAnchor(Component ovrRigComp)
    {
        PropertyInfo prop = ovrRigComp.GetType().GetProperty("centerEyeAnchor", BindingFlags.Public | BindingFlags.Instance);
        if (prop != null)
        {
            Transform anchor = prop.GetValue(ovrRigComp) as Transform;
            if (anchor != null) return anchor;
        }

        // Fallback: search by name
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "CenterEyeAnchor") return child;
        }

        return null;
    }

    private Transform FindLeftEyeAnchor(Component ovrRigComp)
    {
        PropertyInfo prop = ovrRigComp.GetType().GetProperty("leftEyeAnchor", BindingFlags.Public | BindingFlags.Instance);
        if (prop != null)
        {
            Transform anchor = prop.GetValue(ovrRigComp) as Transform;
            if (anchor != null) return anchor;
        }

        // Fallback: search by name
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "LeftEyeAnchor") return child;
        }

        return null;
    }

    private void ConfigureVREyeCameras()
    {
        // Find all eye cameras and set near clip plane for close-up safety
        Camera[] allCameras = GetComponentsInChildren<Camera>(true);
        foreach (Camera cam in allCameras)
        {
            if (cam.name.Contains("Eye") || cam.name.Contains("eye"))
            {
                cam.nearClipPlane = 0.01f;
            }
        }
    }
}

