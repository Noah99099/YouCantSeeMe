using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SettingPanelUIController : MonoBehaviour
{
    // PlayerControls 主要來源於InputStackManager -> InputProvider -> SettingPanelUIController
    [Tooltip("遊戲設置面板")]
    public GameObject settingPanel;
    [Tooltip("右下角的提示視野圖標")]
    public GameObject titleUI;
    [Header("準心")]
    public GameObject crossHair;
    [Header("設置區域")]
    public GameObject setting;
    [Header("操作指示區域")]
    public GameObject operation;
    [Header("左側4個按鈕")]
    public Button[] buttons_settingPanel;
    [Header("遊戲設定slider、標示圖片")]
    public Slider[] sliders_settingPanel; // 共4個
    public Image[] images_hint; // 共4個
    [Header("操作指示ScrollRect")]
    public ScrollRect operation_scrollRect;

    private void OnEnable()
    {
        // *** 關鍵修改: 使用來自 Level1UIController 的共享實例 ***
        if (InputProvider.InputActions == null) return; // 防呆
        InputProvider.InputActions.Setting.CloseSetting.performed += OnCloseSettingPanel;

        // 默認打開 'setting' 區域
        setting.SetActive(true);
        operation.SetActive(false);

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

    private void Update()
    {
        // 根據當前選擇的 Slider 顯示對應的提示圖片
        HandleSliderHintImages();
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
            if (EventSystem.current.currentSelectedGameObject == null)
            {
                SetFocusForCurrentPanel();
            }
        }
        else // 鍵鼠
        {
            // 1. 清除UI焦點，讓滑鼠可以自由點擊
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void OnCloseSettingPanel(InputAction.CallbackContext context)
    {
        ClosePanel();
    }

    /// <summary>
    /// 根據當前開啟的遊戲設定面板，設定手把的UI焦點
    /// </summary>
    private void SetFocusForCurrentPanel()
    {
        // 根據需求："手柄: 默認 selected buttons_settingPanel[0]"
        // 我們總是將 "默認" 焦點設置為左側的第一個按鈕。
        // 子區域的焦點切換由按鈕點擊事件處理。

        EventSystem.current.SetSelectedGameObject(null); // 先清除

        // 檢查是否有設定預設按鈕，避免報錯
        if (buttons_settingPanel.Length > 0 && buttons_settingPanel[0] != null)
        {
            // 將 EventSystem 的焦點設定到您指定的那個按鈕上
            EventSystem.current.SetSelectedGameObject(buttons_settingPanel[0].gameObject);
            Debug.Log($"[{this}] 已將UI焦點設定到默認按鈕: {buttons_settingPanel[0].name}");
        }
    }

    #region === 左側4個按鈕的方法(目前3個) ===
    /// <summary>
    /// 關閉設置面板的公共方法 (供給 Input Action 和按鈕呼叫)
    /// </summary>
    public void ClosePanel()
    {
        // 1. *** PopMap 寫在這裡 ***
        InputStackManager.Instance.PopMap(); // PopMap() 現在會自動處理滑鼠狀態

        // 2. 執行關閉 Panel 的邏輯
        EventSystem.current.SetSelectedGameObject(null); //清除所有UI焦點避免出問題
        
        // 為了保險恢復右側默認
        setting.SetActive(true);
        operation.SetActive(false);

        settingPanel.SetActive(false);
        titleUI.SetActive(true);
        crossHair.SetActive(true);
        Debug.Log($"[{this}] 遊戲設置面板已關閉。");
    }

    /// <summary>
    /// 點擊 "遊戲設置" 按鈕 (應綁定到 buttons_settingPanel[0])
    /// </summary>
    public void OnButtonGameSettingsClicked()
    {
        setting.SetActive(true);
        operation.SetActive(false);

        // 手柄模式下，將焦點切換到右側 Slider 區域
        if (InputDeviceManager.Instance.CurrentInputType == InputDeviceManager.InputType.Gamepad)
        {
            if (sliders_settingPanel.Length > 0 && sliders_settingPanel[0] != null)
            {
                EventSystem.current.SetSelectedGameObject(sliders_settingPanel[0].gameObject);
            }
        }
    }

    /// <summary>
    /// 點擊 "操作指示" 按鈕 (應綁定到 buttons_settingPanel[1])
    /// </summary>
    public void OnButtonOperationClicked()
    {
        setting.SetActive(false);
        operation.SetActive(true);

        // 手柄模式下，將焦點切換到右側 operation_scrollRect
        if (InputDeviceManager.Instance.CurrentInputType == InputDeviceManager.InputType.Gamepad)
        {
            if (operation_scrollRect != null)
            {
                EventSystem.current.SetSelectedGameObject(operation_scrollRect.gameObject);
            }
        }
    }

    /// <summary>
    /// 點擊 "關閉" 或 "返回" 按鈕 (應綁定到 buttons_settingPanel[2] 或 [3])
    /// </summary>
    public void OnButtonCloseClicked()
    {
        // 關閉面板
        ClosePanel();
    }
    #endregion

    /// <summary>
    /// 根據當前 EventSystem 選擇的物件，更新 Setting Panel 中的提示圖片。
    /// (邏輯同 StartSceneUIController)
    /// </summary>
    private void HandleSliderHintImages()
    {
        // 1. 只在 'setting' 子面板開啟時才執行此邏輯
        if (!setting.activeSelf)
        {
            // 確保 'setting' 關閉時，所有提示都隱藏
            for (int i = 0; i < images_hint.Length; i++)
            {
                if (images_hint[i] != null)
                {
                    images_hint[i].gameObject.SetActive(false);
                }
            }
            return;
        }

        // 2. 獲取當前選擇的物件
        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;

        // 3. 遍歷所有 slider，更新對應 hint image 的可見性
        // 假設 sliders_settingPanel 和 images_hint 數量一致
        for (int i = 0; i < sliders_settingPanel.Length; i++)
        {
            // 進行安全檢查，防止陣列未設定或長度不匹配
            if (i < images_hint.Length && sliders_settingPanel[i] != null && images_hint[i] != null)
            {
                // 檢查當前選擇的物件是否為第 i 個 slider
                bool isSelected = (currentSelected == sliders_settingPanel[i].gameObject);

                // 根據是否被選中來設置對應 hint image 的 Active 狀態
                images_hint[i].gameObject.SetActive(isSelected);
            }
        }
    }
    
}
