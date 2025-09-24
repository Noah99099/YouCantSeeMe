using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    [Header("功能：控制背包裡的所有UI，包含開關背包面板。調用{InventoryInputToUI 腳本}")]
    [Header("Canvas面板")]
    public GameObject crossHairCanvas;
    public GameObject uiCanvas;

    [Header("UI 元件")]
    public GameObject inventoryPanel; // 整個背包 UI 的面板
    public Transform itemsContainer;  // 用來放置所有物品格子的容器（現在是 InventoryGrid）
    public Button itemPreviewButton; // 指向右側「預覽物件按鈕」
    public Button useItemButton; // 使用物件按鈕

    [Header("滾動相關")]
    public ScrollRect scrollRect; // 在編輯器中分配 Scroll View 上的 ScrollRect 組件
    private RectTransform viewportRect;
    private RectTransform contentRect;
    public int visibleSlots = 16; // Viewport 可視的格子數量
    public float scrollSmoothTime = 0.2f; // 滾動平滑時間

    [Header("格子管理腳本")]
    public InventorySlotManager slotManager; // 取得管理背包格子的腳本
    [Header("模型預覽面板腳本")]
    public ItemDetailUI itemDetailUI;

    [Header("交互模式設定")]
    public bool isInteractionMode = false; // 是否為交互模式：使用物件模式用到
    [TextArea(3, 4)] public string tips;

    [Header("背包狀態")]
    private bool canToggle = true; //處理背包開關
    private float toggleCooldown = 0.2f; // 防抖時間
    public bool isInventoryVisible { get; private set; } = false; // 背包面板是否顯示
    

    private ItemData currentSelectedItem = null; // 當前選中的物品
    public ItemData CurrentSelectedItem => currentSelectedItem;

    // SwitchInventoryPageButton腳本接收
    public event Action OnInventoryOpened;
    public event Action OnInventoryClosed;

    #region ===== 初始化設置 =====
    private void Awake()
    {
        // 單例模式 + 跨場景存活
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 嘗試取得 InventorySlotManager 腳本
        slotManager = GetComponent<InventorySlotManager>();
        if (slotManager == null) slotManager = gameObject.AddComponent<InventorySlotManager>();

        // 修改初始化方法，只傳入容器，不需要預製體
        slotManager.Initialize(itemsContainer);

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

        inventoryPanel?.SetActive(false);

        itemPreviewButton?.onClick.AddListener(OnItemPreviewButtonClicked);
        useItemButton?.onClick.AddListener(OnUseItemButtonClicked);
        useItemButton?.gameObject.SetActive(false);
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

    #region ===== InventoryPanel 開關邏輯 =====
    /// <summary>
    /// 處理開關背包 UI
    /// </summary>
    public void ToggleInventory(bool interactionMode = false)
    {
        if (!canToggle) return;
        canToggle = false;

        //isInteractionMode = interactionMode;

        if (isInventoryVisible)
        {
            if (InventoryManager.Instance?.ItemDetailUI?.modelPreviewPanel.activeSelf == true) return; // 模型面板開啟中不關閉
            CloseInventory();
        }
        else
        {
            OpenInventoryInternal(false);
        }

        // 重置防抖
        Invoke(nameof(ResetToggleCooldown), toggleCooldown);
    }
    private void ResetToggleCooldown() => canToggle = true;

    /// <summary>
    /// 公用打開背包方法，可選擇是否恢復之前選中 slot。真正打開背包的方法。
    /// </summary>
    private void OpenInventoryInternal(bool restorePreviousSlot) //打開背包面板
    {
        if (isInventoryVisible || inventoryPanel == null) return;

        // 先啟用面板，避免協程報錯
        inventoryPanel.SetActive(true);
        isInventoryVisible = true;
        OnInventoryOpened?.Invoke(); //通知SwitchInventoryPageButton腳本

        currentSelectedItem = null;
        //HideItemDetail();
        UpdateUI();

        //InventoryManager.Instance?.UpdateInformationPanel(null);

        //var firstSlot = slotManager.GetFirstSlot();
        //if (firstSlot != null)
        //    InventorySelection.Instance?.SetSelected(firstSlot.gameObject);

        // 延遲一幀再選中第一個格子
        StartCoroutine(SelectFirstSlotNextFrame(restorePreviousSlot));

        UIInputManager.Instance?.EnterInventoryMode();
        crossHairCanvas.SetActive(false); //2個準心畫布關掉
        uiCanvas.SetActive(false);
        Debug.Log("背包已成功打開");
    }
    // Coroutine 延遲一幀
    private IEnumerator SelectFirstSlotNextFrame(bool restorePreviousSlot)
    {
        yield return null; // 等一幀

        InventorySlotUI firstSlot = null;
        if (restorePreviousSlot)
            firstSlot = slotManager.GetFirstSlot();

        if (firstSlot != null)
        {
            //InventorySelection.Instance.SetSelected(firstSlot.gameObject);
            SetCurrentSelectedItem(firstSlot.BoundItem);
        }
    }

    public void OpenInventoryPublic(bool restorePreviousSlot = true) //其他腳本需要調用
    {
        OpenInventoryInternal(restorePreviousSlot);
    }

    public void CloseInventory() //關閉背包面板
    {
        if (!isInventoryVisible || inventoryPanel == null) return;
        Debug.Log($"[InventoryUI] 開始關閉背包，預覽面板狀態: {itemDetailUI?.modelPreviewPanel?.activeSelf}");

        // 0924新增 強制關閉所有子面板
        if (itemDetailUI != null)
        {
            // 直接關閉預覽面板，不經過複雜邏輯
            if (itemDetailUI.modelPreviewPanel != null)
                itemDetailUI.modelPreviewPanel.SetActive(false);

            itemDetailUI.ClearPreview();
        }

        inventoryPanel.SetActive(false);
        isInventoryVisible = false;
        OnInventoryClosed?.Invoke(); //通知SwitchInventoryPageButton腳本

        // 0924 清空當前選中物品
        currentSelectedItem = null;

        //InventoryManager.Instance?.ItemDetailUI?.ClearPreview();
        //if (useItemButton != null) useItemButton.gameObject.SetActive(false);
        //isInteractionMode = false;
        useItemButton?.gameObject.SetActive(false); // 重置UI狀態

        // 統一在這裡切換到遊戲模式
        UIInputManager.Instance?.EnterGameplayMode();
        crossHairCanvas.SetActive(true); //2個準心畫布打開
        uiCanvas.SetActive(true);
        Debug.Log("背包已成功關閉");
    }
    #endregion

    #region ===== 選中與右側面板 =====
    /// <summary>
    /// 左側格子選中時也更新按鈕狀態
    /// </summary>
    /// <param name="item"></param>
    public void SetCurrentSelectedItem(ItemData item)
    {
        if (currentSelectedItem == item) return; // 避免重複設定

        currentSelectedItem = item;

        // 手柄模式選中格子
        if (InputDeviceManager.Instance?.CurrentInputType == InputDeviceManager.InputType.Gamepad)
        {
            var slotUI = slotManager?.GetSlotByItem(currentSelectedItem);
            //if (slotUI != null) InventorySelection.Instance?.SetSelected(slotUI.gameObject);
        }

        // 更新右側詳情面板文字（InventoryManager）
        InventoryManager.Instance?.UpdateInformationPanel(currentSelectedItem);

        // 更新模型預覽文字，但不生成模型（ItemDetailUI）
        itemDetailUI?.ShowItemDetail(currentSelectedItem);

        // 設定使用按鈕
        SetupUseItemButton(currentSelectedItem);

        // 更新模型預覽按鈕
        if (itemPreviewButton != null)
            itemPreviewButton.gameObject.SetActive(currentSelectedItem != null && currentSelectedItem.modelPrefab != null);
    }

    public void UpdateItemDetail(ItemData item, bool autoPreview = false)
    {
        if (item == null) return;

        InventoryManager.Instance?.UpdateInformationPanel(item); // 更新右側詳情面板文字（InventoryManager）
        itemDetailUI?.ShowItemDetail(item);

        if (autoPreview) itemDetailUI?.ShowModelPreview(item);
    }

    /// <summary>
    /// 顯示物品模型預覽面板，並設定使用按鈕
    /// </summary>
    private void ShowItemDetail(ItemData item, bool triggeredByPlayer = false)
    {
        Debug.Log($"[InventoryUI] ShowItemDetail called. isInventoryVisible={isInventoryVisible}, currentSelectedItem={(currentSelectedItem != null ? currentSelectedItem.itemName : "null")}");
        if (InventoryManager.Instance?.ItemDetailUI == null) return;
        // 如果背包不可見，不顯示 ModelPreview
        if (!InventoryUI.Instance.isInventoryVisible)
        {
            Debug.Log("[InventoryUI] 面板不可見，不顯示 ModelPreview");
            return;
        }

        InventoryManager.Instance?.UpdateInformationPanel(item);
        itemDetailUI?.ShowItemDetail(item);
    }

    /// <summary>
    /// 隱藏物品模型面板
    /// </summary>
    private void HideItemDetail()
    {
        //應該是調錯方法導致Player還能ModelPreview
        //InventoryManager.Instance?.ItemDetailUI?.HideItemDetail();
        InventoryManager.Instance?.ItemDetailUI?.ClosePreviewAndReturnToInventory();
    }
    #endregion

    #region ===== 按鈕事件：使用物品、預覽物品 =====
    /// <summary>
    /// 預覽物件模型按鈕事件方法
    /// </summary>
    public void OnItemPreviewButtonClicked()
    {
        if (currentSelectedItem == null)
        {
            Debug.LogWarning("沒有選中物品，無法預覽模型");
            return;
        }

        //// 呼叫 ItemDetailUI 顯示 3D 模型
        itemDetailUI?.ShowModelPreview(currentSelectedItem);
    }

    /// <summary>
    /// 設定使用物件按鈕狀態與事件
    /// </summary>
    private void SetupUseItemButton(ItemData item)
    {
        if (useItemButton == null) return;

        bool show = item != null && item.modelPrefab != null;
        useItemButton.gameObject.SetActive(show);

        useItemButton.onClick.RemoveAllListeners();
        if (show)
        {
            useItemButton.onClick.AddListener(() =>
            {
                PlayerInteraction.Instance?.OnItemUsed(item);
            });
        }
    }

    /// <summary>
    /// 使用物品按鈕點擊事件
    /// </summary>
    public void OnUseItemButtonClicked()
    {
        if (currentSelectedItem == null) return;

        // 交給 PlayerInteraction 判斷
        PlayerInteraction.Instance.OnItemUsed(currentSelectedItem);
    }
    #endregion

    #region ===== 更新 UI =====
    /// <summary>
    /// 使用物件池更新UI
    /// </summary>
    public void UpdateUI()
    {
        if (InventoryManager.Instance?.items == null)
        {
            Debug.LogWarning("UpdateUI 被跳過: InventoryManager.items 為 null");
            return;
        }

        Debug.Log($"更新背包UI (isInventoryVisible={isInventoryVisible})");

        slotManager.UpdateSlots(InventoryManager.Instance.items, OnSlotClicked); // 更新左側格子，這步驟與 isInventoryVisible 無關

        // 如果背包面板未顯示，不更新右側 UI
        if (!isInventoryVisible) return;

        // 顯示當前選中物品的右側面板
        //if (currentSelectedItem != null) 
        //{
        //    ShowItemDetail(currentSelectedItem);
        //    return;
        //}

        //// 只有在 isInteractionMode == false 或是剛打開背包時，才強制選 slot_0
        //if (!isInteractionMode)
        //{
        //    var firstSlot = slotManager.GetSlotByIndex(0); // slot_0
        //    if (firstSlot != null)
        //    {
        //        currentSelectedItem = firstSlot.BoundItem;
        //        InventoryManager.Instance?.UpdateInformationPanel(currentSelectedItem);

        //        if (InputDeviceManager.Instance?.CurrentInputType == InputDeviceManager.InputType.Gamepad)
        //        {
        //            InventorySelection.Instance.SetSelected(firstSlot.gameObject);
        //        }
        //    }
        //    else
        //    {
        //        InventorySelection.Instance.ClearSelection();
        //        HideItemDetail();
        //    }
        //}

        InventorySlotUI selectedSlot = null;
        if (currentSelectedItem != null)
            selectedSlot = slotManager.GetSlotByItem(currentSelectedItem);

        if (selectedSlot == null)
        {
            selectedSlot = slotManager.GetFirstSlot();
            if (selectedSlot != null) SetCurrentSelectedItem(selectedSlot.BoundItem);
        }
        else SetCurrentSelectedItem(selectedSlot.BoundItem);
    }

    public void OnSlotClicked(ItemData item) //新增
    {
        SetCurrentSelectedItem(item);
    }

    public GameObject GetFirstSelectableSlot()
    {
        var firstSlotUI = slotManager.GetFirstSlot();
        return firstSlotUI != null ? firstSlotUI.gameObject : null;
    }
    #endregion

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