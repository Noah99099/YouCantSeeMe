using XNode;

// [NodeTint] 可以改變節點在編輯器中的顏色
[NodeTint(0.2f, 0.5f, 0.2f)]
public class StartNode : BaseNode
{
    /// StartNode 只需要一個出口
    [Output(connectionType = ConnectionType.Override)] public BaseNode exit;

    protected override void Init()
    {
        base.Init();
        name = "Start";
    }

    // 重寫 GetNextNode 來從 "exit" 連接埠獲取下一個節點
    public override BaseNode GetNextNode()
    {
        NodePort exitPort = GetOutputPort("exit");
        if (exitPort == null || !exitPort.IsConnected) return null;
        return exitPort.Connection.node as BaseNode;
    }
}