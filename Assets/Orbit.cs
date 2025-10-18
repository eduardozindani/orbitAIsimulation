using UnityEngine;

public class Orbit : MonoBehaviour
{
    public Transform planet;
    public float orbitRadius = 5.32f; // ISS altitude: ~408km above Earth (5 + 408*0.000785)
    public float orbitSpeed = 0.00119f; // ISS speed: ~7.66 km/s converted to rad/s

    private float angle;

    void Update()
    {
        if (planet == null) return;

        angle += orbitSpeed * Time.deltaTime;
        float x = Mathf.Cos(angle) * orbitRadius;
        float z = Mathf.Sin(angle) * orbitRadius;

        transform.position = planet.position + new Vector3(x, 0f, z);
        transform.LookAt(planet);
    }
    
}