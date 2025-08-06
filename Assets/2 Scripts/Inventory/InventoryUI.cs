using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("UI 元件")]
    public GameObject inventoryPanel; // 整個背包 UI 的面板
    public Transform itemsContainer;  // 用來放置所有物品格子的容器
    public GameObject itemSlotPrefab; // 單一物品格子的 Prefab

    // +++ 新增：物件池系統 +++
    private Queue<GameObject> itemSlotPool = new Queue<GameObject>();
    private List<GameObject> activeSlots = new List<GameObject>();
    private const int INITIAL_POOL_SIZE = 20;

    private bool isInventoryVisible; //面板是否顯示

    void Start()
    {
        isInventoryVisible = inventoryPanel.activeSelf; // inventoryPanel 在 Inspector 的顯示確認
        InventoryManager.Instance.OnInventoryChanged += UpdateUI;

        // +++ 初始化物件池 +++
        InitializeSlotPool();

        // 如果Inspector是打開的，需要更新一次UI然後再關掉面板
        if (isInventoryVisible) 
        {
            UpdateUI();
            ToggleInventory();
        } 
    }

    /// <summary>
    /// 初始化物品格子物件池
    /// </summary>
    private void InitializeSlotPool()
    {
        for (int i = 0; i < INITIAL_POOL_SIZE; i++)
        {
            GameObject slot = CreateNewSlot();
            itemSlotPool.Enqueue(slot);
        }
    }

    // <summary>
    /// 創建新格子（加入物件池）
    /// </summary>
    private GameObject CreateNewSlot()
    {
        GameObject slot = Instantiate(itemSlotPrefab, itemsContainer);
        slot.SetActive(false);
        return slot;
    }

    /// <summary>
    /// 從物件池獲取格子
    /// </summary>
    private GameObject GetSlotFromPool()
    {
        if (itemSlotPool.Count > 0)
        {
            return itemSlotPool.Dequeue();
        }
        return CreateNewSlot();
    }

    /// <summary>
    /// 歸還格子到物件池
    /// </summary>
    private void ReturnSlotToPool(GameObject slot)
    {
        slot.SetActive(false);
        itemSlotPool.Enqueue(slot);
    }

    void OnDestroy()
    {
        // 當物件被摧毀時，取消訂閱，避免記憶體洩漏
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= UpdateUI;
        }
    }

    void Update()
    {
        // 按下 B 鍵來開關背包
        if (Input.GetKeyDown(KeyCode.B))
        {
            ToggleInventory();
        }
    }

    /// <summary>
    /// 開關背包 UI
    /// </summary>
    public void ToggleInventory()
    {
        isInventoryVisible = !isInventoryVisible;
        inventoryPanel.SetActive(isInventoryVisible);

        // --- 這裡就是關鍵 ---
        if (isInventoryVisible)
        {
            // 如果是打開背包，就進入 UI 模式
            CursorManager.EnterUIMode();
            UpdateUI(); // 更新 UI 顯示
        }
        else
        {
            // 如果是關閉背包，就回到遊戲模式
            CursorManager.EnterGameplayMode();
        }
    }

    /// <summary>
    /// 使用物件池更新UI
    /// </summary>
    private void UpdateUI()
    {
        // 1. 歸還不再需要的格子
        for (int i = activeSlots.Count - 1; i >= 0; i--)
        {
            ReturnSlotToPool(activeSlots[i]);
        }
        activeSlots.Clear();

        // 2. 從池中獲取並設置需要的格子
        foreach (ItemData item in InventoryManager.Instance.items)
        {
            GameObject slotInstance = GetSlotFromPool();
            slotInstance.SetActive(true);

            // 設置物品圖標
            Image itemIcon = slotInstance.transform.Find("ItemIcon")?.GetComponent<Image>();
            if (itemIcon != null)
            {
                itemIcon.sprite = item.icon;
                itemIcon.enabled = true;
            }

            // 綁定點擊事件
            Button button = slotInstance.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners(); // 清除舊監聽器
                button.onClick.AddListener(() => ShowItemDetail(item));
            }

            activeSlots.Add(slotInstance);
        }
    }

    /// <summary>
    /// 顯示物品詳情（從InventoryManager獲取）
    /// </summary>
    private void ShowItemDetail(ItemData item)
    {
        // +++ 修改：從InventoryManager獲取ItemDetailUI +++
        if (InventoryManager.Instance.ItemDetailUI != null)
        {
            InventoryManager.Instance.ItemDetailUI.ShowItemDetail(item);
        }
        else
        {
            Debug.LogWarning("ItemDetailUI 不可用！");
        }
    }
}