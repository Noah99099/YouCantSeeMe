// 檔案：DialogueTriggerSetup.cs

using UnityEngine;
using System;

// 定義所有可能的對話觸發方式
public enum DialogueTriggerType
{
    OnSceneStart,      // 進入場景時觸發
    OnInteraction,     // 與特定物件互動時觸發 (例如按 E)
    OnZoneEnter,       // 進入特定區域時觸發
    OnEvent            // 由遊戲事件觸發 (例如拾取物品、任務完成)
}

// 將對話、觸發方式和相關設定綁定在一起的資料類別
[Serializable]
public class ManagedDialogue
{
    [Tooltip("此設定的描述性名稱 (方便在 Inspector 中辨識)")]
    public string Name;

    [Tooltip("要執行的對話資料 (ScriptableObject)")]
    public DialogueContainerSO DialogueContainer;

    [Tooltip("選擇此對話的觸發方式")]
    public DialogueTriggerType TriggerType;

    // --- 以下是根據 TriggerType 動態顯示的欄位 ---

    [Tooltip("【互動觸發專用】需要與哪個物件互動？(例如 NPC)")]
    public GameObject InteractionTarget;

    [Tooltip("【區域觸發專用】需要進入哪個觸發區？")]
    public Collider ZoneTarget;

    [Tooltip("【事件觸發專用】監聽哪個遊戲事件資產 (Game Event Asset)？")]
    public GameEvent EventToListenFor;
    
    [Tooltip("這個對話是否只應觸發一次？")]
    public bool TriggerOnlyOnce = true;

    // --- 內部狀態 ---
    [HideInInspector]
    public bool HasBeenTriggered = false;
    
    // 我們需要一個 DialogueRunner 來實際執行對話
    // 這個會在 DialogueManager 中自動處理
    [HideInInspector]
    public DialogueRunner Runner;
}