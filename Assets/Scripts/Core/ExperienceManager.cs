using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UI;
using XR;

namespace Core
{
    /// <summary>
    /// Manages the application state and orchestrates transitions between
    /// the introduction cutscene and the main Hub experience.
    ///
    /// States:
    /// - INTRO_CUTSCENE: Welcome sequence with camera animation and text
    /// - HUB_ACTIVE: Normal interactive simulation with Mission Control
    ///
    /// This implements the "Opening Experience" from Phase 3 of the implementation plan.
    /// </summary>
    public class ExperienceManager : MonoBehaviour
    {
        public enum State
        {
            INTRO_CUTSCENE,
            HUB_ACTIVE
        }

        [Header("State")]
        [Tooltip("Current application state")]
        public State currentState = State.INTRO_CUTSCENE;

        [Header("Intro Settings")]
        [Tooltip("Duration of intro cutscene in seconds")]
        public float introDuration = 12f;

        [Tooltip("Skip intro and go straight to Hub (for testing)")]
        public bool skipIntro = false;

        [Header("Camera Animation")]
        [Tooltip("Starting camera distance for intro (deep space)")]
        public float introCameraStartRadius = 50f;

        [Tooltip("Camera zoom duration in seconds (synced with audio narration)")]
        public float cameraZoomDuration = 28.7f;

        [Tooltip("Animation curve for smooth zoom (EaseInOut for cinematic feel)")]
        public AnimationCurve cameraZoomCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Audio")]
        [Tooltip("Voice narration for intro (28.7 seconds)")]
        public AudioClip introNarration;

        [Tooltip("Background music (loops continuously)")]
        public AudioClip backgroundMusic;

        [Tooltip("Background music volume (0-1)")]
        [Range(0f, 1f)]
        public float musicVolume = 0.4f;

        [Header("References")]
        [Tooltip("The satellite orbit controller (will be disabled during intro)")]
        public Orbit satelliteOrbit;

        [Tooltip("The prompt console for user input (will be disabled during intro)")]
        public PromptConsole promptConsole;

        [Tooltip("The intro UI overlay (will show intro text)")]
        public IntroUI introUI;

        [Tooltip("The camera controller (optional - for intro animation)")]
        public CameraSphereController cameraController;

        private bool _introCompleted = false;
        private AudioSource _narrationSource;
        private AudioSource _musicSource;

        private static ExperienceManager _instance;
        private static bool _hasPlayedIntroThisSession = false; // Static flag survives scene reloads and instance recreation

        // ---------------- Lifecycle ----------------

        void Awake()
        {
            Debug.Log($"[ExperienceManager] Awake() called - Current _instance exists: {_instance != null}, This is new instance: {_instance != this}, _hasPlayedIntroThisSession={_hasPlayedIntroThisSession}");

            // Singleton pattern - only allow one ExperienceManager to exist
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[ExperienceManager] Duplicate instance detected - destroying this one (persisted instance continues)");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            Debug.Log("[ExperienceManager] Set as singleton instance, applying DontDestroyOnLoad");

            // Persist across all scenes so music continues playing
            DontDestroyOnLoad(gameObject);

            // Create audio sources
            _narrationSource = gameObject.AddComponent<AudioSource>();
            _narrationSource.playOnAwake = false;
            _narrationSource.loop = false;

            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = true; // Loop forever!
            _musicSource.volume = musicVolume;
        }

        void Start()
        {
            // Safety check: Don't run Start() if this is a duplicate being destroyed
            if (_instance != this)
            {
                Debug.LogWarning("[ExperienceManager] Start() called on duplicate instance - ignoring");
                return;
            }

            // Check if we're returning from a mission space (routing context exists)
            bool returningFromMission = MissionContext.Instance != null &&
                                       !string.IsNullOrEmpty(MissionContext.Instance.routingReason) &&
                                       MissionContext.Instance.routingReason.Contains("Returning from");

            Debug.Log($"[ExperienceManager] Start() - skipIntro={skipIntro}, _hasPlayedIntroThisSession={_hasPlayedIntroThisSession}, returningFromMission={returningFromMission}");

            if (skipIntro || _hasPlayedIntroThisSession || returningFromMission)
            {
                Debug.Log("[ExperienceManager] Skipping intro (already played, skipIntro=true, or returning from mission) → Starting Hub directly");
                _hasPlayedIntroThisSession = true; // Mark as played for future scene loads
                StartHub();
            }
            else
            {
                Debug.Log("[ExperienceManager] Starting intro cutscene (first launch)");
                _hasPlayedIntroThisSession = true;
                StartIntro();
            }
        }

        // ---------------- State Transitions ----------------

        /// <summary>
        /// Begin the introduction cutscene
        /// </summary>
        private void StartIntro()
        {
            currentState = State.INTRO_CUTSCENE;

            // Disable satellite orbit (prevents movement and trail generation)
            if (satelliteOrbit != null)
            {
                satelliteOrbit.SetOrbitActive(false);
            }

            // Setup camera for intro animation (start far away in deep space)
            // Only use desktop camera controller in non-XR mode
            bool isXRMode = XRModeManager.Instance != null && XRModeManager.Instance.IsXRAvailable();

            if (cameraController != null && !isXRMode)
            {
                cameraController.allowExternalRadiusControl = true;
                cameraController.SetRadius(introCameraStartRadius);
                Debug.Log($"[ExperienceManager] Desktop mode - Camera set to deep space (radius: {introCameraStartRadius})");
            }
            else if (isXRMode)
            {
                Debug.Log("[ExperienceManager] VR mode - User camera controlled by XR tracking, no desktop animation");
            }

            // Disable user input console
            if (promptConsole != null)
            {
                promptConsole.DisableConsole();
            }

            // Hide intro UI (audio replaces text)
            if (introUI != null)
            {
                introUI.HideIntro();
            }

            // Start audio
            if (_narrationSource != null && introNarration != null)
            {
                _narrationSource.clip = introNarration;
                _narrationSource.Play();
                Debug.Log("[ExperienceManager] Playing intro narration");
            }

            if (_musicSource != null && backgroundMusic != null)
            {
                _musicSource.clip = backgroundMusic;
                _musicSource.Play();
                Debug.Log($"[ExperienceManager] Playing background music at {musicVolume * 100}% volume");
            }

            // Start intro sequence
            StartCoroutine(IntroSequence());
        }

        /// <summary>
        /// Transition to the Hub (normal interactive mode)
        /// </summary>
        private void StartHub()
        {
            currentState = State.HUB_ACTIVE;
            _introCompleted = true;

            // Enable satellite orbit
            if (satelliteOrbit != null)
            {
                satelliteOrbit.SetOrbitActive(true);
            }

            // Return camera control to user (desktop only)
            bool isXRMode = XRModeManager.Instance != null && XRModeManager.Instance.IsXRAvailable();

            if (cameraController != null && !isXRMode)
            {
                cameraController.allowExternalRadiusControl = false;
                Debug.Log("[ExperienceManager] Desktop mode - Camera control returned to user");
            }

            // Enable user input console
            if (promptConsole != null)
            {
                promptConsole.EnableConsole();
            }

            // Stop narration (but keep background music playing!)
            if (_narrationSource != null && _narrationSource.isPlaying)
            {
                _narrationSource.Stop();
                Debug.Log("[ExperienceManager] Intro narration stopped");
            }
            // Background music continues playing...

            // Hide intro UI (already hidden, but ensure)
            if (introUI != null)
            {
                introUI.HideIntro();
            }

            Debug.Log("[ExperienceManager] Hub is now active - user can interact");
        }

        // ---------------- Intro Sequence ----------------

        /// <summary>
        /// Coroutine that manages the intro cutscene timing and camera animation
        /// </summary>
        private IEnumerator IntroSequence()
        {
            Debug.Log("[ExperienceManager] Intro cutscene started in VR");

            // For VR: Just play narration (no camera animation needed - user naturally looks around)
            // For Desktop: Animate camera zoom
            bool isXRMode = XRModeManager.Instance != null && XRModeManager.Instance.IsXRAvailable();

            if (isXRMode)
            {
                // VR mode: Wait for narration duration (user can look around naturally)
                Debug.Log("[ExperienceManager] VR mode - waiting for narration duration");
                yield return new WaitForSeconds(cameraZoomDuration);
            }
            else
            {
                // Desktop mode: Animate camera zoom
                if (cameraController != null)
                {
                    Debug.Log("[ExperienceManager] Desktop mode - animating camera zoom");
                    yield return StartCoroutine(AnimateCameraZoom());
                }
                else
                {
                    yield return new WaitForSeconds(introDuration);
                }
            }

            // Transition to Hub (VR → AR for XR, or normal Hub for desktop)
            Debug.Log("[ExperienceManager] Intro cutscene complete, transitioning to Hub");

            if (isXRMode)
            {
                // XR mode: Stay in Hub scene, just activate Hub features and enable AR
                Debug.Log("[ExperienceManager] VR mode - activating Hub features in current scene");
                StartHub();

                // Switch to AR mode for passthrough
                if (XRModeManager.Instance != null)
                {
                    XRModeManager.Instance.SwitchToAR();
                    Debug.Log("[ExperienceManager] Switched to AR mode - passthrough should now be enabled");
                }
            }
            else
            {
                // Desktop mode: Continue with normal Hub in same scene
                StartHub();
            }
        }

        /// <summary>
        /// Smoothly animates camera from deep space to normal viewing distance
        /// </summary>
        private IEnumerator AnimateCameraZoom()
        {
            float elapsed = 0f;
            float targetRadius = cameraController.GetTargetRadius();

            Debug.Log($"[ExperienceManager] Camera zoom: {introCameraStartRadius} → {targetRadius} over {cameraZoomDuration}s");

            while (elapsed < cameraZoomDuration)
            {
                elapsed += Time.unscaledDeltaTime; // Unscaled time so works even if simulation paused
                float t = Mathf.Clamp01(elapsed / cameraZoomDuration);

                // Use animation curve for cinematic easing
                float curveValue = cameraZoomCurve.Evaluate(t);

                // Lerp from deep space to normal viewing distance
                float currentRadius = Mathf.Lerp(introCameraStartRadius, targetRadius, curveValue);

                cameraController.SetRadius(currentRadius);

                yield return null; // Wait one frame
            }

            // Ensure we end exactly at target radius
            cameraController.SetRadius(targetRadius);
            Debug.Log("[ExperienceManager] Camera arrived at Earth");
        }

        // ---------------- Public API ----------------

        /// <summary>
        /// Allow user to manually skip intro (can be called from UI button)
        /// </summary>
        public void SkipIntro()
        {
            if (currentState == State.INTRO_CUTSCENE && !_introCompleted)
            {
                Debug.Log("[ExperienceManager] User skipped intro");
                StopAllCoroutines();
                StartHub();
            }
        }

        /// <summary>
        /// Check if currently in intro
        /// </summary>
        public bool IsInIntro()
        {
            return currentState == State.INTRO_CUTSCENE;
        }

        /// <summary>
        /// Check if Hub is active
        /// </summary>
        public bool IsHubActive()
        {
            return currentState == State.HUB_ACTIVE;
        }

        // ---------------- XR Transitions ----------------

        /// <summary>
        /// Transition from VR intro to AR Hub scene
        /// </summary>
        private IEnumerator TransitionToARHub()
        {
            Debug.Log("[ExperienceManager] Transitioning from VR to AR Hub");

            // Fade out VR scene
            if (SceneTransitionManager.Instance != null)
            {
                yield return SceneTransitionManager.Instance.FadeOut();
            }
            else
            {
                // Simple fade if no transition manager
                yield return new WaitForSeconds(0.5f);
            }

            // Switch to AR mode
            if (XRModeManager.Instance != null)
            {
                XRModeManager.Instance.SwitchToAR();
            }

            // Load AR Hub scene
            Debug.Log("[ExperienceManager] Loading ARHub scene");
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("ARHub");

            // Wait for scene to load
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            Debug.Log("[ExperienceManager] ARHub scene loaded - calibration will begin");

            // Fade in AR scene
            if (SceneTransitionManager.Instance != null)
            {
                yield return SceneTransitionManager.Instance.FadeIn();
            }

            // Mark intro as completed
            _introCompleted = true;
            currentState = State.HUB_ACTIVE;
        }
    }
}
