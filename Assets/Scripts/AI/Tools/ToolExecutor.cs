using System;
using System.Collections.Generic;
using UnityEngine;

namespace AI.Tools
{
    /// <summary>
    /// Executes tool calls by invoking the corresponding Unity C# methods.
    /// Maps tool IDs from JSON to actual OrbitController functions.
    /// </summary>
    public class ToolExecutor
    {
        private readonly ToolRegistry _registry;
        private readonly OrbitController _orbitController;
        private readonly TimeController _timeController;

        public ToolExecutor(ToolRegistry registry, OrbitController orbitController = null, TimeController timeController = null)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _orbitController = orbitController; // Optional - orbit tools won't work if null (specialist mode)
            _timeController = timeController; // Optional - time control tools won't work if null
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

                    case "set_simulation_speed":
                        return ExecuteSetSimulationSpeed(parameters, ref result);

                    case "pause_simulation":
                        return ExecutePauseSimulation(parameters, ref result);

                    case "reset_simulation_time":
                        return ExecuteResetSimulationTime(parameters, ref result);

                    case "route_to_mission":
                        return ExecuteRouteToMission(parameters, ref result);

                    case "return_to_hub":
                        return ExecuteReturnToHub(parameters, ref result);

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
            // Check if orbit controller is available
            if (_orbitController == null)
            {
                result.success = false;
                result.errorMessage = "I'm sorry, there was an issue processing your command. The orbit control system is not available.";
                Debug.LogWarning("[ToolExecutor] Cannot create orbit - OrbitController is null (specialist mode)");
                return false;
            }

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
            // Check if orbit controller is available
            if (_orbitController == null)
            {
                result.success = false;
                result.errorMessage = "I'm sorry, there was an issue processing your command. The orbit control system is not available.";
                Debug.LogWarning("[ToolExecutor] Cannot create orbit - OrbitController is null (specialist mode)");
                return false;
            }

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
            // Check if orbit controller is available
            if (_orbitController == null)
            {
                result.success = false;
                result.errorMessage = "I'm sorry, there was an issue processing your command. The orbit control system is not available.";
                Debug.LogWarning("[ToolExecutor] Cannot clear orbit - OrbitController is null (specialist mode)");
                return false;
            }

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

        // ============== TIME CONTROL TOOLS ==============

        private bool ExecuteSetSimulationSpeed(Dictionary<string, object> parameters, ref ToolExecutionResult result)
        {
            // Check if TimeController is available
            if (_timeController == null)
            {
                result.success = false;
                result.errorMessage = "TimeController not available - cannot control simulation speed";
                Debug.LogError($"[ToolExecutor] {result.errorMessage}");
                return false;
            }

            // Extract speed multiplier parameter
            float speedMultiplier = (float)GetDoubleParameter(parameters, "speed_multiplier", 1.0);

            // Set the simulation speed
            _timeController.SetSpeed(speedMultiplier);

            result.success = true;
            result.outputData = new Dictionary<string, object>
            {
                ["speed_multiplier"] = speedMultiplier,
                ["speed_description"] = _timeController.GetSpeedDescription(),
                ["is_paused"] = _timeController.isPaused
            };
            result.message = $"Simulation speed set to {speedMultiplier}x (Time acceleration: {_timeController.GetSpeedDescription()})";

            Debug.Log($"[ToolExecutor] {result.message}");

            return true;
        }

        private bool ExecutePauseSimulation(Dictionary<string, object> parameters, ref ToolExecutionResult result)
        {
            // Check if TimeController is available
            if (_timeController == null)
            {
                result.success = false;
                result.errorMessage = "TimeController not available - cannot pause/resume simulation";
                Debug.LogError($"[ToolExecutor] {result.errorMessage}");
                return false;
            }

            // Extract pause parameter
            bool shouldPause = GetBoolParameter(parameters, "pause", true);

            // Execute pause or resume
            if (shouldPause)
            {
                _timeController.Pause();
            }
            else
            {
                _timeController.Resume();
            }

            result.success = true;
            result.outputData = new Dictionary<string, object>
            {
                ["is_paused"] = _timeController.isPaused,
                ["current_speed"] = _timeController.currentSpeed,
                ["simulation_time"] = _timeController.GetFormattedTime()
            };
            result.message = shouldPause
                ? "Simulation PAUSED - Time is frozen"
                : $"Simulation RESUMED at {_timeController.currentSpeed}x speed";

            Debug.Log($"[ToolExecutor] {result.message}");

            return true;
        }

        private bool ExecuteResetSimulationTime(Dictionary<string, object> parameters, ref ToolExecutionResult result)
        {
            // Check if TimeController is available
            if (_timeController == null)
            {
                result.success = false;
                result.errorMessage = "TimeController not available - cannot reset simulation time";
                Debug.LogError($"[ToolExecutor] {result.errorMessage}");
                return false;
            }

            // Reset simulation time to zero
            _timeController.ResetSimulationTime();

            result.success = true;
            result.outputData = new Dictionary<string, object>
            {
                ["simulation_time"] = _timeController.GetFormattedTime(),
                ["is_paused"] = _timeController.isPaused,
                ["current_speed"] = _timeController.currentSpeed
            };
            result.message = "Mission clock reset to 00:00:00";

            Debug.Log($"[ToolExecutor] {result.message}");

            return true;
        }

        private bool ExecuteRouteToMission(Dictionary<string, object> parameters, ref ToolExecutionResult result)
        {
            // Extract parameters
            string mission = GetStringParameter(parameters, "mission", "");
            string context = GetStringParameter(parameters, "context_for_specialist", "");

            if (string.IsNullOrEmpty(mission))
            {
                result.success = false;
                result.errorMessage = "Mission name not specified";
                Debug.LogWarning($"[ToolExecutor] {result.errorMessage}");
                return false;
            }

            // Validate mission name
            if (mission != "ISS" && mission != "GPS" && mission != "Voyager" && mission != "Hubble")
            {
                result.success = false;
                result.errorMessage = $"Unknown mission: {mission}. Valid missions: ISS, GPS, Voyager, Hubble";
                Debug.LogWarning($"[ToolExecutor] {result.errorMessage}");
                return false;
            }

            // Store routing context in MissionContext singleton
            if (MissionContext.Instance != null)
            {
                MissionContext.Instance.SetRoutingContext(mission, context);
            }
            else
            {
                Debug.LogWarning("[ToolExecutor] MissionContext.Instance is null - context will not be preserved");
            }

            // Verify SceneTransitionManager exists
            if (SceneTransitionManager.Instance == null)
            {
                result.success = false;
                result.errorMessage = "SceneTransitionManager not found - cannot transition to mission";
                Debug.LogError($"[ToolExecutor] {result.errorMessage}");
                return false;
            }

            // DO NOT trigger transition immediately - flag it for after audio finishes
            result.success = true;
            result.requiresSceneTransition = true;
            result.targetMission = mission;
            result.targetScene = mission; // Scene name matches mission name
            result.outputData = new Dictionary<string, object>
            {
                ["mission"] = mission,
                ["context"] = context
            };
            result.message = $"Routing to {mission} Mission Space...";

            Debug.Log($"[ToolExecutor] {result.message} (transition will occur after CAPCOM finishes speaking)");

            return true;
        }

        private bool ExecuteReturnToHub(Dictionary<string, object> parameters, ref ToolExecutionResult result)
        {
            // Store context in MissionContext
            if (MissionContext.Instance != null)
            {
                string departingFrom = MissionContext.Instance.currentLocation ?? "mission space";
                MissionContext.Instance.SetRoutingContext("Hub", $"Returning from {departingFrom}");
            }
            else
            {
                Debug.LogWarning("[ToolExecutor] MissionContext.Instance is null");
            }

            // Verify SceneTransitionManager exists
            if (SceneTransitionManager.Instance == null)
            {
                result.success = false;
                result.errorMessage = "SceneTransitionManager not found - cannot return to Hub";
                Debug.LogError($"[ToolExecutor] {result.errorMessage}");
                return false;
            }

            // DO NOT trigger transition immediately - flag it for after audio finishes
            result.success = true;
            result.requiresSceneTransition = true;
            result.targetMission = "Hub";
            result.targetScene = "Hub";
            result.message = "Returning to Mission Control Hub...";

            Debug.Log($"[ToolExecutor] {result.message} (transition will occur after CAPCOM finishes speaking)");

            return true;
        }

        /// <summary>
        /// Helper to safely extract string parameter with default fallback
        /// </summary>
        private string GetStringParameter(Dictionary<string, object> parameters, string key, string defaultValue)
        {
            if (parameters == null || !parameters.ContainsKey(key))
                return defaultValue;

            object value = parameters[key];
            return value?.ToString() ?? defaultValue;
        }

        /// <summary>
        /// Helper to safely extract boolean parameter with default fallback
        /// </summary>
        private bool GetBoolParameter(Dictionary<string, object> parameters, string key, bool defaultValue)
        {
            if (parameters == null || !parameters.ContainsKey(key))
                return defaultValue;

            object value = parameters[key];
            try
            {
                if (value is bool boolVal)
                    return boolVal;

                // Handle string representations
                if (value is string strVal)
                    return bool.Parse(strVal);

                // Handle numeric representations (0 = false, non-zero = true)
                return Convert.ToDouble(value) != 0;
            }
            catch
            {
                Debug.LogWarning($"[ToolExecutor] Failed to convert parameter '{key}' to bool, using default {defaultValue}");
                return defaultValue;
            }
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

        // Scene transition fields (for route_to_mission tool)
        public bool requiresSceneTransition = false;
        public string targetScene = null;
        public string targetMission = null;
    }
}
