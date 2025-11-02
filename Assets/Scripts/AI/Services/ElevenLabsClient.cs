using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Core.Config;

namespace AI.Services
{
    /// <summary>
    /// Client for ElevenLabs text-to-speech and speech-to-text APIs.
    /// Converts text responses into audio and transcribes voice input using Scribe v2.
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
                        similarity_boost = _settings.similarityBoost,
                        speed = _settings.speed
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

        /// <summary>
        /// Convert speech audio to text using ElevenLabs Scribe v2 API
        /// </summary>
        /// <param name="audioClip">AudioClip containing speech to transcribe</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Transcribed text if successful, null if failed</returns>
        public async Task<string> SpeechToTextAsync(AudioClip audioClip, CancellationToken cancellationToken = default)
        {
            if (audioClip == null)
            {
                Debug.LogError("[ElevenLabsClient] No audio clip provided for transcription");
                return null;
            }

            try
            {
                Debug.Log($"[ElevenLabsClient] Starting speech-to-text for clip: {audioClip.length}s, {audioClip.frequency}Hz");

                // Convert AudioClip to WAV bytes
                byte[] wavData = ConvertAudioClipToWav(audioClip);
                if (wavData == null || wavData.Length == 0)
                {
                    Debug.LogError("[ElevenLabsClient] Failed to convert AudioClip to WAV");
                    return null;
                }

                Debug.Log($"[ElevenLabsClient] WAV data size: {wavData.Length} bytes");

                // Build request URL for Scribe v2
                // The speech-to-text endpoint
                string url = $"{_settings.baseUrl}/speech-to-text";

                // Create form data with the audio file
                var form = new WWWForm();
                // Send as "file" field - this is what most APIs expect
                form.AddBinaryData("file", wavData, "recording.wav", "audio/wav");
                // Add model_id parameter - using scribe v1 as per logs
                form.AddField("model_id", "scribe_v1");

                // Create UnityWebRequest
                using (UnityWebRequest request = UnityWebRequest.Post(url, form))
                {
                    // Add API key header
                    request.SetRequestHeader("xi-api-key", _settings.apiKey);

                    Debug.Log("[ElevenLabsClient] Sending STT request to Scribe v2...");

                    // Send request
                    var operation = request.SendWebRequest();

                    // Wait for completion
                    while (!operation.isDone)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            request.Abort();
                            Debug.Log("[ElevenLabsClient] STT request cancelled");
                            return null;
                        }
                        await Task.Yield();
                    }

                    // Check for errors
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"[ElevenLabsClient] STT request failed: {request.error}");
                        Debug.LogError($"[ElevenLabsClient] Response code: {request.responseCode}");
                        if (!string.IsNullOrEmpty(request.downloadHandler.text))
                        {
                            Debug.LogError($"[ElevenLabsClient] Response: {request.downloadHandler.text}");
                        }
                        return null;
                    }

                    // Parse response
                    string responseText = request.downloadHandler.text;
                    Debug.Log($"[ElevenLabsClient] STT response: {responseText}");

                    // Parse JSON response to get transcribed text
                    var response = JsonConvert.DeserializeObject<ScribeResponse>(responseText);
                    if (response != null && !string.IsNullOrEmpty(response.text))
                    {
                        Debug.Log($"[ElevenLabsClient] Transcribed text: {response.text}");
                        return response.text;
                    }
                    else
                    {
                        Debug.LogError("[ElevenLabsClient] No text in transcription response");
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ElevenLabsClient] Exception during STT: {ex.Message}");
                Debug.LogError($"[ElevenLabsClient] Stack trace: {ex.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// Convert Unity AudioClip to WAV byte array
        /// </summary>
        private byte[] ConvertAudioClipToWav(AudioClip clip)
        {
            if (clip == null) return null;

            // Get audio data from clip
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            // Convert to 16-bit PCM
            short[] intData = new short[samples.Length];
            for (int i = 0; i < samples.Length; i++)
            {
                intData[i] = (short)(samples[i] * 32767f);
            }

            // Create WAV file in memory
            byte[] wavData;
            using (var memoryStream = new System.IO.MemoryStream())
            using (var writer = new System.IO.BinaryWriter(memoryStream))
            {
                // WAV header
                writer.Write(new char[4] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + intData.Length * 2); // File size
                writer.Write(new char[4] { 'W', 'A', 'V', 'E' });
                writer.Write(new char[4] { 'f', 'm', 't', ' ' });
                writer.Write(16); // Subchunk1 size (16 for PCM)
                writer.Write((short)1); // Audio format (1 for PCM)
                writer.Write((short)clip.channels);
                writer.Write(clip.frequency);
                writer.Write(clip.frequency * clip.channels * 2); // Byte rate
                writer.Write((short)(clip.channels * 2)); // Block align
                writer.Write((short)16); // Bits per sample
                writer.Write(new char[4] { 'd', 'a', 't', 'a' });
                writer.Write(intData.Length * 2); // Data size

                // Write audio data
                foreach (short value in intData)
                {
                    writer.Write(value);
                }

                writer.Flush();
                wavData = memoryStream.ToArray();
            }

            return wavData;
        }

        /// <summary>
        /// Response structure from Scribe API
        /// </summary>
        [System.Serializable]
        private class ScribeResponse
        {
            public string text;
            public float confidence;
        }
    }
}
