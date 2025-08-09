using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventoryUI : MonoBehaviour
{
    [Header("功能：控制背包的物件格子UI，包含開關背包面板")]
    [Header("UI 元件")]
    public GameObject inventoryPanel; // 整個背包 UI 的面板
    public Transform itemsContainer;  // 用來放置所有物品格子的容器
    public GameObject itemSlotPrefab; // 單一物品格子的 Prefab
    [Header("開啟背包的綁定")]
    [SerializeField] private InputActionReference openInventoryAction;
    [Header("關閉背包的綁定")]
    [SerializeField] private InputActionReference closeInventoryAction;
    [Header("導航背包物件的綁定")]
    [SerializeField] private InputActionReference navigateAction;

    private UIInputManager uiInputManager; // 取得管理 action map 的單例（若不存在會為 null）

    // +++ 新增：物件池系統 +++
    private Queue<GameObject> itemSlotPool = new Queue<GameObject>(); //閒置的 itemSlot 預設池，用來回收、重複使用格子
    private List<GameObject> activeSlots = new List<GameObject>(); //當前背包中顯示的 slot，會根據 item 數量更新
    private const int INITIAL_POOL_SIZE = 5;

    private bool isInventoryVisible; //面板是否顯示
    int index = 0; //用在控制物件排順的變數

    private void Awake()
    {
        // 嘗試取得 UIInputManager 的 Instance（若 UIInputManager 有設定為 singleton 且存在於場景中）
        uiInputManager = UIInputManager.Instance;
        if (uiInputManager == null)
        {
            Debug.LogWarning("[InventoryUI] 無法取得 UIInputManager.Instance — 請確認場景中有掛 UIInputManager，或其 Script Execution Order 比此腳本早。");
        }
    }

    void Start()
    {
        inventoryPanel.SetActive(false); //初始化強制關閉面板

        Debug.Log("當前背包面板顯示狀態："+ isInventoryVisible);

        // 強制關閉面板（無論初始狀態）
        //CloseInventory();

        // 初始化物件池（預先創建少量格子）
        InitializeSlotPool();

        // +++ 初始更新UI +++
        UpdateUI();
    }

    private void OnEnable()
    {
        // 訂閱 InventoryManager 事件（如果存在）
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= UpdateUI; // 先移除避免重複
            InventoryManager.Instance.OnInventoryChanged += UpdateUI;
        }
        else
        {
            Debug.LogWarning("[InventoryUI] InventoryManager.Instance 為 null，無法訂閱 OnInventoryChanged。");
        }

        // 訂閱 open action（加上 null 檢查，避免在 Inspector 沒設定時崩潰）
        if (openInventoryAction != null && openInventoryAction.action != null)
        {
            openInventoryAction.action.performed += OnOpenInventory;
            openInventoryAction.action.Enable();
        }
        else
        {
            Debug.LogWarning("[InventoryUI] openInventoryAction 未指派或 action 為 null。請在 Inspector 指派對應的 InputActionReference。");
        }

        // 訂閱 close action（同上）
        if (closeInventoryAction != null && closeInventoryAction.action != null)
        {
            closeInventoryAction.action.performed += OnCloseInventory;
            closeInventoryAction.action.Enable();
        }
        else
        {
            Debug.LogWarning("[InventoryUI] closeInventoryAction 未指派或 action 為 null。請在 Inspector 指派對應的 InputActionReference。");
        }

        if (navigateAction != null && navigateAction.action != null) //新增
        {
            navigateAction.action.Enable();
            navigateAction.action.performed += OnNavigate;
        }
    }
    private void OnDisable()
    {
        // 取消訂閱 InventoryManager 事件
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= UpdateUI;
        }

        // 取消訂閱 open/close action（有 null 檢查）
        if (openInventoryAction != null && openInventoryAction.action != null)
        {
            openInventoryAction.action.performed -= OnOpenInventory;
            openInventoryAction.action.Disable();
        }

        if (closeInventoryAction != null && closeInventoryAction.action != null)
        {
            closeInventoryAction.action.performed -= OnCloseInventory;
            closeInventoryAction.action.Disable();
        }

        if (navigateAction != null && navigateAction.action != null) //新增
        {
            navigateAction.action.performed -= OnNavigate;
            navigateAction.action.Disable();
        }
    }
    /// <summary>
    /// 偵測按下開啟或關閉背包的按鍵（Player Action Map 裡的按鍵）
    /// </summary>
    private void OnOpenInventory(InputAction.CallbackContext context)
    {
        // 如果 ItemDetailPanel 開著，則忽略 toggle（邏輯保留）
        if (isInventoryVisible && InventoryManager.Instance != null && InventoryManager.Instance.ItemDetailUI != null)
        {
            if (InventoryManager.Instance.ItemDetailUI.detailPanel.activeSelf)
            {
                Debug.Log("[InventoryUI] B 鍵按下，但 ItemDetailPanel 開啟中，忽略 ToggleInventory。");
                return;
            }
        }

        ToggleInventory();
        Debug.Log("當前背包面板顯示狀態：" + isInventoryVisible);
    }
    private void OnCloseInventory(InputAction.CallbackContext context)
    {
        CloseInventory();
    }

    /// <summary>
    /// 初始化物品格子物件池
    /// </summary>
    private void InitializeSlotPool()
    {
        // 防守式：InventoryManager 可能還沒準備好
        int inventorySize = 0;
        if (InventoryManager.Instance != null && InventoryManager.Instance.items != null)
            inventorySize = InventoryManager.Instance.items.Count;

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
        // 如果 itemsContainer 為 null，Instantiate 會把物件放到根階層（仍可運作，但建議在 Inspector 指派）
        GameObject slot = Instantiate(itemSlotPrefab, itemsContainer);
        slot.SetActive(false);
        return slot;
    }

    /// <summary>
    /// 從物件池獲取格子
    /// </summary>
    private GameObject GetSlotFromPool()
    {
        if (itemSlotPool.Count > 0) return itemSlotPool.Dequeue();
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

    #region → 開啟與關閉 InventoryPanel
    /// <summary>
    /// 處理開關背包 UI
    /// </summary>
    public void ToggleInventory()
    {
        if (inventoryPanel == null)
        {
            Debug.LogError("[InventoryUI] 無法切換背包：inventoryPanel 未設定。");
            return;
        }

        if (inventoryPanel.activeSelf)
        {
            // 若 ItemDetailPanel 開著就不關 InventoryPanel
            if (InventoryManager.Instance != null && InventoryManager.Instance.ItemDetailUI != null
                && InventoryManager.Instance.ItemDetailUI.detailPanel.activeSelf)
            {
                Debug.Log("[InventoryUI] 無法關閉背包：ItemDetailPanel 仍然開啟中");
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
    private void OpenInventory() //打開背包面板
    {
        isInventoryVisible = true;
        if (inventoryPanel != null) inventoryPanel.SetActive(true);

        // 優先使用 UIInputManager，如果沒有則降級使用 CursorManager（僅控制游標）
        if (uiInputManager != null)
        {
            uiInputManager.EnterInventoryModeNoCursor();
        }
        else
        {
            Debug.LogWarning("[InventoryUI] 無法取得 UIInputManager，改用 CursorManager.EnterUIMode() 作為後備（僅控制游標）。");
            CursorManager.EnterUIMode();
        }

        UpdateUI(); // 打開時確保刷新

        // 保險：如果有格子，開啟背包時一定選第一格
        if (activeSlots.Count > 0)
            EventSystem.current.SetSelectedGameObject(activeSlots[0]);
    }

    private void CloseInventory() //關閉背包面板
    {
        isInventoryVisible = false;
        if (inventoryPanel != null) inventoryPanel.SetActive(false);

        if (uiInputManager != null)
        {
            uiInputManager.EnterGameplayMode();
        }
        else
        {
            Debug.LogWarning("[InventoryUI] 無法取得 UIInputManager，改用 CursorManager.EnterGameplayMode() 作為後備（僅控制游標）。");
            CursorManager.EnterGameplayMode();
        }

        // 清除 ItemDetail 的預覽（如果存在）
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ItemDetailUI?.ClearPreview();
        }
    }
    #endregion
    
    /// <summary>
    /// 使用物件池更新UI
    /// </summary>
    private void UpdateUI()
    {
        // 防守式：確保 InventoryManager 與 items 可用
        if (InventoryManager.Instance == null || InventoryManager.Instance.items == null)
        {
            Debug.LogWarning("[InventoryUI] 無法更新 UI：InventoryManager 或 items 為 null。");
            return;
        }

        Debug.Log("[InventoryUI] 更新 UI，物品數：" + InventoryManager.Instance.items.Count);

        // 若面板關閉，跳過重建可見元素（優化）
        if (!isInventoryVisible) return;

        // 歸還所有現有的 active slots 到 pool
        for (int i = activeSlots.Count - 1; i >= 0; i--)
        {
            ReturnSlotToPool(activeSlots[i]);
        }
        activeSlots.Clear();


        
        // 生成對應物品數的格子
        foreach (ItemData item in InventoryManager.Instance.items)
        {
            GameObject slot = GetSlotFromPool();
            slot.SetActive(true);
            SetupSlot(slot, item);

            slot.transform.SetSiblingIndex(index);  // 強制排序
            activeSlots.Add(slot);

            index++;
        }

        // 新增：如果有格子，就自動選中第一個
        if (activeSlots.Count > 0)
        {
            EventSystem.current.SetSelectedGameObject(activeSlots[0]);
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

        slot.transform.SetSiblingIndex(index);
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

    private void OnNavigate(InputAction.CallbackContext context) //新增
    {
        Vector2 move = context.ReadValue<Vector2>();
        // 可以用這個值移動選取，或呼叫 EventSystem.current.SetSelectedGameObject() 
        Debug.Log("Navigate move: " + move);
    }
}