using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace AI.Tools
{
    /// <summary>
    /// Loads and manages tool schemas from JSON configuration.
    /// Provides tool definitions to the agent for routing decisions.
    /// </summary>
    public class ToolRegistry
    {
        private Dictionary<string, ToolSchema> _tools = new Dictionary<string, ToolSchema>();
        private Dictionary<string, CommonOrbitDefinition> _commonOrbits = new Dictionary<string, CommonOrbitDefinition>();

        /// <summary>
        /// Load tool schemas from Resources/Tools/ToolSchemas.json
        /// </summary>
        public bool LoadSchemas()
        {
            try
            {
                // Load JSON file from Resources
                TextAsset jsonFile = Resources.Load<TextAsset>("Tools/ToolSchemas");
                if (jsonFile == null)
                {
                    Debug.LogError("[ToolRegistry] ToolSchemas.json not found in Resources/Tools/");
                    return false;
                }

                // Parse JSON
                JObject root = JObject.Parse(jsonFile.text);

                // Load tools
                JArray toolsArray = root["tools"] as JArray;
                if (toolsArray != null)
                {
                    foreach (var toolToken in toolsArray)
                    {
                        try
                        {
                            ToolSchema tool = toolToken.ToObject<ToolSchema>();
                            if (tool != null && !string.IsNullOrEmpty(tool.id))
                            {
                                _tools[tool.id] = tool;
                                Debug.Log($"[ToolRegistry] Loaded tool: {tool.id} ({tool.name})");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[ToolRegistry] Failed to parse tool: {ex.Message}");
                        }
                    }
                }

                // Load common orbits (optional presets)
                JObject commonOrbitsObj = root["common_orbits"] as JObject;
                if (commonOrbitsObj != null)
                {
                    foreach (var kvp in commonOrbitsObj)
                    {
                        try
                        {
                            CommonOrbitDefinition orbit = kvp.Value.ToObject<CommonOrbitDefinition>();
                            if (orbit != null)
                            {
                                _commonOrbits[kvp.Key] = orbit;
                                Debug.Log($"[ToolRegistry] Loaded preset orbit: {kvp.Key}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[ToolRegistry] Failed to parse common orbit '{kvp.Key}': {ex.Message}");
                        }
                    }
                }

                Debug.Log($"[ToolRegistry] Successfully loaded {_tools.Count} tools and {_commonOrbits.Count} preset orbits");
                return _tools.Count > 0;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ToolRegistry] Failed to load schemas: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get a tool schema by ID
        /// </summary>
        public ToolSchema GetTool(string toolId)
        {
            if (string.IsNullOrEmpty(toolId))
                return null;

            _tools.TryGetValue(toolId, out ToolSchema tool);
            return tool;
        }

        /// <summary>
        /// Get all available tools
        /// </summary>
        public List<ToolSchema> GetAllTools()
        {
            return _tools.Values.ToList();
        }

        /// <summary>
        /// Create a specialist-only registry with just navigation tools (return_to_hub)
        /// Used in Mission Spaces where specialists can help users return to Hub but cannot manipulate orbits
        /// </summary>
        public static ToolRegistry CreateSpecialistRegistry()
        {
            var registry = new ToolRegistry();

            // Manually create return_to_hub tool schema (no orbit manipulation tools)
            var returnToHubTool = new ToolSchema
            {
                id = "return_to_hub",
                name = "Return to Mission Control Hub",
                description = "Return user to Mission Control Hub from a mission space",
                parameters = new Dictionary<string, ToolParameter>(), // No parameters required
                specialist_persona = "Navigator",
                specialist_team = "Navigation",
                unity_function = "ReturnToHub"
            };

            registry._tools["return_to_hub"] = returnToHubTool;

            Debug.Log("[ToolRegistry] Created specialist registry with 1 navigation tool (return_to_hub)");
            return registry;
        }

        /// <summary>
        /// Get a preset orbit definition by name (e.g., "ISS", "GEO")
        /// </summary>
        public CommonOrbitDefinition GetCommonOrbit(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            _commonOrbits.TryGetValue(name, out CommonOrbitDefinition orbit);
            return orbit;
        }

        /// <summary>
        /// Generate a formatted tool list for LLM prompts
        /// </summary>
        public string GetToolDescriptionsForPrompt()
        {
            var descriptions = new List<string>();

            foreach (var tool in _tools.Values)
            {
                var paramList = string.Join(", ", tool.parameters.Keys);
                descriptions.Add($"- {tool.id}: {tool.description} Parameters: {paramList}");
            }

            return string.Join("\n", descriptions);
        }

        /// <summary>
        /// Validate that a tool invocation has all required parameters
        /// </summary>
        public bool ValidateToolCall(string toolId, Dictionary<string, object> parameters, out string errorMessage)
        {
            errorMessage = null;

            ToolSchema tool = GetTool(toolId);
            if (tool == null)
            {
                errorMessage = $"Unknown tool: {toolId}";
                return false;
            }

            // Check required parameters
            foreach (var kvp in tool.parameters)
            {
                string paramName = kvp.Key;
                ToolParameter paramDef = kvp.Value;

                if (paramDef.required && !parameters.ContainsKey(paramName))
                {
                    errorMessage = $"Missing required parameter: {paramName}";
                    return false;
                }

                if (parameters.ContainsKey(paramName))
                {
                    object value = parameters[paramName];

                    // Type validation (basic)
                    if (paramDef.type == "number")
                    {
                        if (!IsNumeric(value))
                        {
                            errorMessage = $"Parameter {paramName} must be numeric";
                            return false;
                        }

                        double numValue = Convert.ToDouble(value);

                        // Range validation
                        if (paramDef.min.HasValue && numValue < paramDef.min.Value)
                        {
                            errorMessage = $"Parameter {paramName} ({numValue}) is below minimum ({paramDef.min})";
                            return false;
                        }

                        if (paramDef.max.HasValue && numValue > paramDef.max.Value)
                        {
                            errorMessage = $"Parameter {paramName} ({numValue}) exceeds maximum ({paramDef.max})";
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private bool IsNumeric(object value)
        {
            return value is int || value is long || value is float || value is double || value is decimal;
        }
    }

    /// <summary>
    /// Tool schema definition matching JSON format
    /// </summary>
    [Serializable]
    public class ToolSchema
    {
        public string id;
        public string name;
        public string description;
        public Dictionary<string, ToolParameter> parameters;
        public string specialist_persona;
        public string specialist_team;
        public string unity_function;
    }

    /// <summary>
    /// Tool parameter definition
    /// </summary>
    [Serializable]
    public class ToolParameter
    {
        public string type;
        public string description;
        public double? min;
        public double? max;
        public bool required;
        [JsonProperty("default")]
        public object defaultValue;
    }

    /// <summary>
    /// Preset orbit definition for common orbits
    /// </summary>
    [Serializable]
    public class CommonOrbitDefinition
    {
        public string tool;
        public double? altitude_km;
        public double? inclination_deg;
        public double? periapsis_km;
        public double? apoapsis_km;
        public string description;
    }
}
