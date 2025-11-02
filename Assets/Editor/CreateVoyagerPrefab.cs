using UnityEngine;
using UnityEditor;

public class CreateVoyagerPrefab : MonoBehaviour
{
    [MenuItem("Tools/Create Voyager Prefab")]
    static void CreatePrefab()
    {
        string fbxPath = "Assets/Models/voyager-nasa/source/VoyagerFBX.fbx";
        string prefabPath = "Assets/Prefabs/Models/Voyager.prefab";
        
        Debug.Log("=== CREATING VOYAGER PREFAB ===");
        
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
        Debug.Log("Step 2: Creating prefab with scale 1.0...");
        GameObject prefabInstance = (GameObject)PrefabUtility.InstantiatePrefab(fbxInstance);
        prefabInstance.name = "Voyager";
        prefabInstance.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        
        // Step 4: Save as prefab
        PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabPath);
        Debug.Log($"✓ Prefab saved to: {prefabPath}");
        
        // Step 5: Get the prefab GUID
        string prefabGuid = AssetDatabase.AssetPathToGUID(prefabPath);
        Debug.Log($"✓ Prefab GUID: {prefabGuid}");
        
        // Step 6: Update the Voyager scene to replace the placeholder GUID
        string scenePath = "Assets/Scenes/Voyager.unity";
        string sceneContent = System.IO.File.ReadAllText(scenePath);
        
        if (sceneContent.Contains("VOYAGER_PREFAB_GUID_PLACEHOLDER"))
        {
            sceneContent = sceneContent.Replace("VOYAGER_PREFAB_GUID_PLACEHOLDER", prefabGuid);
            System.IO.File.WriteAllText(scenePath, sceneContent);
            AssetDatabase.Refresh();
            Debug.Log($"✓ Updated Voyager scene with prefab GUID");
        }
        else
        {
            Debug.LogWarning("Placeholder GUID not found in Voyager scene. You may need to manually assign the prefab.");
        }
        
        // Step 7: Cleanup
        DestroyImmediate(prefabInstance);
        
        Debug.Log("=== SUCCESS ===");
        Debug.Log("Voyager prefab created and scene updated!");
        Debug.Log("The Voyager model should now appear in the scene.");
    }
}

