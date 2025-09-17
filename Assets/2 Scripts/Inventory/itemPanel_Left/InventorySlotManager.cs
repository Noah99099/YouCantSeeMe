using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotManager : MonoBehaviour
{
    //private Queue<GameObject> itemSlotPool = new Queue<GameObject>();
    private List<GameObject> allSlots = new List<GameObject>(); // 核心背包格子列表
    private List<GameObject> activeSlots = new List<GameObject>(); // 當前有物品的格子

    [Header("背包格子容器")]
    [SerializeField] private Transform itemsContainer;

    public int ActiveSlotsCount => activeSlots.Count;

    /// <summary>
    /// 當前有物品的格子列表
    /// </summary>
    public List<GameObject> ActiveSlots => allSlots.FindAll(slot =>
    {
        var slotUI = slot.GetComponent<InventorySlotUI>();
        return slotUI != null && slotUI.BoundItem != null && slotUI.BoundItem != InventoryManager.Instance.defaultItem;
    });

    //初始化
    public void Initialize(Transform container)
    {
        //itemsContainer = container;
        //InitializeAllSlots();
        itemsContainer = container;
        allSlots.Clear();

        foreach (Transform child in container)
        {
            GameObject slot = child.gameObject;
            allSlots.Add(slot);

            // 初始化為默認格子狀態
            SetupSlot(slot, InventoryManager.Instance.defaultItem, null);
        }
    }

    private void InitializeAllSlots()
    {
        allSlots.Clear();
        foreach (Transform child in itemsContainer)
        {
            GameObject slot = child.gameObject;
            allSlots.Add(slot);
            SetupSlot(slot, InventoryManager.Instance.defaultItem, null);
        }
    }

    // 根據 ItemData 找對應格子
    public GameObject GetSlotGOByItem(ItemData item)
    {
        if (item == null) return null;

        foreach (var slot in allSlots)
        {
            var slotUI = slot.GetComponent<InventorySlotUI>();
            if (slotUI != null && slotUI.BoundItem == item)
                return slot;
        }
        return null;
    }

    public void UpdateSlots(List<ItemData> items, System.Action<ItemData> onClickAction)
    {
        //// 先重置所有格子為空狀態
        //foreach (GameObject slot in allSlots)
        //{
        //    SetupSlot(slot, null, onClickAction);
        //}

        //// 更新活躍格子列表
        //activeSlots.Clear();

        //// 設置有物品的格子
        //for (int i = 0; i < items.Count && i < allSlots.Count; i++)
        //{
        //    GameObject slot = allSlots[i];
        //    SetupSlot(slot, items[i], onClickAction);
        //    activeSlots.Add(slot);
        //}
        for (int i = 0; i < allSlots.Count; i++)
        {
            ItemData item = i < items.Count ? items[i] : InventoryManager.Instance.defaultItem;
            SetupSlot(allSlots[i], item, onClickAction);
        }
    }

    /// <summary>
    /// 設置單個格子
    /// </summary>
    private void SetupSlot(GameObject slot, ItemData item, System.Action<ItemData> onClickAction)
    {
        if (slot == null) return;

        // 設置圖標
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

        // 設置按鈕點擊事件
        Button button = slot.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClickAction?.Invoke(item));
            button.interactable = true;
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
    //public GameObject GetFirstSlot()
    //{
    //    return activeSlots.Count > 0 ? activeSlots[0] : (allSlots.Count > 0 ? allSlots[0] : null);
    //}
    public GameObject GetFirstSlot()
    {
        return allSlots.Count > 0 ? allSlots[0] : null;
    }
}