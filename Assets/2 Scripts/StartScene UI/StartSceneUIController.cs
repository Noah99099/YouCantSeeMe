using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 管理 StartScene 的 Action Map：UI。
/// </summary>
public class StartSceneUIController : MonoBehaviour
{
    [Header("主菜單按鈕")]
    public Button[] buttons_mainMenuPanel;
    [Header("2個面板、遊戲設定slider、標示圖片")]
    public GameObject settingPanel;
    public GameObject memberPanel;
    public Slider[] sliders_settingPanel;
    public Image[] images_hint;
    [Header("退出按鈕")]
    public Button exitSettingPanel;
    public Button exitMemberPanel;
    [Header("到下一場景腳本")]
    public SceneLoader loader;

    private PlayerControls inputActions;
    private bool isSettingOpen;
    private bool isMemberOpen;

    private void Awake()
    {
        // 初始化 Input Actions，若未初始化，OnEnable中會報錯。
        inputActions = new PlayerControls();
    }

    void Start()
    {
        // 遊戲開始，初始化為 UI Map
        InputStackManager.Instance.Init(InputActionMaps._UI);

        //默認2面板關閉
        settingPanel.SetActive(false);
        memberPanel.SetActive(false);

        // 啟動時，根據當前模式立即設定一次焦點
        SetFocusForCurrentDevice(InputDeviceManager.Instance.CurrentInputType);
    }

    private void OnEnable()
    {
        // 啟用UI Action Map
        inputActions.UI.Enable();
        inputActions.UI.Cancel.performed += OnCancelAction;

        // --- 核心改動：訂閱輸入設備變更事件 ---
        if (InputDeviceManager.Instance != null)
        {
            InputDeviceManager.Instance.OnInputTypeChanged += OnInputDeviceChanged;
        }
    }
    private void OnDisable()
    {
        // 停用UI Action Map
        inputActions.UI.Disable();
        inputActions.UI.Cancel.performed -= OnCancelAction;

        // --- 核心改動：取消訂閱，防止記憶體洩漏 ---
        if (InputDeviceManager.Instance != null)
        {
            InputDeviceManager.Instance.OnInputTypeChanged -= OnInputDeviceChanged;
        }
    }

    // --- 新增防呆機制 ---
    private void Update()
    {
        // 如果是手把模式，但目前沒有任何UI被選中，則重新設定焦點
        if (InputDeviceManager.Instance.CurrentInputType == InputDeviceManager.InputType.Gamepad &&
            EventSystem.current.currentSelectedGameObject == null)
        {
            SetFocusForCurrentPanel();
        }

        // --- 【新增功能】 ---
        // 根據當前選擇的 Slider 顯示對應的提示圖片
        HandleSliderHintImages();
    }

    /// <summary>
    /// 當輸入設備類型改變時被呼叫
    /// </summary>
    private void OnInputDeviceChanged(InputDeviceManager.InputType newType)
    {
        SetFocusForCurrentDevice(newType);
    }

    private void SetFocusForCurrentDevice(InputDeviceManager.InputType type)
    {
        if (type == InputDeviceManager.InputType.Gamepad)
        {
            // 如果切換到手把，設定UI焦點
            SetFocusForCurrentPanel();
        }
        else
        {
            // 如果切換到鍵鼠，取消UI焦點，讓滑鼠自由操作
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    /// <summary>
    /// 根據當前開啟的面板，設定手把的UI焦點
    /// </summary>
    private void SetFocusForCurrentPanel()
    {
        if (settingPanel.activeSelf)
        {
            EventSystem.current.SetSelectedGameObject(sliders_settingPanel[0].gameObject);
        }
        else if (memberPanel.activeSelf)
        {
            EventSystem.current.SetSelectedGameObject(exitMemberPanel.gameObject);
        }
        else // 在主選單
        {
            // 確保按鈕本身是 active 且 interactable 的
            if (buttons_mainMenuPanel.Length > 0 && buttons_mainMenuPanel[0].IsActive() && buttons_mainMenuPanel[0].IsInteractable())
            {
                EventSystem.current.SetSelectedGameObject(buttons_mainMenuPanel[0].gameObject);
            }
        }
    }

    private void OnCancelAction(InputAction.CallbackContext context)
    {
        // 邏輯：檢查當前哪個面板是開啟的，並關閉它。
        // 如果 settingPanel 是開啟的，則模擬點擊其關閉按鈕。
        if (settingPanel.activeSelf)
        {
            exitSettingPanel.onClick.Invoke();
        }
        // 如果 memberPanel 是開啟的，則模擬點擊其關閉按鈕。
        else if (memberPanel.activeSelf)
        {
            exitMemberPanel.onClick.Invoke();
        }
        // 如果兩個面板都關閉（即在主選單界面），則取消按鈕不執行任何操作。
    }

    public void StartGame() //buttons_mainMenuPanel[0]
    {
        if (loader != null)
        {
            loader.LoadScene("Level1"); //切換到下一場景
        }
        else
        {
            Debug.LogError("SceneLoader not found in scene!");
        }
    }

    public void OpenSettingPanel() //buttons_mainMenuPanel[2]
    {
        settingPanel.SetActive(true);
        memberPanel.SetActive(false); //保險
        isSettingOpen = true;
        isMemberOpen = false;

        //如果是手柄模式，selected在sliders_settingPanel[0]上，切換slider依樣用eventSystem自帶的導航
        if (InputDeviceManager.Instance.CurrentInputType == InputDeviceManager.InputType.Gamepad)
        {
            EventSystem.current.SetSelectedGameObject(sliders_settingPanel[0].gameObject);
        }
    } 

    public void OpenMemberPanel() //buttons_mainMenuPanel[3]
    {
        memberPanel.SetActive(true);
        settingPanel.SetActive(false); //保險
        isSettingOpen = false;
        isMemberOpen = true;

        //如果是手柄模式，按下除Cancel以外的按鍵不作用
    }
    public void ClosePanel() //settingPanel關掉、memberPanel關掉，都通用
    {
        settingPanel.SetActive(false);
        memberPanel.SetActive(false);

        //如果是手柄模式，初始buttons_mainMenuPanel[0]上，切換button依樣用eventSystem自帶的導航
        if (InputDeviceManager.Instance.CurrentInputType == InputDeviceManager.InputType.Gamepad)
        {
            if(isSettingOpen==true && isMemberOpen==false) //上次打開的是遊戲設定面板
            {
                EventSystem.current.SetSelectedGameObject(buttons_mainMenuPanel[2].gameObject);
            }
            else if(isSettingOpen == false && isMemberOpen == true) //上次打開的是人員表面板
            {
                EventSystem.current.SetSelectedGameObject(buttons_mainMenuPanel[3].gameObject);
            }    
        }

        isSettingOpen = false;
        isMemberOpen = false;
    }

    public void QuitGame() //buttons_mainMenuPanel[4]
    {
        Application.Quit();
    }

    // --- 新增功能對應的函式 ---
    /// <summary>
    /// 根據當前 EventSystem 選擇的物件，更新 Setting Panel 中的提示圖片。
    /// </summary>
    private void HandleSliderHintImages()
    {
        // 1. 只在 settingPanel 開啟時才執行此邏輯
        if (!settingPanel.activeSelf)
        {
            // 確保面板關閉時，所有提示都隱藏
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
