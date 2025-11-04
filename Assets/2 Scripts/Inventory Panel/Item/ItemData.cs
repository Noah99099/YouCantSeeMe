using UnityEngine;
using UnityEngine.UI;

// 這個屬性讓我們可以直接在 Project 視窗中右鍵 Create -> Inventory -> Item
[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    [Header("基本資訊")]
    public string itemID; // 物品的唯一識別 ID
    [Header("物品資訊：UI元件")]
    public Sprite itemImage; //物件圖片
    public Sprite icon; //物件圖標
    public string itemName; //物件名稱文本
    [Header("是否為案件物品")]
    public bool isClueItem;

    [TextArea(3,10)]
    public string description; // 物品描述文本

    public GameObject modelPrefab; // 預覽物品的模型
}