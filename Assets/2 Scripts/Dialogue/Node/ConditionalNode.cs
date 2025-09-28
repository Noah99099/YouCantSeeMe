using UnityEngine;
using XNode;

[NodeTint(0.7f, 0.2f, 0.4f)]
public class ConditionalNode : BaseNode
{
    [Input] public BaseNode entry;

    // 這個節點有兩個出口：True 和 False
    [Output] public BaseNode exitTrue;
    [Output] public BaseNode exitFalse;

    [Header("條件")]
    public string variableName;
    public ComparisonType comparison;
    public float valueToCompare;

    protected override void Init() { base.Init(); name = "Conditional"; }
    
    // 根據比較結果返回對應的下一個節點
    public BaseNode GetNextNode(bool conditionResult)
    {
        NodePort port = conditionResult ? GetOutputPort("exitTrue") : GetOutputPort("exitFalse");
        if (port == null || !port.IsConnected) return null;
        return port.Connection.node as BaseNode;
    }
}