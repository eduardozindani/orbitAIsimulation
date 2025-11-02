using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// Bridges AI commands to actual orbit mechanics.
/// Converts altitude (km above surface) and speed (km/s) to Unity orbit parameters.
/// </summary>
public class OrbitController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Orbit component to control")]
    public Orbit targetOrbit;

    [Tooltip("The OrbitVisualizer component to display trajectory")]
    public OrbitVisualizer orbitVisualizer;

    [Tooltip("The planet/Earth transform (center of orbit)")]
    public Transform planetTransform;

    [Header("Earth Properties")]
    [Tooltip("Earth radius in kilometers (real Earth ≈ 6371 km)")]
    public float earthRadiusKm = 6371f;
    
    [Tooltip("Earth radius in Unity units (matches your Planet GameObject scale)")]
    public float earthRadiusUnity = 5f;
    
    [Tooltip("Unity units per kilometer (adjust based on your Earth scale)")]
    public float unityUnitsPerKm = 5f / 6371f; // Earth radius = 5 Unity units, real Earth = 6371 km
    
    [Header("Validation")]
    [Tooltip("Minimum safe altitude above Earth surface (km)")]
    public float minAltitudeKm = 160f; // Below this, atmospheric drag becomes significant
    
    [Tooltip("Maximum reasonable altitude for simulation (km)")]
    public float maxAltitudeKm = 35786f; // Geostationary orbit altitude
    
    [Tooltip("Minimum orbital speed (km/s)")]
    public float minSpeedKmps = 1f; // Very slow orbit
    
    [Tooltip("Maximum orbital speed (km/s)")]
    public float maxSpeedKmps = 11f; // Escape velocity is ~11.2 km/s
    
    [Header("Debug")]
    public bool showDebugLogs = true;

    /// <summary>
    /// Processes AI JSON response and updates orbit parameters
    /// Returns status information for generating conversational responses
    /// </summary>
    public OrbitUpdateResult ProcessAICommand(string jsonResponse)
    {
        var result = new OrbitUpdateResult();

        if (string.IsNullOrWhiteSpace(jsonResponse))
        {
            if (showDebugLogs) Debug.LogWarning("[OrbitController] Empty JSON response");
            result.updateReason = "No response received from AI system";
            return result;
        }

        try
        {
            var aiResponse = JsonConvert.DeserializeObject<AIOrbitResponse>(jsonResponse);
            
            if (aiResponse.intent != "update")
            {
                if (showDebugLogs) Debug.Log("[OrbitController] No update intent, ignoring command");
                result.updateReason = "No specific orbital parameters were requested to be changed";
                return result;
            }

            result = UpdateOrbitParameters(aiResponse);
        }
        catch (JsonException ex)
        {
            if (showDebugLogs) Debug.LogError($"[OrbitController] Failed to parse JSON: {ex.Message}");
            result.updateReason = "Could not understand the command format";
        }

        return result;
    }

    private OrbitUpdateResult UpdateOrbitParameters(AIOrbitResponse response)
    {
        var result = new OrbitUpdateResult();

        if (targetOrbit == null)
        {
            Debug.LogError("[OrbitController] No target orbit assigned!");
            result.updateReason = "Orbit system not properly configured";
            return result;
        }

        bool anyUpdated = false;

        // Update distance (altitude -> orbit radius)
        if (response.distance_km.HasValue)
        {
            float altitudeKm = response.distance_km.Value;
            
            // Validate altitude range
            if (altitudeKm < minAltitudeKm)
            {
                if (showDebugLogs)
                    Debug.LogWarning($"[OrbitController] Altitude {altitudeKm}km is below minimum safe altitude {minAltitudeKm}km. Clamping to minimum.");
                altitudeKm = minAltitudeKm;
            }
            else if (altitudeKm > maxAltitudeKm)
            {
                if (showDebugLogs)
                    Debug.LogWarning($"[OrbitController] Altitude {altitudeKm}km exceeds maximum {maxAltitudeKm}km. Clamping to maximum.");
                altitudeKm = maxAltitudeKm;
            }
            
            if (altitudeKm >= 0) // Basic validation
            {
                float altitudeUnity = altitudeKm * unityUnitsPerKm;
                float orbitRadiusUnity = earthRadiusUnity + altitudeUnity;
                
                targetOrbit.orbitRadius = orbitRadiusUnity;
                result.altitudeKm = altitudeKm;
                anyUpdated = true;
                
                if (showDebugLogs)
                    Debug.Log($"[OrbitController] Set altitude: {altitudeKm}km ({altitudeUnity:F3} Unity units) → orbit radius: {orbitRadiusUnity:F3} Unity units");
            }
        }

        // Update speed (km/s -> angular velocity)
        if (response.speed_kmps.HasValue)
        {
            float speedKmps = response.speed_kmps.Value;
            
            // Validate speed range
            if (speedKmps < minSpeedKmps)
            {
                if (showDebugLogs)
                    Debug.LogWarning($"[OrbitController] Speed {speedKmps}km/s is below minimum {minSpeedKmps}km/s. Clamping to minimum.");
                speedKmps = minSpeedKmps;
            }
            else if (speedKmps > maxSpeedKmps)
            {
                if (showDebugLogs)
                    Debug.LogWarning($"[OrbitController] Speed {speedKmps}km/s exceeds maximum {maxSpeedKmps}km/s. Clamping to maximum.");
                speedKmps = maxSpeedKmps;
            }
            
            if (speedKmps > 0) // Basic validation
            {
                // Convert linear speed to angular velocity
                // Angular velocity (rad/s) = linear speed (Unity units/s) / radius (Unity units)
                float speedUnityPerSec = speedKmps * unityUnitsPerKm;
                float angularVelocityRadPerSec = speedUnityPerSec / targetOrbit.orbitRadius;
                
                targetOrbit.orbitSpeed = angularVelocityRadPerSec;
                result.speedKmps = speedKmps;
                anyUpdated = true;
                
                if (showDebugLogs)
                    Debug.Log($"[OrbitController] Set speed: {speedKmps}km/s ({speedUnityPerSec:F6} Unity/s) → angular velocity: {angularVelocityRadPerSec:F6} rad/s");
            }
        }

        result.parametersUpdated = anyUpdated;
        
        if (anyUpdated)
        {
            // Get current state for response
            if (!result.altitudeKm.HasValue)
                result.altitudeKm = (targetOrbit.orbitRadius - earthRadiusUnity) / unityUnitsPerKm;
            if (!result.speedKmps.HasValue)
            {
                // Convert angular velocity back to linear speed in km/s
                float speedUnityPerSec = targetOrbit.orbitSpeed * targetOrbit.orbitRadius;
                result.speedKmps = speedUnityPerSec / unityUnitsPerKm;
            }
            
            result.updateReason = "Orbital parameters successfully updated";
        }
        else
        {
            result.updateReason = "Valid numeric values were not provided for altitude or speed";
        }

        return result;
    }

    /// <summary>
    /// Data structure matching the AI JSON response format
    /// </summary>
    [System.Serializable]
    private class AIOrbitResponse
    {
        public string intent;
        public float? distance_km;
        public float? speed_kmps;
    }

    /// <summary>
    /// Result of processing an orbit update command
    /// </summary>
    [System.Serializable]
    public class OrbitUpdateResult
    {
        public bool parametersUpdated = false;
        public float? altitudeKm = null;
        public float? speedKmps = null;
        public string updateReason = "";
    }

    // Validation method for Inspector
    private void OnValidate()
    {
        if (targetOrbit == null)
            targetOrbit = GetComponent<Orbit>();
    }

    // ====================================================================================
    // NEW METHODS FOR MISSION CONTROL INTEGRATION
    // ====================================================================================

    /// <summary>
    /// Creates a circular orbit with specified altitude and inclination.
    /// This is called by the new ToolExecutor system.
    /// </summary>
    /// <param name="altitude_km">Altitude above Earth surface in kilometers</param>
    /// <param name="inclination_deg">Orbital inclination in degrees (0=equatorial, 90=polar)</param>
    /// <returns>Result containing orbital parameters</returns>
    public OrbitUpdateResult CreateCircularOrbit(float altitude_km, float inclination_deg = 0f)
    {
        var result = new OrbitUpdateResult();

        if (targetOrbit == null)
        {
            Debug.LogError("[OrbitController] No target orbit assigned!");
            result.updateReason = "Orbit system not properly configured";
            return result;
        }

        // Validate altitude
        altitude_km = Mathf.Clamp(altitude_km, minAltitudeKm, maxAltitudeKm);

        // Convert altitude to Unity orbit radius
        float altitudeUnity = altitude_km * unityUnitsPerKm;
        float orbitRadiusUnity = earthRadiusUnity + altitudeUnity;

        // Set orbit parameters
        targetOrbit.orbitRadius = orbitRadiusUnity;

        // For circular orbit, calculate orbital speed based on altitude
        // Using simplified vis-viva equation: v = sqrt(GM/r)
        // For Earth: v ≈ sqrt(398600 / r_km) km/s
        const float GM_earth = 398600f; // km³/s²
        float r_km = earthRadiusKm + altitude_km;
        float orbitalVelocity_kmps = Mathf.Sqrt(GM_earth / r_km);

        // Convert to angular velocity (rad/s)
        float speedUnityPerSec = orbitalVelocity_kmps * unityUnitsPerKm;
        float angularVelocityRadPerSec = speedUnityPerSec / orbitRadiusUnity;
        targetOrbit.orbitSpeed = angularVelocityRadPerSec;

        // Reset satellite position and clear trail when creating new orbit
        targetOrbit.ResetPosition();

        // Store inclination for future use (Orbit.cs will need to support this)
        // For now, log it
        // Update visualization
        if (orbitVisualizer != null)
        {
            orbitVisualizer.SetCircularOrbit(orbitRadiusUnity, inclination_deg);
            orbitVisualizer.Show();
        }

        if (showDebugLogs)
        {
            Debug.Log($"[OrbitController] Created CIRCULAR orbit:");
            Debug.Log($"  Altitude: {altitude_km:F1} km");
            Debug.Log($"  Inclination: {inclination_deg:F1}°");
            Debug.Log($"  Orbital velocity: {orbitalVelocity_kmps:F2} km/s");
            Debug.Log($"  Orbit radius: {orbitRadiusUnity:F3} Unity units");
            Debug.Log($"  Angular velocity: {angularVelocityRadPerSec:F6} rad/s");
        }

        result.parametersUpdated = true;
        result.altitudeKm = altitude_km;
        result.speedKmps = orbitalVelocity_kmps;
        result.updateReason = $"Circular orbit created at {altitude_km:F0}km altitude, {inclination_deg:F1}° inclination";

        return result;
    }

    /// <summary>
    /// Creates an elliptical orbit with specified periapsis and apoapsis.
    /// This is called by the new ToolExecutor system.
    /// </summary>
    /// <param name="periapsis_km">Lowest altitude in kilometers</param>
    /// <param name="apoapsis_km">Highest altitude in kilometers</param>
    /// <param name="inclination_deg">Orbital inclination in degrees</param>
    /// <returns>Result containing orbital parameters</returns>
    public OrbitUpdateResult CreateEllipticalOrbit(float periapsis_km, float apoapsis_km, float inclination_deg = 0f)
    {
        var result = new OrbitUpdateResult();

        if (targetOrbit == null)
        {
            Debug.LogError("[OrbitController] No target orbit assigned!");
            result.updateReason = "Orbit system not properly configured";
            return result;
        }

        // Validate altitudes
        periapsis_km = Mathf.Clamp(periapsis_km, minAltitudeKm, maxAltitudeKm);
        apoapsis_km = Mathf.Clamp(apoapsis_km, periapsis_km + 1f, 100000f); // Ensure apoapsis > periapsis

        // Calculate semi-major axis (average of periapsis and apoapsis radii)
        float rp_km = earthRadiusKm + periapsis_km;  // Periapsis radius from Earth center
        float ra_km = earthRadiusKm + apoapsis_km;   // Apoapsis radius from Earth center
        float semiMajorAxis_km = (rp_km + ra_km) / 2f;

        // Calculate eccentricity
        float eccentricity = (ra_km - rp_km) / (ra_km + rp_km);

        // For visualization, use semi-major axis as orbit radius
        // (Orbit.cs will need updating to properly render ellipses)
        float semiMajorAxis_unity = (semiMajorAxis_km - earthRadiusKm) * unityUnitsPerKm;
        float orbitRadiusUnity = earthRadiusUnity + semiMajorAxis_unity;

        targetOrbit.orbitRadius = orbitRadiusUnity;

        // Calculate orbital speed at periapsis using vis-viva equation
        // v = sqrt(GM * (2/r - 1/a))
        const float GM_earth = 398600f; // km³/s²
        float speedAtPeriapsis_kmps = Mathf.Sqrt(GM_earth * (2f / rp_km - 1f / semiMajorAxis_km));

        // Convert to angular velocity
        float speedUnityPerSec = speedAtPeriapsis_kmps * unityUnitsPerKm;
        float angularVelocityRadPerSec = speedUnityPerSec / orbitRadiusUnity;
        targetOrbit.orbitSpeed = angularVelocityRadPerSec;

        // Reset satellite position and clear trail when creating new orbit
        targetOrbit.ResetPosition();

        // Update visualization with elliptical orbit
        if (orbitVisualizer != null)
        {
            orbitVisualizer.SetEllipticalOrbit(orbitRadiusUnity, eccentricity, inclination_deg, 0f);
            orbitVisualizer.Show();
        }

        if (showDebugLogs)
        {
            Debug.Log($"[OrbitController] Created ELLIPTICAL orbit:");
            Debug.Log($"  Periapsis: {periapsis_km:F1} km");
            Debug.Log($"  Apoapsis: {apoapsis_km:F1} km");
            Debug.Log($"  Semi-major axis: {semiMajorAxis_km:F1} km");
            Debug.Log($"  Eccentricity: {eccentricity:F3}");
            Debug.Log($"  Inclination: {inclination_deg:F1}°");
            Debug.Log($"  Speed at periapsis: {speedAtPeriapsis_kmps:F2} km/s");
        }

        result.parametersUpdated = true;
        result.altitudeKm = (periapsis_km + apoapsis_km) / 2f; // Average altitude
        result.speedKmps = speedAtPeriapsis_kmps;
        result.updateReason = $"Elliptical orbit created: {periapsis_km:F0}km × {apoapsis_km:F0}km, {inclination_deg:F1}° inclination";

        return result;
    }

    /// <summary>
    /// Clears the current orbit visualization and resets the satellite.
    /// This implements the "single-orbit workspace" concept from Phase 1.
    /// </summary>
    /// <returns>Result containing status information</returns>
    public OrbitUpdateResult ClearOrbit()
    {
        var result = new OrbitUpdateResult();

        // Clear visualization
        if (orbitVisualizer != null)
        {
            orbitVisualizer.ClearOrbit();
        }

        // Stop orbit motion and clear trail
        if (targetOrbit != null)
        {
            targetOrbit.orbitSpeed = 0f;
            targetOrbit.ResetPosition(); // This also clears the trail
        }

        if (showDebugLogs)
        {
            Debug.Log("[OrbitController] Orbit cleared - workspace reset");
        }

        result.parametersUpdated = true;
        result.altitudeKm = null;
        result.speedKmps = null;
        result.updateReason = "Orbit cleared successfully. Workspace is ready for a new orbital configuration.";

        return result;
    }
}
