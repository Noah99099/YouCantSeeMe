/// <summary>
/// 【新腳本 - 輔助列舉】
/// 用於在 PriorityRouterNode 中選擇要檢查的條件類型
/// </summary>
public enum ConditionType
{
    CheckGraphVariable, // 檢查圖形內部變數 (如 NPC_Talk)
    CheckInventoryItem,  // 檢查玩家背包 (如 物品A)
    CheckLastPickedItem // <--- 【新增】檢查剛剛在對話中出示的物品
}