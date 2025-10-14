using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class InventoryPanelUIController : MonoBehaviour
{
    // PlayerControls 主要來源於InputStackManager -> InputProvider -> SettingPanelUIController

    [Tooltip("案件紀錄簿-物品、預覽物品建模")]
    public GameObject inventoryPanel;
    public GameObject modelPreviewPanel;
    public bool IsInventoryPanelOpen { get; private set; } // 用來判斷案件紀錄簿-物品面板是否打開

    // ***** 新增: 供其他腳本訂閱的事件 *****
    public event Action OnPanelOpened;
    public event Action OnPanelClosed;

    public void OpenModelPreview() // OnOpenModelPreview調用，因為按鈕事件所以重點寫這裡
    {
        modelPreviewPanel.SetActive(true);
        Debug.Log($"[{this.name}] 預覽物品建模已打開。");

        // 將 ModelPreview map 推入棧，此時 Inventory map 會被自動禁用
        InputStackManager.Instance.PushMap(InputActionMaps._ModelPreview);
    }

    // ----- 新增的核心方法 -----

    /// <summary>
    /// 從外部呼叫此方法來打開庫存面板。
    /// </summary>
    public void OpenPanel()
    {
        // 防止重複打開
        if (IsInventoryPanelOpen) return;

        // 1. 打開案件紀錄簿-物品
        inventoryPanel.SetActive(true);

        // 2. 更新狀態並觸發事件
        IsInventoryPanelOpen = true;
        OnPanelOpened?.Invoke();
        Debug.Log("InventoryPanelUIController: OpenPanel() 執行，OnPanelOpened 事件已觸發。");
    }

    /// <summary>
    /// 從外部或內部呼叫此方法來關閉庫存面板。
    /// </summary>
    public void ClosePanel()
    {
        // 防止重複關閉
        if (!IsInventoryPanelOpen) return;

        // 1. Pop Map
        InputStackManager.Instance.PopMap();

        // 2. 清除UI焦點
        EventSystem.current.SetSelectedGameObject(null);

        // 3. 關閉案件紀錄簿-物品
        inventoryPanel.SetActive(false);

        // 4. 更新狀態並觸發事件 (注意：這會在 OnDisable 之後發生，但邏輯上更清晰)
        IsInventoryPanelOpen = false;
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
        // --- 取消註冊 ---
        InputProvider.InputActions.Inventory.CloseInventory.performed -= OnCloseInventory;
        InputProvider.InputActions.Inventory.OpenModelPreview.performed -= OnOpenModelPreview;

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
            // 切換到手把模式：
            // 設定UI焦點
            SetFocusForCurrentPanel();
        }
        else // 鍵鼠
        {
            // 1. 清除UI焦點，讓滑鼠可以自由點擊
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    #region --- 所有 Inventory map 註冊方法 ---
    private void OnCloseInventory(InputAction.CallbackContext context)
    {
        ClosePanel();
    }

    private void OnOpenModelPreview(InputAction.CallbackContext context)
    {
        OpenModelPreview();
    }
    #endregion

    /// <summary>
    /// 根據當前開啟的遊戲設定面板，設定手把的UI焦點
    /// </summary>
    private void SetFocusForCurrentPanel()
    {
        // 確保我們清除了之前的焦點，以防萬一
        EventSystem.current.SetSelectedGameObject(null);

        //// 檢查是否有設定預設按鈕，避免報錯
        //if (鎖定的UI != null)
        //{
        //    // 將 EventSystem 的焦點設定到您指定的那個按鈕上
        //    EventSystem.current.SetSelectedGameObject(鎖定的UI);
        //    Debug.Log($"已將UI焦點設定到: {鎖定的UI.name}");
        //}
    }
}
