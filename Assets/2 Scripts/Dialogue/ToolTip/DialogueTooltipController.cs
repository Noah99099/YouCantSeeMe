using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DialogueTooltipController : MonoBehaviour
{
    [Header("UI 元件")]
    [SerializeField] private GameObject tooltipRoot;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private RectTransform rectTransform; // 面板的 RectTransform

    [Header("設定")]
    [SerializeField] private Vector2 offset = new Vector2(15, -15); // 滑鼠偏移量

    private void Awake()
    {
        // 遊戲開始時確保隱藏
        HideTooltip();
    }

    void Update()
    {
        // 讓面板跟隨滑鼠移動
        if (tooltipRoot.activeSelf)
        {
            // 獲取滑鼠位置
            Vector2 mousePos = Input.mousePosition;
            // 設定位置 (加上偏移，避免游標擋住文字)
            rectTransform.position = mousePos + offset;
        }
    }

    public void ShowTooltip(string itemName, string itemDesc)
    {
        nameText.text = itemName;
        descText.text = itemDesc;
        tooltipRoot.SetActive(true);
        
        // 強制刷新 Layout (避免剛顯示時大小計算錯誤)
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }

    public void HideTooltip()
    {
        tooltipRoot.SetActive(false);
    }
}