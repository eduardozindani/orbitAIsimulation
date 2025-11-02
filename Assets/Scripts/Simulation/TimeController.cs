using UnityEngine;

/// <summary>
/// Controls simulation time acceleration using Unity's Time.timeScale.
/// Allows speeding up orbital simulations without breaking physics.
/// Supports keyboard shortcuts for quick speed adjustments.
/// </summary>
public class TimeController : MonoBehaviour
{
    [Header("Time Speed Presets")]
    [Tooltip("Available time multipliers (1x = real-time)")]
    public float[] speedPresets = { 1f, 10f, 50f, 100f, 250f, 500f };

    [Header("Keyboard Controls")]
    [Tooltip("Enable keyboard shortcuts for manual control (disabled by default - use AI control instead)")]
    public bool enableKeyboardControls = false;

    [Tooltip("Key to pause/resume simulation")]
    public KeyCode pauseKey = KeyCode.Space;

    [Tooltip("Keys for speed presets (1-5)")]
    public KeyCode[] speedKeys = {
        KeyCode.Alpha1,
        KeyCode.Alpha2,
        KeyCode.Alpha3,
        KeyCode.Alpha4,
        KeyCode.Alpha5
    };

    [Header("Current State")]
    [Tooltip("Current time multiplier")]
    public float currentSpeed = 1f;

    [Tooltip("Is simulation paused?")]
    public bool isPaused = false;

    [Tooltip("Accumulated simulation time in seconds")]
    public double simulationTime = 0.0;

    private float previousTimeScale = 1f;

    // ---------------- Lifecycle ----------------

    void Start()
    {
        // Initialize with default speed
        SetSpeed(currentSpeed);
        Debug.Log($"[TimeController] Initialized at {currentSpeed}x speed");
    }

    void Update()
    {
        // Handle keyboard input only if enabled
        if (enableKeyboardControls)
        {
            HandleInput();
        }

        // Accumulate simulation time (affected by timeScale)
        if (!isPaused)
        {
            simulationTime += Time.deltaTime;
        }
    }

    // ---------------- Input Handling ----------------

    private void HandleInput()
    {
        // Pause/Resume toggle
        if (Input.GetKeyDown(pauseKey))
        {
            TogglePause();
        }

        // Speed preset keys (1-5)
        for (int i = 0; i < speedKeys.Length && i < speedPresets.Length; i++)
        {
            if (Input.GetKeyDown(speedKeys[i]))
            {
                if (isPaused)
                {
                    // If paused, unpause first
                    Resume();
                }
                SetSpeed(speedPresets[i]);
            }
        }
    }

    // ---------------- Public API ----------------

    /// <summary>
    /// Set simulation speed multiplier
    /// </summary>
    public void SetSpeed(float multiplier)
    {
        multiplier = Mathf.Max(0.1f, multiplier); // Minimum 0.1x
        multiplier = Mathf.Min(500f, multiplier); // Maximum 500x

        currentSpeed = multiplier;

        // If currently paused, update previousTimeScale so resume works correctly
        if (isPaused)
        {
            previousTimeScale = multiplier;
            Debug.Log($"[TimeController] Speed changed to {multiplier}x while paused (will apply on resume)");
        }
        else
        {
            Time.timeScale = multiplier;
            previousTimeScale = multiplier;
            Debug.Log($"[TimeController] Simulation speed set to {multiplier}x");
        }
    }

    /// <summary>
    /// Pause the simulation
    /// </summary>
    public void Pause()
    {
        if (!isPaused)
        {
            isPaused = true;
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            Debug.Log("[TimeController] Simulation PAUSED");
        }
    }

    /// <summary>
    /// Resume the simulation at previous speed
    /// </summary>
    public void Resume()
    {
        if (isPaused)
        {
            isPaused = false;
            Time.timeScale = previousTimeScale;
            Debug.Log($"[TimeController] Simulation RESUMED at {previousTimeScale}x");
        }
    }

    /// <summary>
    /// Toggle pause state
    /// </summary>
    public void TogglePause()
    {
        if (isPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    /// <summary>
    /// Reset simulation time to zero
    /// </summary>
    public void ResetSimulationTime()
    {
        simulationTime = 0.0;
        Debug.Log("[TimeController] Simulation time reset to 00:00:00");
    }

    /// <summary>
    /// Get current simulation time formatted as HH:MM:SS
    /// </summary>
    public string GetFormattedTime()
    {
        int totalSeconds = (int)simulationTime;
        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int seconds = totalSeconds % 60;

        return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
    }

    /// <summary>
    /// Get current speed description for UI
    /// </summary>
    public string GetSpeedDescription()
    {
        if (isPaused)
        {
            return "PAUSED";
        }

        if (currentSpeed == 1f)
        {
            return "Real-Time";
        }

        return $"{currentSpeed:F0}x";
    }

    // ---------------- Unity Callbacks ----------------

    void OnDestroy()
    {
        // Reset time scale when controller is destroyed
        Time.timeScale = 1f;
    }

    void OnApplicationQuit()
    {
        // Ensure time scale is reset when application quits
        Time.timeScale = 1f;
    }

    // ---------------- Debug Display ----------------

    void OnGUI()
    {
        // Only show keyboard shortcuts hint if keyboard controls are enabled
        if (!enableKeyboardControls)
            return;

        // Show keyboard shortcuts hint in bottom-right corner
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 12;
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.LowerRight;

        string hints = "Time Controls:\n" +
                      "1-5: Speed Presets\n" +
                      "Space: Pause/Resume";

        GUI.Label(new Rect(Screen.width - 150, Screen.height - 70, 140, 60), hints, style);
    }
}
