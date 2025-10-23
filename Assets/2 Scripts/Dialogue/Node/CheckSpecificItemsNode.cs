using UnityEngine;
using XNode;
using System.Collections.Generic; // 引用 List

[NodeTint(0.6f, 0.8f, 0.5f)] // 保持物品相關顏色
public class CheckSpecificItemsNode : BaseNode
{
    [Input(connectionType = ConnectionType.Override)] public BaseNode entry;

    [Header("特定物品檢查")]
    [Tooltip("將所有需要檢查是否存在的 ItemData 資產拖入此列表")]
    public List<ItemData> requiredItems; // <--- 核心：允許多個 ItemData

    [Header("流程出口")]
    [Output(connectionType = ConnectionType.Override)] public BaseNode exitPass; // 所有物品都存在
    [Output(connectionType = ConnectionType.Override)] public BaseNode exitFail; // 缺少任何一個物品
    
    protected override void Init()
    {
        base.Init();
        name = "Check Specific Items";
    }

    // 根據檢查結果返回對應的下一個節點
    public BaseNode GetNextNode(bool allItemsFound)
    {
        NodePort port = allItemsFound ? GetOutputPort("exitPass") : GetOutputPort("exitFail");
        if (port == null || !port.IsConnected) return null;
        return port.Connection.node as BaseNode;
    }
}