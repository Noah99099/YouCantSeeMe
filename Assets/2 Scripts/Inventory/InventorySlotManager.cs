using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotManager : MonoBehaviour
{
    private Queue<GameObject> itemSlotPool = new Queue<GameObject>();
    private List<GameObject> activeSlots = new List<GameObject>();
    private Transform itemsContainer;
    private GameObject itemSlotPrefab;
    private const int INITIAL_POOL_SIZE = 5;

    public int ActiveSlotsCount => activeSlots.Count;

    public void Initialize(Transform container, GameObject prefab)
    {
        itemsContainer = container;
        itemSlotPrefab = prefab;
        InitializeSlotPool();
    }

    private void InitializeSlotPool()
    {
        int inventorySize = 0;
        if (InventoryManager.Instance != null && InventoryManager.Instance.items != null)
            inventorySize = InventoryManager.Instance.items.Count;

        for (int i = 0; i < Mathf.Max(INITIAL_POOL_SIZE, inventorySize); i++)
        {
            GameObject slot = CreateNewSlot();
            itemSlotPool.Enqueue(slot);
        }
    }

    private GameObject CreateNewSlot()
    {
        GameObject slot = Instantiate(itemSlotPrefab, itemsContainer);
        slot.SetActive(false);
        return slot;
    }

    private GameObject GetSlotFromPool()
    {
        if (itemSlotPool.Count > 0) return itemSlotPool.Dequeue();
        return CreateNewSlot();
    }

    private void ReturnSlotToPool(GameObject slot)
    {
        slot.SetActive(false);
        Button button = slot.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
        }
        itemSlotPool.Enqueue(slot);
    }

    public void UpdateSlots(List<ItemData> items, System.Action<ItemData> onClickAction)
    {
        // 先歸還所有現有格子
        while (activeSlots.Count > 0)
        {
            ReturnSlotToPool(activeSlots[0]);
            activeSlots.RemoveAt(0);
        }

        // 確保物件池中有足夠格子
        while (itemSlotPool.Count < items.Count)
        {
            GameObject newSlot = CreateNewSlot();
            itemSlotPool.Enqueue(newSlot);
        }

        // 創建新格子
        for (int i = 0; i < items.Count; i++)
        {
            GameObject slot = GetSlotFromPool();
            slot.SetActive(true);
            SetupSlot(slot, items[i], onClickAction);
            slot.transform.SetSiblingIndex(i);
            activeSlots.Add(slot);

            Debug.Log($"創建格子 {i} 顯示物品 {items[i].itemName}");
        }

        Debug.Log($"總共創建 {activeSlots.Count} 個格子，對應 {items.Count} 個物品");
    }

    private void SetupSlot(GameObject slot, ItemData item, System.Action<ItemData> onClickAction)
    {
        Image itemIcon = slot.transform.Find("ItemIcon")?.GetComponent<Image>();
        if (itemIcon != null)
        {
            itemIcon.sprite = item.icon;
            itemIcon.enabled = true;
        }

        Button button = slot.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClickAction?.Invoke(item));
        }
    }

    public GameObject GetFirstSlot()
    {
        return activeSlots.Count > 0 ? activeSlots[0] : null;
    }
}