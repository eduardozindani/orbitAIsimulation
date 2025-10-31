using UnityEngine;

/// <summary>
/// ScriptableObject configuration for mission-specific settings.
/// Centralizes all mission data (specialist info, orbit params, voice settings) in one editable asset.
/// Enables rapid mission creation without scene configuration.
/// </summary>
[CreateAssetMenu(fileName = "NewMission", menuName = "Orbital Missions/Mission Config", order = 1)]
public class MissionConfig : ScriptableObject
{
    [Header("Mission Identity")]
    [Tooltip("Mission identifier (e.g., ISS, GPS, Voyager, Hubble)")]
    public string missionId = "ISS";

    [Tooltip("Full mission name displayed to user")]
    public string missionName = "International Space Station";

    [Tooltip("Mission category for grouping (LEO, MEO, GEO, Interplanetary)")]
    public MissionCategory category = MissionCategory.LEO;

    [Header("Specialist Configuration")]
    [Tooltip("Specialist's name or role (e.g., ISS Flight Engineer, GPS Operations Manager)")]
    public string specialistName = "ISS Flight Engineer";

    [Tooltip("Specialist personality traits and tone (brief description)")]
    [TextArea(2, 4)]
    public string specialistPersonality = "Professional engineer - clear, technical, friendly";

    [Tooltip("Mission-specific knowledge domain for specialist responses")]
    [TextArea(4, 10)]
    public string knowledgeDomain = @"ISS orbits at 420 km altitude with 51.6° inclination.
Inclination constrained by Baikonur launch site.
Period: 92.8 minutes (15.5 orbits per day).
Purpose: Crewed operations, microgravity research, Earth observation.";

    [Tooltip("Voice settings for specialist text-to-speech")]
    public ElevenLabsSettings specialistVoice;

    [Header("Orbit Configuration")]
    [Tooltip("Orbit type for this mission")]
    public OrbitType orbitType = OrbitType.Circular;

    [Tooltip("Altitude in kilometers (for circular orbits)")]
    public float altitudeKm = 420f;

    [Tooltip("Periapsis altitude in km (for elliptical orbits)")]
    public float periapsisKm = 200f;

    [Tooltip("Apoapsis altitude in km (for elliptical orbits)")]
    public float apoapsisKm = 35786f;

    [Tooltip("Orbital inclination in degrees")]
    public float inclinationDeg = 51.6f;

    [Header("Visual Settings")]
    [Tooltip("Mission logo sprite for transitions and UI")]
    public Sprite missionLogo;

    [Tooltip("Primary color theme for mission UI")]
    public Color missionColor = Color.cyan;

    [Header("Educational Content")]
    [Tooltip("Brief description of mission purpose (1-2 sentences)")]
    [TextArea(2, 4)]
    public string missionDescription = "The ISS is a habitable space station in low Earth orbit, serving as a research laboratory for microgravity experiments and international cooperation in space.";

    [Tooltip("Related missions user might want to explore")]
    public string[] relatedMissions = new string[] { };

    /// <summary>
    /// Get formatted specialist context for introduction
    /// </summary>
    public string GetSpecialistContext()
    {
        return $"MISSION: {missionName}\n\nKNOWLEDGE:\n{knowledgeDomain}";
    }

    /// <summary>
    /// Get orbit parameters as a formatted string
    /// </summary>
    public string GetOrbitParametersString()
    {
        if (orbitType == OrbitType.Circular)
        {
            return $"Circular orbit: {altitudeKm} km altitude, {inclinationDeg}° inclination";
        }
        else
        {
            return $"Elliptical orbit: {periapsisKm} km × {apoapsisKm} km, {inclinationDeg}° inclination";
        }
    }
}

/// <summary>
/// Mission categories for grouping and filtering
/// </summary>
public enum MissionCategory
{
    LEO,            // Low Earth Orbit (160-2000 km)
    MEO,            // Medium Earth Orbit (2000-35786 km)
    GEO,            // Geostationary Orbit (35786 km)
    HEO,            // Highly Elliptical Orbit
    Interplanetary  // Beyond Earth orbit
}

/// <summary>
/// Orbit type configuration
/// </summary>
public enum OrbitType
{
    Circular,
    Elliptical
}
