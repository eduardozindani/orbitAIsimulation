using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Core.Config;

public class OpenAIClient
{
    private readonly OpenAISettings _settings;

    public OpenAIClient(OpenAISettings settings)
    {
        if (settings == null) throw new ArgumentNullException(nameof(settings));
        _settings = settings;
    }

    /// <summary>
    /// Primary overload: text -> text roundtrip via OpenAI Responses API,
    /// with optional system/instructions string.
    /// POST { baseUrl }/responses
    /// Body: { "model": "...", "input": "...", "instructions": "..." }
    /// Returns: output_text (if present) or concatenated output[].content[].text
    /// </summary>
    public async Task<string> CompleteAsync(string input, string instructions, CancellationToken ct = default)
    {
        string apiKey = _settings.GetApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OpenAI API key is missing. Set it in OpenAISettings asset or OPENAI_API_KEY environment variable.");

        var url = _settings.NormalizedBaseUrl + "/responses";

        // Build payload; include 'instructions' only if provided
        var payloadObj = new JObject
        {
            ["model"] = _settings.Model,
            ["input"] = input ?? string.Empty
        };
        if (!string.IsNullOrWhiteSpace(instructions))
            payloadObj["instructions"] = instructions;

        var json = payloadObj.ToString(Formatting.None);
        var bodyRaw = Encoding.UTF8.GetBytes(json);

        using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", "Bearer " + apiKey);
            req.timeout = 30; // seconds

            var op = req.SendWebRequest();
            while (!op.isDone)
            {
                if (ct.IsCancellationRequested)
                {
                    req.Abort();
                    ct.ThrowIfCancellationRequested();
                }
                await Task.Yield();
            }

            if (req.result != UnityWebRequest.Result.Success)
            {
                string snippet = Truncate(req.downloadHandler != null ? req.downloadHandler.text : "", 600);
                string status = req.responseCode != 0 ? "HTTP " + req.responseCode.ToString() : "Network error";
                throw new Exception(status + ": " + req.error + "\n" + snippet);
            }

            var responseText = req.downloadHandler != null ? req.downloadHandler.text : "";
            return ExtractOutputText(responseText);
        }
    }

    /// <summary>
    /// Backward-compatible overload with no system/instructions.
    /// </summary>
    public Task<string> CompleteAsync(string input, CancellationToken ct = default)
        => CompleteAsync(input, instructions: null, ct);

    /// <summary>
    /// Extract assistant text from Responses API JSON.
    /// Prefers 'output_text'; falls back to concatenating output[].content[].text;
    /// then tries a legacy 'choices[0].message.content' fallback.
    /// </summary>
    private static string ExtractOutputText(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return "(Empty response)";

        try
        {
            var root = JObject.Parse(json);

            // 1) Convenience field if present
            var outputText = (string)root["output_text"];
            if (!string.IsNullOrWhiteSpace(outputText))
                return outputText.Trim();

            // 2) Walk output[].content[].text
            var output = root["output"] as JArray;
            if (output != null)
            {
                var sb = new StringBuilder();
                foreach (var msg in output)
                {
                    if (msg == null) continue;
                    var content = msg["content"] as JArray;
                    if (content == null) continue;

                    foreach (var c in content)
                    {
                        if (c == null) continue;
                        var type = (string)c["type"];
                        if (type == "output_text" || type == "text" || string.IsNullOrEmpty(type))
                        {
                            var text = (string)c["text"];
                            if (!string.IsNullOrWhiteSpace(text))
                                sb.Append(text);
                        }
                    }
                }

                var aggregated = sb.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(aggregated))
                    return aggregated;
            }

            // 3) Legacy-style fallback (shouldn't be used with /responses, but harmless)
            var choices = root["choices"] as JArray;
            var legacy = (string)choices?[0]?["message"]?["content"];
            if (!string.IsNullOrWhiteSpace(legacy))
                return legacy.Trim();

            return "(No text in response)";
        }
        catch
        {
            return "(Failed to parse model response)";
        }
    }

    private static string Truncate(string s, int max)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Length <= max ? s : s.Substring(0, max);
    }
}
