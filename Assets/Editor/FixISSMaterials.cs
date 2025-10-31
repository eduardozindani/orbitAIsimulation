using UnityEngine;
using UnityEditor;
using System.IO;

public class FixISSMaterials : EditorWindow
{
    [MenuItem("Tools/Fix ISS Materials")]
    static void FixMaterials()
    {
        string materialsPath = "Assets/Models/iss/source/ISS/ISS.fbm";

        if (!Directory.Exists(materialsPath))
        {
            Debug.LogError($"Directory not found: {materialsPath}");
            return;
        }

        string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { materialsPath });
        int fixedCount = 0;

        foreach (string guid in materialGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat != null)
            {
                // Set Base Color to white
                if (mat.HasProperty("_BaseColor"))
                {
                    Color currentColor = mat.GetColor("_BaseColor");
                    if (currentColor != Color.white)
                    {
                        mat.SetColor("_BaseColor", Color.white);
                        Debug.Log($"Fixed material: {mat.name} (was {currentColor})");
                        fixedCount++;
                    }
                }

                // Also set legacy _Color property for compatibility
                if (mat.HasProperty("_Color"))
                {
                    Color currentColor = mat.GetColor("_Color");
                    if (currentColor != Color.white)
                    {
                        mat.SetColor("_Color", Color.white);
                    }
                }

                EditorUtility.SetDirty(mat);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Fixed {fixedCount} ISS materials! All Base Colors set to white.");
        EditorUtility.DisplayDialog("ISS Materials Fixed",
            $"Successfully fixed {fixedCount} materials.\nThe ISS should now display colors correctly!",
            "OK");
    }
}
