using UnityEngine;
using AI.Tools;
using System.Collections.Generic;

/// <summary>
/// Test component to verify ToolRegistry and ToolExecutor work correctly.
/// Attach to a GameObject to test in Play mode.
/// </summary>
public class ToolRegistryTest : MonoBehaviour
{
    [Header("Test Controls")]
    [Tooltip("Run tests on Start")]
    public bool runOnStart = true;

    [Header("Test Results")]
    [TextArea(10, 20)]
    public string testOutput = "Press Play to run tests...";

    private ToolRegistry _registry;
    private System.Text.StringBuilder _output;

    void Start()
    {
        if (runOnStart)
        {
            RunTests();
        }
    }

    [ContextMenu("Run Tool Registry Tests")]
    public void RunTests()
    {
        _output = new System.Text.StringBuilder();
        Log("=== Tool Registry Test ===\n");

        TestLoadSchemas();
        TestGetTool();
        TestToolValidation();
        TestCommonOrbits();
        TestToolExecutor();

        testOutput = _output.ToString();
        Debug.Log(testOutput);
    }

    private void TestLoadSchemas()
    {
        Log("TEST 1: Load Schemas from JSON");
        _registry = new ToolRegistry();
        bool loaded = _registry.LoadSchemas();

        if (loaded)
        {
            var tools = _registry.GetAllTools();
            Log($"✓ SUCCESS: Loaded {tools.Count} tools");
            foreach (var tool in tools)
            {
                Log($"  - {tool.id}: {tool.name}");
            }
        }
        else
        {
            Log("✗ FAILED: Could not load schemas");
        }
        Log("");
    }

    private void TestGetTool()
    {
        Log("TEST 2: Get Tool by ID");

        var circularTool = _registry.GetTool("create_circular_orbit");
        if (circularTool != null)
        {
            Log($"✓ SUCCESS: Found create_circular_orbit");
            Log($"  Description: {circularTool.description}");
            Log($"  Parameters: {circularTool.parameters.Count}");
        }
        else
        {
            Log("✗ FAILED: create_circular_orbit not found");
        }

        var ellipticalTool = _registry.GetTool("create_elliptical_orbit");
        if (ellipticalTool != null)
        {
            Log($"✓ SUCCESS: Found create_elliptical_orbit");
        }
        else
        {
            Log("✗ FAILED: create_elliptical_orbit not found");
        }

        Log("");
    }

    private void TestToolValidation()
    {
        Log("TEST 3: Parameter Validation");

        // Test valid parameters
        var validParams = new Dictionary<string, object>
        {
            { "altitude_km", 420.0 },
            { "inclination_deg", 51.6 }
        };

        bool isValid = _registry.ValidateToolCall("create_circular_orbit", validParams, out string error);
        if (isValid)
        {
            Log("✓ SUCCESS: Valid parameters accepted");
        }
        else
        {
            Log($"✗ FAILED: Valid parameters rejected: {error}");
        }

        // Test invalid parameters (altitude too low)
        var invalidParams = new Dictionary<string, object>
        {
            { "altitude_km", 50.0 }, // Below minimum of 160
            { "inclination_deg", 51.6 }
        };

        isValid = _registry.ValidateToolCall("create_circular_orbit", invalidParams, out error);
        if (!isValid)
        {
            Log($"✓ SUCCESS: Invalid parameters rejected: {error}");
        }
        else
        {
            Log("✗ FAILED: Invalid parameters were accepted");
        }

        // Test missing required parameter
        var missingParams = new Dictionary<string, object>
        {
            { "inclination_deg", 51.6 }
            // Missing altitude_km
        };

        isValid = _registry.ValidateToolCall("create_circular_orbit", missingParams, out error);
        if (!isValid)
        {
            Log($"✓ SUCCESS: Missing parameter detected: {error}");
        }
        else
        {
            Log("✗ FAILED: Missing parameter was not detected");
        }

        Log("");
    }

    private void TestCommonOrbits()
    {
        Log("TEST 4: Common Orbit Presets");

        var iss = _registry.GetCommonOrbit("ISS");
        if (iss != null)
        {
            Log($"✓ SUCCESS: ISS preset found");
            Log($"  Altitude: {iss.altitude_km}km, Inclination: {iss.inclination_deg}°");
        }
        else
        {
            Log("✗ FAILED: ISS preset not found");
        }

        var molniya = _registry.GetCommonOrbit("Molniya");
        if (molniya != null)
        {
            Log($"✓ SUCCESS: Molniya preset found");
            Log($"  Periapsis: {molniya.periapsis_km}km, Apoapsis: {molniya.apoapsis_km}km");
        }
        else
        {
            Log("✗ FAILED: Molniya preset not found");
        }

        Log("");
    }

    private void TestToolExecutor()
    {
        Log("TEST 5: Tool Executor");

        // We need an OrbitController to test ToolExecutor
        // For now, we'll just verify it can be instantiated
        // Actual execution test will happen when OrbitController methods exist

        var orbitController = FindFirstObjectByType<OrbitController>();
        if (orbitController == null)
        {
            Log("⚠ WARNING: No OrbitController in scene, skipping execution test");
            Log("  (This is expected in test environment)");
        }
        else
        {
            var executor = new ToolExecutor(_registry, orbitController);

            var params1 = new Dictionary<string, object>
            {
                { "altitude_km", 420.0 },
                { "inclination_deg", 51.6 }
            };

            bool success = executor.ExecuteTool("create_circular_orbit", params1, out ToolExecutionResult result);

            if (success)
            {
                Log($"✓ SUCCESS: Executed create_circular_orbit");
                Log($"  Result: {result.success}");
                if (result.outputData.ContainsKey("altitude_km"))
                {
                    Log($"  Altitude: {result.outputData["altitude_km"]}km");
                }
            }
            else
            {
                Log($"✗ FAILED: Execution failed: {result.errorMessage}");
            }
        }

        Log("");
    }

    private void Log(string message)
    {
        _output.AppendLine(message);
    }
}
