using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class PlanetRotation : MonoBehaviour
{
    public float rotationSpeed = 0.00417f;     // degrees per second (realistic 24-hour day)
    private float rotY;                        // current angle
    Material mat;                              // cached material

    void Awake()
    {
        mat = GetComponent<MeshRenderer>().material;
    }

    void Update()
    {
        rotY = (rotY + rotationSpeed * Time.deltaTime) % 360f;
        transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
        mat.SetFloat("_RotationDeg", rotY);
    }
}