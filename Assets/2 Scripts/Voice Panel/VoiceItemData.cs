using UnityEngine;

// 這個屬性讓我們可以直接在 Project 視窗中右鍵 Create -> Inventory -> VoiceItem
[CreateAssetMenu(fileName = "NewVoiceItem", menuName = "Inventory/VoiceItem")]
public class VoiceItemData : ScriptableObject
{
    [Header("UI 元件")]
    public string itemName; //名稱
    public Sprite voiceIcon;   // 對應聲音格子的圖片
    public string voiceItemID; // 觸發對話用ID

    [Header("聲音物品模型")]
    public GameObject voiceItem;

    // [!! 新增 !!]
    // 用來定義在 cornerAnchor 生成時的模型縮放比例
    // 1.0 = 原始大小, 0.5 = 50%, 2.0 = 200%
    public float itemScale = 1.0f;

    [Header("右側文字顯示")]
    public string titleText;       // 顯示在 InfoText1

    [Header("使用前的文本")]
    [TextArea(5, 10)]
    public string descText_Before; // 顯示在 InfoText2

    [Header("使用後的文本")]
    [TextArea(5, 10)]
    public string descText_After; // 顯示在 InfoText2
}
