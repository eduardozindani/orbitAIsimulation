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
    
    [Tooltip("Unity units per kilometer (adjust based on your Earth scale)")]
    public float unityUnitsPerKm = 1f / 1000f; // Default: 1 Unity unit = 1000 km
    
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
            if (altitudeKm >= 0) // Basic validation
            {
                float orbitRadiusKm = earthRadiusKm + altitudeKm;
                float orbitRadiusUnity = orbitRadiusKm * unityUnitsPerKm;
                
                targetOrbit.orbitRadius = orbitRadiusUnity;
                result.altitudeKm = altitudeKm;
                anyUpdated = true;
                
                if (showDebugLogs)
                    Debug.Log($"[OrbitController] Set altitude: {altitudeKm}km → orbit radius: {orbitRadiusUnity} Unity units");
            }
        }

        // Update speed (km/s -> angular velocity)
        if (response.speed_kmps.HasValue)
        {
            float speedKmps = response.speed_kmps.Value;
            if (speedKmps > 0) // Basic validation
            {
                // Convert linear speed to angular velocity
                // Angular velocity = linear speed / radius
                float currentRadiusKm = targetOrbit.orbitRadius / unityUnitsPerKm;
                float angularVelocityRadPerSec = speedKmps / currentRadiusKm;
                
                targetOrbit.orbitSpeed = angularVelocityRadPerSec;
                result.speedKmps = speedKmps;
                anyUpdated = true;
                
                if (showDebugLogs)
                    Debug.Log($"[OrbitController] Set speed: {speedKmps}km/s → angular velocity: {angularVelocityRadPerSec} rad/s");
            }
        }

        result.parametersUpdated = anyUpdated;
        
        if (anyUpdated)
        {
            // Get current state for response
            if (!result.altitudeKm.HasValue)
                result.altitudeKm = (targetOrbit.orbitRadius / unityUnitsPerKm) - earthRadiusKm;
            if (!result.speedKmps.HasValue)
            {
                float currentRadiusKm = targetOrbit.orbitRadius / unityUnitsPerKm;
                result.speedKmps = targetOrbit.orbitSpeed * currentRadiusKm;
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
