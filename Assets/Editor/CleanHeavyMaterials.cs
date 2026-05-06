using UnityEngine;
using UnityEditor;

public class CleanHeavyMaterials
{
    [MenuItem("Tools/Clean Heavy Materials (Safe Mode)")]
    static void CleanMaterials()
    {
        if (!EditorUtility.DisplayDialog(
            "警告",
            "這會修改材質（可Undo），建議先備份專案，確定繼續？",
            "確定", "取消"))
            return;

        string[] guids = AssetDatabase.FindAssets("t:Material");

        int count = 0;

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat == null) continue;

            EditorUtility.DisplayProgressBar(
                "Cleaning Materials",
                path,
                (float)i / guids.Length
            );

            bool modified = false;

            Undo.RecordObject(mat, "Clean Material");

            // --- Parallax ---
            if (mat.HasProperty("_ParallaxMap") && mat.GetTexture("_ParallaxMap") != null)
            {
                mat.SetTexture("_ParallaxMap", null);
                mat.SetFloat("_Parallax", 0f);
                mat.DisableKeyword("_PARALLAXMAP");
                modified = true;
            }

            // --- Detail ---
            if (mat.HasProperty("_DetailAlbedoMap") && mat.GetTexture("_DetailAlbedoMap") != null)
            {
                mat.SetTexture("_DetailAlbedoMap", null);
                mat.SetFloat("_DetailAlbedoMapScale", 0f);
                modified = true;
            }

            if (mat.HasProperty("_DetailNormalMap") && mat.GetTexture("_DetailNormalMap") != null)
            {
                mat.SetTexture("_DetailNormalMap", null);
                mat.SetFloat("_DetailNormalMapScale", 0f);
                modified = true;
            }

            if (modified)
            {
                mat.DisableKeyword("_DETAIL_MULX2");
                EditorUtility.SetDirty(mat);
                count++;
                Debug.Log($"[Cleaned] {path}", mat);
            }
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();

        Debug.Log($"完成！共清理 {count} 個材質");
    }
}