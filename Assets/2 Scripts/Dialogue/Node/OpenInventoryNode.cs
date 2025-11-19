using UnityEngine;
using XNode;

[NodeTint(0.2f, 0.6f, 0.6f)]
public class OpenInventoryNode : BaseNode
{
    [Input(connectionType = ConnectionType.Override)] public BaseNode entry;
    [Output(connectionType = ConnectionType.Override)] public BaseNode exit;

    protected override void Init()
    {
        base.Init();
        name = "Open Inventory";
    }

    public override BaseNode GetNextNode()
    {
        NodePort exitPort = GetOutputPort("exit");
        if (exitPort == null || !exitPort.IsConnected) return null;
        return exitPort.Connection.node as BaseNode;
    }
}