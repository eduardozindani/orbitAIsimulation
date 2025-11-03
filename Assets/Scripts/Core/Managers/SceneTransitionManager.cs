using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages scene transitions with beautiful fade effects and mission logos.
/// Singleton that persists across scenes.
/// Provides patient 4-second transition with logo animation.
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Transition Timing")]
    [Tooltip("Duration of fade out/in animations (seconds)")]
    public float fadeDuration = 2f;

    [Tooltip("Total time for logo animation: fade-in (2s) + hold (2s) = 4s total")]
    public float sceneLoadWaitTime = 4f;

    [Header("UI References")]
    [Tooltip("CanvasGroup for fade effect (alpha 0=transparent, 1=black)")]
    public CanvasGroup fadeCanvasGroup;

    [Tooltip("Image component for displaying mission logos")]
    public Image missionLogoImage;

    [Tooltip("Optional text for loading feedback")]
    public TMP_Text loadingText;

    [Header("Mission Logos")]
    [Tooltip("Logo for Mission Control Hub (shown when returning from missions)")]
    public Sprite hubLogo;

    [Tooltip("Logo for ISS Mission Space")]
    public Sprite issLogo;

    [Tooltip("Logo for Voyager Mission Space")]
    public Sprite voyagerLogo;

    [Tooltip("Logo for Hubble Mission Space")]
    public Sprite hubbleLogo;

    private bool isTransitioning = false;
    private OVRScreenFade screenFade;
    private bool isVR = false;
    private Transform cachedCameraAnchor;
    private Transform transitionCanvasTransform;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[SceneTransitionManager] Initialized");

            // FORCE RECREATION: Delete any stale UI components from previous versions
            if (fadeCanvasGroup != null || missionLogoImage != null)
            {
                Debug.Log("[SceneTransitionManager] ⚠️ Found existing UI from previous session - forcing recreation for VR compatibility");

                // Find and destroy old TransitionCanvas (try both names)
                Transform oldCanvas = transform.Find("TransitionOverlayCanvas");
                if (oldCanvas == null)
                {
                    oldCanvas = transform.Find("TransitionCanvas");
                }

                if (oldCanvas != null)
                {
                    Debug.Log($"[SceneTransitionManager] Destroying old canvas: {oldCanvas.name}...");
                    Destroy(oldCanvas.gameObject);
                }

                // Find and destroy old OVRScreenFade
                Transform oldFade = transform.Find("OVRScreenFade");
                if (oldFade != null)
                {
                    Debug.Log("[SceneTransitionManager] Destroying old OVRScreenFade...");
                    Destroy(oldFade.gameObject);
                }

                // Clear references to force recreation
                fadeCanvasGroup = null;
                missionLogoImage = null;
                screenFade = null;

                Debug.Log("[SceneTransitionManager] Old UI destroyed, will recreate with VR support");
            }

            // Create persistent transition UI (Canvas with FadeCanvas and MissionLogo)
            CreateTransitionUIIfNeeded();

            // Load Hub logo from Resources if not assigned in Inspector
            if (hubLogo == null)
            {
                hubLogo = Resources.Load<Sprite>("MissionLogos/simulationlogo");
                if (hubLogo != null)
                {
                    Debug.Log("[SceneTransitionManager] Loaded Hub logo from Resources");
                }
                else
                {
                    Debug.LogWarning("[SceneTransitionManager] Hub logo not found in Resources/MissionLogos/simulationlogo");
                }
            }

            // Ensure fade UI starts transparent
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 0f;
                fadeCanvasGroup.gameObject.SetActive(true);
            }

            // Hide logo initially
            if (missionLogoImage != null)
            {
                missionLogoImage.gameObject.SetActive(false);
            }

            if (loadingText != null)
            {
                loadingText.gameObject.SetActive(false);
            }
        }
        else
        {
            Debug.Log("[SceneTransitionManager] Instance already exists, destroying duplicate");

            // DEBUG: Log what will be destroyed
            Debug.Log($"[SceneTransitionManager] About to destroy duplicate GameObject: {gameObject.name}");
            Debug.Log($"[SceneTransitionManager] Duplicate has {transform.childCount} children:");
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                Debug.Log($"[SceneTransitionManager]   Child[{i}]: {child.name} (will be destroyed with parent)");
            }

            Debug.Log($"[SceneTransitionManager] Persistent Instance has {Instance.transform.childCount} children:");
            for (int i = 0; i < Instance.transform.childCount; i++)
            {
                Transform child = Instance.transform.GetChild(i);
                Debug.Log($"[SceneTransitionManager]   Instance Child[{i}]: {child.name}");
            }

            Destroy(gameObject);
        }
    }

    void LateUpdate()
    {
        // Position the TransitionCanvas 1m in front of the camera each frame
        // This ensures it follows the camera across scene changes without being parented to scene-specific objects

        // Cache canvas transform if not already cached
        if (transitionCanvasTransform == null)
        {
            transitionCanvasTransform = transform.Find("TransitionCanvas");
        }

        if (transitionCanvasTransform != null)
        {
            // Re-find camera anchor if null (scene changed) or if the current one was destroyed
            if (cachedCameraAnchor == null)
            {
                cachedCameraAnchor = FindCameraAnchor();
            }

            if (cachedCameraAnchor != null)
            {
                // Position 1m in front of camera, facing the camera
                transitionCanvasTransform.position = cachedCameraAnchor.position + cachedCameraAnchor.forward * 1f;
                transitionCanvasTransform.rotation = cachedCameraAnchor.rotation;
            }
        }
    }

    /// <summary>
    /// Transition to a mission space (ISS, Voyager, Hubble)
    /// </summary>
    public void TransitionToMission(string mission)
    {
        Debug.Log($"[SceneTransitionManager] ═══════════════════════════════════════");
        Debug.Log($"[SceneTransitionManager] TRANSITION REQUESTED: {mission}");
        Debug.Log($"[SceneTransitionManager] isTransitioning: {isTransitioning}");
        Debug.Log($"[SceneTransitionManager] screenFade exists: {screenFade != null}");
        Debug.Log($"[SceneTransitionManager] fadeCanvasGroup exists: {fadeCanvasGroup != null}");
        Debug.Log($"[SceneTransitionManager] missionLogoImage exists: {missionLogoImage != null}");

        if (fadeCanvasGroup != null && fadeCanvasGroup.transform.parent != null)
        {
            Transform canvasTransform = fadeCanvasGroup.transform.parent;
            Debug.Log($"[SceneTransitionManager] Canvas parent: {canvasTransform.name}");
            Debug.Log($"[SceneTransitionManager] Canvas position: {canvasTransform.position}");
            Debug.Log($"[SceneTransitionManager] Canvas localPosition: {canvasTransform.localPosition}");
            if (canvasTransform.parent != null)
            {
                Debug.Log($"[SceneTransitionManager] Canvas grandparent: {canvasTransform.parent.name}");
            }
        }
        Debug.Log($"[SceneTransitionManager] ═══════════════════════════════════════");

        if (isTransitioning)
        {
            Debug.LogWarning("[SceneTransitionManager] Transition already in progress, ignoring request");
            return;
        }

        string sceneName = GetSceneName(mission);
        Debug.Log($"[SceneTransitionManager] Starting transition to {mission} ({sceneName})");
        StartCoroutine(TransitionSequence(mission, sceneName));
    }

    /// <summary>
    /// Transition back to Mission Control Hub
    /// </summary>
    public void TransitionToHub()
    {
        if (isTransitioning)
        {
            Debug.LogWarning("[SceneTransitionManager] Transition already in progress, ignoring request");
            return;
        }

        Debug.Log("[SceneTransitionManager] Starting transition to Hub");
        StartCoroutine(TransitionSequence("Hub", "Hub"));
    }

    /// <summary>
    /// Main transition sequence: fade out → logo → load scene → fade in
    /// </summary>
    private IEnumerator TransitionSequence(string destination, string sceneName)
    {
        isTransitioning = true;

        // UI is created in Awake() and persists, no need to discover

        // === PHASE 1: Fade to Black ===
        Debug.Log($"[SceneTransitionManager] Phase 1: Fading to black ({fadeDuration}s)");
        yield return FadeOut(fadeDuration);

        // === PHASE 2: Show Logo ===
        // Show logo for all destinations (including Hub)
        ShowMissionLogo(destination);

        // === PHASE 3: Load Scene Asynchronously ===
        Debug.Log($"[SceneTransitionManager] Phase 3: Loading scene '{sceneName}'");
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);
        loadOperation.allowSceneActivation = false; // Don't activate until we're ready

        // === PHASE 4: Logo Fade-In Animation ===
        // Smooth fade-in: 0% → 100% over 2 seconds
        Debug.Log($"[SceneTransitionManager] Phase 4: Fading in logo (0→1 over 2s), logo active: {(missionLogoImage != null && missionLogoImage.gameObject.activeSelf)}");
        float fadeInDuration = 2f;
        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeInDuration);

            if (missionLogoImage != null && missionLogoImage.gameObject.activeSelf)
            {
                Color logoColor = missionLogoImage.color;
                logoColor.a = Mathf.Lerp(0f, 1f, t); // Fade in
                missionLogoImage.color = logoColor;
            }

            yield return null;
        }

        Debug.Log($"[SceneTransitionManager] Phase 4 complete: Logo should be at 100% opacity, actual alpha: {(missionLogoImage != null ? missionLogoImage.color.a.ToString() : "NULL")}");

        // === PHASE 5: Hold Logo Visible (Scene Loading) ===
        // Hold at 100% opacity while scene loads
        float holdDuration = sceneLoadWaitTime - fadeInDuration;
        elapsed = 0f;

        while (elapsed < holdDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            // Keep logo at full opacity
            if (missionLogoImage != null && missionLogoImage.gameObject.activeSelf)
            {
                Color logoColor = missionLogoImage.color;
                logoColor.a = 1f; // Stay at 100%
                missionLogoImage.color = logoColor;
            }

            yield return null;
        }

        // === PHASE 5: Activate Scene ===
        Debug.Log($"[SceneTransitionManager] Phase 5: Activating scene");
        loadOperation.allowSceneActivation = true;

        // Wait for scene activation to complete
        while (!loadOperation.isDone)
        {
            yield return null;
        }

        // UI persists from Awake(), no need to re-discover

        // === PHASE 6 & 7: Fade In from Black (with logo fading out simultaneously) ===
        Debug.Log($"[SceneTransitionManager] Phase 6/7: Fading in from black with logo fade-out ({fadeDuration}s)");
        yield return FadeInWithLogo(fadeDuration);

        isTransitioning = false;
        Debug.Log($"[SceneTransitionManager] Transition to {destination} complete");

        // Note: Specialist introduction will be triggered by MissionSpaceController in the new scene
    }

    /// <summary>
    /// Fade from transparent to black
    /// VR: Uses OVRScreenFade for guaranteed black
    /// Desktop: Falls back to CanvasGroup alpha
    /// </summary>
    private IEnumerator FadeOut(float duration)
    {
        Debug.Log($"[SceneTransitionManager] FadeOut() called");

        // ALWAYS use CanvasGroup (black panel) - OVRScreenFade has shader issues on Quest
        // Re-find reference if lost during scene transition
        if (fadeCanvasGroup == null)
        {
            Debug.LogWarning("[SceneTransitionManager] fadeCanvasGroup reference lost - attempting to re-find...");

            // Try both possible canvas names (TransitionOverlayCanvas from Inspector, TransitionCanvas from code)
            Transform fadeTransform = transform.Find("TransitionOverlayCanvas/FadeCanvas");
            if (fadeTransform == null)
            {
                fadeTransform = transform.Find("TransitionCanvas/FadeCanvas");
            }

            if (fadeTransform != null)
            {
                fadeCanvasGroup = fadeTransform.GetComponent<CanvasGroup>();
                Debug.Log($"[SceneTransitionManager] ✓ Re-found fadeCanvasGroup: {(fadeCanvasGroup != null ? "SUCCESS" : "FAILED")}");
            }
            else
            {
                Debug.LogError("[SceneTransitionManager] ✗ Could not find FadeCanvas in either TransitionOverlayCanvas or TransitionCanvas!");
            }
        }

        if (fadeCanvasGroup == null)
        {
            Debug.LogError("[SceneTransitionManager] FADE FAILED: fadeCanvasGroup is NULL - cannot fade to black!");
            yield break;
        }

        Debug.Log($"[SceneTransitionManager] Using CanvasGroup.FadeOut() - black panel fade (alpha {fadeCanvasGroup.alpha} → 1.0 over {duration}s)");
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }

        fadeCanvasGroup.alpha = 1f; // Ensure fully black
        Debug.Log("[SceneTransitionManager] Fade to black complete - alpha now 1.0 (BLACK PANEL VISIBLE)");
    }

    /// <summary>
    /// Fade from black to transparent
    /// VR: Uses OVRScreenFade for guaranteed reveal
    /// Desktop: Falls back to CanvasGroup alpha
    /// </summary>
    private IEnumerator FadeIn(float duration)
    {
        Debug.Log($"[SceneTransitionManager] FadeIn() called");

        // ALWAYS use CanvasGroup (black panel) - OVRScreenFade has shader issues on Quest
        // Re-find reference if lost during scene transition
        if (fadeCanvasGroup == null)
        {
            Debug.LogWarning("[SceneTransitionManager] fadeCanvasGroup reference lost - attempting to re-find...");

            // Try both possible canvas names (TransitionOverlayCanvas from Inspector, TransitionCanvas from code)
            Transform fadeTransform = transform.Find("TransitionOverlayCanvas/FadeCanvas");
            if (fadeTransform == null)
            {
                fadeTransform = transform.Find("TransitionCanvas/FadeCanvas");
            }

            if (fadeTransform != null)
            {
                fadeCanvasGroup = fadeTransform.GetComponent<CanvasGroup>();
                Debug.Log($"[SceneTransitionManager] ✓ Re-found fadeCanvasGroup: {(fadeCanvasGroup != null ? "SUCCESS" : "FAILED")}");
            }
            else
            {
                Debug.LogError("[SceneTransitionManager] ✗ Could not find FadeCanvas in either TransitionOverlayCanvas or TransitionCanvas!");
            }
        }

        if (fadeCanvasGroup == null)
        {
            Debug.LogWarning("[SceneTransitionManager] FadeCanvasGroup not assigned!");
            yield break;
        }

        Debug.Log($"[SceneTransitionManager] Using CanvasGroup.FadeIn() - black panel fade (alpha {fadeCanvasGroup.alpha} → 0.0 over {duration}s)");
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        fadeCanvasGroup.alpha = 0f; // Ensure fully transparent
        Debug.Log("[SceneTransitionManager] Fade in complete - alpha now 0.0 (BLACK PANEL HIDDEN, SCENE VISIBLE)");
    }

    /// <summary>
    /// Fade from black to transparent while fading out the logo faster
    /// Logo fades out in first half of duration to ensure it always has black backing
    /// VR: Uses OVRScreenFade for black, manual lerp for logo
    /// Desktop: Uses CanvasGroup for black, manual lerp for logo
    /// </summary>
    private IEnumerator FadeInWithLogo(float duration)
    {
        Debug.Log($"[SceneTransitionManager] FadeInWithLogo() called - screenFade exists? {screenFade != null}");

        // Re-find references if lost during scene transition
        if (fadeCanvasGroup == null)
        {
            Debug.LogWarning("[SceneTransitionManager] fadeCanvasGroup reference lost - attempting to re-find...");

            // DEBUG: Log all children to see what exists
            Debug.Log($"[SceneTransitionManager] DEBUG: SceneTransitionManager has {transform.childCount} children:");
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                Debug.Log($"[SceneTransitionManager]   Child[{i}]: {child.name}");
            }

            // Try both possible canvas names (TransitionOverlayCanvas from Inspector, TransitionCanvas from code)
            Transform fadeTransform = transform.Find("TransitionOverlayCanvas/FadeCanvas");
            if (fadeTransform == null)
            {
                fadeTransform = transform.Find("TransitionCanvas/FadeCanvas");
            }

            Debug.Log($"[SceneTransitionManager] transform.Find('FadeCanvas') result: {(fadeTransform != null ? fadeTransform.name : "NULL")}");

            if (fadeTransform != null)
            {
                fadeCanvasGroup = fadeTransform.GetComponent<CanvasGroup>();
                Debug.Log($"[SceneTransitionManager] ✓ Re-found fadeCanvasGroup: {(fadeCanvasGroup != null ? "SUCCESS" : "FAILED")}");
            }
            else
            {
                Debug.LogError("[SceneTransitionManager] ✗ Could not find FadeCanvas in either TransitionOverlayCanvas or TransitionCanvas!");
            }
        }

        if (missionLogoImage == null)
        {
            Debug.LogWarning("[SceneTransitionManager] missionLogoImage reference lost - attempting to re-find...");

            // Try both possible canvas names (TransitionOverlayCanvas from Inspector, TransitionCanvas from code)
            Transform logoTransform = transform.Find("TransitionOverlayCanvas/MissionLogo");
            if (logoTransform == null)
            {
                logoTransform = transform.Find("TransitionCanvas/MissionLogo");
            }

            Debug.Log($"[SceneTransitionManager] transform.Find('MissionLogo') result: {(logoTransform != null ? logoTransform.name : "NULL")}");

            if (logoTransform != null)
            {
                missionLogoImage = logoTransform.GetComponent<Image>();
                Debug.Log($"[SceneTransitionManager] ✓ Re-found missionLogoImage: {(missionLogoImage != null ? "SUCCESS" : "FAILED")}");
            }
            else
            {
                Debug.LogError("[SceneTransitionManager] ✗ Could not find MissionLogo in either TransitionOverlayCanvas or TransitionCanvas!");
            }
        }

        // Get the logo's current alpha (should be 1.0 from hold phase)
        float logoStartAlpha = 1f;
        if (missionLogoImage != null && missionLogoImage.gameObject.activeSelf)
        {
            logoStartAlpha = missionLogoImage.color.a;
        }

        // Logo fades out in first half of duration (faster)
        float logoFadeDuration = duration * 0.5f;

        // ALWAYS use CanvasGroup (black panel) with logo fade
        Debug.Log($"[SceneTransitionManager] Using CanvasGroup with logo fade-out");

        if (fadeCanvasGroup == null)
        {
            Debug.LogWarning("[SceneTransitionManager] FadeCanvasGroup not assigned!");
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Fade out black screen over full duration
            fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

            // Fade out logo faster (in first half of duration)
            if (missionLogoImage != null && missionLogoImage.gameObject.activeSelf)
            {
                float logoT = Mathf.Clamp01(elapsed / logoFadeDuration);
                Color logoColor = missionLogoImage.color;
                logoColor.a = Mathf.Lerp(logoStartAlpha, 0f, logoT);
                missionLogoImage.color = logoColor;
            }

            yield return null;
        }

        // Ensure fully transparent
        fadeCanvasGroup.alpha = 0f;
        Debug.Log("[SceneTransitionManager] Fade in with logo complete - alpha now 0.0 (BLACK PANEL HIDDEN)");

        // Hide logo completely
        if (missionLogoImage != null)
        {
            missionLogoImage.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Show mission logo during transition
    /// </summary>
    private void ShowMissionLogo(string mission)
    {
        // Re-find logo if reference was lost during scene transition
        if (missionLogoImage == null)
        {
            Debug.LogWarning("[SceneTransitionManager] MissionLogoImage reference lost - attempting to re-find...");

            // Try both possible canvas names (TransitionOverlayCanvas from Inspector, TransitionCanvas from code)
            Transform logoTransform = transform.Find("TransitionOverlayCanvas/MissionLogo");
            if (logoTransform == null)
            {
                logoTransform = transform.Find("TransitionCanvas/MissionLogo");
            }

            if (logoTransform != null)
            {
                missionLogoImage = logoTransform.GetComponent<Image>();
                Debug.Log($"[SceneTransitionManager] ✓ Re-found MissionLogoImage: {(missionLogoImage != null ? "SUCCESS" : "FAILED")}");
            }

            if (missionLogoImage == null)
            {
                Debug.LogError("[SceneTransitionManager] ✗ Could not re-find MissionLogoImage - logo will not be displayed!");
                return;
            }
        }

        Sprite logo = GetMissionLogo(mission);
        if (logo != null)
        {
            missionLogoImage.sprite = logo;
            missionLogoImage.gameObject.SetActive(true);
            missionLogoImage.color = new Color(1f, 1f, 1f, 0f); // Start transparent
            Debug.Log($"[SceneTransitionManager] ✓ Showing {mission} logo (sprite: {logo.name}, active: {missionLogoImage.gameObject.activeSelf}, alpha: 0)");
        }
        else
        {
            Debug.LogWarning($"[SceneTransitionManager] ✗ No logo sprite found for mission: {mission}");
        }
    }

    /// <summary>
    /// Hide mission logo
    /// </summary>
    private void HideMissionLogo()
    {
        if (missionLogoImage != null)
        {
            missionLogoImage.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Get scene name for a mission
    /// </summary>
    private string GetSceneName(string mission)
    {
        return mission switch
        {
            "ISS" => "ISS",
            "Voyager" => "Voyager",
            "Hubble" => "Hubble",
            "Hub" => "Hub",
            _ => "Hub" // Default to Hub if unknown
        };
    }

    /// <summary>
    /// Get logo sprite for a mission
    /// </summary>
    private Sprite GetMissionLogo(string mission)
    {
        return mission switch
        {
            "Hub" => hubLogo,
            "ISS" => issLogo,
            "Voyager" => voyagerLogo,
            "Hubble" => hubbleLogo,
            _ => null
        };
    }

    /// <summary>
    /// Discovers UI references in the current scene by searching for known GameObject names
    /// This is needed when SceneTransitionManager persists but UI is recreated in new scenes
    /// </summary>
    private void DiscoverUIReferences()
    {
        Debug.Log($"[SceneTransitionManager] ╔══════════════════════════════════════════════════════════");
        Debug.Log($"[SceneTransitionManager] ║ DISCOVERING UI REFERENCES");
        Debug.Log($"[SceneTransitionManager] ║ Current Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        Debug.Log($"[SceneTransitionManager] ║ fadeCanvasGroup currently: {(fadeCanvasGroup == null ? "NULL" : fadeCanvasGroup.gameObject.name)}");
        Debug.Log($"[SceneTransitionManager] ║ missionLogoImage currently: {(missionLogoImage == null ? "NULL" : missionLogoImage.gameObject.name)}");
        Debug.Log($"[SceneTransitionManager] ╚══════════════════════════════════════════════════════════");

        // Find fade canvas by name (common in scene hierarchy)
        if (fadeCanvasGroup == null)
        {
            Debug.Log("[SceneTransitionManager] Searching for GameObject named 'FadeCanvas'...");
            GameObject fadeObj = GameObject.Find("FadeCanvas");
            if (fadeObj != null)
            {
                Debug.Log($"[SceneTransitionManager] Found GameObject 'FadeCanvas', checking for CanvasGroup component...");
                fadeCanvasGroup = fadeObj.GetComponent<CanvasGroup>();
                if (fadeCanvasGroup != null)
                {
                    Debug.Log($"[SceneTransitionManager] ✓ SUCCESS: Found FadeCanvas CanvasGroup (current alpha: {fadeCanvasGroup.alpha})");
                }
                else
                {
                    Debug.LogError("[SceneTransitionManager] ✗ FAILED: GameObject 'FadeCanvas' exists but has NO CanvasGroup component!");
                }
            }
            else
            {
                Debug.LogError("[SceneTransitionManager] ✗ FAILED: No GameObject named 'FadeCanvas' found in scene!");
            }
        }
        else
        {
            Debug.Log("[SceneTransitionManager] fadeCanvasGroup already assigned, skipping discovery");
        }

        // Find mission logo image by name
        if (missionLogoImage == null)
        {
            Debug.Log("[SceneTransitionManager] Searching for GameObject named 'MissionLogo' or 'MissionLogoImage'...");
            GameObject logoObj = GameObject.Find("MissionLogo");
            if (logoObj == null)
            {
                logoObj = GameObject.Find("MissionLogoImage");
            }

            if (logoObj != null)
            {
                Debug.Log($"[SceneTransitionManager] Found GameObject '{logoObj.name}', checking for Image component...");
                missionLogoImage = logoObj.GetComponent<Image>();
                if (missionLogoImage != null)
                {
                    Debug.Log($"[SceneTransitionManager] ✓ SUCCESS: Found MissionLogo Image component");
                }
                else
                {
                    Debug.LogError($"[SceneTransitionManager] ✗ FAILED: GameObject '{logoObj.name}' exists but has NO Image component!");
                }
            }
            else
            {
                Debug.LogError("[SceneTransitionManager] ✗ FAILED: No GameObject named 'MissionLogo' or 'MissionLogoImage' found in scene!");
            }
        }
        else
        {
            Debug.Log("[SceneTransitionManager] missionLogoImage already assigned, skipping discovery");
        }

        // Find loading text by name (optional)
        if (loadingText == null)
        {
            GameObject textObj = GameObject.Find("LoadingText");
            if (textObj != null)
            {
                loadingText = textObj.GetComponent<TMP_Text>();
                if (loadingText != null)
                {
                    Debug.Log("[SceneTransitionManager] ✓ Found LoadingText (optional)");
                }
            }
        }

        Debug.Log($"[SceneTransitionManager] ╔══════════════════════════════════════════════════════════");
        Debug.Log($"[SceneTransitionManager] ║ DISCOVERY COMPLETE");
        Debug.Log($"[SceneTransitionManager] ║ fadeCanvasGroup: {(fadeCanvasGroup == null ? "❌ NULL" : "✓ ASSIGNED")}");
        Debug.Log($"[SceneTransitionManager] ║ missionLogoImage: {(missionLogoImage == null ? "❌ NULL" : "✓ ASSIGNED")}");
        Debug.Log($"[SceneTransitionManager] ╚══════════════════════════════════════════════════════════");
    }

    /// <summary>
    /// Creates persistent transition UI if it doesn't exist
    /// Called in Awake() to ensure UI exists across all scenes
    /// VR-compatible: Uses OVRScreenFade for black fade + World Space canvas for logo
    /// </summary>
    private void CreateTransitionUIIfNeeded()
    {
        // Only create if both are missing (first time initialization)
        if (fadeCanvasGroup == null && missionLogoImage == null)
        {
            Debug.Log("[SceneTransitionManager] ╔══════════════════════════════════════════════════════════");
            Debug.Log("[SceneTransitionManager] ║ CREATING PERSISTENT TRANSITION UI (VR-COMPATIBLE)");
            Debug.Log("[SceneTransitionManager] ╚══════════════════════════════════════════════════════════");

            // Detect VR mode
            isVR = UnityEngine.XR.XRSettings.isDeviceActive;
            Debug.Log($"[SceneTransitionManager] ═══ VR DETECTION ═══");
            Debug.Log($"[SceneTransitionManager] XRSettings.isDeviceActive: {UnityEngine.XR.XRSettings.isDeviceActive}");
            Debug.Log($"[SceneTransitionManager] XRSettings.loadedDeviceName: '{UnityEngine.XR.XRSettings.loadedDeviceName}'");
            Debug.Log($"[SceneTransitionManager] isVR: {isVR}");
            Debug.Log($"[SceneTransitionManager] Platform: {Application.platform}");
            Debug.Log($"[SceneTransitionManager] Running in Editor: {Application.isEditor}");
            Debug.Log($"[SceneTransitionManager] ══════════════════════");

            // === 1. Create OVRScreenFade for guaranteed black fade ===
            GameObject fadeObj = new GameObject("OVRScreenFade");
            fadeObj.transform.SetParent(transform, false);
            screenFade = fadeObj.AddComponent<OVRScreenFade>();

            // Configure BEFORE Start() runs
            screenFade.fadeTime = fadeDuration;
            screenFade.fadeColor = new Color(0.0f, 0.0f, 0.0f, 1.0f); // Pure black, full alpha
            screenFade.fadeOnStart = false; // We control it manually
            screenFade.renderQueue = 5000; // Ensure it renders on top

            Debug.Log($"[SceneTransitionManager] ✓ Created OVRScreenFade");
            Debug.Log($"[SceneTransitionManager]   - fadeTime: {screenFade.fadeTime}s");
            Debug.Log($"[SceneTransitionManager]   - fadeColor: RGBA({screenFade.fadeColor.r}, {screenFade.fadeColor.g}, {screenFade.fadeColor.b}, {screenFade.fadeColor.a})");
            Debug.Log($"[SceneTransitionManager]   - renderQueue: {screenFade.renderQueue}");

            // === 2. Create World Space Canvas for logo ===
            GameObject canvasObj = new GameObject("TransitionCanvas");
            canvasObj.transform.SetParent(transform, false);
            transitionCanvasTransform = canvasObj.transform; // Cache for LateUpdate()

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100; // Render on top of OVRScreenFade

            // Set world space canvas size (2m x 2m)
            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(2f, 2f);

            Debug.Log("[SceneTransitionManager] ✓ Created TransitionCanvas (WorldSpace, 2m x 2m, SortOrder: 100)");

            // === 3. Keep canvas parented to persistent SceneTransitionManager ===
            // NOTE: We do NOT parent to CenterEyeAnchor because it gets destroyed on scene changes!
            // Instead, we'll position it manually in LateUpdate() to follow the camera
            Debug.Log("[SceneTransitionManager] ✓ Canvas remains child of persistent SceneTransitionManager (will follow camera via LateUpdate)");

            // Position it initially at world origin
            canvasObj.transform.position = Vector3.zero;
            canvasObj.transform.rotation = Quaternion.identity;

            // === 4. Create full-screen black panel (for desktop fallback) ===
            GameObject fadePanelObj = new GameObject("FadeCanvas");
            fadePanelObj.transform.SetParent(canvasObj.transform, false);

            fadeCanvasGroup = fadePanelObj.AddComponent<CanvasGroup>();
            fadeCanvasGroup.alpha = 0f; // Start transparent
            fadeCanvasGroup.interactable = false;
            fadeCanvasGroup.blocksRaycasts = false;

            Image fadeImage = fadePanelObj.AddComponent<Image>();
            fadeImage.color = Color.black;
            fadeImage.raycastTarget = false;

            RectTransform fadeRect = fadePanelObj.GetComponent<RectTransform>();
            fadeRect.anchorMin = Vector2.zero;
            fadeRect.anchorMax = Vector2.one;
            fadeRect.offsetMin = Vector2.zero;
            fadeRect.offsetMax = Vector2.zero;

            Debug.Log($"[SceneTransitionManager] ✓ Created FadeCanvas (CanvasGroup fallback, alpha: {fadeCanvasGroup.alpha})");

            // === 5. Create Mission Logo (centered image) ===
            GameObject logoObj = new GameObject("MissionLogo");
            logoObj.transform.SetParent(canvasObj.transform, false);

            missionLogoImage = logoObj.AddComponent<Image>();
            missionLogoImage.raycastTarget = false;
            missionLogoImage.preserveAspect = true;

            RectTransform logoRect = logoObj.GetComponent<RectTransform>();
            logoRect.anchorMin = new Vector2(0.5f, 0.5f);
            logoRect.anchorMax = new Vector2(0.5f, 0.5f);
            logoRect.pivot = new Vector2(0.5f, 0.5f);
            logoRect.sizeDelta = new Vector2(512, 512); // Logo size: 512x512
            logoRect.anchoredPosition = Vector2.zero;

            logoObj.SetActive(false); // Hidden by default

            Debug.Log("[SceneTransitionManager] ✓ Created MissionLogo (Image, 512x512, centered, hidden)");

            Debug.Log("[SceneTransitionManager] ╔══════════════════════════════════════════════════════════");
            Debug.Log("[SceneTransitionManager] ║ PERSISTENT UI CREATION COMPLETE (VR + DESKTOP COMPATIBLE)");
            Debug.Log("[SceneTransitionManager] ║ OVRScreenFade: Black fade (VR-optimized)");
            Debug.Log("[SceneTransitionManager] ║ World Space Canvas: Logo display (1m from camera)");
            Debug.Log("[SceneTransitionManager] ╚══════════════════════════════════════════════════════════");
        }
        else
        {
            Debug.Log($"[SceneTransitionManager] Persistent UI already exists (fadeCanvasGroup: {fadeCanvasGroup != null}, missionLogoImage: {missionLogoImage != null})");
        }
    }

    /// <summary>
    /// Find camera anchor for World Space canvas parenting
    /// VR: Returns OVRCameraRig.centerEyeAnchor
    /// Desktop: Returns Camera.main.transform
    /// </summary>
    private Transform FindCameraAnchor()
    {
        Debug.Log($"[SceneTransitionManager] ═══ FINDING CAMERA ANCHOR ═══");

        // Try to find OVRCameraRig component
        Debug.Log($"[SceneTransitionManager] Searching for OVRCameraRig with FindObjectOfType...");
        OVRCameraRig rig = FindObjectOfType<OVRCameraRig>();
        Debug.Log($"[SceneTransitionManager] FindObjectOfType<OVRCameraRig>() result: {(rig != null ? rig.name : "NULL")}");

        if (rig != null)
        {
            Debug.Log($"[SceneTransitionManager] OVRCameraRig found! Checking centerEyeAnchor...");
            Debug.Log($"[SceneTransitionManager] centerEyeAnchor: {(rig.centerEyeAnchor != null ? rig.centerEyeAnchor.name : "NULL")}");

            if (rig.centerEyeAnchor != null)
            {
                Debug.Log($"[SceneTransitionManager] ✓ Using centerEyeAnchor at position: {rig.centerEyeAnchor.position}");
                Debug.Log($"[SceneTransitionManager] ═══════════════════════════════");
                return rig.centerEyeAnchor;
            }
        }

        // Fallback to Camera.main for desktop
        Debug.Log($"[SceneTransitionManager] OVRCameraRig not found or no centerEyeAnchor, falling back to Camera.main...");
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            Debug.Log($"[SceneTransitionManager] ✓ Using Camera.main: {mainCam.name} at position: {mainCam.transform.position}");
            Debug.Log($"[SceneTransitionManager] ═══════════════════════════════");
            return mainCam.transform;
        }

        Debug.LogError("[SceneTransitionManager] ✗ No camera anchor found!");
        Debug.Log($"[SceneTransitionManager] ═══════════════════════════════");
        return null;
    }

    // ---------------- Public Fade Methods ----------------

    /// <summary>
    /// Public method to fade out to black
    /// </summary>
    public IEnumerator FadeOut()
    {
        Debug.Log("[SceneTransitionManager] Public FadeOut() called");
        yield return FadeOut(fadeDuration);
    }

    /// <summary>
    /// Public method to fade in from black
    /// </summary>
    public IEnumerator FadeIn()
    {
        Debug.Log("[SceneTransitionManager] Public FadeIn() called");
        yield return FadeIn(fadeDuration);
    }
}
