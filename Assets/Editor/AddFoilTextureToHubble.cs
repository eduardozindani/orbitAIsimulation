using UnityEngine;
using UnityEditor;
using System.IO;

public class AddFoilTextureToHubble : MonoBehaviour
{
    [MenuItem("Tools/Add Foil Texture to Hubble")]
    static void AddFoilTexture()
    {
        string materialsPath = "Assets/Models/Hubble/source/Materials";
        string foilTexturePath = "Assets/Models/Hubble/textures/bump_foil.jpg";
        
        Debug.Log("=== ADDING FOIL TEXTURE TO HUBBLE ===");
        
        // Load the foil texture
        Texture2D foilTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(foilTexturePath);
        if (foilTexture == null)
        {
            Debug.LogError($"Could not find foil texture at {foilTexturePath}");
            return;
        }
        
        // Configure texture as normal map
        TextureImporter texImporter = AssetImporter.GetAtPath(foilTexturePath) as TextureImporter;
        if (texImporter != null)
        {
            texImporter.textureType = TextureImporterType.NormalMap;
            AssetDatabase.WriteImportSettingsIfDirty(foilTexturePath);
            AssetDatabase.ImportAsset(foilTexturePath, ImportAssetOptions.ForceUpdate);
            Debug.Log("✓ Configured bump_foil.jpg as normal map");
        }
        
        // Find all materials
        string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { materialsPath });
        int updatedCount = 0;
        
        foreach (string guid in materialGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            
            if (mat != null)
            {
                string matName = mat.name;
                
                // Apply foil texture to body/metal materials, but NOT solar panels or black plastic
                if (!matName.Contains("Solar Panel") && 
                    !matName.Contains("Black Plastic"))
                {
                    // Add the foil normal map
                    mat.SetTexture("_BumpMap", foilTexture);
                    mat.SetFloat("_BumpScale", 0.3f); // Subtle bump
                    mat.EnableKeyword("_NORMALMAP");
                    
                    // Enhance metallic properties for foil look
                    mat.SetFloat("_Metallic", 0.7f);
                    mat.SetFloat("_Smoothness", 0.4f);
                    
                    EditorUtility.SetDirty(mat);
                    updatedCount++;
                    Debug.Log($"✓ Added foil texture to: {matName}");
                }
                else
                {
                    Debug.Log($"  Skipped: {matName} (not a foil material)");
                }
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"=== SUCCESS: Updated {updatedCount} material(s) ===");
        Debug.Log("Check the Hubble scene - you should now see the foil texture!");
    }
}

