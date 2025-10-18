using System;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using Prompts; // << add this to access Prompts.SystemPrompt.Text and ResponsePrompt.Text

/// <summary>
/// Minimal, robust bridge between a TMP input box and your OpenAI client.
/// - Press Enter to send (Shift+Enter inserts newline).
/// - Works via Update() key handling AND/OR TMP's On Submit (String) UnityEvent.
/// - Shows status/errors in OutputText.
/// </summary>
public class PromptConsole : MonoBehaviour
{
    [Header("References")]
    [Tooltip("User input box (TMP_InputField).")]
    public TMP_InputField InputField;

    [Tooltip("Output label to display the model response or errors.")]
    public TMP_Text OutputText;

    [Tooltip("ScriptableObject holding API key/model/base URL.")]
    public OpenAISettings Settings;

    [Header("Behavior")]
    [Tooltip("If true, pressing Enter while the input is focused will submit.")]
    public bool SubmitOnEnter = true;

    [Tooltip("If true, Shift+Enter inserts a newline instead of submitting.")]
    public bool AllowShiftEnterNewline = true;

    [Tooltip("Automatically hooks TMP_InputField.onSubmit to OnSubmit(string).")]
    public bool AutoWireTmpOnSubmit = true;

    [Tooltip("Clear input after a successful send.")]
    public bool ClearAfterSend = true;

    [Tooltip("Keep keyboard focus on the input field after sending.")]
    public bool KeepFocusAfterSend = true;

    [Header("Prompt")]
    [Tooltip("If true, injects a global system prompt (Prompts.SystemPrompt.Text) into the request.")]
    public bool UseSystemPrompt = true;

    [Header("Orbit Integration")]
    [Tooltip("Controller that will process AI commands and update orbit parameters.")]
    public OrbitController OrbitController;

    private OpenAIClient _client;
    private bool _busy;
    private CancellationTokenSource _cts;

    // ---------------- Lifecycle ----------------

    private void Awake()
    {
        if (Settings == null)
            Debug.LogWarning("[PromptConsole] OpenAISettings not assigned. Assign the OpenAISettings.asset in the Inspector.");

        try
        {
            _client = new OpenAIClient(Settings);
        }
        catch (Exception e)
        {
            Debug.LogError("[PromptConsole] Failed to construct OpenAIClient: " + e.Message);
        }

        _cts = new CancellationTokenSource();
    }

    private void OnEnable()
    {
        if (AutoWireTmpOnSubmit && InputField != null)
            InputField.onSubmit.AddListener(OnSubmit);
    }

    private void Start()
    {
        // Give user immediate focus so Enter works right away.
        ActivateInputSafely();
    }

    private void OnDisable()
    {
        if (InputField != null)
            InputField.onSubmit.RemoveListener(OnSubmit);
    }

    private void OnDestroy()
    {
        try
        {
            _cts.Cancel();
            _cts.Dispose();
        }
        catch { /* no-op */ }
    }

    // ---------------- Input Handling ----------------

    private void Update()
    {
        if (!SubmitOnEnter || InputField == null)
            return;

        if (!InputField.isFocused)
            return;

        // Enter or keypad Enter
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (AllowShiftEnterNewline && shift)
            {
                InsertNewlineAtCaret();
            }
            else
            {
                // Fire-and-forget; guarded inside.
                _ = SubmitAsync();
            }
        }

        // Optional: ESC cancels an in-flight request
        if (Input.GetKeyDown(KeyCode.Escape) && _busy)
        {
            CancelInFlight();
        }
    }

    /// <summary>
    /// UnityEvent target for TMP_InputField's On Submit (String).
    /// This is called by TMP when Enter is pressed (depending on TMP settings).
    /// </summary>
    public void OnSubmit(string text)
    {
        if (!_busy)
            _ = SubmitAsync();
    }

    // ---------------- Core Send Flow ----------------

    public async Task SubmitAsync()
    {
        if (_busy) return;

        if (!ValidateRefs(out string validationError))
        {
            SafeSetOutput($"Error:\n{validationError}");
            return;
        }

        var prompt = InputField.text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(prompt))
            return;

        try
        {
            _busy = true;
            InputField.interactable = false;
            SafeSetOutput("Sending…");

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);

            // >>> STAGE 1: Extract parameters using system prompt
            string systemInstructions = UseSystemPrompt ? SystemPrompt.Text : null;
            string extractionResponse = await _client.CompleteAsync(prompt, systemInstructions, linked.Token);

            // >>> STAGE 2: Process orbit update and get status
            OrbitController.OrbitUpdateResult updateResult = null;
            if (OrbitController != null)
            {
                updateResult = OrbitController.ProcessAICommand(extractionResponse);
            }

            // >>> STAGE 3: Generate conversational response
            string conversationalResponse = await GenerateConversationalResponse(prompt, updateResult, linked.Token);
            
            SafeSetOutput(conversationalResponse);
        }
        catch (OperationCanceledException)
        {
            SafeSetOutput("Canceled.");
        }
        catch (Exception ex)
        {
            // Show concise, readable error
            SafeSetOutput($"Error:\n{ex.Message}");
        }
        finally
        {
            _busy = false;
            InputField.interactable = true;

            if (ClearAfterSend)
                InputField.text = string.Empty;

            if (KeepFocusAfterSend)
                ActivateInputSafely();
        }
    }

    public void CancelInFlight()
    {
        if (!_busy) return;

        try
        {
            _cts.Cancel();
            _cts.Dispose();
        }
        catch { /* no-op */ }

        _cts = new CancellationTokenSource();
    }

    // ---------------- Utilities ----------------

    private bool ValidateRefs(out string error)
    {
        if (InputField == null) { error = "InputField is not assigned."; return false; }
        if (OutputText == null) { error = "OutputText is not assigned."; return false; }
        if (Settings == null) { error = "OpenAISettings is not assigned."; return false; }
        if (_client == null) { error = "OpenAIClient not constructed."; return false; }
        error = null;
        return true;
    }

    private void SafeSetOutput(string message)
    {
        if (OutputText != null)
            OutputText.text = message ?? string.Empty;
        else
            Debug.Log(message);
    }

    private void ActivateInputSafely()
    {
        if (InputField == null) return;

        // Two calls help across platforms to re-focus reliably after UI changes
        InputField.ActivateInputField();
        InputField.Select();
    }

    private void InsertNewlineAtCaret()
    {
        if (InputField == null) return;

        int pos = InputField.stringPosition;
        string text = InputField.text ?? string.Empty;

        if (pos < 0 || pos > text.Length)
            pos = text.Length;

        text = text.Insert(pos, "\n");
        InputField.text = text;

        // Advance caret by 1
        InputField.stringPosition = pos + 1;
        InputField.caretPosition = InputField.stringPosition;
    }

    /// <summary>
    /// Generates a conversational response based on what the orbit system actually did
    /// </summary>
    private async Task<string> GenerateConversationalResponse(string userCommand, OrbitController.OrbitUpdateResult updateResult, CancellationToken ct)
    {
        if (updateResult == null)
        {
            return "I'm sorry, there was an issue processing your command. The orbit control system is not available.";
        }

        // Build context for the response generation
        string contextPrompt = $@"user_command: {userCommand}
parameters_updated: {updateResult.parametersUpdated}
altitude_km: {(updateResult.altitudeKm?.ToString("F1") ?? "null")}
speed_kmps: {(updateResult.speedKmps?.ToString("F2") ?? "null")}
update_reason: {updateResult.updateReason}

Generate a conversational response:";

        try
        {
            string response = await _client.CompleteAsync(contextPrompt, ResponsePrompt.Text, ct);
            return response;
        }
        catch (Exception ex)
        {
            // Fallback to a basic response if AI fails
            if (updateResult.parametersUpdated)
            {
                string altText = updateResult.altitudeKm.HasValue ? $"altitude to {updateResult.altitudeKm:F1} km" : "";
                string speedText = updateResult.speedKmps.HasValue ? $"speed to {updateResult.speedKmps:F2} km/s" : "";
                string separator = updateResult.altitudeKm.HasValue && updateResult.speedKmps.HasValue ? " and " : "";
                
                return $"Roger that! I've updated the satellite {altText}{separator}{speedText}.";
            }
            else
            {
                return $"I couldn't process that command. {updateResult.updateReason}. Please specify altitude (in km) or speed (in km/s) with numeric values.";
            }
        }
    }
}
