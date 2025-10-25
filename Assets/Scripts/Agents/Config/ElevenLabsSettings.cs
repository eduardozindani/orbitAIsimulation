using UnityEngine;

/// <summary>
/// Configuration for ElevenLabs text-to-speech API.
/// Stores API credentials and voice settings for audio response generation.
/// </summary>
[CreateAssetMenu(fileName = "ElevenLabsSettings", menuName = "Agents/ElevenLabs Settings", order = 2)]
public class ElevenLabsSettings : ScriptableObject
{
    [Header("API Configuration")]
    [Tooltip("ElevenLabs API key")]
    public string apiKey = "sk_3ec3c03f00fce191107dd999afd752104d169cb833b57d24";

    [Tooltip("Voice ID for text-to-speech (Mission Control voice)")]
    public string voiceId = "NOpBlnGInO9m6vDvFkFC";

    [Tooltip("Model to use for generation (eleven_flash_v2_5 is fastest)")]
    public string modelId = "eleven_flash_v2_5";

    [Header("Voice Settings")]
    [Tooltip("Stability (0-1): Higher = more consistent, less variation")]
    [Range(0f, 1f)]
    public float stability = 0.7f;

    [Tooltip("Similarity Boost (0-1): Higher = closer to training voice")]
    [Range(0f, 1f)]
    public float similarityBoost = 0.8f;

    [Header("Advanced")]
    [Tooltip("API endpoint base URL")]
    public string baseUrl = "https://api.elevenlabs.io/v1";
}
