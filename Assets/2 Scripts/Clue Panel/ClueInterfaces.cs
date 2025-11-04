using UnityEngine;

/// <summary>
/// 定義線索的類型
/// </summary>
public enum EClueType
{
    Item,       // 物品
    Memory,     // 回憶
    Sound       // 聲音
}

/// <summary>
/// 所有線索類型的統一介面 (Interface)
/// 這是讓系統能同時處理 物品、回憶、聲音 的關鍵
/// </summary>
public interface IClue
{
    string ClueID { get; }          // 唯一的ID，用於比對答案 (例如 "ITEM_Pineapple", "MEM_RoleC_1")
    string ClueName { get; }        // 顯示的名稱 (例如 "鳳梨")
    string ClueDescription { get; } // 顯示的描述 (例如 "刺刺的")
    Sprite ClueIcon { get; }        // 顯示的圖標
    EClueType ClueType { get; }     // 該線索的類型
    object OriginalData { get; }    // 對原始數據的引用 (例如 原始的Item class)
}
