using UnityEngine;
using XNode;

[NodeTint(0.9f, 0.6f, 0.4f)] // 給它一個任務相關的顏色
public class UpdateQuestNode : BaseNode
{
    [Input(connectionType = ConnectionType.Override)] public BaseNode entry;
    [Output(connectionType = ConnectionType.Override)] public BaseNode exit;

    [Header("任務更新")]
    [Tooltip("要更新的任務的唯一 ID")]
    public string questID;

    [Tooltip("要將任務更新到的新狀態")]
    public QuestStatus newStatus;
    
    protected override void Init()
    {
        base.Init();
        name = "Update Quest";
    }

    public override BaseNode GetNextNode()
    {
        NodePort exitPort = GetOutputPort("exit");
        if (exitPort == null || !exitPort.IsConnected) return null;
        return exitPort.Connection.node as BaseNode;
    }
}