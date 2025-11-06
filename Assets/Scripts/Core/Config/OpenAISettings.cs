using UnityEngine;
using System;

namespace Core.Config
{
    [CreateAssetMenu(fileName = "OpenAISettings", menuName = "Config/OpenAI Settings")]
    public class OpenAISettings : ScriptableObject
{
    [Header("Auth")]
    [Tooltip("API key - will be loaded from environment variable OPENAI_API_KEY if empty")]
    public string ApiKey;

    [Header("Endpoint")]
    public string BaseUrl = "https://api.openai.com/v1";

    [Header("Model")]
    [Tooltip("Default model for simple text tests.")]
    public string Model = "gpt-4.1";

    /// <summary>
    /// Gets the API key, preferring environment variable if asset field is empty.
    /// Priority: 1) This asset's ApiKey field, 2) OPENAI_API_KEY environment variable
    /// </summary>
    public string GetApiKey()
    {
        if (!string.IsNullOrWhiteSpace(ApiKey))
            return ApiKey;

        string envKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (!string.IsNullOrWhiteSpace(envKey))
            return envKey;

        Debug.LogWarning("OpenAI API Key not found! Set it in the asset or OPENAI_API_KEY environment variable.");
        return string.Empty;
    }

    /// <summary>
    /// Convenience: trim trailing slashes to avoid double-slash URLs.
    /// </summary>
    public string NormalizedBaseUrl => string.IsNullOrWhiteSpace(BaseUrl)
        ? "https://api.openai.com/v1"
        : BaseUrl.TrimEnd('/');
}
}
