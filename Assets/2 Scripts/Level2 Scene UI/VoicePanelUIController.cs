// VoicePanelUIController.cs
using System;
using System.Collections.Generic; // 引用 List
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI; // 引用 UI
using TMPro; // 引用 TextMeshPro

public class VoicePanelUIController : MonoBehaviour
{
    // PlayerControls 主要來源於InputStackManager -> InputProvider -> VoicePanelUIController
    // **一律呼叫 InventoryPanelUIController.cs 的 ClosePanel() 來關閉案件紀錄簿
    // **切案件紀錄簿的其他頁用 SwitchInventoryPageButton.cs 的 OnButtonClicked(int index)
    [Header("引用腳本")]
    public InventoryPanelUIController _inventoryPanelUI;
    public SwitchInventoryPageButton _switchInventoryPage; // 案件紀錄簿下方4個按鈕

    // ----- 狀態管理 -----
    private List<VoiceSlotUI> itemSlots = new List<VoiceSlotUI>(); // 緩存所有格子腳本
    private VoiceSlotUI currentSelectedSlot; // 追蹤當前選中的格子

    [Header("聲音面板 (左側)")]
    [SerializeField] private ScrollRect scrollRect; // 將您的 ScrollRect 拖曳到此
    [SerializeField] private Transform slotsContainer; // 掛載 VoiceSlot prefab 的那個 Content 物件

    [Header("聲音面板 (右側)")]
    [SerializeField] private TMP_Text itemNameText; // 標題
    [SerializeField] private TMP_Text itemDescText; // 使用前後的文本組件是同一個
    [SerializeField] private Button useItemButton; // 使用聲音物品

    private void Awake()
    {
        // 1. 診斷 slotsContainer
        if (slotsContainer == null)
        {
            Debug.LogError($"[VoicePanelUIController] FATAL ERROR: 'Slots Container' 欄位是空的！請在 Inspector 中拖曳 'Content' 物件！", this.gameObject);
            return;
        }

        Debug.Log($"[VoicePanelUIController] 正在從 '{slotsContainer.name}' 物件中尋找所有 InventorySlotUI...", this.gameObject);
        slotsContainer.GetComponentsInChildren<VoiceSlotUI>(true, itemSlots);
        Debug.Log($"[VoicePanelUIController] 總共找到了 {itemSlots.Count} 個格子。");

        // 2. 診斷找到的格子
        if (itemSlots.Count > 0)
        {
            bool allSlotsOk = true;
            foreach (var slot in itemSlots)
            {
                if (slot.iconImage == null)
                {
                    Debug.LogError($"[VoicePanelUIController] 診斷失敗：找到的格子 '{slot.gameObject.name}' 的 iconImage 欄位是 null！這就是崩潰的原因。", slot.gameObject);
                    allSlotsOk = false;
                }
            }
            if (allSlotsOk)
            {
                Debug.Log("[VoicePanelUIController] 診斷成功：所有 {itemSlots.Count} 個格子的 iconImage 都已被正確引用。");
            }
        }
        else
        {
            Debug.LogWarning("[VoicePanelUIController] 警告：在 'Slots Container' 底下沒有找到任何 InventorySlotUI 腳本！", this.gameObject);
        }

        // 3. 訂閱數據層 (InventoryManager) 的變化
        if (VoiceItemManager.Instance != null)
        {
            VoiceItemManager.Instance.OnVoiceChanged += RefreshVoiceSlots;
        }

        // 4. 綁定右側按鈕的點擊事件
        if (useItemButton != null) useItemButton.onClick.AddListener(OnUseVoiceItemClicked);
    }

    private void Start()
    {
        // 遊戲開始時，先用預設值刷新一次所有格子
        RefreshVoiceSlots();
    }

    private void OnDestroy()
    {
        // 務必取消訂閱
        if (VoiceItemManager.Instance != null)
        {
            VoiceItemManager.Instance.OnVoiceChanged -= RefreshVoiceSlots;
        }
    }

    private void OnEnable()
    {
        if (InputProvider.InputActions == null) return; // 防呆
        // --- 註冊打開組合線索面板，關聲音面板 ---
        InputProvider.InputActions.VoicePanel.ToCluePanel.performed += OnToCluePanel;
        // --- 註冊打開鬼面板，關聲音面板 ---
        InputProvider.InputActions.VoicePanel.ToGhostPanel.performed += OnToGhostPanel;
        // --- 註冊關閉案件紀錄簿 ---
        InputProvider.InputActions.VoicePanel.CloseInventory.performed += OnCloseInventory;

        // **必要：隨時切換輸入模式
        if (InputDeviceManager.Instance != null)
        {
            InputDeviceManager.Instance.OnInputTypeChanged += HandleInputTypeChange;

            // 立即根據當前的設備類型，初始化一次面板狀態
            HandleInputTypeChange(InputDeviceManager.Instance.CurrentInputType);
        }
    }

    private void OnDisable()
    {
        if (InputProvider.InputActions == null) return; // 防呆
        // --- 取消註冊事件 ---
        InputProvider.InputActions.VoicePanel.ToCluePanel.performed -= OnToCluePanel;
        InputProvider.InputActions.VoicePanel.ToGhostPanel.performed -= OnToGhostPanel;
        InputProvider.InputActions.VoicePanel.CloseInventory.performed -= OnCloseInventory;

        // ***** 必要: 取消訂閱設備變更事件 *****
        if (InputDeviceManager.Instance != null)
        {
            InputDeviceManager.Instance.OnInputTypeChanged -= HandleInputTypeChange;
        }
    }

    /// <summary>
    /// 當輸入設備改變時，此方法會被 InputDeviceManager 自動呼叫。
    /// </summary>
    private void HandleInputTypeChange(InputDeviceManager.InputType newType)
    {
        if (newType == InputDeviceManager.InputType.Gamepad) // 手柄
        {
            // 1. 隱藏並鎖定滑鼠，防止它干擾 EventSystem
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            // 2. 設定UI焦點
            // 檢查 currentSelectedSlot 是否為 null（例如剛從鍵鼠切換過來），就選中第一個。
            if (currentSelectedSlot == null && itemSlots.Count > 0)
            {
                EventSystem.current.SetSelectedGameObject(itemSlots[0].gameObject);
            }
            else if (currentSelectedSlot != null)
            {
                // 否則，重新選中當前的格子（確保焦點不會丟失）
                EventSystem.current.SetSelectedGameObject(currentSelectedSlot.gameObject);
            }
        }
        else // 鍵鼠
        {
            // 1. 顯示並解鎖滑鼠
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // 2. 清除手把的UI焦點，讓滑鼠可以自由點擊
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    #region --- 所有 VoicePanel Map 的註冊事件 ---
    private void OnToCluePanel(InputAction.CallbackContext context) //右
    {
        EventSystem.current.SetSelectedGameObject(null); // 清除UI焦點

        _switchInventoryPage.OnButtonClicked(3); // 聲音到組合線索

        // 將 Inventory map 推入棧，此時前一個 map 會被自動禁用
        InputStackManager.Instance.PushMap(InputActionMaps._CluePanel);
    }

    private void OnToGhostPanel(InputAction.CallbackContext context) //左
    {
        EventSystem.current.SetSelectedGameObject(null); // 清除UI焦點

        _switchInventoryPage.OnButtonClicked(1); // 聲音到鬼

        // 將 Inventory map 推入棧，此時前一個 map 會被自動禁用
        InputStackManager.Instance.PushMap(InputActionMaps._GhostPanel);
    }

    private void OnCloseInventory(InputAction.CallbackContext context) //關
    {
        EventSystem.current.SetSelectedGameObject(null); // 清除UI焦點

        _inventoryPanelUI.ClosePanel(); // InventoryPanelUIController 有寫 Init()
    }
    #endregion

    #region ----- 核心 UI 邏輯 -----
    /// <summary>
    /// 當背包數據改變時 (OnVoiceChanged)，刷新所有格子
    /// </summary>
    private void RefreshVoiceSlots()
    {
        if (itemSlots.Count == 0 || VoiceItemManager.Instance == null) return;

        List<VoiceItemData> currentItems = VoiceItemManager.Instance.items;
        VoiceItemData defaultItem = VoiceItemManager.Instance.defaultVoiceItem;

        for (int i = 0; i < itemSlots.Count; i++)
        {
            if (i < currentItems.Count)
            {
                // 列表
                itemSlots[i].Setup(currentItems[i], defaultItem, HandleSlotSelected);
            }
            else
            {
                // 超出列表範圍的格子，使用 defaultItem 填充
                itemSlots[i].Setup(null, defaultItem, HandleSlotSelected);
            }
        }
    }

    /// <summary>
    /// 這是所有格子 (InventorySlotUI) 的中央回調
    /// </summary>
    private void HandleSlotSelected(VoiceSlotUI slot)
    {
        currentSelectedSlot = slot;
        UpdateRightPanel(slot.CurrentVoiceItemData); // 更新右側面板

        // 2. 如果是手把模式，自動滾動
        if (InputDeviceManager.Instance != null &&
            InputDeviceManager.Instance.CurrentInputType == InputDeviceManager.InputType.Gamepad)
        {
            // ***** 修改: 呼叫新的滾動方法 *****
            // 找到當前選中格子的索引
            int index = itemSlots.IndexOf(slot);
            if (index != -1) // 確保找到了索引
            {
                ScrollToIndex(index);
            }
        }
    }

    /// <summary>
    /// 根據選中的物品，更新右側 UI
    /// </summary>
    private void UpdateRightPanel(VoiceItemData data)
    {
        // 檢查 data 是否為 null 或 defaultItem
        bool isDefault = (data == null || data == VoiceItemManager.Instance.defaultVoiceItem);

        if (isDefault)
        {
            // 顯示預設資訊
            itemNameText.text = VoiceItemManager.Instance.defaultVoiceItem ? VoiceItemManager.Instance.defaultVoiceItem.titleText : "---";
            itemDescText.text = VoiceItemManager.Instance.defaultVoiceItem ? VoiceItemManager.Instance.defaultVoiceItem.descText_Before : "空格子。";

            useItemButton.gameObject.SetActive(false);
        }
        else
        {
            // 顯示真實物品資訊
            itemNameText.text = data.titleText;
            itemDescText.text = data.descText_Before;

            // 根據您的需求設定按鈕可見性
            useItemButton.gameObject.SetActive(true); // 有獲得聲音物品就顯示
        }
    }

    /// <summary>
    /// 打開面板時，設定初始焦點
    /// </summary>
    private void UpdatePanelStateOnOpen()
    {
        // ***** 新增: 初始化 Scrollbar 位置 *****
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f; // 1f = 最頂部
        }

        // 預設選中第一個格子
        VoiceSlotUI firstSlot = (itemSlots.Count > 0) ? itemSlots[0] : null;
        if (firstSlot == null) return;

        if (InputDeviceManager.Instance.CurrentInputType == InputDeviceManager.InputType.Gamepad)
        {
            // 手把：隱藏滑鼠，並自動選中第一個格子
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            EventSystem.current.SetSelectedGameObject(firstSlot.gameObject);

            // 手把模式也需要立即顯示第一格的內容
            HandleSlotSelected(firstSlot);
        }
        else
        {
            // 鍵鼠：顯示滑鼠，清除選中，並手動更新右側面板以顯示第一個格子的內容
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            EventSystem.current.SetSelectedGameObject(null);

            // 這會滿足您的需求：「默認顯示第一格內容無論有無點擊」
            HandleSlotSelected(firstSlot);
        }
    }

    #endregion

    #region ----- 自動滾動 (手把導航) -----
    // ***** 新增: 使用索引和區間來滾動 *****
    /// <summary>
    /// 根據選中格子的索引，將 Scrollbar 滾動到預設位置。
    /// </summary>
    /// <param name="index">選中格子的索引 (0-39)</param>
    private void ScrollToIndex(int index)
    {
        if (scrollRect == null || itemSlots.Count == 0) return;

        float targetValue;

        // 根據您定義的區間設定 value
        if (index >= 0 && index <= 15) // 第 1-16 格 (前 4 行)
        {
            targetValue = 1f; // 保持在頂部
        }
        else if (index >= 16 && index <= 19) // 第 17-32 格
        {
            targetValue = 0f;
        }
        else // 第 33-40 格 (後 2 行) //不知道為甚麼刪掉這裡後targetValue不能用了
        {
            targetValue = 0f; // 滾動到底部
        }

        // 設置 Scrollbar 的垂直位置
        // 使用 Mathf.Approximately 避免因浮點數精度問題導致不必要的滾動
        if (!Mathf.Approximately(scrollRect.verticalNormalizedPosition, targetValue))
        {
            scrollRect.verticalNormalizedPosition = targetValue;
        }
    }
    #endregion

    // ----- 點擊按鈕事件，不是map的輸入事件 -----
    private void OnUseVoiceItemClicked()
    {
        if (currentSelectedSlot != null && currentSelectedSlot.CurrentVoiceItemData != VoiceItemManager.Instance.defaultVoiceItem)
        {
            Debug.Log($"使用物品: {currentSelectedSlot.CurrentVoiceItemData.itemName}");
            // 使用聲音物品方法
        }
    }
}
