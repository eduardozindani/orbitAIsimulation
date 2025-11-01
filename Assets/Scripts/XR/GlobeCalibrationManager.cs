using System;
using System.Collections;
using UnityEngine;

namespace XR
{
    /// <summary>
    /// Manages physical globe calibration in AR Hub.
    /// Allows user to position holographic Earth to match their physical globe
    /// using controller thumbstick, then creates spatial anchor for persistence.
    ///
    /// Calibration Flow:
    /// 1. User sees semi-transparent Earth preview
    /// 2. Thumbstick moves Earth up/down/left/right
    /// 3. Trigger confirms position → Creates spatial anchor
    /// 4. Grip button resets to default position
    /// </summary>
    public class GlobeCalibrationManager : MonoBehaviour
    {
        [Header("Calibration State")]
        [Tooltip("Is calibration currently active?")]
        public bool isCalibrating = false;

        [Tooltip("Has globe been calibrated this session?")]
        public bool isCalibrated = false;

        [Header("Preview Earth")]
        [Tooltip("The Earth GameObject to position (should be semi-transparent during calibration)")]
        public GameObject earthPreview;

        [Tooltip("Initial distance from camera (meters)")]
        public float initialDistance = 1.5f;

        [Tooltip("Initial height offset from camera (meters)")]
        public float initialHeightOffset = -0.3f;

        [Header("Movement Settings")]
        [Tooltip("Horizontal/vertical movement speed (meters per second)")]
        public float moveSpeed = 0.5f;

        [Tooltip("Depth movement speed (meters per second)")]
        public float depthSpeed = 0.3f;

        [Header("Visual Feedback")]
        [Tooltip("Material for Earth during calibration (semi-transparent)")]
        public Material calibrationMaterial;

        [Tooltip("Material for Earth after calibration (opaque)")]
        public Material calibratedMaterial;

        [Tooltip("Controller ray visualizer (optional)")]
        public LineRenderer controllerRay;

        [Header("References")]
        [Tooltip("Main camera (OVRCameraRig center eye)")]
        public Camera mainCamera;

        // Events
        public event Action OnCalibrationStarted;
        public event Action OnCalibrationCompleted;
        public event Action OnCalibrationReset;

        // Spatial anchor (Quest-specific, optional for desktop testing)
        private OVRSpatialAnchor _spatialAnchor;
        private Vector3 _calibratedPosition;
        private Quaternion _calibratedRotation;

        // Singleton instance
        private static GlobeCalibrationManager _instance;
        public static GlobeCalibrationManager Instance => _instance;

        // ---------------- Lifecycle ----------------

        void Awake()
        {
            // Singleton pattern
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[GlobeCalibrationManager] Duplicate instance detected - destroying this one");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            Debug.Log("[GlobeCalibrationManager] Initialized as singleton");

            // Find main camera if not assigned
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }

        void Start()
        {
            // Start calibration immediately (onboarding phase)
            StartCalibration();
        }

        void Update()
        {
            if (!isCalibrating || earthPreview == null)
                return;

            // Handle controller input for positioning
            HandleCalibrationInput();

            // Optional: Show controller ray
            if (controllerRay != null)
            {
                UpdateControllerRay();
            }
        }

        // ---------------- Calibration Control ----------------

        /// <summary>
        /// Begin calibration process
        /// </summary>
        public void StartCalibration()
        {
            if (isCalibrating)
            {
                Debug.LogWarning("[GlobeCalibrationManager] Already calibrating");
                return;
            }

            isCalibrating = true;
            isCalibrated = false;

            Debug.Log("[GlobeCalibrationManager] Starting calibration");

            // Create or show Earth preview
            if (earthPreview == null)
            {
                Debug.LogError("[GlobeCalibrationManager] No Earth preview assigned! Please assign in Inspector.");
                return;
            }

            // Position Earth in front of camera
            PositionEarthAtDefault();

            // Set semi-transparent material
            if (calibrationMaterial != null)
            {
                SetEarthMaterial(calibrationMaterial);
            }

            earthPreview.SetActive(true);

            // Trigger event
            OnCalibrationStarted?.Invoke();

            Debug.Log("[GlobeCalibrationManager] Calibration started - use thumbstick to position Earth");
        }

        /// <summary>
        /// Confirm calibration and create spatial anchor
        /// </summary>
        public void ConfirmCalibration()
        {
            if (!isCalibrating)
            {
                Debug.LogWarning("[GlobeCalibrationManager] Not currently calibrating");
                return;
            }

            Debug.Log("[GlobeCalibrationManager] Confirming calibration...");

            // Save position and rotation
            _calibratedPosition = earthPreview.transform.position;
            _calibratedRotation = earthPreview.transform.rotation;

            // Create spatial anchor (Quest-specific)
            #if UNITY_ANDROID
            CreateSpatialAnchor();
            #else
            Debug.Log("[GlobeCalibrationManager] Desktop mode - spatial anchor not created");
            #endif

            // Set opaque material
            if (calibratedMaterial != null)
            {
                SetEarthMaterial(calibratedMaterial);
            }

            // Mark as calibrated
            isCalibrating = false;
            isCalibrated = true;

            // Hide controller ray
            if (controllerRay != null)
            {
                controllerRay.enabled = false;
            }

            // Trigger event
            OnCalibrationCompleted?.Invoke();

            Debug.Log($"[GlobeCalibrationManager] ✓ Calibration complete at position {_calibratedPosition}");
        }

        /// <summary>
        /// Reset calibration to start over
        /// </summary>
        public void ResetCalibration()
        {
            Debug.Log("[GlobeCalibrationManager] Resetting calibration");

            // Destroy spatial anchor if exists
            if (_spatialAnchor != null)
            {
                Destroy(_spatialAnchor);
                _spatialAnchor = null;
            }

            // Reset state
            isCalibrating = false;
            isCalibrated = false;

            // Trigger event
            OnCalibrationReset?.Invoke();

            // Restart calibration
            StartCalibration();
        }

        // ---------------- Input Handling ----------------

        private void HandleCalibrationInput()
        {
            #if UNITY_ANDROID
            // Quest controller input using OVRInput
            Vector2 thumbstick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);

            // Horizontal/vertical movement (thumbstick XY)
            Vector3 cameraRight = mainCamera.transform.right;
            Vector3 cameraUp = mainCamera.transform.up;

            Vector3 movement = (cameraRight * thumbstick.x + cameraUp * thumbstick.y) * moveSpeed * Time.deltaTime;
            earthPreview.transform.position += movement;

            // Depth movement (secondary thumbstick or buttons)
            float depthInput = 0f;
            if (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger))
            {
                depthInput = 1f; // Move closer
            }
            else if (OVRInput.Get(OVRInput.Button.SecondaryHandTrigger))
            {
                depthInput = -1f; // Move farther
            }

            if (depthInput != 0f)
            {
                Vector3 cameraForward = mainCamera.transform.forward;
                earthPreview.transform.position += cameraForward * depthInput * depthSpeed * Time.deltaTime;
            }

            // Confirm with trigger
            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger))
            {
                ConfirmCalibration();
            }

            // Reset with grip button
            if (OVRInput.GetDown(OVRInput.Button.One)) // A button for reset
            {
                PositionEarthAtDefault();
            }
            #else
            // Desktop testing input (WASD for positioning)
            float h = 0f, v = 0f, d = 0f;

            if (Input.GetKey(KeyCode.D)) h = 1f;
            if (Input.GetKey(KeyCode.A)) h = -1f;
            if (Input.GetKey(KeyCode.W)) v = 1f;
            if (Input.GetKey(KeyCode.S)) v = -1f;
            if (Input.GetKey(KeyCode.Q)) d = 1f;
            if (Input.GetKey(KeyCode.E)) d = -1f;

            Vector3 cameraRight = mainCamera.transform.right;
            Vector3 cameraUp = mainCamera.transform.up;
            Vector3 cameraForward = mainCamera.transform.forward;

            Vector3 movement = (cameraRight * h + cameraUp * v + cameraForward * d) * moveSpeed * Time.deltaTime;
            earthPreview.transform.position += movement;

            // Confirm with Enter
            if (Input.GetKeyDown(KeyCode.Return))
            {
                ConfirmCalibration();
            }

            // Reset with R
            if (Input.GetKeyDown(KeyCode.R))
            {
                PositionEarthAtDefault();
            }
            #endif
        }

        // ---------------- Helper Methods ----------------

        private void PositionEarthAtDefault()
        {
            if (earthPreview == null || mainCamera == null)
                return;

            // Position in front of camera at initial distance
            Vector3 cameraPos = mainCamera.transform.position;
            Vector3 cameraForward = mainCamera.transform.forward;
            Vector3 cameraUp = mainCamera.transform.up;

            Vector3 defaultPos = cameraPos +
                                cameraForward * initialDistance +
                                cameraUp * initialHeightOffset;

            earthPreview.transform.position = defaultPos;
            earthPreview.transform.rotation = Quaternion.identity;

            Debug.Log($"[GlobeCalibrationManager] Earth positioned at default: {defaultPos}");
        }

        private void SetEarthMaterial(Material material)
        {
            if (earthPreview == null)
                return;

            Renderer renderer = earthPreview.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = material;
            }
            else
            {
                // Check children (Earth might be a parent GameObject)
                Renderer[] childRenderers = earthPreview.GetComponentsInChildren<Renderer>();
                foreach (var childRenderer in childRenderers)
                {
                    childRenderer.material = material;
                }
            }
        }

        private async void CreateSpatialAnchor()
        {
            #if UNITY_ANDROID
            // Add OVRSpatialAnchor component to Earth GameObject
            if (_spatialAnchor == null)
            {
                _spatialAnchor = earthPreview.AddComponent<OVRSpatialAnchor>();
            }

            // Save anchor to Quest storage (using async method)
            var result = await _spatialAnchor.SaveAsync();
            if (result.Success)
            {
                Debug.Log("[GlobeCalibrationManager] ✓ Spatial anchor saved successfully");
            }
            else
            {
                Debug.LogError($"[GlobeCalibrationManager] Failed to save spatial anchor: {result.Status}");
            }
            #endif
        }

        private void UpdateControllerRay()
        {
            #if UNITY_ANDROID
            // Get controller position
            Vector3 controllerPos = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
            Quaternion controllerRot = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch);

            // Draw ray from controller to Earth
            controllerRay.SetPosition(0, controllerPos);
            controllerRay.SetPosition(1, earthPreview.transform.position);
            controllerRay.enabled = true;
            #endif
        }

        // ---------------- Public Query Methods ----------------

        /// <summary>
        /// Get the calibrated Earth position
        /// </summary>
        public Vector3 GetCalibratedPosition()
        {
            return isCalibrated ? _calibratedPosition : earthPreview.transform.position;
        }

        /// <summary>
        /// Check if calibration is complete
        /// </summary>
        public bool IsCalibrated()
        {
            return isCalibrated;
        }

        // ---------------- Debug Display ----------------

        void OnGUI()
        {
            if (!isCalibrating)
                return;

            // Show calibration instructions
            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.fontSize = 18;
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.MiddleCenter;

            string instructions;
            #if UNITY_ANDROID
            instructions = "Calibration Mode\n\n" +
                          "Thumbstick: Move Earth\n" +
                          "Grip Buttons: Adjust depth\n" +
                          "Trigger: Confirm\n" +
                          "A Button: Reset position";
            #else
            instructions = "Calibration Mode (Desktop)\n\n" +
                          "WASD: Move Earth\n" +
                          "Q/E: Adjust depth\n" +
                          "Enter: Confirm\n" +
                          "R: Reset position";
            #endif

            GUI.Box(new Rect(Screen.width / 2 - 200, 50, 400, 150), instructions, style);
        }
    }
}
