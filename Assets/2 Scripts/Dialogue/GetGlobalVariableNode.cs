using UnityEngine;
using XNode;

[NodeTint(0.2f, 0.7f, 0.4f)]
public class GetGlobalVariableNode : BaseNode
{
    [Input] public BaseNode entry;
    [Output] public BaseNode exit;

    [Header("來源資料庫")]
    public GlobalVariableDatabase database;
    
    [Header("讀取與儲存")]
    public string globalVariableName;
    [Tooltip("將讀取到的值，存入當前圖形的這個局部變數中")]
    public string localVariableName;
    
    protected override void Init() { base.Init(); name = "Get Global Variable"; }

    public override BaseNode GetNextNode()
    {
        NodePort exitPort = GetOutputPort("exit");
        if (exitPort == null || !exitPort.IsConnected) return null;
        return exitPort.Connection.node as BaseNode;
    }
}