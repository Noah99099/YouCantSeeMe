using UnityEngine;
using UnityEngine.UI;

// 這個屬性讓我們可以直接在 Project 視窗中右鍵 Create -> Inventory -> Item
[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    [Header("物品資訊：UI元件")]
    public Sprite itemImage; //物件圖片
    public Sprite icon; //物件圖標
    public string itemName; //物件名稱文本

    [TextArea(3,10)]
    public string description; // 物品描述文本

    // 你未來可以擴充更多屬性，例如：
    // public int maxStack; // 最大堆疊數量
    public GameObject modelPrefab; // 預覽物品的模型
}