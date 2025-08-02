using UnityEngine;

// 這個屬性讓我們可以直接在 Project 視窗中右鍵 Create -> Inventory -> Item
[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    [Header("物品資訊")]
    public string itemName; // 物品名稱
    public Sprite icon;     // 物品在 UI 上顯示的圖示
    
    [TextArea(3,10)]
    public string description; // 物品描述

    // 你未來可以擴充更多屬性，例如：
    // public int maxStack; // 最大堆疊數量
    public GameObject modelPrefab; // 物品的模型
}