using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.ProBuilder.Shapes;

/// <summary>
/// 管理右側單一個「填入物品格子」
/// </summary>
public class CombinationSlotUI : MonoBehaviour
{
    [Header("UI 引用")]
    public UnityEngine.Sprite spriteRed; // 用於 Item
    public UnityEngine.Sprite spriteGreen; // 用於 Memory
    public UnityEngine.Sprite spriteBlue; // 用於 Sound
    public Image outFrame;        // 外框 (素材 A/B/C)
    //public Image borderImage;     // 外框 (黃/紅/藍)
    public Image iconImage;       // 內框 (顯示物品圖標)
    public TMP_Text hintText;     // 提示文本
    public Button clickButton; // 綁定在 OutFrame 上的按鈕

    [Header("顏色配置")]
    //public Color colorItem = Color.yellow; // 外框
    //public Color colorMemory = Color.red; // 外框
    //public Color colorSound = Color.blue; // 外框
    //public Color colorDefaultBorder = Color.white;
    public Color colorDefaultIcon = Color.white; // 內框(空)的顏色
    public Color colorFilledIcon = Color.white;  // 內框(有圖)的顏色

    // --- 狀態 ---
    public int SlotIndex { get; private set; }
    public EClueType RequiredClueType { get; private set; }
    //public IClue FilledClue { get; private set; }
    public bool IsLocked { get; private set; }
    private IClue _filledClue = null;
    private System.Action<CombinationSlotUI> _onClickCallback;

    private void Start()
    {
        if (clickButton != null)
        {
            clickButton.onClick.RemoveAllListeners(); //跟著加，不確定對不對
            clickButton.onClick.AddListener(OnSlotClicked);
        }
    }

    /// <summary>
    /// [已更新] 初始化格子 (在 LoadPuzzle 時被呼叫)
    /// </summary>
    public void Setup(ClueSlotDefinition slotDefinition, int index, IClue existingClue, Action<CombinationSlotUI> onClick)
    {
        SlotIndex = index;
        RequiredClueType = slotDefinition.requiredClueType;
        _onClickCallback = onClick;
        IsLocked = false;

        // --- [!!] 修正 #1 和 #2 [!!] ---
        // 1. 提示文本 (HintText) 始終顯示
        hintText.text = slotDefinition.hintText;
        hintText.gameObject.SetActive(true);

        // 2. 內框 (IconImage) 始終顯示
        iconImage.gameObject.SetActive(true);
        // --- [!!] 修正結束 [!!] ---

        // 設置外框顏色
        switch (RequiredClueType)
        {
            case EClueType.Item: outFrame.sprite = spriteRed; break;
            case EClueType.Memory: outFrame.sprite = spriteGreen; break;
            case EClueType.Sound: outFrame.sprite = spriteBlue; break;
            //default: borderImage.color = colorDefaultBorder; break;
        }

        // 檢查是否有存檔 (或上一次) 填入的線索
        if (existingClue != null)
        {
            FillSlot(existingClue);
        }
        else
        {
            // 如果是空的，顯示為「空狀態」
            iconImage.sprite = null;
            iconImage.color = colorDefaultIcon; // 顯示為白色內框
            _filledClue = null;
        }
    }

    /// <summary>
    /// 將一個線索填入此格子
    /// </summary>
    public void FillSlot(IClue clue)
    {
        _filledClue = clue;

        // --- [!!] 修正 #1 和 #2 [!!] ---
        // 1. 內框 (IconImage) 始終顯示
        iconImage.gameObject.SetActive(true);
        iconImage.sprite = clue.ClueIcon;
        iconImage.color = colorFilledIcon; // 確保有圖標時也是白色 (或您想要的顏色)

        // 2. 提示文本 (HintText) 始終顯示 (不需要動它，保持原樣)
        // hintText.gameObject.SetActive(true); // 這一行在 Setup 已經做過
        // --- [!!] 修正結束 [!!] ---
    }

    private void OnSlotClicked()
    {
        if (IsLocked || _onClickCallback == null) return;
        _onClickCallback(this);
    }

    /// <summary>
    /// 鎖定格子，使其無法再次點擊
    /// </summary>
    public void Lock()
    {
        IsLocked = true;
    }
}

