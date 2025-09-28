using UnityEngine;
using XNode;

[NodeTint(0.2f, 0.4f, 0.7f)] // 給它一個不同的顏色
public class SetVariableNode : BaseNode
{
    [Input] public BaseNode entry;
    [Output] public BaseNode exit;

    public string variableName;
    public float value;
    
    protected override void Init() { base.Init(); name = "Set Variable"; }

    public override BaseNode GetNextNode()
    {
        NodePort exitPort = GetOutputPort("exit");
        if (exitPort == null || !exitPort.IsConnected) return null;
        return exitPort.Connection.node as BaseNode;
    }
}