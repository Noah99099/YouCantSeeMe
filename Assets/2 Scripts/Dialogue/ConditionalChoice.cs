using UnityEngine;

/// <summary>
/// 【輔助類別】
/// 一個資料容器，用來定義 "一個" 動態選項及其顯示條件。
/// [System.Serializable] 讓我們可以在 Inspector 中編輯它。
/// </summary>
[System.Serializable]
public class ConditionalChoice
{
    [Header("選項內容")]
    [Tooltip("【選項A】如果 'Use Localization' 為 false，則直接顯示此處的文字。")]
    public string choiceContent; // <--- 【新增】
    
    [Tooltip("【選項B】如果 'Use Localization' 為 true，則使用此 Key 去本地化系統查找文字。")]
    public string choiceKey;     //
    
    [Tooltip("是否使用本地化 (Localization)？\n- false: 直接顯示 Choice Content\n- true: 使用 Choice Key 查找")]
    public bool useLocalization = false; // <--- 【新增】

    [Header("顯示條件")]
    [Tooltip("此條件的類型 (檢查變數 或是 檢查物品)")]
    public ConditionType checkType = ConditionType.CheckInventoryItem;
    
    [Header("A: 檢查背包物品")]
    [Tooltip("要檢查的物品 ItemData 資產")]
    public ItemData itemToCheck;
    [Tooltip("需要的物品數量")]
    public int requiredQuantity = 1;
    
    [Header("B: 檢查圖形變數")]
    [Tooltip("要比較的圖形變數名稱")]
    public string variableName;
    [Tooltip("要比較的數值")]
    public float valueToCompare;

    [Header("共用比較")]
    [Tooltip("A 或 B 的比較方式 (例如：大於等於)")]
    public ComparisonType comparison = ComparisonType.GreaterThanOrEqualTo;
}