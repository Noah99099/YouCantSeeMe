using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class SettingPanelUIController : MonoBehaviour
{
    // PlayerControls 主要來源於InputStackManager -> InputProvider -> SettingPanelUIController
    private void OnEnable()
    {
        // *** 關鍵修改: 移除 playerControls.Setting.Enable(); ***
        // *** 關鍵修改: 使用來自 Level1UIController 的共享實例 ***
        if (InputProvider.InputActions == null) return; // 防呆
        InputProvider.InputActions.Setting.CloseSetting.performed += OnCloseSettingPanel;

        // 2. 處理手把UI焦點
        // 只在手把模式下，才需要設定預設焦點
        if (InputDeviceManager.Instance != null &&
            InputDeviceManager.Instance.CurrentInputType == InputDeviceManager.InputType.Gamepad)
        {
            SetFocusForCurrentPanel();
        }
    }

    private void OnDisable()
    {
        // *** 關鍵修改: 移除 playerControls.Setting.Disable(); ***
        if (InputProvider.InputActions == null) return; // 防呆
        InputProvider.InputActions.Setting.CloseSetting.performed -= OnCloseSettingPanel;
    }

    private void OnCloseSettingPanel(InputAction.CallbackContext context)
    {
        // 1. *** PopMap 寫在這裡 ***
        // 從棧中彈出 UI map，此時 Player map 會被自動重新啟用
        InputStackManager.Instance.PopMap();

        // 2. 執行關閉 Panel 的邏輯
        EventSystem.current.SetSelectedGameObject(null); //清除所有UI焦點避免出問題
        gameObject.SetActive(false);       
        Debug.Log("Panel 已關閉。");
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
