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
    public float fadeDuration = 1.5f;

    [Tooltip("Total time for black screen with logo (seconds)")]
    public float sceneLoadWaitTime = 4f;

    [Header("UI References")]
    [Tooltip("CanvasGroup for fade effect (alpha 0=transparent, 1=black)")]
    public CanvasGroup fadeCanvasGroup;

    [Tooltip("Image component for displaying mission logos")]
    public Image missionLogoImage;

    [Tooltip("Optional text for loading feedback")]
    public TMP_Text loadingText;

    [Header("Mission Logos")]
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

        // === PHASE 1: Fade to Black ===
        Debug.Log($"[SceneTransitionManager] Phase 1: Fading to black ({fadeDuration}s)");
        yield return FadeOut(fadeDuration);

        // === PHASE 2: Show Mission Logo (if not Hub) ===
        if (destination != "Hub")
        {
            ShowMissionLogo(destination);
        }

        // === PHASE 3: Load Scene Asynchronously ===
        Debug.Log($"[SceneTransitionManager] Phase 3: Loading scene '{sceneName}'");
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);
        loadOperation.allowSceneActivation = false; // Don't activate until we're ready

        // === PHASE 4: Wait Fixed Duration (Patient Loading) ===
        // During this time:
        // - Scene is loading in background
        // - Logo is animating (fade in/out)
        // - User experiences calm, beautiful transition
        float elapsed = 0f;
        while (elapsed < sceneLoadWaitTime)
        {
            elapsed += Time.unscaledDeltaTime;

            // Animate logo (pulse fade in/out)
            if (missionLogoImage != null && missionLogoImage.gameObject.activeSelf)
            {
                float logoAlpha = Mathf.PingPong(elapsed * 0.8f, 1f);
                Color logoColor = missionLogoImage.color;
                logoColor.a = logoAlpha;
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

        // === PHASE 6: Hide Logo ===
        HideMissionLogo();

        // === PHASE 7: Fade In from Black ===
        Debug.Log($"[SceneTransitionManager] Phase 7: Fading in from black ({fadeDuration}s)");
        yield return FadeIn(fadeDuration);

        isTransitioning = false;
        Debug.Log($"[SceneTransitionManager] Transition to {destination} complete");

        // Note: Specialist introduction will be triggered by MissionSpaceController in the new scene
    }

    /// <summary>
    /// Fade from transparent to black
    /// </summary>
    private IEnumerator FadeOut(float duration)
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
            fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }

        fadeCanvasGroup.alpha = 1f; // Ensure fully black
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
            "ISS" => "ISS_Mission",
            "GPS" => "GPS_Mission",
            "Voyager" => "Voyager_Mission",
            "Hubble" => "Hubble_Mission",
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
            "ISS" => issLogo,
            "GPS" => gpsLogo,
            "Voyager" => voyagerLogo,
            "Hubble" => hubbleLogo,
            _ => null
        };
    }
}
