using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace Agents.Services
{
    /// <summary>
    /// Client for ElevenLabs text-to-speech API.
    /// Converts text responses into audio using Mission Control's voice.
    /// </summary>
    public class ElevenLabsClient
    {
        private readonly ElevenLabsSettings _settings;

        public ElevenLabsClient(ElevenLabsSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// Convert text to speech audio using ElevenLabs API
        /// </summary>
        /// <param name="text">Text to convert to speech</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>AudioClip if successful, null if failed</returns>
        public async Task<AudioClip> TextToSpeechAsync(string text, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                Debug.LogWarning("[ElevenLabsClient] Empty text provided, skipping TTS");
                return null;
            }

            try
            {
                // Build request URL
                string url = $"{_settings.baseUrl}/text-to-speech/{_settings.voiceId}";

                // Build request body
                var requestBody = new
                {
                    text = text,
                    model_id = _settings.modelId,
                    voice_settings = new
                    {
                        stability = _settings.stability,
                        similarity_boost = _settings.similarityBoost
                    }
                };

                string jsonBody = JsonConvert.SerializeObject(requestBody);
                byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);

                // Create UnityWebRequest
                using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
                {
                    request.uploadHandler = new UploadHandlerRaw(bodyBytes);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.SetRequestHeader("xi-api-key", _settings.apiKey);

                    Debug.Log($"[ElevenLabsClient] Sending TTS request: {text.Substring(0, Math.Min(50, text.Length))}...");

                    // Send request
                    var operation = request.SendWebRequest();

                    // Wait for completion (with cancellation support)
                    while (!operation.isDone)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            request.Abort();
                            Debug.Log("[ElevenLabsClient] Request cancelled");
                            return null;
                        }
                        await Task.Yield();
                    }

                    // Check for errors
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"[ElevenLabsClient] API request failed: {request.error}");
                        Debug.LogError($"[ElevenLabsClient] Response code: {request.responseCode}");
                        if (!string.IsNullOrEmpty(request.downloadHandler.text))
                        {
                            Debug.LogError($"[ElevenLabsClient] Response body: {request.downloadHandler.text}");
                        }
                        return null;
                    }

                    // Get audio data
                    byte[] audioData = request.downloadHandler.data;

                    if (audioData == null || audioData.Length == 0)
                    {
                        Debug.LogError("[ElevenLabsClient] No audio data received");
                        return null;
                    }

                    Debug.Log($"[ElevenLabsClient] Received {audioData.Length} bytes of audio data");

                    // Convert MP3 bytes to AudioClip
                    AudioClip audioClip = await ConvertMp3ToAudioClipAsync(audioData);

                    if (audioClip != null)
                    {
                        Debug.Log($"[ElevenLabsClient] Successfully created AudioClip (length: {audioClip.length}s)");
                    }
                    else
                    {
                        Debug.LogError("[ElevenLabsClient] Failed to convert audio data to AudioClip");
                    }

                    return audioClip;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ElevenLabsClient] Exception during TTS: {ex.Message}");
                Debug.LogError($"[ElevenLabsClient] Stack trace: {ex.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// Convert MP3 bytes to Unity AudioClip
        /// Uses Unity's built-in audio loading system
        /// </summary>
        private async Task<AudioClip> ConvertMp3ToAudioClipAsync(byte[] mp3Data)
        {
            try
            {
                // Create a temporary file to load the MP3
                // Unity's AudioClip.LoadAudioData doesn't work directly with MP3 bytes in all cases
                // so we use a more reliable approach

                // Use Unity's multimedia type loading
                // Create AudioClip from MP3 data using Unity's internal decoder
                AudioClip clip = await LoadAudioClipFromBytesAsync(mp3Data, AudioType.MPEG);

                return clip;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ElevenLabsClient] Error converting MP3 to AudioClip: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Load AudioClip from byte array using Unity's multimedia loading
        /// </summary>
        private async Task<AudioClip> LoadAudioClipFromBytesAsync(byte[] audioData, AudioType audioType)
        {
            // Write to temporary file
            string tempPath = System.IO.Path.Combine(Application.temporaryCachePath, $"tts_{Guid.NewGuid()}.mp3");

            try
            {
                System.IO.File.WriteAllBytes(tempPath, audioData);

                // Load using UnityWebRequestMultimedia
                using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip($"file://{tempPath}", audioType))
                {
                    // Set to stream to avoid loading entire file into memory
                    ((DownloadHandlerAudioClip)request.downloadHandler).streamAudio = true;

                    var operation = request.SendWebRequest();

                    // Wait for completion
                    while (!operation.isDone)
                    {
                        await Task.Yield();
                    }

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"[ElevenLabsClient] Failed to load audio clip: {request.error}");
                        return null;
                    }

                    AudioClip clip = DownloadHandlerAudioClip.GetContent(request);

                    if (clip == null)
                    {
                        Debug.LogError("[ElevenLabsClient] AudioClip is null after download");
                        return null;
                    }

                    // Set a name for debugging
                    clip.name = "TTS_Response";

                    return clip;
                }
            }
            finally
            {
                // Clean up temp file
                if (System.IO.File.Exists(tempPath))
                {
                    try
                    {
                        System.IO.File.Delete(tempPath);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }
        }
    }
}
