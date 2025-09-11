using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotManager : MonoBehaviour
{
    //private Queue<GameObject> itemSlotPool = new Queue<GameObject>();
    private List<GameObject> allSlots = new List<GameObject>(); // 所有格子
    private List<GameObject> activeSlots = new List<GameObject>(); // 當前有物品的格子
    private Transform itemsContainer;
    //private GameObject itemSlotPrefab;
    //private const int INITIAL_POOL_SIZE = 5;

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
    #region ===== 舊代碼 =====
    //private void InitializeSlotPool()
    //{
    //    int inventorySize = 0;
    //    if (InventoryManager.Instance != null && InventoryManager.Instance.items != null)
    //        inventorySize = InventoryManager.Instance.items.Count;

    //    for (int i = 0; i < Mathf.Max(INITIAL_POOL_SIZE, inventorySize); i++)
    //    {
    //        GameObject slot = CreateNewSlot();
    //        itemSlotPool.Enqueue(slot);
    //    }
    //}

    //private GameObject CreateNewSlot()
    //{
    //    GameObject slot = Instantiate(itemSlotPrefab, itemsContainer);
    //    slot.SetActive(false);
    //    return slot;
    //}

    //private GameObject GetSlotFromPool()
    //{
    //    if (itemSlotPool.Count > 0) return itemSlotPool.Dequeue();
    //    return CreateNewSlot();
    //}

    //private void ReturnSlotToPool(GameObject slot)
    //{
    //    slot.SetActive(false);
    //    Button button = slot.GetComponent<Button>();
    //    if (button != null)
    //    {
    //        button.onClick.RemoveAllListeners();
    //    }
    //    itemSlotPool.Enqueue(slot);
    //}
    #endregion

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
            if (item != null)
            {
                itemIcon.sprite = item.icon;
                itemIcon.enabled = true;
            }
            else
            {
                // 顯示空格子的預設圖（若沒有預設圖就隱藏）
                itemIcon.sprite = InventoryManager.Instance.defaultItem != null ? InventoryManager.Instance.defaultItem.icon : null;
                itemIcon.enabled = itemIcon.sprite != null;
            }
        }

        Button button = slot.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            if (item != null)
            {
                button.onClick.AddListener(() => onClickAction?.Invoke(item));

                // 更新右側資訊面板
                InventoryManager.Instance.UpdateDetailPanel(item);

                button.interactable = true; // 有物品的格子可以交互
            }
            else
            {
                button.onClick.AddListener(() => onClickAction?.Invoke(null));

                // 如果沒有物品，顯示默認內容
                InventoryManager.Instance.itemImage.sprite = InventoryManager.Instance.defaultItem.icon;
                InventoryManager.Instance.itemNameText.text = InventoryManager.Instance.defaultItem.itemName;
                InventoryManager.Instance.itemDescriptionText.text = InventoryManager.Instance.defaultItem.description;

                button.interactable = true; // 空格子也可以交互，但會傳遞null
            }
        }
    }

    public GameObject GetFirstSlot()
    {
        return activeSlots.Count > 0 ? activeSlots[0] : (allSlots.Count > 0 ? allSlots[0] : null);
    }
}