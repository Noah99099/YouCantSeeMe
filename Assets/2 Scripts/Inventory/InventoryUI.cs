using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    [Header("功能：控制背包的物件格子UI，包含開關背包面板")]
    [Header("UI 元件")]
    public GameObject inventoryPanel; // 整個背包 UI 的面板
    public Transform itemsContainer;  // 用來放置所有物品格子的容器（現在是 InventoryGrid）
    //public GameObject itemSlotPrefab; // 單一物品格子的 Prefab

    [Header("交互模式設定")]
    public bool isInteractionMode = false; // 是否為交互模式
    public GameObject useItemButton; // 使用物件按鈕
    [TextArea(3, 4)] public string tips;

    private UIInputManager uiInputManager; // 取得管理 action map 的單例（若不存在會為 null）
    private InventorySlotManager slotManager; // 取得管理背包格子的腳本
    private bool isInventoryVisible = false; //面板是否顯示

    // 防止短時間內同一個 action 重複執行
    private float lastOpenInvTime = -1f;
    private const float openInvDebounceSeconds = 0.12f;

    private ItemData currentSelectedItem = null; // 當前選中的物品

    private void Awake()
    {
        if (Instance == null) // 單例設置
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 嘗試取得 UIInputManager 的 Instance（若 UIInputManager 有設定為 singleton 且存在於場景中）
        uiInputManager = UIInputManager.Instance; // 這個是常駐的

        // 嘗試取得 InventorySlotManager 腳本
        slotManager = GetComponent<InventorySlotManager>();
        if (slotManager == null)
        {
            slotManager = gameObject.AddComponent<InventorySlotManager>();
        }

        // 修改初始化方法，只傳入容器，不需要預製體
        slotManager.Initialize(itemsContainer);

        // 初始化使用按鈕狀態
        if (useItemButton != null)
        {
            useItemButton.SetActive(false);
        }

        // 確保背包面板初始狀態正確
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
            isInventoryVisible = false;
        }
        else
        {
            Debug.LogError("inventoryPanel 未設置！");
        }
    }

    private void OnEnable()
    {
        // InventoryManager 事件訂閱（先 -= 再 += 確保只會訂閱一次）
        // 訂閱 InventoryManager 事件（如果存在）
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= UpdateUI; // 先移除避免重複
            InventoryManager.Instance.OnInventoryChanged += UpdateUI;
        }
    }
    private void OnDisable()
    {
        // 取消訂閱 InventoryManager 事件
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= UpdateUI;
        }
    }
    /// <summary>
    /// 使用物件池更新UI
    /// </summary>
    private void UpdateUI()
    {
        if (!isInventoryVisible || InventoryManager.Instance?.items == null)
        {
            Debug.Log($"UpdateUI 被跳過: isInventoryVisible={isInventoryVisible}, items={InventoryManager.Instance?.items}");
            return;
        }

        Debug.Log("更新背包UI");
        slotManager.UpdateSlots(InventoryManager.Instance.items, OnSlotSelected);

        // 如果有選中的物品，更新詳情顯示
        if (currentSelectedItem != null)
        {
            ShowItemDetail(currentSelectedItem);
        }
        else if (slotManager.ActiveSlotsCount > 0 && EventSystem.current != null)
        {
            // 自動選擇第一個格子
            EventSystem.current.SetSelectedGameObject(slotManager.GetFirstSlot());
        }
        else
        {
            // 確保沒有選中物品時隱藏詳情面板
            HideItemDetail();
        }
    }

    #region → 開啟與關閉 InventoryPanel
    /// <summary>
    /// 處理開關背包 UI
    /// </summary>
    public void ToggleInventory(bool interactionMode = false)
    {
        Debug.Log($"ToggleInventory 被調用: isInventoryVisible={isInventoryVisible}, interactionMode={interactionMode}");

        // 防止短時間內重複操作
        if (Time.time - lastOpenInvTime < openInvDebounceSeconds) return;
        lastOpenInvTime = Time.time;

        isInteractionMode = interactionMode;

        if (isInventoryVisible)
        {
            // 檢查 ItemDetailPanel 是否開啟
            if (InventoryManager.Instance?.ItemDetailUI?.detailPanel.activeSelf == true)
            {
                Debug.Log("[InventoryUI] 無法關閉背包：ItemDetailPanel 仍然開啟中");
                return;
            }
            CloseInventory();
        }
        else
        {
            OpenInventory();
        }
    }
    
    // +++ 分離開關邏輯 +++
    private void OpenInventory() //打開背包面板
    {
        Debug.Log("嘗試打開背包");

        // 添加狀態檢查
        if (isInventoryVisible)
        {
            Debug.Log("背包已經打開，跳過");
            return;
        }

        if (inventoryPanel == null)
        {
            Debug.LogError("inventoryPanel 為 null，無法打開");
            return;
        }

        inventoryPanel.SetActive(true);
        isInventoryVisible = true;
        uiInputManager?.EnterInventoryMode();

        // 重置選擇
        currentSelectedItem = null;

        // 確保詳情面板隱藏
        HideItemDetail();

        UpdateUI(); // 打開時確保刷新

        Debug.Log("背包已成功打開");
    }

    public void CloseInventory() //關閉背包面板
    {
        Debug.Log("嘗試關閉背包");

        if (!isInventoryVisible)
        {
            Debug.Log("背包已經關閉，跳過");
            return;
        }

        if (inventoryPanel == null)
        {
            Debug.LogError("inventoryPanel 為 null，無法關閉");
            return;
        }

        inventoryPanel.SetActive(false);
        isInventoryVisible = false;
        if (uiInputManager?.IsInInventoryMode == true) uiInputManager.EnterGameplayMode();

        // 清除 ItemDetail 的預覽
        InventoryManager.Instance?.ItemDetailUI?.ClearPreview();

        // 隱藏使用按鈕
        if (useItemButton != null)
        {
            useItemButton.SetActive(false);
        }

        // 重置交互模式
        isInteractionMode = false;

        Debug.Log("背包已成功關閉");
    }
    #endregion

    /// <summary>
    /// 當格子被選擇時調用
    /// </summary>
    private void OnSlotSelected(ItemData item)
    {
        currentSelectedItem = item;
        ShowItemDetail(item);
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

            // 根據模式和物品顯示/隱藏使用按鈕
            if (useItemButton != null)
            {
                useItemButton.SetActive(isInteractionMode && item != null);

                // 設置使用按鈕的點擊事件
                Button button = useItemButton.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    if (isInteractionMode && item != null)
                    {
                        button.onClick.AddListener(OnUseItemButtonClicked);
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("ItemDetailUI 不可用！");
        }
    }

    /// <summary>
    /// 隱藏物品詳情
    /// </summary>
    private void HideItemDetail()
    {
        if (InventoryManager.Instance.ItemDetailUI != null)
        {
            InventoryManager.Instance.ItemDetailUI.HideItemDetail();
        }
    }

    /// <summary>
    /// 當使用物品按鈕被點擊時調用
    /// </summary>
    private void OnUseItemButtonClicked()
    {
        if (currentSelectedItem != null)
        {
            // 這裡可以觸發物品使用事件或直接處理物品使用邏輯
            Debug.Log($"使用物品: {currentSelectedItem.itemName}");

            // 通知交互系統物品被使用
            // 假設有一個交互管理器處理物品使用
            PlayerInteraction.Instance?.OnItemUsed(currentSelectedItem);

            // 從背包中移除物品
            InventoryManager.Instance.RemoveItem(currentSelectedItem);

            // 關閉背包
            CloseInventory();
        }
    }

    public GameObject GetFirstSelectableSlot() //ItemDetailUI腳本需要
    {
        return slotManager?.GetFirstSlot();
    }

    void OnDestroy()
    {
        // 當物件被摧毀時，取消訂閱，避免記憶體洩漏
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= UpdateUI;
        }
        if (Instance == this) Instance = null;
    }
}