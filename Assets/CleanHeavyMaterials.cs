using UnityEngine;
using UnityEditor;

public class CleanHeavyMaterials
{
    [MenuItem("Tools/Clean Heavy Materials (Remove Maps)")]
    static void CleanMaterials()
    {
        if (!EditorUtility.DisplayDialog(
            "警告",
            "這會直接修改材質（不可復原），確定要繼續？",
            "確定", "取消"))
        {
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Material");

        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat == null) continue;

            bool modified = false;

            // --- Parallax ---
            if (mat.HasProperty("_ParallaxMap") && mat.GetTexture("_ParallaxMap") != null)
            {
                mat.SetTexture("_ParallaxMap", null);
                mat.DisableKeyword("_PARALLAXMAP");
                modified = true;
            }

            // --- Detail ---
            if (mat.HasProperty("_DetailAlbedoMap") && mat.GetTexture("_DetailAlbedoMap") != null)
            {
                mat.SetTexture("_DetailAlbedoMap", null);
                modified = true;
            }

            if (mat.HasProperty("_DetailNormalMap") && mat.GetTexture("_DetailNormalMap") != null)
            {
                mat.SetTexture("_DetailNormalMap", null);
                modified = true;
            }

            mat.DisableKeyword("_DETAIL_MULX2");

            // --- Occlusion ---
            if (mat.HasProperty("_OcclusionMap") && mat.GetTexture("_OcclusionMap") != null)
            {
                mat.SetTexture("_OcclusionMap", null);
                mat.DisableKeyword("_OCCLUSIONMAP");
                modified = true;
            }

            if (modified)
            {
                EditorUtility.SetDirty(mat);
                count++;
                Debug.Log($"[Cleaned] {path}", mat);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"完成！共清理 {count} 個材質");
    }
}