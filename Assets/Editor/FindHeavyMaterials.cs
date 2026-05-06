using UnityEngine;
using UnityEditor;

public class FindHeavyMaterials
{
    [MenuItem("Tools/Find Heavy Materials")]
    static void FindMaterials()
    {
        string[] guids = AssetDatabase.FindAssets("t:Material");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat == null) continue;

            string[] keywords = mat.shaderKeywords;

            bool hasParallax = System.Array.Exists(keywords, k => k == "_PARALLAXMAP");
            bool hasDetail = System.Array.Exists(keywords, k => k == "_DETAIL_MULX2");
            bool hasOcclusion = System.Array.Exists(keywords, k => k == "_OCCLUSIONMAP");

            if (hasParallax || hasDetail || hasOcclusion)
            {
                Debug.Log(
                    $"[Heavy Material] {path}\n" +
                    $"Parallax: {hasParallax}, Detail: {hasDetail}, Occlusion: {hasOcclusion}",
                    mat
                );
            }
        }

        Debug.Log("Scan Complete");
    }
}