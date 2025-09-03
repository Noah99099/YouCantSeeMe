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
    public Transform itemsContainer;  // 用來放置所有物品格子的容器
    public GameObject itemSlotPrefab; // 單一物品格子的 Prefab

    private UIInputManager uiInputManager; // 取得管理 action map 的單例（若不存在會為 null）
    private InventorySlotManager slotManager; // 取得管理背包格子的腳本
    private bool isInventoryVisible = false; //面板是否顯示

    // 防止短時間內同一個 action 重複執行
    private float lastOpenInvTime = -1f;
    private const float openInvDebounceSeconds = 0.12f;

    private void Awake()
    {
        // 嘗試取得 UIInputManager 的 Instance（若 UIInputManager 有設定為 singleton 且存在於場景中）
        uiInputManager = UIInputManager.Instance; // 這個是常駐的

        // 嘗試取得 InventorySlotManager 腳本
        slotManager = GetComponent<InventorySlotManager>();
        if (slotManager == null)
        {
            slotManager = gameObject.AddComponent<InventorySlotManager>();
        }

        slotManager.Initialize(itemsContainer, itemSlotPrefab); //執行 InventorySlotManager 的 Initialize 方法
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
        if (!isInventoryVisible || InventoryManager.Instance?.items == null) return;
        slotManager.UpdateSlots(InventoryManager.Instance.items, ShowItemDetail);
        if (slotManager.ActiveSlotsCount > 0 && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(slotManager.GetFirstSlot());
        }
    }

    #region → 開啟與關閉 InventoryPanel
    /// <summary>
    /// 處理開關背包 UI
    /// </summary>
    public void ToggleInventory()
    {
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
        // 添加狀態檢查
        if (isInventoryVisible) return;
        inventoryPanel.SetActive(true);
        isInventoryVisible = true;
        uiInputManager?.EnterInventoryMode();

        UpdateUI(); // 打開時確保刷新
    }

    public void CloseInventory() //關閉背包面板
    {
        if (!isInventoryVisible) return; // 添加狀態檢查避免重複調用
        inventoryPanel.SetActive(false);
        isInventoryVisible = false;
        if (uiInputManager?.IsInInventoryMode == true) uiInputManager.EnterGameplayMode();

        // 清除 ItemDetail 的預覽
        InventoryManager.Instance?.ItemDetailUI?.ClearPreview();
    }
    #endregion

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