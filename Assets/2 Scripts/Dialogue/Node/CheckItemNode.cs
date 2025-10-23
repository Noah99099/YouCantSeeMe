using UnityEngine;
using XNode;

[NodeTint(0.6f, 0.8f, 0.5f)] // 給它一個物品相關的顏色
public class CheckItemNode : BaseNode
{
    [Input(connectionType = ConnectionType.Override)] public BaseNode entry;

    [Header("物品檢查")]
    [Tooltip("要檢查的物品 ItemData 資產")]
    public ItemData itemToCheck; // 直接引用 ItemData ScriptableObject

    [Tooltip("需要的數量")]
    public int requiredQuantity = 1;

    [Tooltip("比較方式")]
    public ComparisonType comparison = ComparisonType.GreaterThanOrEqualTo; // 預設為 >=

    [Header("流程出口")]
    [Output(connectionType = ConnectionType.Override)] public BaseNode exitPass; // 條件滿足
    [Output(connectionType = ConnectionType.Override)] public BaseNode exitFail; // 條件不滿足
    
    protected override void Init()
    {
        base.Init();
        name = "Check Item";
    }

    // 根據檢查結果返回對應的下一個節點
    public BaseNode GetNextNode(bool conditionResult)
    {
        NodePort port = conditionResult ? GetOutputPort("exitPass") : GetOutputPort("exitFail");
        if (port == null || !port.IsConnected) return null;
        return port.Connection.node as BaseNode;
    }
}