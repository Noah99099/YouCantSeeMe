using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    [Header("功能：控制背包裡的所有UI，包含開關背包面板。調用{InventoryInputToUI 腳本}")]
    [Header("UI 元件")]
    public GameObject inventoryPanel; // 整個背包 UI 的面板
    public Transform itemsContainer;  // 用來放置所有物品格子的容器（現在是 InventoryGrid）

    [Header("滾動相關")]
    public ScrollRect scrollRect; // 在編輯器中分配 Scroll View 上的 ScrollRect 組件
    private RectTransform viewportRect;
    private RectTransform contentRect;

    [Header("滾動設置")]
    public int visibleSlots = 16; // Viewport 可視的格子數量
    public float scrollSmoothTime = 0.2f; // 滾動平滑時間

    [Header("交互模式設定")]
    public bool isInteractionMode = false; // 是否為交互模式：使用物件模式用到
    public GameObject useItemButton; // 使用物件按鈕
    [TextArea(3, 4)] public string tips;

    public InventorySlotManager slotManager; // 取得管理背包格子的腳本
    private bool isInventoryVisible = false; //面板是否顯示

    private ItemData currentSelectedItem = null; // 當前選中的物品

    #region ===== 初始化設置 =====
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // 嘗試取得 InventorySlotManager 腳本
        slotManager = GetComponent<InventorySlotManager>();
        if (slotManager == null) slotManager = gameObject.AddComponent<InventorySlotManager>();

        // 獲取滾動相關組件
        if (scrollRect != null)
        {
            viewportRect = scrollRect.viewport;
            contentRect = scrollRect.content;
        }
        else
        {
            Debug.LogError("ScrollRect 未分配！");
        }

        // 修改初始化方法，只傳入容器，不需要預製體
        slotManager.Initialize(itemsContainer);

        // 初始化使用按鈕狀態
        if (useItemButton != null) useItemButton.SetActive(false);

        // 確保背包面板初始狀態正確
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        isInventoryVisible = false;
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
        UpdateUI();
    }
    private void OnDisable()
    {
        // 取消訂閱 InventoryManager 事件
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= UpdateUI;
        }
    }
    #endregion

    #region → 開啟與關閉 InventoryPanel
    /// <summary>
    /// 處理開關背包 UI
    /// </summary>
    public void ToggleInventory(bool interactionMode = false)
    {
        Debug.Log($"ToggleInventory 被調用: isInventoryVisible={isInventoryVisible}, interactionMode={interactionMode}");

        isInteractionMode = interactionMode;

        if (isInventoryVisible)
        {
            if (InventoryManager.Instance?.ItemDetailUI?.detailPanel.activeSelf == true) return;
            CloseInventory();
        }
        else OpenInventory();
    }
    
    // +++ 分離開關邏輯 +++
    private void OpenInventory() //打開背包面板
    {
        Debug.Log("嘗試打開背包");

        if (isInventoryVisible || inventoryPanel == null) return;

        inventoryPanel.SetActive(true);
        isInventoryVisible = true;

        currentSelectedItem = null;
        HideItemDetail();
        UpdateUI();

        // === 新增：顯示 defaultItem ===
        InventoryManager.Instance.UpdateInformationPanel(null);

        // === 新增：手柄模式才強制選第一格 ===
        if (InputDeviceManager.Instance != null &&
            InputDeviceManager.Instance.CurrentInputType == InputDeviceManager.InputType.Gamepad)
        {
            StartCoroutine(SelectFirstSlotNextFrame());
        }

        // === 已有 ===
        UIInputManager.Instance?.EnterInventoryMode();

        Debug.Log("背包已成功打開");
    }
    /// <summary>
    /// 協程：延遲一幀後選中第一個可選格子
    /// </summary>
    private System.Collections.IEnumerator SelectFirstSlotNextFrame()
    {
        yield return null; // 等一幀，確保 UI 已更新

        var firstSlot = GetFirstSelectableSlot();
        if (firstSlot != null && InventorySelection.Instance != null)
        {
            InventorySelection.Instance.SetSelected(firstSlot);
        }
    }

    public void CloseInventory() //關閉背包面板
    {
        Debug.Log("嘗試關閉背包");

        if (!isInventoryVisible || inventoryPanel == null) return;

        inventoryPanel.SetActive(false);
        isInventoryVisible = false;

        InventoryManager.Instance?.ItemDetailUI?.ClearPreview();

        if (useItemButton != null) useItemButton.SetActive(false);
        isInteractionMode = false;

        // === 新增 ===
        UIInputManager.Instance?.EnterGameplayMode();

        Debug.Log("背包已成功關閉");
    }
    #endregion

    // 被 slot 的 Button.onClick 與 InventorySlotUI 的 OnPointerClick 呼叫
    public void OnSlotClicked(ItemData item)
    {
        currentSelectedItem = item;

        // 找到對應的 slotGO
        var slotGO = slotManager.GetSlotGOByItem(item);
        if (slotGO != null)
            InventoryManager.Instance.SelectSlot(slotGO, item);


        // == 確保交互模式下顯示使用按鈕 ==
        if (isInteractionMode && useItemButton != null)
            useItemButton.SetActive(true);
    }

    // 當選中（Selection Changed）時要呼叫此方法（InventorySlotUI.OnSelect 會呼）
    public void SetCurrentSelectedItem(ItemData item)
    {
        currentSelectedItem = item;
        // 不直接打開模型，只更新右側（必要時）
        InventoryManager.Instance?.UpdateInformationPanel(item);
        // == 確保交互模式下顯示使用按鈕 ==
        if (isInteractionMode && useItemButton != null)
            useItemButton.SetActive(true);
    }

    #region ===== 預覽物件相關（可能重寫一個） =====
    /// <summary>
    /// 顯示物品詳情（從InventoryManager獲取）［不知道舊程式有沒有衝突OpenSelectedItemDetail］
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

    // 由 Inventory Action Map 的 OpenItemDetail (Submit) 觸發
    // 公開供 Input handler 呼叫
    public void OpenSelectedItemDetail()
    {
        if (currentSelectedItem == null) return;

        // 顯示模型/詳情面板（你的 ItemDetailUI）
        InventoryManager.Instance.ItemDetailUI.ShowItemDetail(currentSelectedItem);

        // 根據是否為 interactionMode 顯示使用按鈕
        if (useItemButton != null)
        {
            useItemButton.SetActive(isInteractionMode && currentSelectedItem != null);
            var btn = useItemButton.GetComponent<UnityEngine.UI.Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                if (isInteractionMode)
                    btn.onClick.AddListener(OnUseItemButtonClicked);
            }
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
    #endregion

    /// <summary>
    /// 使用物件池更新UI
    /// </summary>
    private void UpdateUI()
    {
        if (InventoryManager.Instance?.items == null)
        {
            Debug.LogWarning("UpdateUI 被跳過: InventoryManager.items 為 null");
            return;
        }

        Debug.Log($"更新背包UI (isInventoryVisible={isInventoryVisible})");
        slotManager.UpdateSlots(InventoryManager.Instance.items, OnSlotClicked); // 更新所有格子
        if (!isInventoryVisible) return; //新增

        if (isInventoryVisible)
        {
            if (currentSelectedItem != null)
            {
                ShowItemDetail(currentSelectedItem);
            }
            else if (slotManager.ActiveSlotsCount > 0 && EventSystem.current != null)
            {
                // 手柄模式下自動選中第一個格子
                if (InputDeviceManager.Instance != null &&
                    InputDeviceManager.Instance.CurrentInputType == InputDeviceManager.InputType.Gamepad)
                {
                    var firstSlot = slotManager.GetFirstSlot();
                    if (firstSlot != null)
                    {
                        InventorySelection.Instance.SetSelected(firstSlot);
                    }
                }
            }
            else
            {
                InventorySelection.Instance.ClearSelection();
                HideItemDetail();
            }
        }
    }

    public GameObject GetFirstSelectableSlot() //ItemDetailUI腳本需要
    {
        return slotManager?.GetFirstSlot();
    }

    #region ===== 滾動系統===== 
    /// <summary>
    /// 確保指定索引的格子可見
    /// </summary>
    public void EnsureSlotVisible(int slotIndex)
    {
        if (scrollRect == null || itemsContainer == null) return;

        int totalSlots = itemsContainer.childCount;
        if (totalSlots <= visibleSlots) return; // 不需要滾動

        // 計算行數和列數
        int columns = GetColumnsCount();
        int rows = Mathf.CeilToInt(totalSlots / (float)columns);
        int visibleRows = Mathf.CeilToInt(visibleSlots / (float)columns);

        // 計算目標格子所在的行
        int targetRow = Mathf.FloorToInt(slotIndex / (float)columns);

        // 計算需要滾動到的行
        int scrollToRow = 0;
        if (targetRow >= visibleRows - 1)
        {
            scrollToRow = targetRow - (visibleRows - 1);
        }

        // 計算滾動位置 (Bottom To Top: 0=底部, 1=頂部)
        float scrollableHeight = Mathf.Max(0, contentRect.rect.height - viewportRect.rect.height);
        if (scrollableHeight <= 0) return;

        GridLayoutGroup gridLayout = itemsContainer.GetComponent<GridLayoutGroup>();
        if (gridLayout == null) return;

        float rowHeight = gridLayout.cellSize.y + gridLayout.spacing.y;
        float targetPosition = scrollToRow * rowHeight;

        // Bottom To Top: 需要從底部計算位置
        float normalizedPosition = 1f - (targetPosition / scrollableHeight);

        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(normalizedPosition);
    }

    /// <summary>
    /// 確保指定格子可見
    /// </summary>
    public void EnsureSlotVisible(Transform slotTransform)
    {
        if (slotTransform == null || itemsContainer == null) return;

        // 找到格子的索引
        for (int i = 0; i < itemsContainer.childCount; i++)
        {
            if (itemsContainer.GetChild(i) == slotTransform)
            {
                EnsureSlotVisible(i);
                return;
            }
        }
    }

    /// <summary>
    /// 計算指定行的標準化滾動位置
    /// </summary>
    private float CalculateNormalizedPositionForRow(int row)
    {
        if (contentRect == null || viewportRect == null) return 0f;

        float contentHeight = contentRect.rect.height;
        float viewportHeight = viewportRect.rect.height;
        float scrollableHeight = Mathf.Max(0, contentHeight - viewportHeight);

        if (scrollableHeight <= 0) return 0f;

        // 計算該行對應的滾動位置
        GridLayoutGroup gridLayout = itemsContainer.GetComponent<GridLayoutGroup>();
        if (gridLayout == null) return 0f;

        float rowHeight = gridLayout.cellSize.y + gridLayout.spacing.y;
        float rowPosition = row * rowHeight;

        // Top To Bottom: 0=頂部, 1=底部
        return 1 - Mathf.Clamp01(rowPosition / scrollableHeight);
    }

    /// <summary>
    /// 獲取列數
    /// </summary>
    private int GetColumnsCount()
    {
        GridLayoutGroup gridLayout = itemsContainer.GetComponent<GridLayoutGroup>();
        return gridLayout != null ? gridLayout.constraintCount : 4; // 默認4列
    }
    #endregion
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