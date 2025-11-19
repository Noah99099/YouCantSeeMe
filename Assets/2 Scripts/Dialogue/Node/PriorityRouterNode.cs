using UnityEngine;
using XNode;
using System.Collections.Generic; // 引用 List

/// <summary>
/// 【輔助類別】
/// 用於定義 "一個" 優先級條件
/// [System.Serializable] 讓我們可以在 Inspector 中編輯它
/// </summary>
[System.Serializable]
public class PriorityCondition
{
    [Tooltip("此條件的類型:\n- CheckGraphVariable: 檢查變數\n- CheckInventoryItem: 檢查背包有無此物品\n- CheckLastPickedItem: 檢查剛剛是否出示了此物品")]
    public ConditionType checkType;

    [Header("A: 檢查圖形變數")]
    [Tooltip("要比較的圖形變數名稱")]
    public string variableName;
    [Tooltip("要比較的數值")]
    public float valueToCompare;

    // --- 【修改】更新了標題與提示，表明這裡共用於兩種物品檢查 ---
    [Header("B: 檢查物品 (背包 / 出示)")]
    [Tooltip("要檢查的 ItemData。\n1. 如果是檢查背包：檢查是否擁有此物品。\n2. 如果是檢查出示：檢查剛剛選的是不是這個物品。")]
    public ItemData itemToCheck; 
    
    [Tooltip("需要的物品數量 (僅用於 '檢查背包' 模式)")]
    public int requiredQuantity = 1;
    
    [Header("共用比較")]
    [Tooltip("A 或 B 的比較方式 (例如：大於等於)。\n注意：'檢查出示' 模式會忽略此欄位 (只檢查是否相等)。")]
    public ComparisonType comparison = ComparisonType.GreaterThanOrEqualTo;
}


/// <summary>
/// 【核心節點】
/// 條列式優先級路由器。
/// 會依序檢查列表，並執行第一個滿足條件的出口。
/// </summary>
[NodeTint(0.8f, 0.6f, 0.2f)] // 醒目的橘色
public class PriorityRouterNode : BaseNode
{
    [Input(connectionType = ConnectionType.Override)] 
    public BaseNode entry;

    [Header("條件 (依 0, 1, 2... 順序檢查)")]
    [Output(dynamicPortList = true, connectionType = ConnectionType.Override)]
    public List<PriorityCondition> conditions; // 核心：條件列表

    [Header("預設出口")]
    [Tooltip("如果 '所有' 條件都不滿足，則執行此出口")]
    [Output(connectionType = ConnectionType.Override)] 
    public BaseNode exitElse; // 預設出口
    
    protected override void Init()
    {
        base.Init();
        name = "Priority Router";
    }

    // 獲取條件列表 "i" 對應的出口節點
    public BaseNode GetNextNodeForCondition(int i)
    {
        if (i < 0 || i >= conditions.Count) return null;
        
        NodePort port = GetOutputPort("conditions " + i);
        if (port == null || !port.IsConnected) return null;
        
        return port.Connection.node as BaseNode;
    }

    // 獲取 "Else" 出口節點
    public BaseNode GetNextNodeElse()
    {
        NodePort port = GetOutputPort("exitElse");
        if (port == null || !port.IsConnected) return null;
        return port.Connection.node as BaseNode;
    }
}