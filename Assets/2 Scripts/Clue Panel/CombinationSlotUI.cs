using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 管理右側單一個「填入物品格子」
/// </summary>
public class CombinationSlotUI : MonoBehaviour
{
    [Header("UI 引用")]
    public Image borderImage;     // 外框 (黃/紅/藍)
    public Image iconImage;       // 內框 (顯示物品圖標)
    public TMP_Text hintText;     // 提示文本
    public Button clickButton;

    [Header("顏色配置")]
    public Color itemBorderColor = Color.yellow;
    public Color memoryBorderColor = Color.red;
    public Color soundBorderColor = Color.blue;

    // --- 狀態 ---
    public int SlotIndex { get; private set; }
    public EClueType RequiredClueType { get; private set; }
    public IClue FilledClue { get; private set; }
    public bool IsLocked { get; private set; }

    private System.Action<CombinationSlotUI> _onClickCallback;

    public void Initialize(ClueSlotDefinition definition, int index, System.Action<CombinationSlotUI> onClickCallback)
    {
        SlotIndex = index;
        RequiredClueType = definition.requiredClueType;
        hintText.text = definition.hintText;
        _onClickCallback = onClickCallback;
        IsLocked = false;

        // 設置外框顏色
        switch (RequiredClueType)
        {
            case EClueType.Item:
                borderImage.color = itemBorderColor;
                break;
            case EClueType.Memory:
                borderImage.color = memoryBorderColor;
                break;
            case EClueType.Sound:
                borderImage.color = soundBorderColor;
                break;
        }

        iconImage.sprite = null; // 默認清空
        iconImage.gameObject.SetActive(false);
        clickButton.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (IsLocked) return;
        _onClickCallback?.Invoke(this);
    }

    /// <summary>
    /// 將一個線索填入此格子
    /// </summary>
    public void FillSlot(IClue clue)
    {
        if (clue == null) return;
        if (clue.ClueType != RequiredClueType)
        {
            Debug.LogError($"類型不匹配！格子需要 {RequiredClueType} 但填入的是 {clue.ClueType}");
            return;
        }

        FilledClue = clue;
        iconImage.sprite = clue.ClueIcon;
        iconImage.gameObject.SetActive(true);
        hintText.gameObject.SetActive(false); // 填入後隱藏提示
    }

    /// <summary>
    /// 鎖定格子，使其無法再次點擊
    /// </summary>
    public void Lock()
    {
        IsLocked = true;
    }
}

