using UnityEngine;
using XNode;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
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

    [Header("變數")]
    [Tooltip("遊戲開始時，變數會被重置為這個列表中的初始值")]
    public List<Variable> initialVariables = new List<Variable>();
    [Tooltip("遊戲執行期間，實際會被修改的變數列表")]
    public List<Variable> runtimeVariables = new List<Variable>();
    [Header("執行期間狀態")]
    // 【新增】用來儲存玩家在 OpenInventoryNode 中選擇的物品 ID
    public string lastPickedItemID;

    // --- 新增的重置方法 ---
    public void ResetVariables()
    {
        runtimeVariables.Clear();
        foreach (var initialVar in initialVariables)
        {
            runtimeVariables.Add(new Variable { name = initialVar.name, value = initialVar.value });
        }
        lastPickedItemID = "";
    }
    // 根據名稱獲取變數的值
    public float GetVariable(string variableName)
    {
        var variable = runtimeVariables.FirstOrDefault(v => v.name == variableName);
        if (variable != null) return variable.value;
        Debug.LogWarning($"在圖形 '{this.name}' 的執行期變數中找不到: {variableName}");
        return 0f;
    }

    // 根據名稱設定變數的值
    public void SetVariable(string variableName, float newValue)
    {
        var variable = runtimeVariables.FirstOrDefault(v => v.name == variableName);
        if (variable != null) variable.value = newValue;
        else runtimeVariables.Add(new Variable { name = variableName, value = newValue });
    }
    
    // 獲取當前圖形的狀態，用於存檔
    public DialogueGraphState GetState()
    {
        #if UNITY_EDITOR
        string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(this));
        return new DialogueGraphState(guid, this.runtimeVariables);
        #else
        return new DialogueGraphState(this.name, this.runtimeVariables);
        #endif
    }

    // 應用讀取進來的狀態，用於讀檔
    public void ApplyState(DialogueGraphState state)
    {
        foreach (var savedVar in state.variables)
        {
            var localVer = this.runtimeVariables.FirstOrDefault(v => v.name == savedVar.name);
            if (localVer != null) localVer.value = savedVar.value;
        }
    }
}