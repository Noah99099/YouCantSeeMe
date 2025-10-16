using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class CluePanelUIController : MonoBehaviour
{
    // PlayerControls 主要來源於InputStackManager -> InputProvider -> CluePanelUIController
    // **一律呼叫 InventoryPanelUIController.cs 的 ClosePanel() 來關閉案件紀錄簿
    // **切案件紀錄簿的其他頁用 SwitchInventoryPageButton.cs 的 OnButtonClicked(int index)

    public InventoryPanelUIController _inventoryPanelUI;
    public SwitchInventoryPageButton _switchInventoryPage; // 案件紀錄簿下方4個按鈕

    private void OnEnable()
    {
        if (InputProvider.InputActions == null) return; // 防呆
        // --- 註冊打開物品面板，關組合線索面板 ---
        InputProvider.InputActions.CluePanel.ToInventoryPanel.performed += OnToInventoryPanel;
        // --- 註冊打開聲音面板，關組合線索面板 ---
        InputProvider.InputActions.CluePanel.ToVoicePanel.performed += OnToVoicePanel;
        // --- 註冊關閉案件紀錄簿 ---
        InputProvider.InputActions.CluePanel.CloseInventory.performed += OnCloseInventory;

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
        InputProvider.InputActions.CluePanel.ToInventoryPanel.performed -= OnToInventoryPanel;
        InputProvider.InputActions.CluePanel.ToVoicePanel.performed -= OnToVoicePanel;
        InputProvider.InputActions.CluePanel.CloseInventory.performed -= OnCloseInventory;

        // ***** 必要: 取消訂閱設備變更事件 *****
        if (InputDeviceManager.Instance != null)
        {
            InputDeviceManager.Instance.OnInputTypeChanged -= HandleInputTypeChange;
        }
    }

    #region --- 所有 CluePanel Map 的註冊事件 ---
    private void OnToInventoryPanel(InputAction.CallbackContext context) //右
    {
        EventSystem.current.SetSelectedGameObject(null); // 清除UI焦點

        _switchInventoryPage.OnButtonClicked(0); // 組合線索到物品

        // 將 Inventory map 推入棧，此時前一個 map 會被自動禁用
        InputStackManager.Instance.PushMap(InputActionMaps._Inventory);
    }

    private void OnToVoicePanel(InputAction.CallbackContext context) //左
    {
        EventSystem.current.SetSelectedGameObject(null); // 清除UI焦點

        _switchInventoryPage.OnButtonClicked(2); // 組合線索到聲音

        // 將 Inventory map 推入棧，此時前一個 map 會被自動禁用
        InputStackManager.Instance.PushMap(InputActionMaps._VoicePanel);
    }

    private void OnCloseInventory(InputAction.CallbackContext context) //關
    {
        EventSystem.current.SetSelectedGameObject(null); // 清除UI焦點

        _inventoryPanelUI.ClosePanel(); // InventoryPanelUIController 有寫 Init()
    }
    #endregion

    #region --- 其他方法 ---
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
    #endregion
}
