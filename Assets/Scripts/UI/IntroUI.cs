using System.Collections;
using UnityEngine;
using TMPro;

namespace UI
{
    /// <summary>
    /// Manages the introduction cutscene UI overlay.
    /// Displays Mission Control's welcome message with fade in/out animations.
    ///
    /// This implements the visual component of the "Opening Experience" from Phase 3
    /// of the implementation plan.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class IntroUI : MonoBehaviour
    {
        [Header("Intro Text")]
        [Tooltip("Mission Control's introduction message")]
        [TextArea(4, 10)]
        public string introMessage = "Welcome to Mission Control.\n\n" +
                                     "I'm here to guide you through the fundamentals of orbital mechanics.\n\n" +
                                     "Together, we'll design trajectories, understand the physics, " +
                                     "and explore how satellites navigate space.";

        [Header("References")]
        [Tooltip("TextMeshPro component for displaying intro text")]
        public TMP_Text introText;

        [Header("Timing")]
        [Tooltip("Fade in duration (seconds)")]
        public float fadeInDuration = 1.5f;

        [Tooltip("How long to display message before fading out (seconds)")]
        public float displayDuration = 8f;

        [Tooltip("Fade out duration (seconds)")]
        public float fadeOutDuration = 2f;

        private CanvasGroup _canvasGroup;
        private Coroutine _introCoroutine;

        // ---------------- Lifecycle ----------------

        void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();

            // Start invisible
            _canvasGroup.alpha = 0f;

            // Set intro text
            if (introText != null)
            {
                introText.text = introMessage;
            }
        }

        // ---------------- Public API ----------------

        /// <summary>
        /// Show the intro with fade animation
        /// </summary>
        public void ShowIntro()
        {
            if (_introCoroutine != null)
            {
                StopCoroutine(_introCoroutine);
            }

            _introCoroutine = StartCoroutine(IntroSequence());
        }

        /// <summary>
        /// Hide the intro immediately (called when transitioning to Hub)
        /// </summary>
        public void HideIntro()
        {
            if (_introCoroutine != null)
            {
                StopCoroutine(_introCoroutine);
            }

            _canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        // ---------------- Animation Sequence ----------------

        /// <summary>
        /// Coroutine that handles the full intro animation sequence
        /// </summary>
        private IEnumerator IntroSequence()
        {
            // Ensure visible
            gameObject.SetActive(true);

            // Fade in
            yield return FadeCanvasGroup(_canvasGroup, 0f, 1f, fadeInDuration);

            // Display for duration
            yield return new WaitForSeconds(displayDuration);

            // Fade out
            yield return FadeCanvasGroup(_canvasGroup, 1f, 0f, fadeOutDuration);

            // Hide
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Smoothly fades a CanvasGroup from one alpha value to another
        /// </summary>
        private IEnumerator FadeCanvasGroup(CanvasGroup cg, float startAlpha, float endAlpha, float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime; // Use unscaled time so intro works even if simulation is paused
                float t = Mathf.Clamp01(elapsed / duration);
                cg.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
                yield return null;
            }

            cg.alpha = endAlpha;
        }
    }
}
