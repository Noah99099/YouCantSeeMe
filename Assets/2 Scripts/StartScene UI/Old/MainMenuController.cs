using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("主菜單按鈕")]
    public Button startButton;
    public Button continueButton;
    public Button optionButton;
    public Button memberListButton;
    public Button quitButton;

    [Header("遊戲設定面板")]
    public Slider bgmSlider;
    public Slider seSlider;
    public Slider mouseSensitivitySlider;
    public Slider gamepadSensitivitySlider;
    [Tooltip("Setting 面板")] public GameObject settingPanel;
    [Tooltip("退出遊戲設定")] public Button settingPanelExitButton;

    [Header("人員表面板")]
    [Tooltip("Member List 面板")] public GameObject memberPanel;
    [Tooltip("退出人員表")] public Button memberPanelExitButton;

    // 輸入系統
    private PlayerControls controls; //input system腳本
    private InputAction cancelAction; //退出

    private GameObject lastSelectedObject; //紀錄上次選中按鈕

    private void Awake() //獲取輸入動作
    {
        controls = new PlayerControls();
        cancelAction = controls.UI.Cancel;
    }
    private void Start() 
    {
        // 確保 InputDeviceManager 已初始化
        if (InputDeviceManager.Instance == null)
        {
            Debug.LogWarning("InputDeviceManager 尚未初始化，等待一幀");
            StartCoroutine(InitializeAfterDelay());
            return;
        }

        // 原有的初始化代碼...
        HandleInputTypeChanged(InputDeviceManager.Instance.CurrentInputType);
    }
    private System.Collections.IEnumerator InitializeAfterDelay()
    {
        yield return null; // 等待一幀

        // 再次檢查 InputDeviceManager
        if (InputDeviceManager.Instance == null)
        {
            Debug.LogError("InputDeviceManager 仍未初始化，創建新實例");
            GameObject managerObj = new GameObject("InputDeviceManager");
            managerObj.AddComponent<InputDeviceManager>();
        }

        // 執行原有的初始化代碼
        HandleInputTypeChanged(InputDeviceManager.Instance.CurrentInputType);

        // 初始化 UI 狀態
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        settingPanel.SetActive(false);
        memberPanel.SetActive(false);

        //綁定按鈕事件，像是直接在inspector手動綁事件那樣
        startButton.onClick.AddListener(StartGame);
        optionButton.onClick.AddListener(OpenSettings);
        memberListButton.onClick.AddListener(OpenMemberPanel);
        quitButton.onClick.AddListener(QuitGame);

        settingPanelExitButton.onClick.AddListener(CloseSettings);
        memberPanelExitButton.onClick.AddListener(CloseMemberPanel);

        // 監聽 Slider 被滑鼠點擊
        bgmSlider.onValueChanged.AddListener((v) => OnSliderSelected(bgmSlider));
        seSlider.onValueChanged.AddListener((v) => OnSliderSelected(seSlider));
        mouseSensitivitySlider.onValueChanged.AddListener((v) => OnSliderSelected(mouseSensitivitySlider));
    }

    private void OnEnable()
    {
        controls.Enable();
        cancelAction.performed += OnCancelAction;
        cancelAction.Enable();
        Debug.Log($"Cancel enabled: {cancelAction.enabled}, bindings: {cancelAction.bindings.Count}");

        // 使用協程確保正確訂閱事件
        StartCoroutine(SubscribeToInputEvents());
    }

    private System.Collections.IEnumerator SubscribeToInputEvents()
    {
        // 等待直到 InputDeviceManager 實例可用
        while (InputDeviceManager.Instance == null)
        {
            yield return null;
        }

        // 取消訂閱並重新訂閱，確保沒有重複訂閱
        InputDeviceManager.Instance.OnInputTypeChanged -= HandleInputTypeChanged;
        InputDeviceManager.Instance.OnInputTypeChanged += HandleInputTypeChanged;
        Debug.Log("已訂閱輸入類型改變事件");

        // 立即觸發一次以更新當前狀態
        HandleInputTypeChanged(InputDeviceManager.Instance.CurrentInputType);
    }
    private void OnDisable()
    {
        cancelAction.performed -= OnCancelAction;
        cancelAction.Disable();
        controls.Disable();

        // 取消訂閱事件
        if (InputDeviceManager.Instance != null)
        {
            InputDeviceManager.Instance.OnInputTypeChanged -= HandleInputTypeChanged;
            Debug.Log("已取消訂閱輸入類型改變事件");
        }
    }

    private void Update()
    {
        // 只在 Gamepad 模式下更新 lastSelectedObject
        if (InputDeviceManager.Instance.CurrentInputType == InputDeviceManager.InputType.Gamepad)
        {
            if (EventSystem.current.currentSelectedGameObject != null)
            {
                lastSelectedObject = EventSystem.current.currentSelectedGameObject;
            }
        }
    }

    #region 1.按下開始遊戲按鈕執行的方法
    public void StartGame()
    {
        SceneLoader loader = FindObjectOfType<SceneLoader>();
        if (loader != null)
        {
            loader.LoadScene("Level1");
        }
        else
        {
            Debug.LogError("SceneLoader not found in scene!");
        }
    }
    #endregion

    #region 3.按下遊戲設定按鈕執行的方法
    public void OpenSettings() //打開遊戲設定panel，選中退出遊戲設定panel按鈕
    {
        settingPanel.SetActive(true);
        memberPanel.SetActive(false); //保險

        EventSystem.current.SetSelectedGameObject(bgmSlider.gameObject);
        lastSelectedObject = bgmSlider.gameObject; //更新 lastSelected，避免 Update 覆蓋
    }
    #endregion

    #region 4.按下人員表按鈕執行的方法
    public void OpenMemberPanel()
    {
        memberPanel.SetActive(true);
        settingPanel.SetActive(false); //保險

        EventSystem.current.SetSelectedGameObject(memberPanelExitButton.gameObject);
        lastSelectedObject = memberPanelExitButton.gameObject;
    }
    #endregion

    // 5.按下退出遊戲按鈕執行的方法
    public void QuitGame()
    {
        Application.Quit();
    }

    #region 關掉Panels功能
    private void CloseSettings() //關掉遊戲設定面板，選中遊戲設定按鈕
    {
        settingPanel.SetActive(false);
        // 根據當前輸入模式設置選中物件
        if (InputDeviceManager.Instance.CurrentInputType == InputDeviceManager.InputType.Gamepad)
            EventSystem.current.SetSelectedGameObject(optionButton.gameObject);
        else
            EventSystem.current.SetSelectedGameObject(null);
    }
    private void CloseMemberPanel() //關掉人員表面板，選中人員表按鈕
    {
        memberPanel.SetActive(false);
        // 根據當前輸入模式設置選中物件
        if (InputDeviceManager.Instance.CurrentInputType == InputDeviceManager.InputType.Gamepad)
            EventSystem.current.SetSelectedGameObject(memberListButton.gameObject);
        else
            EventSystem.current.SetSelectedGameObject(null);
    }
    #endregion

    private void OnCancelAction(InputAction.CallbackContext context) //用退出鍵退出 settingPanel 和 memberPanel
    {
        Debug.Log("Cancel pressed!");

        if (settingPanel.activeSelf)
            CloseSettings();
        else if (memberPanel.activeSelf)
            CloseMemberPanel();
        // 否則讓按鈕照自己正常邏輯處理（交給 Unity 自己執行 Button.onClick）
    }

    private void HandleInputTypeChanged(InputDeviceManager.InputType newType)
    {
        print("測試1");
        if (newType == InputDeviceManager.InputType.Gamepad)
        {
            print("測試2");
            // 隱藏滑鼠
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            // 如果沒有面板打開，選中 startButton
            if (!settingPanel.activeSelf && !memberPanel.activeSelf)
            {
                print("測試3");
                // 直接強制選中 startButton
                ForceSelectUIElement(startButton.gameObject);
            }
            else if (settingPanel.activeSelf)
            {
                print("測試4");
                // 開啟設定面板時，確保選中 bgmSlider 或 lastSelectedObject
                GameObject toSelect = lastSelectedObject != null ? lastSelectedObject : bgmSlider.gameObject;
                ForceSelectUIElement(toSelect);
            }
        }
        else // KeyboardMouse
        {
            print("測試5");
            // 顯示滑鼠
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            if (!settingPanel.activeSelf && !memberPanel.activeSelf)
            {
                print("測試6");
                // 清除選中，讓滑鼠可以正常工作
                EventSystem.current.SetSelectedGameObject(null);
            }
            else if (settingPanel.activeSelf)
            {
                print("測試7");
                // 保持當前選中，但允許滑鼠交互
                // 不需要特別處理
            }
        }
    }
    #region 棘手情況：強制選擇開始按鈕的非常手段方法
    private void ForceSelectUIElement(GameObject uiElement)
    {
        if (uiElement == null) return;

        Debug.Log($"強制選中: {uiElement.name}");

        // 方法 1: 直接設置選中物件
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(uiElement);

        // 方法 2: 如果方法1無效，使用協程延遲設置
        StartCoroutine(DelayedSelectUIElement(uiElement));

        // 方法 3: 如果以上方法都無效，使用反射強制設置
        if (EventSystem.current.currentSelectedGameObject != uiElement)
        {
            Debug.Log("使用反射強制設置選中物件");
            SetSelectedGameObjectWithReflection(uiElement);
        }

        Debug.Log($"最終選中: {EventSystem.current.currentSelectedGameObject?.name}");
    }

    private void SetSelectedGameObjectWithReflection(GameObject uiElement)
    {
        try
        {
            var eventSystemType = typeof(EventSystem);
            var setSelectedGameObjectMethod = eventSystemType.GetMethod("SetSelectedGameObject",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (setSelectedGameObjectMethod != null)
            {
                // 修正：使用 uiElement 而不是 obj
                setSelectedGameObjectMethod.Invoke(EventSystem.current, new object[] { uiElement, null });
            }
            else
            {
                Debug.LogError("無法找到 SetSelectedGameObject 方法");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"使用反射設置選中物件時出錯: {e.Message}");
        }
    }

    private System.Collections.IEnumerator DelayedSelectUIElement(GameObject uiElement)
    {
        yield return new WaitForEndOfFrame(); // 等待當前幀結束
        EventSystem.current.SetSelectedGameObject(uiElement);
        yield return null; // 再多等一幀
        EventSystem.current.SetSelectedGameObject(uiElement);
    }

    private System.Collections.IEnumerator SelectWithInputDisabled(GameObject uiElement)
    {
        // 暫時禁用輸入模組
        var inputModule = EventSystem.current.currentInputModule;
        if (inputModule != null)
        {
            inputModule.enabled = false;
        }

        yield return null; // 等待一幀

        // 強制設置選中
        EventSystem.current.SetSelectedGameObject(uiElement);

        yield return null; // 再等待一幀

        // 重新啟用輸入模組
        if (inputModule != null)
        {
            inputModule.enabled = true;
        }

        // 最後再次確認選中狀態
        yield return null;
        if (EventSystem.current.currentSelectedGameObject != uiElement)
        {
            EventSystem.current.SetSelectedGameObject(uiElement);
        }
    }
    #endregion

    // 新增：滑鼠點擊 Slider 時選中
    private void OnSliderSelected(Slider slider)
    {
        lastSelectedObject = slider.gameObject;
        EventSystem.current.SetSelectedGameObject(slider.gameObject);
    }
}