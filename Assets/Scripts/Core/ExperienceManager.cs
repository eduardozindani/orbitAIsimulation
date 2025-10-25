using System.Collections;
using UnityEngine;
using UI;

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

        [Tooltip("Camera zoom duration in seconds")]
        public float cameraZoomDuration = 15f;

        [Tooltip("Animation curve for smooth zoom (EaseInOut for cinematic feel)")]
        public AnimationCurve cameraZoomCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

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

        // ---------------- Lifecycle ----------------

        void Start()
        {
            if (skipIntro)
            {
                Debug.Log("[ExperienceManager] Skipping intro, going straight to Hub");
                StartHub();
            }
            else
            {
                Debug.Log("[ExperienceManager] Starting intro cutscene");
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
            if (cameraController != null)
            {
                cameraController.allowExternalRadiusControl = true;
                cameraController.SetRadius(introCameraStartRadius);
                Debug.Log($"[ExperienceManager] Camera set to deep space (radius: {introCameraStartRadius})");
            }

            // Disable user input console
            if (promptConsole != null)
            {
                promptConsole.DisableConsole();
            }

            // Show intro UI
            if (introUI != null)
            {
                introUI.ShowIntro();
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

            // Return camera control to user
            if (cameraController != null)
            {
                cameraController.allowExternalRadiusControl = false;
                Debug.Log("[ExperienceManager] Camera control returned to user");
            }

            // Enable user input console
            if (promptConsole != null)
            {
                promptConsole.EnableConsole();
            }

            // Hide intro UI
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
            Debug.Log("[ExperienceManager] Intro cutscene started");

            // Animate camera zoom if camera controller is assigned
            if (cameraController != null)
            {
                yield return StartCoroutine(AnimateCameraZoom());
            }
            else
            {
                // No camera animation, just wait for intro duration
                yield return new WaitForSeconds(introDuration);
            }

            // Immediate transition to Hub (no pause)
            Debug.Log("[ExperienceManager] Intro cutscene complete, transitioning to Hub");
            StartHub();
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
    }
}
