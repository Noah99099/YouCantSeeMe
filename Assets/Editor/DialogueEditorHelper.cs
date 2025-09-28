using UnityEditor;
using UnityEngine;
using System.Linq;

// 這是一個靜態類別，不需要掛載到任何物件上，純粹作為工具使用
public static class DialogueEditorHelper
{
    // 一個共用的方法，用來繪製變數的下拉選單
    public static string DrawVariablePopup(DialogueGraph graph, string currentVariable)
    {
        // 如果 graph 為空，或裡面沒有任何變數，就顯示一個普通的文字輸入框
        if (graph == null || graph.variables.Count == 0)
        {
            return EditorGUILayout.TextField("Variable Name", currentVariable);
        }

        // 獲取 graph 中所有變數的名稱
        string[] variableNames = graph.variables.Select(v => v.name).ToArray();

        // 找到當前變數名稱在陣列中的索引
        int currentIndex = ArrayUtility.IndexOf(variableNames, currentVariable);
        
        // 如果找不到 (例如手動輸入了一個不存在的名稱)，給予一個提示
        if (currentIndex < 0)
        {
            EditorGUILayout.HelpBox($"變數 '{currentVariable}' 不存在於此圖形中。", MessageType.Warning);
            return EditorGUILayout.TextField("Variable Name", currentVariable);
        }

        // 繪製下拉選單
        int newIndex = EditorGUILayout.Popup("Variable Name", currentIndex, variableNames);

        // 返回玩家新選擇的變數名稱
        return variableNames[newIndex];
    }
}