using UnityEngine;

public class Orbit : MonoBehaviour
{
    public Transform planet;
    public float orbitRadius = 5.32f; // ISS altitude: ~408km above Earth (5 + 408*0.000785)
    public float orbitSpeed = 0.00119f; // ISS speed: ~7.66 km/s converted to rad/s
    public float zRotationOffset = 0f; // Z-axis roll offset for satellite orientation
    public float startingAngle = 0f; // Starting position on orbit (in degrees, 0-360)

    private float angle;
    private TrailRenderer trailRenderer;

    void Awake()
    {
        // Cache TrailRenderer reference if it exists
        trailRenderer = GetComponent<TrailRenderer>();

        // Initialize angle from starting angle (convert degrees to radians)
        angle = startingAngle * Mathf.Deg2Rad;

        // CRITICAL: Set position BEFORE TrailRenderer starts recording
        // This must happen in Awake() to prevent trail from default position
        if (planet != null)
        {
            float x = Mathf.Cos(angle) * orbitRadius;
            float z = Mathf.Sin(angle) * orbitRadius;
            transform.position = planet.position + new Vector3(x, 0f, z);
        }
    }

    void Start()
    {
        // Clear any trail remnants after position is set
        // This ensures we start with a clean trail
        if (trailRenderer != null)
        {
            // Disable and re-enable to force complete reset
            trailRenderer.enabled = false;
            trailRenderer.Clear();
            trailRenderer.enabled = true;
        }
    }

    void Update()
    {
        if (planet == null) return;

        angle += orbitSpeed * Time.deltaTime;
        float x = Mathf.Cos(angle) * orbitRadius;
        float z = Mathf.Sin(angle) * orbitRadius;

        Vector3 newPosition = planet.position + new Vector3(x, 0f, z);

        // Calculate velocity direction (tangent to orbit)
        Vector3 velocity = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle));

        // Point satellite along velocity (flies "forward") with optional Z roll offset
        if (velocity.magnitude > 0.001f)
        {
            Quaternion forwardRotation = Quaternion.LookRotation(velocity, Vector3.up);
            Quaternion rollOffset = Quaternion.Euler(0f, 0f, zRotationOffset);
            transform.rotation = forwardRotation * rollOffset;
        }

        transform.position = newPosition;
    }

    /// <summary>
    /// Resets the satellite to the starting position on the orbit and clears the trail.
    /// Call this when creating a new orbit to prevent trail artifacts.
    /// </summary>
    public void ResetPosition()
    {
        angle = startingAngle * Mathf.Deg2Rad;

        // Immediately update position (don't wait for next Update)
        if (planet != null)
        {
            float x = Mathf.Cos(angle) * orbitRadius;
            float z = Mathf.Sin(angle) * orbitRadius;
            transform.position = planet.position + new Vector3(x, 0f, z);
        }

        // Clear trail renderer if present
        if (trailRenderer != null)
        {
            trailRenderer.Clear();
        }
    }

    /// <summary>
    /// Enable or disable orbital motion.
    /// When disabled, satellite stops moving and trail is cleared.
    /// Used by ExperienceManager to disable orbit during intro cutscene.
    /// </summary>
    public void SetOrbitActive(bool active)
    {
        enabled = active; // Disables/enables the Update() loop

        if (!active)
        {
            // Clear trail when disabling orbit
            if (trailRenderer != null)
            {
                trailRenderer.Clear();
            }
        }
    }

}