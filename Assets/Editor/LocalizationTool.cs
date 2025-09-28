using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Text; // 用於建立 CSV 字串
using System.IO;   // 用於寫入檔案

public class LocalizationTool : EditorWindow
{
    private Dictionary<string, string> extractedData = new Dictionary<string, string>();
    private string baseLanguage = "zh_TW"; // 設定您的基礎寫作語言

    [MenuItem("Tools/Dialogue System/Localization Tool")]
    public static void ShowWindow()
    {
        GetWindow<LocalizationTool>("Localization Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("Dialogue Localization Tool", EditorStyles.boldLabel);
        GUILayout.Space(10);

        baseLanguage = EditorGUILayout.TextField("基礎語言代碼", baseLanguage);
        EditorGUILayout.HelpBox("1. 掃描專案，提取所有對話文本。\n2. (可選) 匯出成 CSV 檔供翻譯。\n3. 將生成的 Keys 寫回 Dialogue Graph。", MessageType.Info);
        
        GUILayout.Space(10);

        if (GUILayout.Button("1. 掃描並生成 Keys", GUILayout.Height(30)))
        {
            ScanAndGenerateKeys();
        }

        if (extractedData.Count > 0)
        {
            if (GUILayout.Button("2. 匯出為 CSV 檔案", GUILayout.Height(30)))
            {
                ExportToCsv();
            }
            if (GUILayout.Button("3. 將 Keys 寫回 Graph Assets", GUILayout.Height(30)))
            {
                WriteKeysBackToAssets();
            }
            
            // 顯示預覽
            GUILayout.Space(10);
            EditorGUILayout.LabelField("預覽 (前10條):");
            foreach (var pair in extractedData.Take(10))
            {
                EditorGUILayout.LabelField(pair.Key, pair.Value);
            }
        }
    }

    private void ScanAndGenerateKeys()
    {
        extractedData.Clear();
        string[] guids = AssetDatabase.FindAssets("t:DialogueGraph");
        int keyCounter = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            DialogueGraph graph = AssetDatabase.LoadAssetAtPath<DialogueGraph>(path);

            foreach (var node in graph.nodes)
            {
                if (node is LineNode lineNode && !string.IsNullOrEmpty(lineNode.line.content))
                {
                    string key = $"line_{graph.name}_{keyCounter++}";
                    if (!extractedData.ContainsValue(lineNode.line.content))
                        extractedData[key] = lineNode.line.content;
                }
                else if (node is ChoiceNode choiceNode)
                {
                    foreach (string choice in choiceNode.choices)
                    {
                        if (!string.IsNullOrEmpty(choice) && !extractedData.ContainsValue(choice))
                        {
                             string key = $"choice_{graph.name}_{keyCounter++}";
                             extractedData[key] = choice;
                        }
                       
                    }
                }
            }
        }
        EditorUtility.DisplayDialog("掃描完成", $"掃描完畢！共生成 {extractedData.Count} 條不重複的 Keys。", "確定");
    }

    private void ExportToCsv()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Key,{baseLanguage}"); // CSV 標頭

        foreach (var pair in extractedData)
        {
            string value = $"\"{pair.Value.Replace("\"", "\"\"")}\""; // 處理引號
            sb.AppendLine($"{pair.Key},{value}");
        }
        
        string path = EditorUtility.SaveFilePanel("儲存 CSV", "", "localization_export.csv", "csv");
        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            EditorUtility.DisplayDialog("匯出成功", $"已成功匯出檔案至:\n{path}", "確定");
        }
    }

    private void WriteKeysBackToAssets()
    {
        if (!EditorUtility.DisplayDialog("警告", "這個操作將會修改您的 DialogueGraph 資產，為其中的對話和選項填上對應的 Key。\n\n這個操作通常是不可逆的，建議先備份專案。\n\n您確定要繼續嗎？", "確定，寫入 Keys", "取消"))
        {
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:DialogueGraph");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            DialogueGraph graph = AssetDatabase.LoadAssetAtPath<DialogueGraph>(path);
            bool assetModified = false;

            foreach (var node in graph.nodes)
            {
                if (node is LineNode lineNode && !string.IsNullOrEmpty(lineNode.line.content))
                {
                    var pair = extractedData.FirstOrDefault(x => x.Value == lineNode.line.content);
                    if (!string.IsNullOrEmpty(pair.Key))
                    {
                        lineNode.line.contentKey = pair.Key;
                        assetModified = true;
                    }
                }
                else if (node is ChoiceNode choiceNode)
                {
                    choiceNode.choiceKeys = new List<string>();
                    foreach (string choice in choiceNode.choices)
                    {
                        var pair = extractedData.FirstOrDefault(x => x.Value == choice);
                        choiceNode.choiceKeys.Add(string.IsNullOrEmpty(pair.Key) ? "" : pair.Key);
                        assetModified = true;
                    }
                }
            }
            if (assetModified) EditorUtility.SetDirty(graph);
        }
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("完成", "所有 Keys 已成功寫回 DialogueGraph Assets！", "太棒了");
    }
}