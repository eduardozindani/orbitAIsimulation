using UnityEngine;
using UnityEditor;
using Core.Config;

/// <summary>
/// Editor utility to create ElevenLabsSettings asset.
/// This will run once and create the asset, then you can delete this file.
/// </summary>
public class CreateElevenLabsSettings
{
    [MenuItem("Tools/Create ElevenLabs Settings Asset")]
    public static void CreateAsset()
    {
        // Check if asset already exists
        string assetPath = "Assets/Resources/Agents/ElevenLabsSettings.asset";
        var existingAsset = AssetDatabase.LoadAssetAtPath<ElevenLabsSettings>(assetPath);

        if (existingAsset != null)
        {
            Debug.Log("[CreateElevenLabsSettings] Asset already exists at: " + assetPath);
            Selection.activeObject = existingAsset;
            EditorGUIUtility.PingObject(existingAsset);
            return;
        }

        // Create new instance
        ElevenLabsSettings settings = ScriptableObject.CreateInstance<ElevenLabsSettings>();

        // Set default values (they should already be set in the class, but just in case)
        settings.apiKey = "sk_3ec3c03f00fce191107dd999afd752104d169cb833b57d24";
        settings.voiceId = "NOpBlnGInO9m6vDvFkFC";
        settings.modelId = "eleven_flash_v2_5";
        settings.stability = 0.7f;
        settings.similarityBoost = 0.8f;
        settings.baseUrl = "https://api.elevenlabs.io/v1";

        // Create asset
        AssetDatabase.CreateAsset(settings, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Select and ping the asset
        Selection.activeObject = settings;
        EditorGUIUtility.PingObject(settings);

        Debug.Log("[CreateElevenLabsSettings] Successfully created ElevenLabsSettings asset at: " + assetPath);
    }
}
