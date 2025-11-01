using UnityEngine;
using UnityEngine.XR.Management;

namespace XR
{
    /// <summary>
    /// Manages VR/AR mode switching for Meta Quest mixed reality experiences.
    /// Singleton pattern ensures consistent mode state across scene transitions.
    ///
    /// Mode Flow:
    /// - VR: Intro cutscene and mission spaces (ISS, GPS, etc.)
    /// - AR: Hub scene with physical globe calibration and satellite creation
    /// </summary>
    public class XRModeManager : MonoBehaviour
    {
        public enum XRMode
        {
            VR,  // Full virtual reality (no passthrough)
            AR   // Augmented reality (passthrough enabled, real room visible)
        }

        [Header("Current State")]
        [Tooltip("Current XR mode (VR for intro/missions, AR for Hub)")]
        public XRMode currentMode = XRMode.VR;

        [Header("Debug")]
        [Tooltip("Enable detailed logging for XR mode transitions")]
        public bool verboseLogging = true;

        // Singleton instance
        private static XRModeManager _instance;
        public static XRModeManager Instance => _instance;

        // OVR Manager reference (will be assigned at runtime if Meta XR SDK is present)
        private bool _hasOVRManager = false;

        // ---------------- Lifecycle ----------------

        void Awake()
        {
            // Singleton pattern
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[XRModeManager] Duplicate instance detected - destroying this one");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            Debug.Log("[XRModeManager] Initialized as singleton");

            // Check if XR is available
            CheckXRAvailability();
        }

        void Start()
        {
            // Initialize in VR mode (intro cutscene starts in VR)
            SwitchToVR();
        }

        // ---------------- XR Availability ----------------

        private void CheckXRAvailability()
        {
            var xrSettings = XRGeneralSettings.Instance;
            if (xrSettings != null && xrSettings.Manager != null && xrSettings.Manager.activeLoader != null)
            {
                Debug.Log($"[XRModeManager] XR system active: {xrSettings.Manager.activeLoader.name}");

                // Check if OVRManager exists (Meta Quest SDK)
                #if UNITY_ANDROID
                var ovrManager = FindFirstObjectByType<OVRManager>();
                if (ovrManager != null)
                {
                    _hasOVRManager = true;
                    Debug.Log("[XRModeManager] OVRManager detected - Meta Quest features available");
                }
                else
                {
                    Debug.LogWarning("[XRModeManager] OVRManager not found - some features may not work");
                }
                #else
                Debug.Log("[XRModeManager] Not on Android - XR features limited");
                #endif
            }
            else
            {
                Debug.LogWarning("[XRModeManager] XR system not initialized - running in non-XR mode");
            }
        }

        // ---------------- Mode Switching ----------------

        /// <summary>
        /// Switch to VR mode (full immersion, no passthrough)
        /// Used for: Intro cutscene, mission spaces (ISS, GPS, Voyager, Hubble)
        /// </summary>
        public void SwitchToVR()
        {
            if (currentMode == XRMode.VR)
            {
                if (verboseLogging)
                    Debug.Log("[XRModeManager] Already in VR mode - no action needed");
                return;
            }

            currentMode = XRMode.VR;

            #if UNITY_ANDROID
            // Disable passthrough using OVRManager
            if (_hasOVRManager)
            {
                var ovrManager = FindFirstObjectByType<OVRManager>();
                if (ovrManager != null)
                {
                    ovrManager.isInsightPassthroughEnabled = false;

                    if (verboseLogging)
                        Debug.Log("[XRModeManager] ✓ Switched to VR mode - passthrough disabled");
                }
            }
            #endif

            Debug.Log("[XRModeManager] Mode: VR (full immersion)");
        }

        /// <summary>
        /// Switch to AR mode (passthrough enabled, real room visible)
        /// Used for: Hub scene with physical globe calibration
        /// </summary>
        public void SwitchToAR()
        {
            if (currentMode == XRMode.AR)
            {
                if (verboseLogging)
                    Debug.Log("[XRModeManager] Already in AR mode - no action needed");
                return;
            }

            currentMode = XRMode.AR;

            #if UNITY_ANDROID
            // Enable passthrough using OVRManager
            if (_hasOVRManager)
            {
                var ovrManager = FindFirstObjectByType<OVRManager>();
                if (ovrManager != null)
                {
                    ovrManager.isInsightPassthroughEnabled = true;

                    if (verboseLogging)
                        Debug.Log("[XRModeManager] ✓ Switched to AR mode - passthrough enabled");
                }
            }
            #endif

            Debug.Log("[XRModeManager] Mode: AR (passthrough, real room visible)");
        }

        // ---------------- Query Methods ----------------

        /// <summary>
        /// Check if currently in VR mode
        /// </summary>
        public bool IsVRMode()
        {
            return currentMode == XRMode.VR;
        }

        /// <summary>
        /// Check if currently in AR mode
        /// </summary>
        public bool IsARMode()
        {
            return currentMode == XRMode.AR;
        }

        /// <summary>
        /// Check if XR system is available
        /// </summary>
        public bool IsXRAvailable()
        {
            var xrSettings = XRGeneralSettings.Instance;
            return xrSettings != null &&
                   xrSettings.Manager != null &&
                   xrSettings.Manager.activeLoader != null;
        }

        /// <summary>
        /// Check if Meta Quest features are available
        /// </summary>
        public bool HasMetaQuestSupport()
        {
            return _hasOVRManager;
        }

        // ---------------- Debug Display ----------------

        void OnGUI()
        {
            if (!verboseLogging)
                return;

            // Show current mode in top-left corner
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 16;
            style.normal.textColor = currentMode == XRMode.VR ? Color.cyan : Color.green;
            style.alignment = TextAnchor.UpperLeft;

            string modeText = $"XR Mode: {currentMode}";
            if (!IsXRAvailable())
            {
                modeText += " (Desktop)";
                style.normal.textColor = Color.yellow;
            }

            GUI.Label(new Rect(10, 10, 300, 30), modeText, style);
        }
    }
}
