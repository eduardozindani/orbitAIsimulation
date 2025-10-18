using UnityEngine;
using UnityEngine.UI;         // for Slider
using TMPro;                  // for TMP_Text

/// <summary>
/// Connects Radius & Speed sliders to the Orbit.cs component and shows
/// live read-outs using TextMeshPro labels.
/// Attach to Canvas; wire all references in Inspector.
/// </summary>
public class SatelliteUIController : MonoBehaviour
{
    [Header("Scene Links – assign in Inspector")]     
    public Orbit  orbit;              // drag SatelliteRoot (has Orbit.cs)
    public Slider radiusSlider;       // drag RadiusSlider
    public Slider speedSlider;        // drag SpeedSlider

    [Header("Value Labels (TextMeshPro)")]            
    public TMP_Text radiusLabel;      // drag TMP_Text child next to radius slider
    public TMP_Text speedLabel;       // drag TMP_Text child next to speed slider

    void Start()
    {
        // safety
        if (!orbit || !radiusSlider || !speedSlider)
        {
            Debug.LogError("SatelliteUIController: missing Orbit or Slider reference.");
            enabled = false;
            return;
        }

        // init UI
        radiusSlider.value = orbit.orbitRadius;
        speedSlider.value  = orbit.orbitSpeed;
        UpdateRadiusLabel(radiusSlider.value);
        UpdateSpeedLabel (speedSlider.value );

        // subscribe
        radiusSlider.onValueChanged.AddListener(SetRadius);
        speedSlider.onValueChanged.AddListener(SetSpeed);
    }

    /* ---------- slider callbacks ---------- */
    void SetRadius(float r) { orbit.orbitRadius = r;  UpdateRadiusLabel(r); }
    void SetSpeed (float s) { orbit.orbitSpeed  = s;  UpdateSpeedLabel (s); }

    /* ---------- label helpers ---------- */
    void UpdateRadiusLabel(float r)
    {
        if (radiusLabel)
            radiusLabel.text = $"Radius: {r:0.0}";   // one decimal
    }

    void UpdateSpeedLabel(float s)
    {
        if (speedLabel)
            speedLabel.text = $"Speed: {s:0.0}°/s";
    }

    void OnDestroy()
    {
        // tidy up listeners
        radiusSlider?.onValueChanged.RemoveListener(SetRadius);
        speedSlider ?.onValueChanged.RemoveListener(SetSpeed );
    }
}