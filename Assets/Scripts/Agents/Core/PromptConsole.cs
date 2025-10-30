using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using Prompts;
using Agents.Tools;
using Agents.Core;
using Agents.Services;
using Newtonsoft.Json.Linq;

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

    [Header("Time Control Integration")]
    [Tooltip("Controller for simulation time acceleration (optional - will be found automatically if not set).")]
    public TimeController TimeController;

    [Header("Tool System")]
    [Tooltip("If true, uses the new tool-based system (ToolRegistry + ToolExecutor). If false, uses legacy altitude/speed system.")]
    public bool UseToolSystem = true;

    [Header("Audio Response")]
    [Tooltip("ElevenLabs settings for text-to-speech audio responses")]
    public ElevenLabsSettings elevenLabsSettings;

    private OpenAIClient _client;
    private ToolRegistry _toolRegistry;
    private ToolExecutor _toolExecutor;
    private ConversationHistory _conversationHistory;
    private ElevenLabsClient _elevenLabsClient;
    private AudioSource _responseAudioSource;
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

        // Initialize tool system if enabled
        if (UseToolSystem && OrbitController != null)
        {
            try
            {
                // Find TimeController if not manually assigned
                if (TimeController == null)
                {
                    TimeController = FindFirstObjectByType<TimeController>();
                    if (TimeController == null)
                    {
                        Debug.LogWarning("[PromptConsole] TimeController not found - time control tools will not be available");
                    }
                    else
                    {
                        Debug.Log("[PromptConsole] TimeController found automatically");
                    }
                }

                _toolRegistry = new ToolRegistry();
                if (_toolRegistry.LoadSchemas())
                {
                    _toolExecutor = new ToolExecutor(_toolRegistry, OrbitController, TimeController);
                    Debug.Log("[PromptConsole] Tool system initialized successfully" +
                             (TimeController != null ? " (with time control)" : " (without time control)"));
                }
                else
                {
                    Debug.LogError("[PromptConsole] Failed to load tool schemas");
                    UseToolSystem = false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[PromptConsole] Failed to initialize tool system: " + e.Message);
                UseToolSystem = false;
            }
        }

        // Initialize conversation history
        _conversationHistory = new ConversationHistory
        {
            maxHistorySize = 15,
            currentLocation = "Hub" // Starting in Mission Control Hub
        };
        Debug.Log("[PromptConsole] Conversation history initialized");

        // Initialize ElevenLabs client
        if (elevenLabsSettings != null)
        {
            _elevenLabsClient = new ElevenLabsClient(elevenLabsSettings);
            Debug.Log("[PromptConsole] ElevenLabs client initialized");
        }
        else
        {
            Debug.LogWarning("[PromptConsole] ElevenLabsSettings not assigned - responses will be text only");
        }

        // Create audio source for responses
        _responseAudioSource = gameObject.AddComponent<AudioSource>();
        _responseAudioSource.playOnAwake = false;
        _responseAudioSource.loop = false;
        _responseAudioSource.volume = 1f;

        _cts = new CancellationTokenSource();
    }

    private void OnEnable()
    {
        if (AutoWireTmpOnSubmit && InputField != null)
        {
            InputField.onSubmit.AddListener(OnSubmit);
            // Add click handler to ensure focus is restored when clicking the input field
            InputField.onSelect.AddListener(OnInputFieldSelected);
        }
    }

    private void Start()
    {
        // Give user immediate focus so Enter works right away.
        ActivateInputSafely();
    }

    private void OnDisable()
    {
        if (InputField != null)
        {
            InputField.onSubmit.RemoveListener(OnSubmit);
            InputField.onSelect.RemoveListener(OnInputFieldSelected);
        }
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

        // Auto-refocus when user starts typing (any key) but field isn't focused
        if (!InputField.isFocused && !_busy)
        {
            if (Input.anyKeyDown && !Input.GetMouseButtonDown(0) && !Input.GetMouseButtonDown(1))
            {
                // User is trying to type but field isn't focused - refocus it
                ActivateInputSafely();
            }
        }

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

    /// <summary>
    /// Called when the input field is selected/clicked.
    /// Ensures the field properly gains focus for keyboard input.
    /// </summary>
    private void OnInputFieldSelected(string text)
    {
        // Force activation to ensure keyboard input works
        if (InputField != null && !InputField.isFocused)
        {
            InputField.ActivateInputField();
        }
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

            if (UseToolSystem && _toolExecutor != null)
            {
                // NEW TOOL-BASED WORKFLOW
                await SubmitWithToolSystemAsync(prompt, linked.Token);
            }
            else
            {
                // LEGACY WORKFLOW (altitude/speed system)
                await SubmitWithLegacySystemAsync(prompt, linked.Token);
            }
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

    // ---------------- Tool System Workflow ----------------

    /// <summary>
    /// Parse AI response into a ToolCall object
    /// </summary>
    private ToolCall ParseToolCall(string jsonResponse)
    {
        if (string.IsNullOrWhiteSpace(jsonResponse))
            return null;

        try
        {
            var root = JObject.Parse(jsonResponse);

            var toolCall = new ToolCall
            {
                intent = (string)root["intent"],
                tool = (string)root["tool"]
            };

            // Parse parameters dictionary
            var parametersObj = root["parameters"] as JObject;
            if (parametersObj != null)
            {
                foreach (var prop in parametersObj.Properties())
                {
                    toolCall.parameters[prop.Name] = prop.Value.ToObject<object>();
                }
            }

            return toolCall;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PromptConsole] Failed to parse tool call JSON: {ex.Message}");
            return null;
        }
    }

    private async Task SubmitWithToolSystemAsync(string prompt, CancellationToken ct)
    {
        // Stop any currently playing audio
        if (_responseAudioSource != null && _responseAudioSource.isPlaying)
        {
            _responseAudioSource.Stop();
        }

        // Show loading message
        SafeSetOutput("Mission Control is responding...");

        // >>> STAGE 1: Extract tool call using ToolSelectionPrompt (with context)
        string contextualPrompt = BuildContextualPrompt(prompt);
        string toolSelectionResponse = await _client.CompleteAsync(contextualPrompt, ToolSelectionPrompt.Text, ct);

        // >>> STAGE 2: Parse tool call JSON
        ToolCall toolCall = ParseToolCall(toolSelectionResponse);

        if (toolCall == null || toolCall.intent != "execute_tool")
        {
            // Generate conversational response for non-commands (greetings, questions, etc.)
            string response = await GenerateNonToolResponseAsync(prompt, ct);

            // Convert to audio and play
            await PlayResponseAsAudioAsync(response, ct);

            // Add to history (no tool executed)
            _conversationHistory.AddExchange(prompt, response, null);
            return;
        }

        // >>> STAGE 3: Execute tool via ToolExecutor
        ToolExecutionResult executionResult;
        bool success = _toolExecutor.ExecuteTool(toolCall.tool, toolCall.parameters, out executionResult);

        if (!success)
        {
            string errorMsg = executionResult.errorMessage ?? "Unknown error";
            string failureResponse = $"I couldn't execute that command: {errorMsg}";

            // Convert to audio and play
            await PlayResponseAsAudioAsync(failureResponse, ct);

            // Add to history (tool execution failed)
            _conversationHistory.AddExchange(prompt, failureResponse, $"{toolCall.tool} (failed)");
            return;
        }

        // >>> STAGE 4: Generate conversational response
        string conversationalResponse = await GenerateToolResponseAsync(prompt, executionResult, ct);

        // Convert to audio and play
        await PlayResponseAsAudioAsync(conversationalResponse, ct);

        // Add to history (successful tool execution)
        _conversationHistory.AddExchange(prompt, conversationalResponse, toolCall.tool);

        // >>> STAGE 5: If tool requires scene transition, trigger it AFTER audio finishes
        if (executionResult.requiresSceneTransition && !string.IsNullOrEmpty(executionResult.targetMission))
        {
            Debug.Log($"[PromptConsole] Audio finished - now triggering scene transition to {executionResult.targetMission}");

            if (executionResult.targetMission == "Hub")
            {
                SceneTransitionManager.Instance?.TransitionToHub();
            }
            else
            {
                SceneTransitionManager.Instance?.TransitionToMission(executionResult.targetMission);
            }
        }
    }

    private async Task SubmitWithLegacySystemAsync(string prompt, CancellationToken ct)
    {
        // >>> STAGE 1: Extract parameters using system prompt
        string systemInstructions = UseSystemPrompt ? SystemPrompt.Text : null;
        string extractionResponse = await _client.CompleteAsync(prompt, systemInstructions, ct);

        // >>> STAGE 2: Process orbit update and get status
        OrbitController.OrbitUpdateResult updateResult = null;
        if (OrbitController != null)
        {
            updateResult = OrbitController.ProcessAICommand(extractionResponse);
        }

        // >>> STAGE 3: Generate conversational response
        string conversationalResponse = await GenerateConversationalResponse(prompt, updateResult, ct);
        SafeSetOutput(conversationalResponse);
    }

    // ---------------- Context Building ----------------

    /// <summary>
    /// Builds contextual prompt with conversation history
    /// </summary>
    private string BuildContextualPrompt(string userMessage)
    {
        if (_conversationHistory.GetExchangeCount() == 0)
        {
            // First message, no context needed
            return userMessage;
        }

        // Include brief context from recent conversation
        string context = _conversationHistory.GetContextSummary(3);
        return $"{context}\nCurrent user message: {userMessage}";
    }

    // ---------------- Response Generation ----------------

    /// <summary>
    /// Generates a conversational response when no tool was executed (greetings, questions, vague requests)
    /// </summary>
    private async Task<string> GenerateNonToolResponseAsync(string userCommand, CancellationToken ct)
    {
        // Check if user has been asking vague questions - might benefit from Mission Space visit
        bool suggestMissionSpace = _conversationHistory.HasRecentVagueQuestions(3);
        string missionSpaceHint = suggestMissionSpace
            ? "\n- If they seem unsure about parameters, suggest they could visit Mission Specialists who can show real examples (Phase 2 feature - mention coming soon)"
            : "";

        // Include conversation history for context
        string conversationContext = _conversationHistory.GetExchangeCount() > 0
            ? $"\n\nRecent conversation:\n{_conversationHistory.GetFormattedHistory(5)}\n"
            : "";

        string contextPrompt = $@"You are Mission Control CAPCOM. {conversationContext}

The user just said: ""{userCommand}""

This was NOT a command to create an orbit. Respond naturally and helpfully:
- If it's a greeting, greet them back as Mission Control CAPCOM
- If it's a question about capabilities, explain you can create circular and elliptical orbits, and clear the workspace
- If it's a vague request without numbers, politely ask for specific altitude/periapsis/apoapsis values
- If they're asking about previous messages, refer to the conversation history above
- If they're checking on the system, confirm everything is operational{missionSpaceHint}

Be conversational, professional, and helpful. Keep responses under 3 sentences.";

        try
        {
            return await _client.CompleteAsync(contextPrompt, ResponsePrompt.Text, ct);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[PromptConsole] Failed to generate non-tool response: {ex.Message}");
            return "I'm ready to help you design orbits. Please specify the orbit parameters you'd like to create.";
        }
    }

    /// <summary>
    /// Generates a conversational response for tool-based execution
    /// </summary>
    private async Task<string> GenerateToolResponseAsync(string userCommand, ToolExecutionResult executionResult, CancellationToken ct)
    {
        if (executionResult == null || !executionResult.success)
        {
            return "I'm sorry, there was an issue executing that command.";
        }

        // Include conversation history for context
        string conversationContext = _conversationHistory.GetExchangeCount() > 0
            ? $"\n\nRecent conversation:\n{_conversationHistory.GetFormattedHistory(5)}\n"
            : "";

        // Build context for response generation
        string contextPrompt = $@"You are Mission Control CAPCOM. {conversationContext}

user_command: {userCommand}
tool_executed: {executionResult.toolId}
success: {executionResult.success}
result_message: {executionResult.message}
output_data: {string.Join(", ", executionResult.outputData)}

Generate a conversational response acknowledging what was done:";

        try
        {
            return await _client.CompleteAsync(contextPrompt, ResponsePrompt.Text, ct);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[PromptConsole] Failed to generate tool response: {ex.Message}");
            return executionResult.message ?? "Orbit parameters updated successfully.";
        }
    }

    /// <summary>
    /// Generates a conversational response based on what the orbit system actually did (legacy)
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
            Debug.LogWarning($"[PromptConsole] Failed to generate conversational response: {ex.Message}");
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

    // ---------------- Audio Response ----------------

    /// <summary>
    /// Convert text response to audio and play it.
    /// If audio generation fails, show error message.
    /// </summary>
    private async Task PlayResponseAsAudioAsync(string responseText, CancellationToken ct)
    {
        if (_elevenLabsClient == null || _responseAudioSource == null)
        {
            // Fallback: No audio system available, show text
            SafeSetOutput(responseText);
            return;
        }

        try
        {
            // Generate audio from text
            AudioClip audioClip = await _elevenLabsClient.TextToSpeechAsync(responseText, ct);

            if (audioClip != null)
            {
                // Play audio, clear text output (audio only!)
                _responseAudioSource.clip = audioClip;
                _responseAudioSource.Play();
                SafeSetOutput(""); // Clear text - audio only

                // WAIT for audio to finish playing
                float audioDuration = audioClip.length;
                Debug.Log($"[PromptConsole] Playing audio ({audioDuration:F1}s) - waiting for completion...");

                // Wait for the full duration of the audio clip
                float elapsed = 0f;
                while (elapsed < audioDuration && _responseAudioSource.isPlaying)
                {
                    await Task.Delay(100, ct); // Check every 100ms
                    elapsed += 0.1f;
                }

                Debug.Log($"[PromptConsole] Audio playback complete");
            }
            else
            {
                // Audio generation failed
                SafeSetOutput("Error: Could not generate audio");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PromptConsole] Audio generation failed: {ex.Message}");
            SafeSetOutput("Error: Could not generate audio");
        }
    }

    // ---------------- Experience Manager Integration ----------------

    /// <summary>
    /// Disable the console UI (called by ExperienceManager during intro)
    /// </summary>
    public void DisableConsole()
    {
        if (InputField != null)
        {
            InputField.gameObject.SetActive(false);
        }

        if (OutputText != null)
        {
            OutputText.gameObject.SetActive(false);
        }

        Debug.Log("[PromptConsole] Console disabled for intro");
    }

    /// <summary>
    /// Enable the console UI (called by ExperienceManager when transitioning to Hub)
    /// </summary>
    public void EnableConsole()
    {
        if (InputField != null)
        {
            InputField.gameObject.SetActive(true);
        }

        if (OutputText != null)
        {
            OutputText.gameObject.SetActive(true);
            OutputText.text = "Mission Control: Ready for your commands.";
        }

        // Give input field focus
        ActivateInputSafely();

        Debug.Log("[PromptConsole] Console enabled - Hub is active");
    }

    // ---------------- Helper Classes ----------------

    /// <summary>
    /// Represents a parsed tool call from the AI
    /// </summary>
    private class ToolCall
    {
        public string intent;
        public string tool;
        public Dictionary<string, object> parameters = new Dictionary<string, object>();
    }
}
