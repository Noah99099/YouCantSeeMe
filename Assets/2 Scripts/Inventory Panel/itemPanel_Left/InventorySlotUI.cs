using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

/// <summary>
/// 負責單一背包格子的 UI 行為：
/// - 綁定資料
/// - 處理選中（鍵盤 / 手柄）
/// - 處理點擊（滑鼠）
/// </summary>

public class InventorySlotUI : MonoBehaviour, ISelectHandler, IPointerClickHandler
{
    private ItemData boundItem;
    private Button button;
    private Image itemIcon;

    /// <summary>
    /// 提供外部讀取當前綁定的 ItemData
    /// </summary>
    public ItemData BoundItem => boundItem; // ← 新增公開屬性。新增 getter，供外部查詢

    private void Awake()
    {
        button = GetComponent<Button>();
        itemIcon = transform.Find("ItemIcon")?.GetComponent<Image>();
    }

    /// <summary>
    /// 綁定物品，並設定點擊回調
    /// </summary>
    public void SetItem(ItemData item, Action<ItemData> onClick)
    {
        boundItem = item;

        // 顯示圖標
        if (itemIcon != null)
        {
            itemIcon.sprite = item?.icon;
            itemIcon.enabled = item != null;
        }

        // 設定按鈕點擊
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            if (item != null && onClick != null)
                button.onClick.AddListener(() => onClick(boundItem));
            button.interactable = item != null;
        }
    }

    /// <summary>
    /// 清空格子
    /// </summary>
    public void ClearSlot()
    {
        boundItem = null;
        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
        }
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.interactable = false;
        }
    }
    public void Bind(ItemData item) 
    {
        boundItem = item;
    }

    // 手柄 / 鍵盤選中 → 更新右側資訊 (selected 行為)
    public void OnSelect(BaseEventData eventData)
    {
        // 只更新面板和 currentSelectedItem，不再重複呼叫 SetSelectedGameObject
        // 更新 InventoryUI → 通知「目前選中的 item」
        InventoryManager.Instance?.SelectSlot(gameObject, boundItem);
        InventoryUI.Instance?.SetCurrentSelectedItem(boundItem);

        // 手柄模式下，不自動開啟 ModelPreview
        // 只更新右側文字面板
        InventoryUI.Instance?.UpdateItemDetail(boundItem, false);
    }

    // 滑鼠點擊 → 視為「選中 / 點擊格子」(但不直接開模型面板)
    public void OnPointerClick(PointerEventData eventData)
    {
        InventoryUI.Instance?.OnSlotClicked(boundItem);
    }
}
