using UnityEngine;

public class Orbit : MonoBehaviour
{
    public Transform planet;
    public float orbitRadius = 3f;
    public float orbitSpeed = 2f;

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