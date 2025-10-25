using System;
using System.Collections.Generic;
using UnityEngine;

namespace Agents.Tools
{
    /// <summary>
    /// Executes tool calls by invoking the corresponding Unity C# methods.
    /// Maps tool IDs from JSON to actual OrbitController functions.
    /// </summary>
    public class ToolExecutor
    {
        private readonly ToolRegistry _registry;
        private readonly OrbitController _orbitController;

        public ToolExecutor(ToolRegistry registry, OrbitController orbitController)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _orbitController = orbitController ?? throw new ArgumentNullException(nameof(orbitController));
        }

        /// <summary>
        /// Execute a tool call with the given parameters
        /// </summary>
        /// <param name="toolId">Tool identifier from schema</param>
        /// <param name="parameters">Parameter dictionary</param>
        /// <param name="result">Output result information</param>
        /// <returns>True if execution succeeded</returns>
        public bool ExecuteTool(string toolId, Dictionary<string, object> parameters, out ToolExecutionResult result)
        {
            result = new ToolExecutionResult { toolId = toolId };

            // Validate tool exists
            ToolSchema tool = _registry.GetTool(toolId);
            if (tool == null)
            {
                result.success = false;
                result.errorMessage = $"Unknown tool: {toolId}";
                Debug.LogError($"[ToolExecutor] {result.errorMessage}");
                return false;
            }

            // Validate parameters
            if (!_registry.ValidateToolCall(toolId, parameters, out string validationError))
            {
                result.success = false;
                result.errorMessage = validationError;
                Debug.LogWarning($"[ToolExecutor] Validation failed for {toolId}: {validationError}");
                return false;
            }

            // Execute the appropriate Unity function based on tool ID
            try
            {
                switch (toolId)
                {
                    case "create_circular_orbit":
                        return ExecuteCreateCircularOrbit(parameters, ref result);

                    case "create_elliptical_orbit":
                        return ExecuteCreateEllipticalOrbit(parameters, ref result);

                    case "clear_orbit":
                        return ExecuteClearOrbit(parameters, ref result);

                    default:
                        result.success = false;
                        result.errorMessage = $"Tool {toolId} is defined but not implemented in ToolExecutor";
                        Debug.LogError($"[ToolExecutor] {result.errorMessage}");
                        return false;
                }
            }
            catch (Exception ex)
            {
                result.success = false;
                result.errorMessage = $"Execution error: {ex.Message}";
                Debug.LogError($"[ToolExecutor] Exception executing {toolId}: {ex}");
                return false;
            }
        }

        private bool ExecuteCreateCircularOrbit(Dictionary<string, object> parameters, ref ToolExecutionResult result)
        {
            // Extract parameters
            float altitude_km = (float)GetDoubleParameter(parameters, "altitude_km", 420); // Default ISS altitude
            float inclination_deg = (float)GetDoubleParameter(parameters, "inclination_deg", 0); // Default equatorial

            // Call OrbitController to actually create the orbit
            OrbitController.OrbitUpdateResult orbitResult = _orbitController.CreateCircularOrbit(altitude_km, inclination_deg);

            result.success = orbitResult.parametersUpdated;
            result.outputData = new Dictionary<string, object>
            {
                ["altitude_km"] = orbitResult.altitudeKm ?? altitude_km,
                ["inclination_deg"] = inclination_deg,
                ["orbital_velocity_kmps"] = orbitResult.speedKmps ?? 0f,
                ["orbit_type"] = "circular"
            };
            result.message = orbitResult.updateReason;

            Debug.Log($"[ToolExecutor] {result.message}");

            return result.success;
        }

        private bool ExecuteCreateEllipticalOrbit(Dictionary<string, object> parameters, ref ToolExecutionResult result)
        {
            // Extract parameters
            float periapsis_km = (float)GetDoubleParameter(parameters, "periapsis_km", 500);
            float apoapsis_km = (float)GetDoubleParameter(parameters, "apoapsis_km", 40000);
            float inclination_deg = (float)GetDoubleParameter(parameters, "inclination_deg", 0);

            // Validation: periapsis must be less than apoapsis
            if (periapsis_km >= apoapsis_km)
            {
                result.success = false;
                result.errorMessage = $"Periapsis ({periapsis_km}km) must be less than apoapsis ({apoapsis_km}km)";
                Debug.LogWarning($"[ToolExecutor] {result.errorMessage}");
                return false;
            }

            // Call OrbitController to actually create the orbit
            OrbitController.OrbitUpdateResult orbitResult = _orbitController.CreateEllipticalOrbit(periapsis_km, apoapsis_km, inclination_deg);

            result.success = orbitResult.parametersUpdated;
            result.outputData = new Dictionary<string, object>
            {
                ["periapsis_km"] = periapsis_km,
                ["apoapsis_km"] = apoapsis_km,
                ["inclination_deg"] = inclination_deg,
                ["orbit_type"] = "elliptical",
                ["eccentricity"] = CalculateEccentricity(periapsis_km, apoapsis_km),
                ["orbital_velocity_kmps"] = orbitResult.speedKmps ?? 0f
            };
            result.message = orbitResult.updateReason;

            Debug.Log($"[ToolExecutor] {result.message}");

            return result.success;
        }

        /// <summary>
        /// Helper to safely extract numeric parameter with default fallback
        /// </summary>
        private double GetDoubleParameter(Dictionary<string, object> parameters, string key, double defaultValue)
        {
            if (parameters == null || !parameters.ContainsKey(key))
                return defaultValue;

            object value = parameters[key];
            try
            {
                return Convert.ToDouble(value);
            }
            catch
            {
                Debug.LogWarning($"[ToolExecutor] Failed to convert parameter '{key}' to double, using default {defaultValue}");
                return defaultValue;
            }
        }

        private bool ExecuteClearOrbit(Dictionary<string, object> parameters, ref ToolExecutionResult result)
        {
            // Clear orbit has no parameters - just execute
            OrbitController.OrbitUpdateResult orbitResult = _orbitController.ClearOrbit();

            result.success = orbitResult.parametersUpdated;
            result.outputData = new Dictionary<string, object>
            {
                ["workspace_status"] = "cleared",
                ["ready_for_new_orbit"] = true
            };
            result.message = orbitResult.updateReason;

            Debug.Log($"[ToolExecutor] {result.message}");

            return result.success;
        }

        /// <summary>
        /// Calculate orbital eccentricity from periapsis and apoapsis
        /// </summary>
        private double CalculateEccentricity(double periapsis_km, double apoapsis_km)
        {
            const double earthRadius_km = 6371.0;
            double rp = earthRadius_km + periapsis_km; // Periapsis radius from Earth center
            double ra = earthRadius_km + apoapsis_km;  // Apoapsis radius from Earth center
            double eccentricity = (ra - rp) / (ra + rp);
            return eccentricity;
        }
    }

    /// <summary>
    /// Result of a tool execution
    /// </summary>
    [Serializable]
    public class ToolExecutionResult
    {
        public string toolId;
        public bool success;
        public string errorMessage;
        public string message;
        public Dictionary<string, object> outputData = new Dictionary<string, object>();
    }
}
