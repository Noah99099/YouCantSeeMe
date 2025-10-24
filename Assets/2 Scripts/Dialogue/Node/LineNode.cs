using XNode;

public class LineNode : BaseNode
{
    // LineNode 需要一個入口和一個出口
    [Input(connectionType = ConnectionType.Override)] public BaseNode entry;
    [Output(connectionType = ConnectionType.Override)] public BaseNode exit;

    public DialogueLine line;

    protected override void Init()
    {
        base.Init();
        name = "Line";
    }

    public override BaseNode GetNextNode()
    {
        NodePort exitPort = GetOutputPort("exit");
        if (exitPort == null || !exitPort.IsConnected) return null;
        return exitPort.Connection.node as BaseNode;
    }

}