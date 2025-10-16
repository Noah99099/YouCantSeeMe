using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class SettingPanelUIController : MonoBehaviour
{
    // PlayerControls 主要來源於InputStackManager -> InputProvider -> SettingPanelUIController
    [Tooltip("遊戲設置")]
    public GameObject settingPanel;
    [Tooltip("右下角的提示視野圖標")]
    public GameObject titleUI;
    private void OnEnable()
    {
        // *** 關鍵修改: 移除 playerControls.Setting.Enable(); ***
        // *** 關鍵修改: 使用來自 Level1UIController 的共享實例 ***
        if (InputProvider.InputActions == null) return; // 防呆
        InputProvider.InputActions.Setting.CloseSetting.performed += OnCloseSettingPanel;

        // ***** 新增: 取消訂閱設備變更事件 *****
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
        // *** 關鍵修改: 移除 playerControls.Setting.Disable(); ***
        if (InputProvider.InputActions == null) return; // 防呆
        InputProvider.InputActions.Setting.CloseSetting.performed -= OnCloseSettingPanel;

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

    private void OnCloseSettingPanel(InputAction.CallbackContext context)
    {
        // 1. *** PopMap 寫在這裡 ***
        // 從棧中彈出 UI map，此時 Player map 會被自動重新啟用
        InputStackManager.Instance.PopMap(); // PopMap() 現在會自動處理滑鼠狀態

        // 2. 執行關閉 Panel 的邏輯
        EventSystem.current.SetSelectedGameObject(null); //清除所有UI焦點避免出問題
        settingPanel.SetActive(false);
        titleUI.SetActive(true);
        Debug.Log($"[{this}] 遊戲設置面板已關閉。");
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
