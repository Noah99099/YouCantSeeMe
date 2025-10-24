using UnityEngine;
using XNode;

[NodeTint(0.2f, 0.4f, 0.7f)]
public class SetGlobalVariableNode : BaseNode
{
    [Input] public BaseNode entry;
    [Output] public BaseNode exit;

    [Header("目標資料庫")]
    public GlobalVariableDatabase database;
    
    [Header("設定值")]
    public string globalVariableName;
    public float valueToSet;
    
    protected override void Init() { base.Init(); name = "Set Global Variable"; }

    public override BaseNode GetNextNode()
    {
        NodePort exitPort = GetOutputPort("exit");
        if (exitPort == null || !exitPort.IsConnected) return null;
        return exitPort.Connection.node as BaseNode;
    }
}