using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SettingPanelUIController : MonoBehaviour
{
    // PlayerControls 統一由InputStackManager -> InputProvider -> SettingPanelUIController
    [Tooltip("設定面板的根物件")]
    public GameObject settingPanel;
    [Tooltip("標題或其他會被隱藏的UI")]
    public GameObject titleUI;
    [Header("準心")]
    public GameObject crossHair;
    [Header("設定分頁")]
    public GameObject setting;
    [Header("操作說明分頁")]
    public GameObject operation;
    [Header("面板導覽按鈕")]
    public Button[] buttons_settingPanel;
    [Header("設定Slider與提示圖片")]
    public Slider[] sliders_settingPanel; // 範例為4個
    public Image[] images_hint; // 對應Slider的提示圖
    [Header("操作說明ScrollRect")]
    public ScrollRect operation_scrollRect;
    [Header("場景名稱")]
    [Tooltip("您在 Build Settings 中的主選單場景名稱")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private void OnEnable()
    {
        // *** 重要修改: 改為使用 Level1UIController 的同套邏輯 ***
        if (InputProvider.InputActions == null) return; // 防呆
        InputProvider.InputActions.Setting.CloseSetting.performed += OnCloseSettingPanel;

        // 預設顯示 'setting' 分頁
        setting.SetActive(true);
        operation.SetActive(false);

        // **根據目前使用的輸入裝置
        if (InputDeviceManager.Instance != null)
        {
            InputDeviceManager.Instance.OnInputTypeChanged += HandleInputTypeChange;

            // 處理當前的裝置類型，並觸發一次刷新
            HandleInputTypeChange(InputDeviceManager.Instance.CurrentInputType);
        }
    }

    private void OnDisable()
    {
        // *** 重要修改: 移除 playerControls.Setting.Disable(); ***
        if (InputProvider.InputActions == null) return; // 防呆
        InputProvider.InputActions.Setting.CloseSetting.performed -= OnCloseSettingPanel;

        // ***** 新增: 取消訂閱裝置變更事件 *****
        if (InputDeviceManager.Instance != null)
        {
            InputDeviceManager.Instance.OnInputTypeChanged -= HandleInputTypeChange;
        }
    }

    private void Update()
    {
        // 持續檢查當前選取的 Slider 並更新提示圖片
        HandleSliderHintImages();
    }

    /// <summary>
    /// 處理輸入裝置變更，此方法由 InputDeviceManager 呼叫觸發。
    /// </summary>
    private void HandleInputTypeChange(InputDeviceManager.InputType newType)
    {
        if (newType == InputDeviceManager.InputType.Gamepad) // 手把
        {
            // 啟用手把自動導航
            // 設定UI焦點
            if (EventSystem.current.currentSelectedGameObject == null)
            {
                SetFocusForCurrentPanel();
            }
        }
        else // 鍵鼠
        {
            // 1. 清除UI焦點，讓滑鼠可以自由點選
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void OnCloseSettingPanel(InputAction.CallbackContext context)
    {
        ClosePanel();
    }

    /// <summary>
    /// 當前啟動設定面板時，設定預設的UI焦點
    /// </summary>
    private void SetFocusForCurrentPanel()
    {
        // 目前邏輯: "手把: 預設 selected buttons_settingPanel[0]"
        // 因為手把是將 "預設" 焦點設定在面板的第一個按鈕上。
        // 各分頁的焦點會由按鈕點擊後轉移。

        EventSystem.current.SetSelectedGameObject(null); // 先清除

        // 檢查是否有設定按鈕，避免錯誤
        if (buttons_settingPanel.Length > 0 && buttons_settingPanel[0] != null)
        {
            // 將 EventSystem 的焦點設定到指定的第一個按鈕上
            EventSystem.current.SetSelectedGameObject(buttons_settingPanel[0].gameObject);
            Debug.Log($"[{this}] 已將UI焦點設定到預設按鈕: {buttons_settingPanel[0].name}");
        }
    }

    #region === 面板導覽按鈕功能 ===
    /// <summary>
    /// 關閉設定面板的主要功能 (會被 Input Action 和按鈕點擊呼叫)
    /// </summary>
    public void ClosePanel()
    {
        // 1. *** PopMap 改在這裡 ***
        InputStackManager.Instance.PopMap(); // PopMap() 會自動呼叫並啟用前一個 Map

        // 2. 清除 EventSystem 的選取
        EventSystem.current.SetSelectedGameObject(null); //清除手把UI焦點避免出錯
        
        // 恢復分頁的預設狀態
        setting.SetActive(true);
        operation.SetActive(false);

        settingPanel.SetActive(false);
        titleUI.SetActive(true);
        crossHair.SetActive(true);
        Debug.Log($"[{this}] 設定面板已被關閉");
    }

    /// <summary>
    /// 點擊 "遊戲設定" 按鈕 (對應 buttons_settingPanel[0])
    /// </summary>
    public void OnButtonGameSettingsClicked()
    {
        setting.SetActive(true);
        operation.SetActive(false);

        // 手把模式下，將焦點轉移到第一個 Slider 上
        if (InputDeviceManager.Instance.CurrentInputType == InputDeviceManager.InputType.Gamepad)
        {
            if (sliders_settingPanel.Length > 0 && sliders_settingPanel[0] != null)
            {
                EventSystem.current.SetSelectedGameObject(sliders_settingPanel[0].gameObject);
            }
        }
    }

    /// <summary>
    /// 點擊 "操作說明" 按鈕 (對應 buttons_settingPanel[1])
    /// </summary>
    public void OnButtonOperationClicked()
    {
        setting.SetActive(false);
        operation.SetActive(true);

        // 手把模式下，將焦點轉移到 operation_scrollRect
        if (InputDeviceManager.Instance.CurrentInputType == InputDeviceManager.InputType.Gamepad)
        {
            if (operation_scrollRect != null)
            {
                EventSystem.current.SetSelectedGameObject(operation_scrollRect.gameObject);
            }
        }
    }

    /// <summary>
    /// 點擊 "關閉" 或 "返回" 按鈕 (對應 buttons_settingPanel[2] 或 [3])
    /// </summary>
    public void OnButtonCloseClicked()
    {
        // 關閉面板
        ClosePanel();
    }

    /// <summary>
    /// 這是一個公開 (public) 方法，所以 Unity 的 Button 可以呼叫它。
    /// </summary>
    public void QuitGame() //buttons_mainMenuPanel[4]
    {
        Application.Quit();
    }
    #endregion

    /// <summary>
    /// 持續檢查 EventSystem 的選取物件，更新 Setting Panel 上的Slider提示圖片。
    /// (邏輯同 StartSceneUIController)
    /// </summary>
    private void HandleSliderHintImages()
    {
        // 1. 只在 'setting' 分頁啟用時才執行此邏輯
        if (!setting.activeSelf)
        {
            // 如果 'setting' 未啟用，隱藏所有提示圖
            for (int i = 0; i < images_hint.Length; i++)
            {
                if (images_hint[i] != null)
                {
                    images_hint[i].gameObject.SetActive(false);
                }
            }
            return;
        }

        // 2. 獲取當前選取的物件
        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;

        // 3. 遍歷所有 slider，更新其 hint image 的可見度
        // 假設 sliders_settingPanel 與 images_hint 的順序一致
        for (int i = 0; i < sliders_settingPanel.Length; i++)
        {
            // 做好空值檢查，避免陣列長度不符或空引用
            if (i < images_hint.Length && sliders_settingPanel[i] != null && images_hint[i] != null)
            {
                // 檢查當前選取的物件是否為第 i 個 slider
                bool isSelected = (currentSelected == sliders_settingPanel[i].gameObject);

                // 根據是否被選取來設定 hint image 的 Active 狀態
                images_hint[i].gameObject.SetActive(isSelected);
            }
        }
    }
    
}
