using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotManager : MonoBehaviour
{
    //private Queue<GameObject> itemSlotPool = new Queue<GameObject>();
    private List<GameObject> allSlots = new List<GameObject>(); // 所有格子
    private List<GameObject> activeSlots = new List<GameObject>(); // 當前有物品的格子
    private Transform itemsContainer;

    public int ActiveSlotsCount => activeSlots.Count;

    public void Initialize(Transform container)
    {
        itemsContainer = container;
        //itemSlotPrefab = prefab;
        InitializeAllSlots();
    }

    private void InitializeAllSlots()
    {
        // 獲取所有格子
        allSlots.Clear();
        for (int i = 0; i < itemsContainer.childCount; i++)
        {
            GameObject slot = itemsContainer.GetChild(i).gameObject;
            allSlots.Add(slot);

            // 初始化為空狀態
            SetupSlot(slot, null, null);
        }
    }

    public void UpdateSlots(List<ItemData> items, System.Action<ItemData> onClickAction)
    {
        // 先重置所有格子為空狀態
        foreach (GameObject slot in allSlots)
        {
            SetupSlot(slot, null, onClickAction);
        }

        // 更新活躍格子列表
        activeSlots.Clear();

        // 設置有物品的格子
        for (int i = 0; i < items.Count && i < allSlots.Count; i++)
        {
            GameObject slot = allSlots[i];
            SetupSlot(slot, items[i], onClickAction);
            activeSlots.Add(slot);
        }
    }

    private void SetupSlot(GameObject slot, ItemData item, System.Action<ItemData> onClickAction)
    {
        Image itemIcon = slot.transform.Find("ItemIcon")?.GetComponent<Image>();
        if (itemIcon != null)
        {
            if (item != null && item.icon != null)
            {
                itemIcon.sprite = item.icon;
                itemIcon.enabled = true;
            }
            else
            {
                itemIcon.sprite = null;
                itemIcon.enabled = false;
            }
        }

        // 綁定 InventorySlotUI
        InventorySlotUI slotUI = slot.GetComponent<InventorySlotUI>();
        if (slotUI != null)
        {
            slotUI.Bind(item);
        }

        Button button = slot.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClickAction?.Invoke(item));
            button.interactable = (item != null) || true;
        }
    }

    /// <summary>
    /// 獲取首個可用的遊戲物件插槽
    /// 優先返回活躍插槽列表中的第一個元素，
    /// 若無活躍插槽則返回全部插槽列表中的第一個元素，
    /// 若兩個列表皆空則返回 null
    /// </summary>
    /// <returns>
    /// 返回找到的首個遊戲物件插槽，若無可用插槽則返回 null
    /// </returns>
    public GameObject GetFirstSlot()
    {
        return activeSlots.Count > 0 ? activeSlots[0] : (allSlots.Count > 0 ? allSlots[0] : null);
    }
}