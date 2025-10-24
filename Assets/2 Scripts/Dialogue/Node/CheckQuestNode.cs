using UnityEngine;
using XNode;

[NodeTint(0.9f, 0.7f, 0.5f)] // 給它一個與 UpdateQuestNode 類似的顏色
public class CheckQuestNode : BaseNode
{
    [Input(connectionType = ConnectionType.Override)] public BaseNode entry;

    [Header("條件檢查")]
    [Tooltip("要檢查的任務的唯一 ID")]
    public string questID;

    [Tooltip("要檢查任務是否處於這個狀態")]
    public QuestStatus statusToCheck;

    [Header("流程出口")]
    [Output(connectionType = ConnectionType.Override)] public BaseNode exitPass;
    [Output(connectionType = ConnectionType.Override)] public BaseNode exitFail;
    
    protected override void Init()
    {
        base.Init();
        name = "Check Quest";
    }

    // 根據檢查結果返回對應的下一個節點
    public BaseNode GetNextNode(bool conditionResult)
    {
        NodePort port = conditionResult ? GetOutputPort("exitPass") : GetOutputPort("exitFail");
        if (port == null || !port.IsConnected) return null;
        return port.Connection.node as BaseNode;
    }
}