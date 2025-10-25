using UnityEngine;

/// <summary>
/// Renders orbital trajectory paths using LineRenderer.
/// Supports both circular and elliptical orbits with physically accurate geometry.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class OrbitVisualizer : MonoBehaviour
{
    [Header("Visualization Settings")]
    [Tooltip("Number of points to draw the orbit path (higher = smoother)")]
    public int pathResolution = 128;

    [Tooltip("Width of the orbit line in Unity units")]
    public float lineWidth = 0.05f;

    [Tooltip("Color of the orbit trajectory")]
    public Color orbitColor = new Color(0f, 1f, 1f, 0.7f); // Cyan with transparency

    [Header("Orbit Parameters")]
    [Tooltip("Center point of the orbit (usually Earth)")]
    public Transform orbitCenter;

    [Tooltip("Radius of circular orbit or semi-major axis for elliptical (Unity units)")]
    public float orbitRadius = 5.32f;

    [Tooltip("Eccentricity: 0 = circular, 0 < e < 1 = elliptical")]
    [Range(0f, 0.99f)]
    public float eccentricity = 0f;

    [Tooltip("Inclination angle in degrees (0 = equatorial, 90 = polar)")]
    [Range(0f, 180f)]
    public float inclination = 0f;

    [Tooltip("Argument of periapsis in degrees (rotation of ellipse in orbital plane)")]
    [Range(0f, 360f)]
    public float argumentOfPeriapsis = 0f;

    [Header("Visibility")]
    [Tooltip("Show or hide the orbit path")]
    public bool isVisible = true;

    private LineRenderer lineRenderer;
    private bool needsUpdate = true;

    // ---------------- Lifecycle ----------------

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        ConfigureLineRenderer();
    }

    void Start()
    {
        UpdateOrbitVisualization();
    }

    void LateUpdate()
    {
        // Update visualization if parameters changed or requested
        if (needsUpdate)
        {
            UpdateOrbitVisualization();
            needsUpdate = false;
        }

        // Update visibility
        lineRenderer.enabled = isVisible;
    }

    // ---------------- Public API ----------------

    /// <summary>
    /// Set circular orbit parameters
    /// </summary>
    public void SetCircularOrbit(float radius, float inclinationDeg = 0f)
    {
        orbitRadius = radius;
        eccentricity = 0f;
        inclination = inclinationDeg;
        argumentOfPeriapsis = 0f;
        needsUpdate = true;
    }

    /// <summary>
    /// Set elliptical orbit parameters
    /// </summary>
    public void SetEllipticalOrbit(float semiMajorAxis, float ecc, float inclinationDeg = 0f, float argPeriapsisDeg = 0f)
    {
        orbitRadius = semiMajorAxis;
        eccentricity = Mathf.Clamp(ecc, 0f, 0.99f);
        inclination = inclinationDeg;
        argumentOfPeriapsis = argPeriapsisDeg;
        needsUpdate = true;
    }

    /// <summary>
    /// Clear the orbit visualization
    /// </summary>
    public void ClearOrbit()
    {
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
            lineRenderer.enabled = false;
        }
    }

    /// <summary>
    /// Show the orbit visualization
    /// </summary>
    public void Show()
    {
        isVisible = true;
        lineRenderer.enabled = true;
    }

    /// <summary>
    /// Hide the orbit visualization
    /// </summary>
    public void Hide()
    {
        isVisible = false;
        lineRenderer.enabled = false;
    }

    /// <summary>
    /// Force immediate update of visualization
    /// </summary>
    public void ForceUpdate()
    {
        UpdateOrbitVisualization();
    }

    // ---------------- Internal Methods ----------------

    private void ConfigureLineRenderer()
    {
        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = true; // Orbits are closed loops

        // Set line width
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        // Set color
        lineRenderer.startColor = orbitColor;
        lineRenderer.endColor = orbitColor;

        // Material setup (use default material or assign custom)
        if (lineRenderer.material == null)
        {
            // Use Unity's built-in sprite default material for nice looking lines
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        }

        // Disable shadows
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;

        // Enable alignment for better appearance
        lineRenderer.alignment = LineAlignment.View;
    }

    private void UpdateOrbitVisualization()
    {
        if (orbitCenter == null)
        {
            Debug.LogWarning("[OrbitVisualizer] No orbit center assigned, cannot draw orbit");
            return;
        }

        if (orbitRadius <= 0f)
        {
            Debug.LogWarning("[OrbitVisualizer] Orbit radius must be positive");
            return;
        }

        // Set number of points
        lineRenderer.positionCount = pathResolution;

        // Calculate orbital plane rotation
        Quaternion orbitalPlaneRotation = CalculateOrbitalPlaneRotation();

        // Generate orbit points
        for (int i = 0; i < pathResolution; i++)
        {
            float angle = (float)i / pathResolution * 2f * Mathf.PI;
            Vector3 pointInOrbitalPlane = CalculateOrbitalPoint(angle);

            // Rotate to correct inclination and transform to world space
            Vector3 worldPoint = orbitCenter.position + orbitalPlaneRotation * pointInOrbitalPlane;

            lineRenderer.SetPosition(i, worldPoint);
        }
    }

    /// <summary>
    /// Calculate rotation from equatorial plane to inclined orbital plane
    /// </summary>
    private Quaternion CalculateOrbitalPlaneRotation()
    {
        // Rotate around X-axis for inclination (assuming Y-up coordinate system)
        // This tilts the orbital plane from equatorial (XZ) to inclined
        return Quaternion.Euler(inclination, 0f, 0f);
    }

    /// <summary>
    /// Calculate position on orbital ellipse/circle at given angle
    /// </summary>
    private Vector3 CalculateOrbitalPoint(float trueAnomaly)
    {
        // For circular orbit (e = 0), this simplifies to a circle
        // For elliptical orbit (e > 0), uses proper ellipse equation

        // Orbital equation: r = a(1 - e²) / (1 + e*cos(θ))
        // where a = semi-major axis, e = eccentricity, θ = true anomaly
        float radius = orbitRadius * (1f - eccentricity * eccentricity) / (1f + eccentricity * Mathf.Cos(trueAnomaly));

        // Position in orbital plane (XZ plane before inclination rotation)
        float x = radius * Mathf.Cos(trueAnomaly + argumentOfPeriapsis * Mathf.Deg2Rad);
        float z = radius * Mathf.Sin(trueAnomaly + argumentOfPeriapsis * Mathf.Deg2Rad);

        return new Vector3(x, 0f, z);
    }

    // ---------------- Inspector Validation ----------------

    void OnValidate()
    {
        // Clamp values when changed in Inspector
        eccentricity = Mathf.Clamp(eccentricity, 0f, 0.99f);
        inclination = Mathf.Clamp(inclination, 0f, 180f);
        argumentOfPeriapsis = Mathf.Clamp(argumentOfPeriapsis, 0f, 360f);
        pathResolution = Mathf.Max(16, pathResolution); // Minimum 16 points
        orbitRadius = Mathf.Max(0.1f, orbitRadius);

        // Request update when values change in editor
        needsUpdate = true;
    }

    // ---------------- Debugging ----------------

    void OnDrawGizmosSelected()
    {
        if (orbitCenter == null) return;

        // Draw center point
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(orbitCenter.position, 0.1f);

        // Draw periapsis and apoapsis for elliptical orbits
        if (eccentricity > 0.01f)
        {
            float periapsis = orbitRadius * (1f - eccentricity);
            float apoapsis = orbitRadius * (1f + eccentricity);

            Quaternion rotation = CalculateOrbitalPlaneRotation();

            // Periapsis (closest point)
            Vector3 periapsisDir = rotation * (Vector3.right * Mathf.Cos(argumentOfPeriapsis * Mathf.Deg2Rad) +
                                                Vector3.forward * Mathf.Sin(argumentOfPeriapsis * Mathf.Deg2Rad));
            Vector3 periapsisPos = orbitCenter.position + periapsisDir * periapsis;
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(periapsisPos, 0.15f);

            // Apoapsis (farthest point)
            Vector3 apoapsisPos = orbitCenter.position - periapsisDir * apoapsis;
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(apoapsisPos, 0.15f);
        }
    }
}
