using UnityEngine;

namespace Core.Config
{
    [CreateAssetMenu(fileName = "OpenAISettings", menuName = "Config/OpenAI Settings")]
    public class OpenAISettings : ScriptableObject
{
    [Header("Auth")]
    [Tooltip("Paste your API key here for local testing ONLY. Do not commit this asset.")]
    public string ApiKey;

    [Header("Endpoint")]
    public string BaseUrl = "https://api.openai.com/v1";

    [Header("Model")]
    [Tooltip("Default model for simple text tests.")]
    public string Model = "gpt-4.1";

    /// <summary>
    /// Convenience: trim trailing slashes to avoid double-slash URLs.
    /// </summary>
    public string NormalizedBaseUrl => string.IsNullOrWhiteSpace(BaseUrl)
        ? "https://api.openai.com/v1"
        : BaseUrl.TrimEnd('/');
}
}
