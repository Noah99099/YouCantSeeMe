using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class FindHeavyMaterials
{
    [MenuItem("Tools/Find Heavy Materials (Advanced)")]
    static void Find()
    {
        string[] guids = AssetDatabase.FindAssets("t:Material");

        List<string> results = new List<string>();

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat == null) continue;

            EditorUtility.DisplayProgressBar("Scanning Materials", path, (float)i / guids.Length);

            int score = 0;

            // Parallax
            if (mat.HasProperty("_ParallaxMap") && mat.GetTexture("_ParallaxMap") != null)
                score += 3;

            // Detail
            if (mat.HasProperty("_DetailAlbedoMap") && mat.GetTexture("_DetailAlbedoMap") != null)
                score += 2;

            // Normal
            if (mat.HasProperty("_BumpMap") && mat.GetTexture("_BumpMap") != null)
                score += 1;

            // BaseMap size
            if (mat.HasProperty("_BaseMap"))
            {
                Texture tex = mat.GetTexture("_BaseMap");
                if (tex is Texture2D t2d && t2d.width >= 2048)
                    score += 2;
            }

            // Transparent
            if (mat.renderQueue >= 3000)
                score += 3;

            if (score >= 3)
            {
                results.Add($"[Score:{score}] {path}");
            }
        }

        EditorUtility.ClearProgressBar();

        Debug.Log("===== Heavy Materials =====\n" + string.Join("\n", results));
    }
}