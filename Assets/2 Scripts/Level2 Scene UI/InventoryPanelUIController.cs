// 檔案名稱: InventoryPanelUIController.cs
using System;
using System.Collections.Generic; // 引用 List
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI; // 引用 UI
using TMPro; // 引用 TextMeshPro

public class InventoryPanelUIController : MonoBehaviour
{
    // PlayerControls 主要來源於InputStackManager -> InputProvider -> SettingPanelUIController
    // **一律呼叫 InventoryPanelUIController.cs 的 ClosePanel() 來關閉案件紀錄簿
    // **切案件紀錄簿的其他頁用 SwitchInventoryPageButton.cs 的 OnButtonClicked(int index)

    [Header("案件紀錄簿-物品、預覽物品建模、鬼、聲音、組合線索")]
    public GameObject inventoryPanel;
    public GameObject modelPreviewPanel;
    public GameObject ghostPanel;
    public GameObject voicePanel;
    public GameObject cluePanel;
    [Header("右下角的提示視野圖標")]
    public GameObject titleUI;
    [Header("準心")]
    public GameObject crossHair;
    [Header("案件紀錄簿下方4個按鈕")]
    public SwitchInventoryPageButton _switchInventoryPage;

    [Header("物品面板 (左側)")]
    [SerializeField] private ScrollRect scrollRect; // 將您的 ScrollRect 拖曳到此
    [SerializeField] private Transform slotsContainer; // 掛載 ItemSlot prefab 的那個 Content 物件

    [Header("物品面板 (右側)")]
    [SerializeField] private Image itemImage;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemDescriptionText;
    [SerializeField] private Button useItemButton;
    [SerializeField] private Button previewItemButton;

    // ----- 狀態管理 -----
    public bool IsInventoryPanelOpen { get; private set; } // 用來判斷案件紀錄簿-物品面板是否打開
    private bool isInteractionMode; // 追蹤是否為交互模式
    private List<InventorySlotUI> itemSlots = new List<InventorySlotUI>(); // 緩存所有格子腳本
    private InventorySlotUI currentSelectedSlot; // 追蹤當前選中的格子

    // ***** 新增: 供其他腳本訂閱的事件 *****
    public event Action OnPanelOpened;
    public event Action OnPanelClosed;

    private void Awake()
    {
        // 1. 診斷 slotsContainer
        if (slotsContainer == null)
        {
            Debug.LogError($"[InventoryPanelUIController] FATAL ERROR: 'Slots Container' 欄位是空的！請在 Inspector 中拖曳 'Content' 物件！", this.gameObject);
            return;
        }

        Debug.Log($"[InventoryPanelUIController] 正在從 '{slotsContainer.name}' 物件中尋找所有 InventorySlotUI...", this.gameObject);
        slotsContainer.GetComponentsInChildren<InventorySlotUI>(true, itemSlots);
        Debug.Log($"[InventoryPanelUIController] 總共找到了 {itemSlots.Count} 個格子。");

        // 2. 診斷找到的格子
        if (itemSlots.Count > 0)
        {
            bool allSlotsOk = true;
            foreach (var slot in itemSlots)
            {
                if (slot.iconImage == null)
                {
                    Debug.LogError($"[InventoryPanelUIController] 診斷失敗：找到的格子 '{slot.gameObject.name}' 的 iconImage 欄位是 null！這就是崩潰的原因。", slot.gameObject);
                    allSlotsOk = false;
                }
            }
            if (allSlotsOk)
            {
                Debug.Log("[InventoryPanelUIController] 診斷成功：所有 {itemSlots.Count} 個格子的 iconImage 都已被正確引用。");
            }
        }
        else
        {
            Debug.LogWarning("[InventoryPanelUIController] 警告：在 'Slots Container' 底下沒有找到任何 InventorySlotUI 腳本！", this.gameObject);
        }

        // 3. 訂閱數據層 (InventoryManager) 的變化
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += RefreshInventorySlots;
        }

        // 4. 綁定右側按鈕的點擊事件
        if (useItemButton != null) useItemButton.onClick.AddListener(OnUseItemClicked);
        if (previewItemButton != null) previewItemButton.onClick.AddListener(OnPreviewItemClicked);
    }

    private void Start()
    {
        // 遊戲開始時，先用預設值刷新一次所有格子
        RefreshInventorySlots();
    }

    private void OnDestroy()
    {
        // 務必取消訂閱
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= RefreshInventorySlots;
        }
    }

    public void OpenModelPreview() // OnOpenModelPreview註冊事件調用，因為按鈕事件所以重點寫這裡
    {
        EventSystem.current.SetSelectedGameObject(null); //清除所有UI焦點避免出問題

        modelPreviewPanel.SetActive(true);
        Debug.Log($"[{this.name}] 預覽物品建模已打開。");

        // 將 ModelPreview map 推入棧，此時 Inventory map 會被自動禁用
        InputStackManager.Instance.PushMap(InputActionMaps._ModelPreview);
    }

    // ----- 新增的核心方法 -----

    /// <summary>
    /// 從外部呼叫此方法來打開庫存面板。
    /// </summary>
    /// <param name="inInteractionMode">是否是因交互而打開的</param>
    public void OpenPanel(bool inInteractionMode = false) // 打開案件紀錄簿，默認為物品面板
    {  
        if (IsInventoryPanelOpen) return; // 防止重複打開

        isInteractionMode = inInteractionMode; // 儲存打開模式
        inventoryPanel.SetActive(true); // 打開案件紀錄簿-物品
        titleUI.SetActive(false); // 關掉右下提示
        crossHair.SetActive(false); // 關掉準心

        // ***** 新增：在這裡集中呼叫 PushMap *****
        // 這確保了只要這個面板被打開，它就一定會正確地 Push Map
        InputStackManager.Instance.PushMap(InputActionMaps._Inventory);

        // 更新狀態並觸發事件
        IsInventoryPanelOpen = true;
        OnPanelOpened?.Invoke();
        Debug.Log("InventoryPanelUIController: OpenPanel() 執行，OnPanelOpened 事件已觸發。");

        // 刷新一次所有格子的內容
        RefreshInventorySlots();
        // 打開面板後，立即更新UI狀態（設定焦點、更新右側面板）
        UpdatePanelStateOnOpen();
    }

    /// <summary>
    /// 從外部或內部呼叫此方法來關閉庫存面板。
    /// </summary>
    public void ClosePanel()
    {       
        if (!IsInventoryPanelOpen) return; // 防止重複關閉

        // 不使用Pop Map，因為無法得知玩家在案件紀錄簿中切換多少次。只有 modelPreviewPanel 可以 Pop Map
        InputStackManager.Instance.Init(InputActionMaps._Player); 
        EventSystem.current.SetSelectedGameObject(null); // 清除UI焦點

        // 關閉案件紀錄簿的所有面板 - 物品、預覽物品建模、鬼、聲音、組合線索
        inventoryPanel.SetActive(false);
        modelPreviewPanel.SetActive(false);
        ghostPanel.SetActive(false);
        voicePanel.SetActive(false);
        cluePanel.SetActive(false);
        titleUI.SetActive(true); // 右下提示打開
        crossHair.SetActive(true); // 打開準心

        // 4. 更新狀態並觸發事件 (注意：這會在 OnDisable 之後發生，但邏輯上更清晰)
        IsInventoryPanelOpen = false;
        isInteractionMode = false; // 關閉時重置模式
        OnPanelClosed?.Invoke();
        Debug.Log("InventoryPanelUIController: ClosePanel() 執行，OnPanelClosed 事件已觸發。");
    }

    private void OnEnable()
    {
        if (InputProvider.InputActions == null) return; // 防呆

        // --- 註冊關閉案件紀錄簿 ---
        InputProvider.InputActions.Inventory.CloseInventory.performed += OnCloseInventory;
        // --- 註冊打開預覽物品面板 ---
        InputProvider.InputActions.Inventory.OpenModelPreview.performed += OnOpenModelPreview;
        // --- 註冊打開鬼面板，關物品面板 ---
        InputProvider.InputActions.Inventory.ToGhostPanel.performed += OnToGhostPanel;
        // --- 註冊打開組合線索面板，關物品面板 ---
        InputProvider.InputActions.Inventory.ToCluePanel.performed += OnToCluePanel;

        // **必要：隨時切換輸入模式
        if (InputDeviceManager.Instance != null)
        {
            InputDeviceManager.Instance.OnInputTypeChanged += HandleInputTypeChange;
            // 注意：這裡不再需要立即呼叫 HandleInputTypeChange，
            // 因為 OpenPanel() 中的 UpdatePanelStateOnOpen() 會處理
            // 立即根據當前的設備類型，初始化一次面板狀態
            //HandleInputTypeChange(InputDeviceManager.Instance.CurrentInputType);
        }
    }

    private void OnDisable()
    {
        if (InputProvider.InputActions == null) return; // 防呆
        // --- 取消註冊 ---
        InputProvider.InputActions.Inventory.CloseInventory.performed -= OnCloseInventory;
        InputProvider.InputActions.Inventory.OpenModelPreview.performed -= OnOpenModelPreview;
        InputProvider.InputActions.Inventory.ToGhostPanel.performed -= OnToGhostPanel;
        InputProvider.InputActions.Inventory.ToCluePanel.performed -= OnToCluePanel;

        // ***** 新增: 取消訂閱設備變更事件 *****
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

    #region ----- 核心 UI 邏輯 -----
    /// <summary>
    /// 當背包數據改變時 (OnInventoryChanged)，刷新所有格子
    /// </summary>
    private void RefreshInventorySlots()
    {
        if (itemSlots.Count == 0 || InventoryManager.Instance == null) return;

        List<ItemData> currentItems = InventoryManager.Instance.items;
        ItemData defaultItem = InventoryManager.Instance.defaultItem;

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
    private void HandleSlotSelected(InventorySlotUI slot)
    {
        currentSelectedSlot = slot;
        UpdateRightPanel(slot.CurrentItemData); // 更新右側面板

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
    private void UpdateRightPanel(ItemData data)
    {
        // 檢查 data 是否為 null 或 defaultItem
        bool isDefault = (data == null || data == InventoryManager.Instance.defaultItem);

        if (isDefault)
        {
            // 顯示預設資訊
            itemImage.enabled = false;
            itemNameText.text = InventoryManager.Instance.defaultItem ? InventoryManager.Instance.defaultItem.itemName : "---";
            itemDescriptionText.text = InventoryManager.Instance.defaultItem ? InventoryManager.Instance.defaultItem.description : "空格子。";

            useItemButton.gameObject.SetActive(false);
            previewItemButton.gameObject.SetActive(false);
        }
        else
        {
            // 顯示真實物品資訊
            itemImage.sprite = data.itemImage;
            itemImage.enabled = (data.itemImage != null);
            itemNameText.text = data.itemName;
            itemDescriptionText.text = data.description;

            // 根據您的需求設定按鈕可見性
            useItemButton.gameObject.SetActive(isInteractionMode); // 僅在交互模式下顯示

            bool hasModel = (data.modelPrefab != null);
            previewItemButton.gameObject.SetActive(hasModel); // 僅在有模型時顯示
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
        InventorySlotUI firstSlot = (itemSlots.Count > 0) ? itemSlots[0] : null;
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
        else if (index >= 16 && index <= 31) // 第 17-32 格 (中間 4 行)
        {
            targetValue = 0.34f; // 滾動到中間 (根據您的值)
        }
        else // 第 33-40 格 (後 2 行)
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

    #region --- 所有 Inventory map 註冊方法 ---
    private void OnCloseInventory(InputAction.CallbackContext context) //關
    {
        ClosePanel();
    }

    private void OnOpenModelPreview(InputAction.CallbackContext context)
    {
        OpenModelPreview();
    }

    private void OnToGhostPanel(InputAction.CallbackContext context) //直接調用 SwitchInventoryPageButton.cs 的方法。右
    {
        EventSystem.current.SetSelectedGameObject(null); // 清除UI焦點

        _switchInventoryPage.OnButtonClicked(1); // 物品到鬼

        // 將 GhostPanel map 推入棧，此時前一個 map 會被自動禁用
        InputStackManager.Instance.PushMap(InputActionMaps._GhostPanel);
    }
    private void OnToCluePanel(InputAction.CallbackContext context) //直接調用 SwitchInventoryPageButton.cs 的方法。左
    {
        _switchInventoryPage.OnButtonClicked(3); // 物品到組合線索

        // 將 CluePanel map 推入棧，此時 Inventory map 會被自動禁用
        InputStackManager.Instance.PushMap(InputActionMaps._CluePanel);
    }
    #endregion

    // ----- 按鈕與不是map的輸入事件 -----

    private void OnUseItemClicked()
    {
        if (currentSelectedSlot != null && currentSelectedSlot.CurrentItemData != InventoryManager.Instance.defaultItem)
        {
            Debug.Log($"使用物品: {currentSelectedSlot.CurrentItemData.itemName}");
            // 呼叫 PlayerInteraction 的使用物品方法
            PlayerInteraction.Instance?.OnItemUsed(currentSelectedSlot.CurrentItemData);
            // PlayerInteraction 的 OnItemUsed 應該會負責關閉面板
        }
    }

    private void OnPreviewItemClicked()
    {
        if (currentSelectedSlot != null && currentSelectedSlot.CurrentItemData.modelPrefab != null)
        {
            // 您的原有邏輯
            OpenModelPreview();
        }
    }
}
