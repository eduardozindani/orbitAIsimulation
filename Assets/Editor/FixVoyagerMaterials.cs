using UnityEngine;
using UnityEditor;

public class FixVoyagerMaterials : EditorWindow
{
    [MenuItem("Tools/Fix Voyager Materials")]
    static void FixMaterials()
    {
        Debug.Log("=== FIXING VOYAGER MATERIALS ===");
        
        string materialsPath = "Assets/Models/voyager-nasa/source/Materials";
        string texturesPath = "Assets/Models/voyager-nasa/textures";
        
        // Load textures
        Texture2D group1Diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>($"{texturesPath}/VoyagerFBX_group1_Diffuse.png");
        Texture2D group1Normal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{texturesPath}/VoyagerFBX_group1_Normal.png");
        Texture2D group1Glossiness = AssetDatabase.LoadAssetAtPath<Texture2D>($"{texturesPath}/VoyagerFBX_group1_Glossiness.png");
        
        Texture2D group2Diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>($"{texturesPath}/VoyagerFBX_group2_Diffuse.png");
        Texture2D group2Normal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{texturesPath}/VoyagerFBX_group2_Normal.png");
        Texture2D group2Glossiness = AssetDatabase.LoadAssetAtPath<Texture2D>($"{texturesPath}/VoyagerFBX_group2_Glossiness.png");
        
        // Configure normal maps
        ConfigureAsNormalMap($"{texturesPath}/VoyagerFBX_group1_Normal.png");
        ConfigureAsNormalMap($"{texturesPath}/VoyagerFBX_group2_Normal.png");
        
        // Fix Group 1 Material
        Material group1Mat = AssetDatabase.LoadAssetAtPath<Material>($"{materialsPath}/voyager_group_1.mat");
        if (group1Mat != null)
        {
            Debug.Log("Fixing voyager_group_1 material...");
            
            // Set Base Color to white (to show textures properly)
            group1Mat.SetColor("_BaseColor", Color.white);
            group1Mat.SetColor("_Color", Color.white);
            
            // Assign textures
            group1Mat.SetTexture("_BaseMap", group1Diffuse);
            group1Mat.SetTexture("_MainTex", group1Diffuse);
            
            if (group1Normal != null)
            {
                group1Mat.SetTexture("_BumpMap", group1Normal);
                group1Mat.SetFloat("_BumpScale", 1.0f);
                group1Mat.EnableKeyword("_NORMALMAP");
            }
            
            // Set metallic properties for spacecraft look
            group1Mat.SetFloat("_Metallic", 0.6f);
            group1Mat.SetFloat("_Smoothness", 0.5f);
            
            EditorUtility.SetDirty(group1Mat);
            Debug.Log("✓ Fixed voyager_group_1");
        }
        
        // Fix Group 2 Material
        Material group2Mat = AssetDatabase.LoadAssetAtPath<Material>($"{materialsPath}/voyager_group_2.mat");
        if (group2Mat != null)
        {
            Debug.Log("Fixing voyager_group_2 material...");
            
            // Set Base Color to white
            group2Mat.SetColor("_BaseColor", Color.white);
            group2Mat.SetColor("_Color", Color.white);
            
            // Assign textures
            group2Mat.SetTexture("_BaseMap", group2Diffuse);
            group2Mat.SetTexture("_MainTex", group2Diffuse);
            
            if (group2Normal != null)
            {
                group2Mat.SetTexture("_BumpMap", group2Normal);
                group2Mat.SetFloat("_BumpScale", 1.0f);
                group2Mat.EnableKeyword("_NORMALMAP");
            }
            
            // Set metallic properties
            group2Mat.SetFloat("_Metallic", 0.6f);
            group2Mat.SetFloat("_Smoothness", 0.5f);
            
            EditorUtility.SetDirty(group2Mat);
            Debug.Log("✓ Fixed voyager_group_2");
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("=== SUCCESS ===");
        Debug.Log("Voyager materials fixed! The spacecraft should now display with proper textures.");
        
        EditorUtility.DisplayDialog("Voyager Materials Fixed",
            "Successfully fixed Voyager materials!\nTextures have been applied and materials configured.",
            "OK");
    }
    
    static void ConfigureAsNormalMap(string texturePath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.NormalMap)
        {
            importer.textureType = TextureImporterType.NormalMap;
            AssetDatabase.WriteImportSettingsIfDirty(texturePath);
            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);
            Debug.Log($"✓ Configured {System.IO.Path.GetFileName(texturePath)} as normal map");
        }
    }
}

