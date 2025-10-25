using UnityEngine;

public class Orbit : MonoBehaviour
{
    public Transform planet;
    public float orbitRadius = 5.32f; // ISS altitude: ~408km above Earth (5 + 408*0.000785)
    public float orbitSpeed = 0.00119f; // ISS speed: ~7.66 km/s converted to rad/s

    private float angle;
    private TrailRenderer trailRenderer;

    void Awake()
    {
        // Cache TrailRenderer reference if it exists
        trailRenderer = GetComponent<TrailRenderer>();

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

        transform.position = planet.position + new Vector3(x, 0f, z);
        transform.LookAt(planet);
    }

    /// <summary>
    /// Resets the satellite to the starting position on the orbit and clears the trail.
    /// Call this when creating a new orbit to prevent trail artifacts.
    /// </summary>
    public void ResetPosition()
    {
        angle = 0f;

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