using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ModelPreviewPanelUIController : MonoBehaviour
{
    // PlayerControls 主要來源於InputStackManager -> InputProvider -> ModelPreviewPanelUIController
    [Tooltip("預覽物品建模")]
    public GameObject modelPreviewPanel;

    private void OnEnable()
    {
        if (InputProvider.InputActions == null) return; // 防呆

        // --- 註冊關閉預覽物品建模面板 ---
        InputProvider.InputActions.ModelPreview.CloseModelPreview.performed += OnCloseModelPreview;

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
        InputProvider.InputActions.ModelPreview.CloseModelPreview.performed -= OnCloseModelPreview;

        // ** 必要: 取消訂閱設備變更事件
        if (InputDeviceManager.Instance != null)
        {
            InputDeviceManager.Instance.OnInputTypeChanged -= HandleInputTypeChange;
        }
    }

    public void CloseModelPreviewPanel() // OnCloseModelPreview調用，因為有按鈕事件所以重點寫這裡
    {
        // 1. *** PopMap 寫在這裡 ***
        // 從棧中彈出 ModelPreview map，此時 Inventory map 會被自動重新啟用
        InputStackManager.Instance.PopMap(); // PopMap() 現在會自動處理滑鼠狀態

        // 2. 執行關閉 Panel 的邏輯
        EventSystem.current.SetSelectedGameObject(null); //清除所有UI焦點避免出問題

        modelPreviewPanel.SetActive(false);
        Debug.Log($"[{this}] 預覽物品建模面板已關閉。");
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

    private void OnCloseModelPreview(InputAction.CallbackContext context)
    {
        CloseModelPreviewPanel();
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
}
