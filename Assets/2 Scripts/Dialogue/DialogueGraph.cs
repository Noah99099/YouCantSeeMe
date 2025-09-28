#if UNITY_EDITOR
using UnityEditor; // 需要引用 UnityEditor 來獲取 GUID
#endif
using UnityEngine;
using XNode;
using System.Collections.Generic;
using System.Linq;

public enum DialogueTriggerType
{
    OnSceneStart,   // 場景開始時
    OnInteract,     // 與物件互動時 (e.g., NPC)
    OnEvent,        // 由其他事件觸發 (進階用法)
    OnAreaEnter,    // 進入區域時
    Cutscene        // 用於一般的劇情演出
}

[CreateAssetMenu(fileName = "New Dialogue Graph", menuName = "Dialogue/Graph")]
public class DialogueGraph : NodeGraph
{
    [Header("對話觸發設置")]
    public DialogueTriggerType triggerType;

    [Header("對話變數")]
    public List<Variable> variables = new List<Variable>();

    // --- 輔助方法 ---

    // 根據名稱獲取變數的值
    public float GetVariable(string variableName)
    {
        var variable = variables.FirstOrDefault(v => v.name == variableName);
        if (variable != null)
        {
            return variable.value;
        }
        Debug.LogWarning($"在圖形中找不到變數: {variableName}");
        return 0f;
    }

    // 根據名稱設定變數的值
    public void SetVariable(string variableName, float newValue)
    {
        var variable = variables.FirstOrDefault(v => v.name == variableName);
        if (variable != null)
        {
            variable.value = newValue;
        }
        else
        {
            // 如果變數不存在，就新增一個
            variables.Add(new Variable { name = variableName, value = newValue });
        }
    }
    
    // 獲取當前圖形的狀態，用於存檔
    public DialogueGraphState GetState()
    {
        // 為了確保唯一性，我們使用 Asset 在專案中的 GUID
        #if UNITY_EDITOR
        string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(this));
        return new DialogueGraphState(guid, this.variables);
        #else
        // 在遊戲執行檔中，我們可能需要另一種方式來獲取唯一ID，但暫時用 name
        return new DialogueGraphState(this.name, this.variables);
        #endif
    }

    // 應用讀取進來的狀態，用於讀檔
    public void ApplyState(DialogueGraphState state)
    {
        // 遍歷讀取進來的變數
        foreach (var savedVar in state.variables)
        {
            // 在當前圖形中尋找同名變數並更新其值
            var localVer = this.variables.FirstOrDefault(v => v.name == savedVar.name);
            if (localVer != null)
            {
                localVer.value = savedVar.value;
            }
        }
    }
}