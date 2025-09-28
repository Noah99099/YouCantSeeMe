using UnityEngine;
using XNode;

[NodeTint(0.9f, 0.7f, 0.3f)]
public class InvokeEventNode : BaseNode
{
    [Input(connectionType = ConnectionType.Override)] public BaseNode entry;
    [Output(connectionType = ConnectionType.Override)] public BaseNode exit;

    [Header("廣播事件")]
    [Tooltip("要廣播的事件 ID，場景中的監聽器將會接收這個 ID")]
    public string eventID;

    protected override void Init()
    {
        base.Init();
        name = "Invoke Event";
    }

    public override BaseNode GetNextNode()
    {
        NodePort exitPort = GetOutputPort("exit");
        if (exitPort == null || !exitPort.IsConnected) return null;
        return exitPort.Connection.node as BaseNode;
    }
}