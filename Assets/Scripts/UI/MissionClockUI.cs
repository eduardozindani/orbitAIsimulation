using UnityEngine;
using TMPro;

/// <summary>
/// Displays mission elapsed time and current simulation speed in top-left corner.
/// Updates every frame to show real-time simulation progress.
/// Requires a TimeController in the scene.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class MissionClockUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The TimeController managing simulation speed")]
    public TimeController timeController;

    [Tooltip("TextMeshPro component for displaying time")]
    public TMP_Text clockText;

    [Header("Display Settings")]
    [Tooltip("Show 'GMT' suffix after time")]
    public bool showGMT = true;

    [Tooltip("Show current speed multiplier")]
    public bool showSpeed = true;

    [Tooltip("Text color for clock display")]
    public Color textColor = new Color(0f, 1f, 1f, 1f); // Cyan

    [Tooltip("Background panel opacity (0-1)")]
    [Range(0f, 1f)]
    public float backgroundOpacity = 0.7f;

    [Header("Formatting")]
    [Tooltip("Font size for clock text")]
    public float fontSize = 24f;

    private CanvasGroup canvasGroup;

    // ---------------- Lifecycle ----------------

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    void Start()
    {
        // Find TimeController if not assigned
        if (timeController == null)
        {
            timeController = FindFirstObjectByType<TimeController>();

            if (timeController == null)
            {
                Debug.LogError("[MissionClockUI] No TimeController found in scene! Clock will not function.");
                enabled = false;
                return;
            }
        }

        // Configure text appearance
        if (clockText != null)
        {
            clockText.color = textColor;
            clockText.fontSize = fontSize;
        }

        // Configure background opacity
        if (canvasGroup != null)
        {
            canvasGroup.alpha = backgroundOpacity;
        }

        Debug.Log("[MissionClockUI] Mission clock initialized");
    }

    void Update()
    {
        UpdateClockDisplay();
    }

    // ---------------- Display Update ----------------

    private void UpdateClockDisplay()
    {
        if (clockText == null || timeController == null)
            return;

        // Build clock string
        string timeString = timeController.GetFormattedTime();

        if (showGMT)
        {
            timeString += " GMT";
        }

        string speedString = "";
        if (showSpeed)
        {
            speedString = $"\nSpeed: {timeController.GetSpeedDescription()}";
        }

        clockText.text = $"Mission Time: {timeString}{speedString}";
    }

    // ---------------- Public API ----------------

    /// <summary>
    /// Show the clock UI
    /// </summary>
    public void Show()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = backgroundOpacity;
        }
        enabled = true;
    }

    /// <summary>
    /// Hide the clock UI
    /// </summary>
    public void Hide()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
        enabled = false;
    }

    /// <summary>
    /// Toggle clock visibility
    /// </summary>
    public void Toggle()
    {
        if (canvasGroup != null && canvasGroup.alpha > 0.5f)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }

    // ---------------- Inspector Validation ----------------

    void OnValidate()
    {
        // Update display immediately when values change in Inspector
        if (clockText != null && Application.isPlaying)
        {
            clockText.color = textColor;
            clockText.fontSize = fontSize;
        }

        if (canvasGroup != null && Application.isPlaying)
        {
            canvasGroup.alpha = backgroundOpacity;
        }
    }
}
