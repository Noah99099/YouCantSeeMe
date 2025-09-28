using UnityEngine;
using XNode;

[NodeTint(0.4f, 0.4f, 0.6f)] // 給它一個不同的顏色，例如藍紫色
public class WaitNode : BaseNode
{
    [Input(connectionType = ConnectionType.Override)] public BaseNode entry;
    [Output(connectionType = ConnectionType.Override)] public BaseNode exit;

    [Header("等待時長 (秒)")]
    public float waitDuration = 1.0f;

    protected override void Init()
    {
        base.Init();
        name = "Wait";
    }

    public override BaseNode GetNextNode()
    {
        NodePort exitPort = GetOutputPort("exit");
        if (exitPort == null || !exitPort.IsConnected) return null;
        return exitPort.Connection.node as BaseNode;
    }
}