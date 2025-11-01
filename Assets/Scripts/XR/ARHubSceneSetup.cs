using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XR
{
    /// <summary>
    /// Helper script to set up the ARHub scene with all necessary components.
    /// This is an Editor-only script that provides a menu command to auto-configure the scene.
    /// </summary>
    public class ARHubSceneSetup : MonoBehaviour
    {
        #if UNITY_EDITOR
        [MenuItem("Tools/Setup ARHub Scene")]
        public static void SetupARHubScene()
        {
            Debug.Log("[ARHubSceneSetup] ═══════════════════════════════════════════════");
            Debug.Log("[ARHubSceneSetup] Starting ARHub Scene Setup...");
            Debug.Log("[ARHubSceneSetup] ═══════════════════════════════════════════════");

            // 1. Create OVRCameraRig (Quest camera system)
            CreateOVRCameraRig();

            // 2. Create OVRManager (Passthrough controller)
            CreateOVRManager();

            // 3. Create persistent managers
            CreatePersistentManagers();

            // 4. Create Earth preview for calibration
            CreateEarthPreview();

            // 5. Create GlobeCalibrationManager
            CreateGlobeCalibrationManager();

            // 6. Set up scene lighting for AR
            SetupARLighting();

            Debug.Log("[ARHubSceneSetup] ═══════════════════════════════════════════════");
            Debug.Log("[ARHubSceneSetup] ✓ ARHub Scene Setup Complete!");
            Debug.Log("[ARHubSceneSetup] ═══════════════════════════════════════════════");
        }

        private static void CreateOVRCameraRig()
        {
            if (FindFirstObjectByType<OVRCameraRig>() != null)
            {
                Debug.Log("[ARHubSceneSetup] OVRCameraRig already exists - skipping");
                return;
            }

            // Create from Meta XR prefab if available
            GameObject ovrCameraRigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.meta.xr.sdk.core/Prefabs/OVRCameraRig.prefab");

            if (ovrCameraRigPrefab != null)
            {
                GameObject cameraRig = (GameObject)PrefabUtility.InstantiatePrefab(ovrCameraRigPrefab);
                cameraRig.name = "OVRCameraRig";
                cameraRig.transform.position = Vector3.zero;
                Debug.Log("[ARHubSceneSetup] ✓ Created OVRCameraRig from prefab");
            }
            else
            {
                // Manual creation if prefab not found
                GameObject cameraRig = new GameObject("OVRCameraRig");
                cameraRig.AddComponent<OVRCameraRig>();
                cameraRig.transform.position = Vector3.zero;
                Debug.Log("[ARHubSceneSetup] ✓ Created OVRCameraRig manually");
            }

            // Disable default Main Camera if it exists
            Camera mainCam = Camera.main;
            if (mainCam != null && mainCam.gameObject.name == "Main Camera")
            {
                mainCam.gameObject.SetActive(false);
                Debug.Log("[ARHubSceneSetup] Disabled default Main Camera (OVRCameraRig provides camera)");
            }
        }

        private static void CreateOVRManager()
        {
            if (FindFirstObjectByType<OVRManager>() != null)
            {
                Debug.Log("[ARHubSceneSetup] OVRManager already exists - skipping");
                return;
            }

            GameObject managerObj = new GameObject("OVRManager");
            OVRManager manager = managerObj.AddComponent<OVRManager>();

            // Configure for AR passthrough
            manager.isInsightPassthroughEnabled = true;

            Debug.Log("[ARHubSceneSetup] ✓ Created OVRManager with passthrough enabled");
        }

        private static void CreatePersistentManagers()
        {
            // XRModeManager
            if (FindFirstObjectByType<XRModeManager>() == null)
            {
                GameObject xrManager = new GameObject("XRModeManager");
                xrManager.AddComponent<XRModeManager>();
                Debug.Log("[ARHubSceneSetup] ✓ Created XRModeManager");
            }
            else
            {
                Debug.Log("[ARHubSceneSetup] XRModeManager already exists (likely from previous scene)");
            }

            // MissionContext (not in namespace)
            if (FindFirstObjectByType<MissionContext>() == null)
            {
                GameObject missionContext = new GameObject("MissionContext");
                missionContext.AddComponent<MissionContext>();
                Debug.Log("[ARHubSceneSetup] ✓ Created MissionContext");
            }
            else
            {
                Debug.Log("[ARHubSceneSetup] MissionContext already exists (persisted from intro)");
            }

            // SceneTransitionManager (not in namespace)
            if (FindFirstObjectByType<SceneTransitionManager>() == null)
            {
                GameObject transitionManager = new GameObject("SceneTransitionManager");
                transitionManager.AddComponent<SceneTransitionManager>();
                Debug.Log("[ARHubSceneSetup] ✓ Created SceneTransitionManager");
            }
            else
            {
                Debug.Log("[ARHubSceneSetup] SceneTransitionManager already exists (persisted from intro)");
            }
        }

        private static void CreateEarthPreview()
        {
            if (GameObject.Find("EarthPreview") != null)
            {
                Debug.Log("[ARHubSceneSetup] EarthPreview already exists - skipping");
                return;
            }

            // Create Earth sphere for calibration
            GameObject earthPreview = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            earthPreview.name = "EarthPreview";

            // Scale to reasonable physical size (30cm diameter = 0.3 units)
            earthPreview.transform.localScale = Vector3.one * 0.3f;

            // Position in front of camera (will be moved during calibration)
            earthPreview.transform.position = new Vector3(0, -0.3f, 1.5f);

            // Create semi-transparent material for calibration
            Material calibrationMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            calibrationMat.name = "EarthCalibrationMaterial";
            calibrationMat.color = new Color(0.3f, 0.5f, 1f, 0.5f); // Blue, semi-transparent

            // Enable transparency
            calibrationMat.SetFloat("_Surface", 1); // Transparent mode
            calibrationMat.SetFloat("_Blend", 0); // Alpha blend
            calibrationMat.renderQueue = 3000;

            Renderer renderer = earthPreview.GetComponent<Renderer>();
            renderer.material = calibrationMat;

            Debug.Log("[ARHubSceneSetup] ✓ Created EarthPreview (30cm sphere, semi-transparent)");
        }

        private static void CreateGlobeCalibrationManager()
        {
            if (FindFirstObjectByType<GlobeCalibrationManager>() != null)
            {
                Debug.Log("[ARHubSceneSetup] GlobeCalibrationManager already exists - skipping");
                return;
            }

            GameObject calibrationManager = new GameObject("GlobeCalibrationManager");
            GlobeCalibrationManager manager = calibrationManager.AddComponent<GlobeCalibrationManager>();

            // Assign Earth preview
            GameObject earthPreview = GameObject.Find("EarthPreview");
            if (earthPreview != null)
            {
                manager.earthPreview = earthPreview;
                Debug.Log("[ARHubSceneSetup] ✓ Linked EarthPreview to GlobeCalibrationManager");
            }

            // Assign main camera (from OVRCameraRig)
            OVRCameraRig cameraRig = FindFirstObjectByType<OVRCameraRig>();
            if (cameraRig != null)
            {
                Camera centerEyeCamera = cameraRig.centerEyeAnchor?.GetComponent<Camera>();
                if (centerEyeCamera != null)
                {
                    manager.mainCamera = centerEyeCamera;
                    Debug.Log("[ARHubSceneSetup] ✓ Linked OVRCameraRig center eye to GlobeCalibrationManager");
                }
            }

            Debug.Log("[ARHubSceneSetup] ✓ Created GlobeCalibrationManager");
        }

        private static void SetupARLighting()
        {
            // AR uses real-world lighting, so disable scene lighting
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.white;

            // Remove directional light if exists (AR uses real lighting)
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    light.intensity = 0.3f; // Dim it for hologram visibility
                    Debug.Log("[ARHubSceneSetup] Dimmed directional light for AR");
                }
            }

            Debug.Log("[ARHubSceneSetup] ✓ Configured AR lighting");
        }
        #endif
    }
}
