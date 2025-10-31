using System.Collections;
using UnityEngine;

/// <summary>
/// Controls a Mission Space scene.
/// Sets up pre-built orbit configuration and triggers specialist introduction.
/// Each Mission Space (ISS, GPS, Voyager, Hubble) has one of these components.
/// NEW: Now uses MissionConfig ScriptableObject for centralized configuration.
/// </summary>
public class MissionSpaceController : MonoBehaviour
{
    [Header("Mission Configuration")]
    [Tooltip("Mission configuration ScriptableObject (centralized settings)")]
    public MissionConfig missionConfig;

    [Header("Legacy Fallback (will be removed)")]
    [Tooltip("DEPRECATED: Mission name (use MissionConfig instead)")]
    public string missionName = "ISS";

    [Tooltip("DEPRECATED: Specialist name")]
    public string specialistName = "ISS Flight Engineer";

    [Tooltip("DEPRECATED: Orbit type")]
    public bool isCircular = true;

    [Tooltip("DEPRECATED: Altitude (circular orbits)")]
    public float altitude_km = 420f;

    [Tooltip("DEPRECATED: Inclination")]
    public float inclination_deg = 51.6f;

    [Tooltip("DEPRECATED: Periapsis (elliptical)")]
    public float periapsis_km = 500f;

    [Tooltip("DEPRECATED: Apoapsis (elliptical)")]
    public float apoapsis_km = 40000f;

    [Tooltip("DEPRECATED: Specialist voice")]
    public ElevenLabsSettings specialistVoiceSettings;

    [Tooltip("DEPRECATED: Specialist personality")]
    [TextArea(2, 4)]
    public string specialistPersonality = "Professional engineer - clear, technical, friendly";

    [Tooltip("DEPRECATED: Mission knowledge")]
    [TextArea(4, 8)]
    public string knowledgeDomain = @"ISS orbits at 420 km altitude with 51.6° inclination.
Inclination constrained by Baikonur launch site.
Period: 92.8 minutes (15.5 orbits per day).
Purpose: Crewed operations, microgravity research, Earth observation.";

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
        // Try to load mission config from MissionRegistry if not assigned
        if (missionConfig == null && !string.IsNullOrEmpty(missionName))
        {
            if (MissionRegistry.Instance != null)
            {
                missionConfig = MissionRegistry.Instance.GetMission(missionName);
                if (missionConfig != null)
                {
                    Debug.Log($"[MissionSpaceController] Loaded config from MissionRegistry: {missionName}");
                }
            }
        }

        // Determine if using MissionConfig or legacy fields
        bool usingLegacy = (missionConfig == null);
        string activeMissionName = usingLegacy ? missionName : missionConfig.missionName;

        if (usingLegacy)
        {
            Debug.LogWarning($"[MissionSpaceController] Using LEGACY configuration for {missionName} - consider creating MissionConfig asset");
        }
        else
        {
            Debug.Log($"[MissionSpaceController] Using MissionConfig for {missionConfig.missionName}");
        }

        Debug.Log($"[MissionSpaceController] Initializing {activeMissionName} Mission Space");

        // 1. Create pre-built orbit immediately
        CreateMissionOrbit();

        // 2. Set specialist voice and context for ongoing conversation
        if (promptConsole != null)
        {
            ElevenLabsSettings voiceToUse = usingLegacy ? specialistVoiceSettings : missionConfig.specialistVoice;
            if (voiceToUse != null)
            {
                promptConsole.SetActiveVoice(voiceToUse);
            }

            string knowledge = usingLegacy ? knowledgeDomain : missionConfig.knowledgeDomain;
            promptConsole.SetSpecialistContext(activeMissionName, knowledge);

            string specialist = usingLegacy ? specialistName : missionConfig.specialistName;
            Debug.Log($"[MissionSpaceController] Specialist configured for {specialist}");
        }
        else
        {
            Debug.LogWarning("[MissionSpaceController] Could not configure specialist - PromptConsole not assigned");
        }

        // 3. Wait briefly, then trigger specialist introduction
        StartCoroutine(TriggerSpecialistIntroduction());
    }

    /// <summary>
    /// Creates the pre-configured orbit for this mission
    /// </summary>
    private void CreateMissionOrbit()
    {
        if (orbitController == null)
        {
            Debug.LogError($"[MissionSpaceController] OrbitController not assigned!");
            return;
        }

        bool usingLegacy = (missionConfig == null);

        if (usingLegacy)
        {
            // Use legacy fields
            if (isCircular)
            {
                Debug.Log($"[MissionSpaceController] Creating circular orbit (legacy): {altitude_km}km, {inclination_deg}°");
                orbitController.CreateCircularOrbit(altitude_km, inclination_deg);
            }
            else
            {
                Debug.Log($"[MissionSpaceController] Creating elliptical orbit (legacy): {periapsis_km}km - {apoapsis_km}km, {inclination_deg}°");
                orbitController.CreateEllipticalOrbit(periapsis_km, apoapsis_km, inclination_deg);
            }
        }
        else
        {
            // Use MissionConfig
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
    }

    /// <summary>
    /// Triggers specialist introduction with context from routing
    /// </summary>
    private IEnumerator TriggerSpecialistIntroduction()
    {
        // Wait for scene to settle visually
        yield return new WaitForSeconds(introDelay);

        if (promptConsole == null)
        {
            Debug.LogError($"[MissionSpaceController] PromptConsole not assigned!");
            yield break;
        }

        bool usingLegacy = (missionConfig == null);
        ElevenLabsSettings voiceToUse = usingLegacy ? specialistVoiceSettings : missionConfig.specialistVoice;

        if (voiceToUse == null)
        {
            Debug.LogError($"[MissionSpaceController] Specialist voice settings not assigned!");
            yield break;
        }

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

        string activeMissionName = usingLegacy ? missionName : missionConfig.missionName;
        Debug.Log($"[MissionSpaceController] Triggering specialist introduction for {activeMissionName}");

        // Generate and play introduction via PromptConsole
        // Uses specialist voice settings and context-aware prompt
        var introTask = promptConsole.GenerateSpecialistIntroductionAsync(
            introPrompt,
            voiceToUse,
            System.Threading.CancellationToken.None
        );

        // Wait for introduction to complete
        yield return new WaitUntil(() => introTask.IsCompleted);

        string activeSpecialistName = usingLegacy ? specialistName : missionConfig.specialistName;
        if (introTask.IsFaulted)
        {
            Debug.LogError($"[MissionSpaceController] Introduction failed: {introTask.Exception?.Message}");
        }
        else
        {
            Debug.Log($"[MissionSpaceController] {activeSpecialistName} introduction complete");
        }
    }

    /// <summary>
    /// Builds the prompt for specialist introduction
    /// Includes context awareness and mission-specific knowledge
    /// </summary>
    private string BuildSpecialistIntroPrompt(string routingContext)
    {
        bool usingLegacy = (missionConfig == null);

        // Get mission details
        string activeMissionName = usingLegacy ? missionName : missionConfig.missionName;
        string activeSpecialistName = usingLegacy ? specialistName : missionConfig.specialistName;
        string activePersonality = usingLegacy ? specialistPersonality : missionConfig.specialistPersonality;
        string activeKnowledge = usingLegacy ? knowledgeDomain : missionConfig.knowledgeDomain;

        // Get related missions for cross-mission awareness (only if using MissionConfig)
        string relatedMissionsHint = "";
        if (!usingLegacy && MissionRegistry.Instance != null)
        {
            var related = MissionRegistry.Instance.GetRelatedMissions(missionConfig.missionId);
            if (related.Count > 0)
            {
                relatedMissionsHint = $"\n\nRelated missions: {string.Join(", ", related.ConvertAll(m => m.missionId))}";
            }
        }

        return $@"IDENTITY
You are the {activeSpecialistName} for the {activeMissionName} mission.
Personality: {activePersonality}

MISSION KNOWLEDGE
{activeKnowledge}{relatedMissionsHint}

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
