using System.Collections;
using UnityEngine;

/// <summary>
/// Controls a Mission Space scene.
/// Sets up pre-built orbit configuration and triggers specialist introduction.
/// Each Mission Space (ISS, GPS, Voyager, Hubble) has one of these components.
/// REQUIRES: MissionConfig ScriptableObject must be assigned in Inspector.
/// </summary>
public class MissionSpaceController : MonoBehaviour
{
    [Header("Mission Configuration")]
    [Tooltip("REQUIRED: Mission configuration ScriptableObject (all mission settings)")]
    public MissionConfig missionConfig;

    [Header("References")]
    [Tooltip("OrbitController to create the pre-built orbit")]
    public OrbitController orbitController;

    [Tooltip("PromptConsole for specialist conversation")]
    public PromptConsole promptConsole;

    [Header("Timing")]
    [Tooltip("Delay before specialist speaks (seconds) - should match fade-in duration + 0.5s buffer")]
    public float introDelay = 2.5f;

    void Start()
    {
        // Validate MissionConfig is assigned
        if (missionConfig == null)
        {
            Debug.LogError("[MissionSpaceController] CRITICAL ERROR: MissionConfig is not assigned! " +
                          "Create MissionConfig asset via: Assets > Create > Orbital Missions > Mission Config, " +
                          "then assign it to this component in Inspector.");
            return;
        }

        Debug.Log($"[MissionSpaceController] Initializing {missionConfig.missionName} Mission Space");

        // Validate required references
        if (orbitController == null)
        {
            Debug.LogError($"[MissionSpaceController] OrbitController not assigned for {missionConfig.missionName}!");
            return;
        }

        if (promptConsole == null)
        {
            Debug.LogError($"[MissionSpaceController] PromptConsole not assigned for {missionConfig.missionName}!");
            return;
        }

        if (missionConfig.specialistVoice == null)
        {
            Debug.LogError($"[MissionSpaceController] Specialist voice not assigned in {missionConfig.name}!");
            return;
        }

        // 1. Create pre-built orbit immediately
        CreateMissionOrbit();

        // 2. Set specialist voice and context for ongoing conversation
        promptConsole.SetActiveVoice(missionConfig.specialistVoice);
        promptConsole.SetSpecialistContext(missionConfig.missionName, missionConfig.knowledgeDomain);
        Debug.Log($"[MissionSpaceController] Specialist configured for {missionConfig.specialistName}");

        // 3. Wait briefly, then trigger specialist introduction
        StartCoroutine(TriggerSpecialistIntroduction());
    }

    /// <summary>
    /// Creates the pre-configured orbit for this mission using MissionConfig data
    /// </summary>
    private void CreateMissionOrbit()
    {
        if (missionConfig.orbitType == OrbitType.Circular)
        {
            Debug.Log($"[MissionSpaceController] Creating circular orbit: {missionConfig.altitudeKm}km, {missionConfig.inclinationDeg}°");
            orbitController.CreateCircularOrbit(missionConfig.altitudeKm, missionConfig.inclinationDeg);
        }
        else
        {
            Debug.Log($"[MissionSpaceController] Creating elliptical orbit: {missionConfig.periapsisKm}km - {missionConfig.apoapsisKm}km, {missionConfig.inclinationDeg}°");
            orbitController.CreateEllipticalOrbit(missionConfig.periapsisKm, missionConfig.apoapsisKm, missionConfig.inclinationDeg);
        }
    }

    /// <summary>
    /// Triggers specialist introduction with context from routing
    /// </summary>
    private IEnumerator TriggerSpecialistIntroduction()
    {
        // Wait for scene to settle visually
        yield return new WaitForSeconds(introDelay);

        // Get routing context from MissionContext
        string routingContext = "";
        if (MissionContext.Instance != null)
        {
            routingContext = MissionContext.Instance.GetContextForSpecialist();
            Debug.Log($"[MissionSpaceController] Routing context: {routingContext}");
        }
        else
        {
            Debug.LogWarning("[MissionSpaceController] MissionContext.Instance is null - no routing context available");
            routingContext = "User arrived at mission space to learn more";
        }

        // Build specialist introduction prompt
        string introPrompt = BuildSpecialistIntroPrompt(routingContext);

        Debug.Log($"[MissionSpaceController] Triggering specialist introduction for {missionConfig.missionName}");

        // Generate and play introduction via PromptConsole
        var introTask = promptConsole.GenerateSpecialistIntroductionAsync(
            introPrompt,
            missionConfig.specialistVoice,
            System.Threading.CancellationToken.None
        );

        // Wait for introduction to complete
        yield return new WaitUntil(() => introTask.IsCompleted);

        if (introTask.IsFaulted)
        {
            Debug.LogError($"[MissionSpaceController] Introduction failed: {introTask.Exception?.Message}");
        }
        else
        {
            Debug.Log($"[MissionSpaceController] {missionConfig.specialistName} introduction complete");
        }
    }

    /// <summary>
    /// Builds the prompt for specialist introduction using MissionConfig data
    /// Includes context awareness and cross-mission recommendations
    /// </summary>
    private string BuildSpecialistIntroPrompt(string routingContext)
    {
        // Get related missions for cross-mission awareness
        string relatedMissionsHint = "";
        if (MissionRegistry.Instance != null)
        {
            var related = MissionRegistry.Instance.GetRelatedMissions(missionConfig.missionId);
            if (related.Count > 0)
            {
                relatedMissionsHint = $"\n\nRelated missions: {string.Join(", ", related.ConvertAll(m => m.missionId))}";
            }
        }

        return $@"IDENTITY
You are the {missionConfig.specialistName} for the {missionConfig.missionName} mission.
Personality: {missionConfig.specialistPersonality}

MISSION KNOWLEDGE
{missionConfig.knowledgeDomain}{relatedMissionsHint}

ROUTING CONTEXT
{routingContext}

YOUR TASK
Generate a SHORT, warm introduction greeting.

STRICT REQUIREMENTS:
- MAXIMUM 2-3 SHORT SENTENCES
- MAXIMUM 40 WORDS TOTAL
- 10-15 seconds of speech when spoken
- Acknowledge why they came (routing context)
- Invite them to ask questions

Example: Hello! I'm Anastasia, crew member aboard ISS. I heard you want to learn about our orbit. What would you like to know?

Be warm, concise, and enthusiastic. DO NOT explain orbital mechanics in the introduction - keep it brief!";
    }

    /// <summary>
    /// Get specialist system prompt for ongoing conversation
    /// This can be used by PromptConsole when in this mission space
    /// </summary>
    public string GetSpecialistSystemPrompt()
    {
        // Get available missions for cross-mission awareness
        string availableMissions = "";
        if (MissionRegistry.Instance != null)
        {
            availableMissions = $"\n\nAVAILABLE MISSIONS YOU CAN RECOMMEND:\n{MissionRegistry.Instance.GetAvailableMissionsString()}";
        }

        return $@"IDENTITY
You are the {missionConfig.specialistName} specialist for the {missionConfig.missionName} mission.
Personality: {missionConfig.specialistPersonality}

YOUR MISSION KNOWLEDGE
{missionConfig.knowledgeDomain}{availableMissions}

YOUR ROLE
- Answer questions about {missionConfig.missionName} orbit design and mission rationale
- Explain orbital mechanics principles using this mission as example
- Help users understand real-world orbital design tradeoffs
- Recommend related missions when relevant
- Be encouraging and educational

When user wants to leave, acknowledge gracefully and let them know they can return anytime.

RESPONSE STYLE
- Professional but approachable
- Technical but clear
- Enthusiastic about the mission
- Patient with questions
- Keep responses conversational (2-4 sentences)";
    }
}
