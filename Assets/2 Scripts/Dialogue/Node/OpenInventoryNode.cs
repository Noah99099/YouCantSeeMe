using UnityEngine;
using XNode;

[NodeTint(0.2f, 0.6f, 0.6f)]
public class OpenInventoryNode : BaseNode
{
    // --- 【核心修正】 ---
    // 將 ConnectionType.Override 改為 ConnectionType.Multiple
    // 這樣它就可以同時接收 "Start節點的線" 和 "迴圈回來的線"
    [Input(connectionType = ConnectionType.Multiple)] public BaseNode entry; 
    // -------------------
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