using UnityEngine;
using XNode;
using System.Collections.Generic; // 引用 List

/// <summary>
/// 【新節點 - 核心】
/// 動態選項節點。
/// 只會向玩家顯示 "滿足條件" 的選項。
/// </summary>
[NodeTint(0.7f, 0.3f, 0.3f)] // 給它一個醒目的紅色
public class ConditionalChoiceNode : BaseNode
{
    [Input(connectionType = ConnectionType.Override)] 
    public BaseNode entry;

    [Header("動態選項 (依序檢查)")]
    [Tooltip("只有 '滿足條件' 的選項才會顯示給玩家")]
    [Output(dynamicPortList = true, connectionType = ConnectionType.Override)]
    public List<ConditionalChoice> conditionalChoices = new List<ConditionalChoice>();

    [Header("預設選項 (永遠顯示)")]
    [Tooltip("這些選項 '永遠' 都會顯示在列表末尾 (它們的 '顯示條件' 會被忽略)")]
    [Output(dynamicPortList = true, connectionType = ConnectionType.Override)]
    public List<ConditionalChoice> defaultChoices = new List<ConditionalChoice>(); // <--- 【修改】
    
    protected override void Init()
    {
        base.Init();
        name = "Conditional Choice";
    }

    // 這個節點沒有 GetNextNode()，因為它會等待 OnChoiceMade() 回呼
    // DialogueManager 將會負責處理哪個出口被選中了
}