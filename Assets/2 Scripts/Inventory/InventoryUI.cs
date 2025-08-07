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
    private Queue<GameObject> itemSlotPool = new Queue<GameObject>(); //閒置的 itemSlot 預設池，用來回收、重複使用格子
    private List<GameObject> activeSlots = new List<GameObject>(); //當前背包中顯示的 slot，會根據 item 數量更新
    private const int INITIAL_POOL_SIZE = 5;

    private bool isInventoryVisible; //面板是否顯示

    void Start()
    {
        // +++ 確保狀態同步 +++
        isInventoryVisible = inventoryPanel.activeSelf;
        Debug.Log("當前背包面板顯示狀態："+ isInventoryVisible);

        // 強制關閉面板（無論初始狀態）
        CloseInventory();

        // 初始化物件池（預先創建少量格子）
        InitializeSlotPool();

        // +++ 初始更新UI +++
        UpdateUI();
    }

    private void OnEnable()
    {
        // +++ 安全訂閱事件 +++
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= UpdateUI; // 先取消避免重複
            InventoryManager.Instance.OnInventoryChanged += UpdateUI;
        }
    }
    private void OnDisable()
    {
        // +++ 安全取消訂閱 +++
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= UpdateUI;
        }
    }

    /// <summary>
    /// 初始化物品格子物件池
    /// </summary>
    private void InitializeSlotPool()
    {
        int inventorySize = InventoryManager.Instance.items.Count;
        for (int i = 0; i < Mathf.Max(INITIAL_POOL_SIZE, inventorySize); i++)
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
        // +++重置格子狀態++ +
        slot.SetActive(false);

        // 清除按鈕監聽器
        Button button = slot.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
        }

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
        if (Input.GetKeyDown(KeyCode.B))
        {
            // 如果打開了 Inventory 且 ItemDetailPanel 也開著，就不執行 Toggle
            if (isInventoryVisible && InventoryManager.Instance.ItemDetailUI != null)
            {
                if (InventoryManager.Instance.ItemDetailUI.detailPanel.activeSelf)
                {
                    Debug.Log("B 鍵按下，但 ItemDetailPanel 開啟中，忽略 ToggleInventory。");
                    return;
                }
            }

            ToggleInventory();
            Debug.Log("當前背包面板顯示狀態：" + isInventoryVisible);
        }
    }

    /// <summary>
    /// 開關背包 UI
    /// </summary>
    public void ToggleInventory()
    {
        if (inventoryPanel.activeSelf)
        {
            // 檢查是否開啟了 ItemDetailPanel，若是就不關閉背包
            if (InventoryManager.Instance.ItemDetailUI != null &&
                InventoryManager.Instance.ItemDetailUI.detailPanel.activeSelf)
            {
                Debug.Log("無法關閉背包：ItemDetailPanel 仍然開啟中");
                return;
            }

            CloseInventory(); // 正常關閉
        }
        else
        {
            OpenInventory(); // 正常開啟
        }
    }

    // +++ 分離開關邏輯 +++
    private void OpenInventory()
    {
        isInventoryVisible = true;
        inventoryPanel.SetActive(true);
        CursorManager.EnterUIMode();
        UpdateUI(); // 打開時確保刷新
    }

    private void CloseInventory()
    {
        isInventoryVisible = false;
        inventoryPanel.SetActive(false);
        CursorManager.EnterGameplayMode();
        InventoryManager.Instance.ItemDetailUI?.ClearPreview();
    }

    /// <summary>
    /// 使用物件池更新UI
    /// </summary>
    private void UpdateUI()
    {
        Debug.Log("更新 UI，物品數：" + InventoryManager.Instance.items.Count);

        // +++ 面板關閉時不更新可見元素 +++
        if (!isInventoryVisible) return;

        // 1. 歸還不再需要的格子
        for (int i = activeSlots.Count - 1; i >= 0; i--)
        {
            ReturnSlotToPool(activeSlots[i]);
        }
        activeSlots.Clear();

        // 2. 重新生成對應物品的格子
        foreach (ItemData item in InventoryManager.Instance.items)
        {
            GameObject slot = GetSlotFromPool();
            slot.SetActive(true);
            SetupSlot(slot, item);
            activeSlots.Add(slot);
        }
    }

    /// <summary>
    /// 提取共用設定方
    /// </summary>
    /// <param name="slot">背包格子</param>
    /// <param name="item">物件</param>
    private void SetupSlot(GameObject slot, ItemData item)
    {
        // 設置圖標
        Image itemIcon = slot.transform.Find("ItemIcon")?.GetComponent<Image>();
        if (itemIcon != null)
        {
            itemIcon.sprite = item.icon;
            itemIcon.enabled = true;
        }

        // 設置按鈕事件
        Button button = slot.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => ShowItemDetail(item));
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