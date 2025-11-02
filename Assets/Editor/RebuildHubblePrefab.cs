using UnityEngine;
using UnityEditor;

public class RebuildHubblePrefab : MonoBehaviour
{
    [MenuItem("Tools/Rebuild Hubble Prefab (Clean)")]
    static void RebuildPrefab()
    {
        string fbxPath = "Assets/Models/Hubble/source/hubble123.fbx";
        string prefabPath = "Assets/Prefabs/Models/Hubble.prefab";
        
        Debug.Log("=== REBUILDING HUBBLE PREFAB ===");
        
        // Step 1: Configure FBX to extract materials
        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer != null)
        {
            Debug.Log("Step 1: Configuring FBX import settings...");
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.External;
            importer.materialName = ModelImporterMaterialName.BasedOnMaterialName;
            importer.materialSearch = ModelImporterMaterialSearch.Local;
            
            AssetDatabase.WriteImportSettingsIfDirty(fbxPath);
            AssetDatabase.ImportAsset(fbxPath, ImportAssetOptions.ForceUpdate);
            Debug.Log("✓ FBX configured to extract materials");
        }
        
        // Step 2: Load the FBX as a GameObject
        GameObject fbxInstance = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbxInstance == null)
        {
            Debug.LogError("Could not load FBX!");
            return;
        }
        
        // Step 3: Create a new prefab instance
        Debug.Log("Step 2: Creating clean prefab...");
        GameObject prefabInstance = (GameObject)PrefabUtility.InstantiatePrefab(fbxInstance);
        prefabInstance.name = "Hubble";
        prefabInstance.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
        
        // Step 4: Save as prefab (this will have NO material overrides)
        PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabPath);
        Debug.Log($"✓ Clean prefab saved to: {prefabPath}");
        
        // Step 5: Cleanup
        DestroyImmediate(prefabInstance);
        
        Debug.Log("=== SUCCESS ===");
        Debug.Log("The Hubble prefab now uses the FBX's embedded materials.");
        Debug.Log("Check the Hubble scene!");
    }
}

