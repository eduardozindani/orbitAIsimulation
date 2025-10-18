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
}
