using System.Collections;
using UnityEngine;

/// <summary>
/// Controls a Mission Space scene.
/// Sets up pre-built orbit configuration and triggers specialist introduction.
/// Each Mission Space (ISS, GPS, Voyager, Hubble) has one of these components.
/// </summary>
public class MissionSpaceController : MonoBehaviour
{
    [Header("Mission Identity")]
    [Tooltip("Mission name: ISS, GPS, Voyager, or Hubble")]
    public string missionName = "ISS";

    [Tooltip("Display name of the specialist character")]
    public string specialistName = "ISS Flight Engineer";

    [Header("Pre-Built Orbit Configuration")]
    [Tooltip("Is this orbit circular or elliptical?")]
    public bool isCircular = true;

    [Tooltip("Altitude in km (for circular orbits)")]
    public float altitude_km = 420f;

    [Tooltip("Orbital inclination in degrees (0=equatorial, 90=polar)")]
    public float inclination_deg = 51.6f;

    [Tooltip("Periapsis in km (for elliptical orbits)")]
    public float periapsis_km = 500f;

    [Tooltip("Apoapsis in km (for elliptical orbits)")]
    public float apoapsis_km = 40000f;

    [Header("Specialist Configuration")]
    [Tooltip("ElevenLabs voice settings for this specialist")]
    public ElevenLabsSettings specialistVoiceSettings;

    [Tooltip("Specialist personality description for prompt")]
    [TextArea(3, 6)]
    public string specialistPersonality = "Professional engineer - clear, technical, friendly";

    [Tooltip("Specialist's domain knowledge for prompt")]
    [TextArea(5, 10)]
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
    [Tooltip("Delay before specialist speaks (seconds)")]
    public float introDelay = 0.5f;

    void Start()
    {
        Debug.Log($"[MissionSpaceController] Initializing {missionName} Mission Space");

        // 1. Create pre-built orbit immediately
        CreateMissionOrbit();

        // 2. Wait briefly, then trigger specialist introduction
        StartCoroutine(TriggerSpecialistIntroduction());
    }

    /// <summary>
    /// Creates the pre-configured orbit for this mission
    /// </summary>
    private void CreateMissionOrbit()
    {
        if (orbitController == null)
        {
            Debug.LogError($"[MissionSpaceController] OrbitController not assigned for {missionName}!");
            return;
        }

        if (isCircular)
        {
            Debug.Log($"[MissionSpaceController] Creating circular orbit: {altitude_km}km, {inclination_deg}°");
            orbitController.CreateCircularOrbit(altitude_km, inclination_deg);
        }
        else
        {
            Debug.Log($"[MissionSpaceController] Creating elliptical orbit: {periapsis_km}km - {apoapsis_km}km, {inclination_deg}°");
            orbitController.CreateEllipticalOrbit(periapsis_km, apoapsis_km, inclination_deg);
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
            Debug.LogError($"[MissionSpaceController] PromptConsole not assigned for {missionName}!");
            yield break;
        }

        if (specialistVoiceSettings == null)
        {
            Debug.LogError($"[MissionSpaceController] Specialist voice settings not assigned for {missionName}!");
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

        Debug.Log($"[MissionSpaceController] Triggering specialist introduction for {missionName}");

        // Generate and play introduction via PromptConsole
        // Uses specialist voice settings and context-aware prompt
        var introTask = promptConsole.GenerateSpecialistIntroductionAsync(
            introPrompt,
            specialistVoiceSettings,
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
            Debug.Log($"[MissionSpaceController] {specialistName} introduction complete");
        }
    }

    /// <summary>
    /// Builds the prompt for specialist introduction
    /// Includes context awareness and mission-specific knowledge
    /// </summary>
    private string BuildSpecialistIntroPrompt(string routingContext)
    {
        return $@"IDENTITY
You are the {specialistName} for the {missionName} mission.
Personality: {specialistPersonality}

MISSION KNOWLEDGE
{knowledgeDomain}

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

Example length: Hello! I'm Anastasia, crew member aboard ISS. I heard you want to learn about our orbit. What would you like to know?

Be warm, concise, and enthusiastic. DO NOT explain orbital mechanics in the introduction - keep it brief!";
    }

    /// <summary>
    /// Get specialist system prompt for ongoing conversation
    /// This can be used by PromptConsole when in this mission space
    /// </summary>
    public string GetSpecialistSystemPrompt()
    {
        return $@"IDENTITY
You are the {specialistName} specialist for the {missionName} mission.
Personality: {specialistPersonality}

YOUR MISSION KNOWLEDGE
{knowledgeDomain}

YOUR ROLE
- Answer questions about {missionName} orbit design and mission rationale
- Explain orbital mechanics principles using this mission as example
- Help users understand real-world orbital design tradeoffs
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
