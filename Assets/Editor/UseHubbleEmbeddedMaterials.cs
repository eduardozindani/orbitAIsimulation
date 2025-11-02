using UnityEngine;
using UnityEditor;

public class UseHubbleEmbeddedMaterials : MonoBehaviour
{
    [MenuItem("Tools/Use Hubble Embedded Materials")]
    static void ConfigureHubbleMaterials()
    {
        string fbxPath = "Assets/Models/Hubble/source/hubble123.fbx";
        string prefabPath = "Assets/Prefabs/Models/Hubble.prefab";
        
        // Configure FBX to use embedded materials
        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer != null)
        {
            Debug.Log($"Configuring FBX to use embedded materials...");
            
            // Use embedded materials from the FBX
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importer.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName, ModelImporterMaterialSearch.Local);
            
            AssetDatabase.WriteImportSettingsIfDirty(fbxPath);
            AssetDatabase.ImportAsset(fbxPath, ImportAssetOptions.ForceUpdate);
            
            Debug.Log($"✓ FBX configured to use embedded materials");
            Debug.Log($"✓ Materials location: In Prefab");
            Debug.Log($"✓ Check the Hubble scene - it should now show proper colors!");
        }
        else
        {
            Debug.LogError($"Could not find FBX at {fbxPath}");
        }
    }
}

