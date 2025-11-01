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

    [Tooltip("Logo for GPS Mission Space")]
    public Sprite gpsLogo;

    [Tooltip("Logo for Voyager Mission Space")]
    public Sprite voyagerLogo;

    [Tooltip("Logo for Hubble Mission Space")]
    public Sprite hubbleLogo;

    private bool isTransitioning = false;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[SceneTransitionManager] Initialized");

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
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Transition to a mission space (ISS, GPS, Voyager, Hubble)
    /// </summary>
    public void TransitionToMission(string mission)
    {
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
    /// </summary>
    private IEnumerator FadeOut(float duration)
    {
        Debug.Log($"[SceneTransitionManager] FadeOut() called - fadeCanvasGroup null? {fadeCanvasGroup == null}");

        if (fadeCanvasGroup == null)
        {
            Debug.LogError("[SceneTransitionManager] FADE FAILED: fadeCanvasGroup is NULL - cannot fade to black!");
            yield break;
        }

        Debug.Log($"[SceneTransitionManager] Starting fade to black from alpha {fadeCanvasGroup.alpha} over {duration}s");
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }

        fadeCanvasGroup.alpha = 1f; // Ensure fully black
        Debug.Log("[SceneTransitionManager] Fade to black complete - alpha now 1.0");
    }

    /// <summary>
    /// Fade from black to transparent
    /// </summary>
    private IEnumerator FadeIn(float duration)
    {
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
            fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        fadeCanvasGroup.alpha = 0f; // Ensure fully transparent
    }

    /// <summary>
    /// Fade from black to transparent while fading out the logo faster
    /// Logo fades out in first half of duration to ensure it always has black backing
    /// </summary>
    private IEnumerator FadeInWithLogo(float duration)
    {
        if (fadeCanvasGroup == null)
        {
            Debug.LogWarning("[SceneTransitionManager] FadeCanvasGroup not assigned!");
            yield break;
        }

        // Get the logo's current alpha (should be 1.0 from hold phase)
        float logoStartAlpha = 1f;
        if (missionLogoImage != null && missionLogoImage.gameObject.activeSelf)
        {
            logoStartAlpha = missionLogoImage.color.a;
        }

        // Logo fades out in first half of duration (faster)
        float logoFadeDuration = duration * 0.5f;

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
        if (missionLogoImage == null)
        {
            Debug.LogWarning("[SceneTransitionManager] MissionLogoImage not assigned!");
            return;
        }

        Sprite logo = GetMissionLogo(mission);
        if (logo != null)
        {
            missionLogoImage.sprite = logo;
            missionLogoImage.gameObject.SetActive(true);
            missionLogoImage.color = new Color(1f, 1f, 1f, 0f); // Start transparent
            Debug.Log($"[SceneTransitionManager] Showing {mission} logo");
        }
        else
        {
            Debug.LogWarning($"[SceneTransitionManager] No logo found for mission: {mission}");
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
            "GPS" => "GPS",
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
            "GPS" => gpsLogo,
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
    /// This creates a Canvas with fade panel and logo as children of SceneTransitionManager
    /// </summary>
    private void CreateTransitionUIIfNeeded()
    {
        // Only create if both are missing (first time initialization)
        if (fadeCanvasGroup == null && missionLogoImage == null)
        {
            Debug.Log("[SceneTransitionManager] ╔══════════════════════════════════════════════════════════");
            Debug.Log("[SceneTransitionManager] ║ CREATING PERSISTENT TRANSITION UI");
            Debug.Log("[SceneTransitionManager] ╚══════════════════════════════════════════════════════════");

            // Create root Canvas as child of SceneTransitionManager
            GameObject canvasObj = new GameObject("TransitionCanvas");
            canvasObj.transform.SetParent(transform, false);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // Render on top of everything

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            Debug.Log("[SceneTransitionManager] ✓ Created TransitionCanvas (ScreenSpace-Overlay, SortOrder: 1000)");

            // Create Fade Panel (full screen black overlay)
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

            Debug.Log($"[SceneTransitionManager] ✓ Created FadeCanvas (CanvasGroup, alpha: {fadeCanvasGroup.alpha})");

            // Create Mission Logo (centered image)
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
            Debug.Log("[SceneTransitionManager] ║ PERSISTENT UI CREATION COMPLETE");
            Debug.Log("[SceneTransitionManager] ║ This UI will persist across all scene transitions");
            Debug.Log("[SceneTransitionManager] ╚══════════════════════════════════════════════════════════");
        }
        else
        {
            Debug.Log($"[SceneTransitionManager] Persistent UI already exists (fadeCanvasGroup: {fadeCanvasGroup != null}, missionLogoImage: {missionLogoImage != null})");
        }
    }
}
