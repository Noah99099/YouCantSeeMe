using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 管理 StartScene 的 Action Map：UI。
/// 管理 StartScene 的 UI 佈局，支援四頁輪播與滑鼠懸停顯示。
/// </summary>
public class StartSceneUIController : MonoBehaviour
{
    [Header("分頁系統設定")]
    [Tooltip("0:開始, 1:設定, 2:製作人員, 3:結束。每個物件應包含黑色遮罩與按鈕。")]
    public GameObject[] pageCenterGroups; // 用來控制「顯示」與「隱藏」
    [Tooltip("對應四個分頁中的功能按鈕")]
    public Button[] pageActionButtons; // 用來控制「功能」與「焦點 (Focus)」
    public Button leftArrowButton;
    public Button rightArrowButton;

    [Header("動畫設定")]
    [Tooltip("黑色遮罩漸變所需時間")]
    public float maskFadeDuration = 0.3f;

    [Header("原有面板與設定")]
    [Header("2個面板、遊戲設定slider、標示圖片")]
    public GameObject settingPanel;
    public GameObject memberPanel;
    public Slider[] sliders_settingPanel;
    public Image[] images_hint;   
    [Header("退出按鈕")]
    public Button exitSettingPanel;
    public Button exitMemberPanel;

    private PlayerControls inputActions;

    // 新增
    private int currentPageIndex = 0; // 追蹤目前在第幾頁 (0~3)
    private bool isHoveringCenter = false; // 追蹤滑鼠是否懸停在中心區域

    private bool isSettingOpen;
    private bool isMemberOpen;

    // 儲存各分頁正在執行的漸變協程，避免衝突
    private Coroutine[] fadeCoroutines;
    private Coroutine hoverDelayCoroutine;

    private void Awake()
    {
        // 初始化 Input Actions，若未初始化，OnEnable中會報錯。
        inputActions = new PlayerControls();
        EnsureFadeArrayInitialized();
    }

    void Start()
    {
        // 遊戲開始，初始化為 UI Map
        InputStackManager.Instance.Init(InputActionMaps._UI);

        // 默認2面板關閉
        settingPanel.SetActive(false);
        memberPanel.SetActive(false);

        // 新增: 默認當前在第一頁(0):遊戲開始
        currentPageIndex = 0;

        // 初始隱藏所有群組並設 alpha 為 0
        foreach (var group in pageCenterGroups)
        {
            if (group != null)
            {
                SetGroupAlpha(group, 0);
                group.SetActive(false);
            }
        }

        UpdateUIState();
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

    #region ================= 分頁切換邏輯 =================
    /// <summary>
    /// 供左右箭頭按鈕呼叫 (左箭頭帶入 -1, 右箭頭帶入 1)
    /// </summary>
    public void NavigatePage(int direction)
    {
        currentPageIndex = Mathf.Clamp(currentPageIndex + direction, 0, 3);

        // --- 補充優化：取消點擊(選取)紀錄，避免滑鼠懸停狀態失效 ---
        // 只有在鍵鼠模式下才清空選擇。手把模式的焦點會由後續的 UpdateUIState 重新指派。
        if (InputDeviceManager.Instance.CurrentInputType == InputDeviceManager.InputType.KeyboardMouse)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        UpdateUIState();
    }

    /// <summary>
    /// 更新 UI 顯示狀態：處理分頁顯示、箭頭隱藏以及滑鼠懸停邏輯
    /// </summary>
    private void UpdateUIState()
    {
        // 1. 處理箭頭顯示 (第一頁沒左箭頭，最後一頁沒右箭頭)
        if (leftArrowButton != null) leftArrowButton.gameObject.SetActive(currentPageIndex > 0);
        if (rightArrowButton != null) rightArrowButton.gameObject.SetActive(currentPageIndex < 3);

        // 2. 處理中間內容顯示
        for (int i = 0; i < pageCenterGroups.Length; i++)
        {
            if (pageCenterGroups[i] == null) continue;

            bool shouldShow = (i == currentPageIndex) &&
                              (isHoveringCenter || InputDeviceManager.Instance.CurrentInputType == InputDeviceManager.InputType.Gamepad);

            if (shouldShow)
            {
                // 如果原本是關閉的，才啟動漸變開啟
                if (!pageCenterGroups[i].activeSelf)
                {
                    pageCenterGroups[i].SetActive(true);
                    StartFade(i, 0f, 1f);
                }
            }
            else
            {
                // 離開時直接關閉 (或可改為 FadeOut 後關閉)
                if (pageCenterGroups[i].activeSelf)
                {
                    StopExistingFade(i);
                    pageCenterGroups[i].SetActive(false);
                    SetGroupAlpha(pageCenterGroups[i], 0);
                }
            }
        }

        // 3. 手把模式下自動切換焦點
        if (InputDeviceManager.Instance.CurrentInputType == InputDeviceManager.InputType.Gamepad)
        {
            SetFocusForCurrentPanel();
        }
    }
    #endregion

    // ================= CanvasGroup 漸變核心邏輯 =================

    // 新增：集中處理初始化的防呆機制
    private void EnsureFadeArrayInitialized()
    {
        if (fadeCoroutines == null)
        {
            // 如果 pageCenterGroups 有設定則取其長度，否則給予預設長度 4
            int length = (pageCenterGroups != null) ? pageCenterGroups.Length : 4;
            fadeCoroutines = new Coroutine[length];
        }
    }

    private void StartFade(int index, float startAlpha, float endAlpha)
    {
        EnsureFadeArrayInitialized(); // 呼叫前先確保陣列存在
        StopExistingFade(index);

        // 確保 index 沒有超出範圍
        if (index >= 0 && index < fadeCoroutines.Length)
        {
            fadeCoroutines[index] = StartCoroutine(FadeRoutine(index, startAlpha, endAlpha));
        }
    }

    private void StopExistingFade(int index)
    {
        EnsureFadeArrayInitialized(); // 呼叫前先確保陣列存在

        // 加入邊界防護，避免 IndexOutOfRangeException
        if (index >= 0 && index < fadeCoroutines.Length && fadeCoroutines[index] != null)
        {
            StopCoroutine(fadeCoroutines[index]);
            fadeCoroutines[index] = null;
        }
    }

    private IEnumerator FadeRoutine(int index, float startAlpha, float endAlpha)
    {
        GameObject groupObj = pageCenterGroups[index];
        CanvasGroup canvasGroup = groupObj.GetComponent<CanvasGroup>();

        // 如果沒掛載 CanvasGroup，自動補上
        if (canvasGroup == null) canvasGroup = groupObj.AddComponent<CanvasGroup>();

        float elapsed = 0f;
        while (elapsed < maskFadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / maskFadeDuration);
            yield return null;
        }
        canvasGroup.alpha = endAlpha;
        fadeCoroutines[index] = null;
    }

    private void SetGroupAlpha(GameObject groupObj, float alpha)
    {
        CanvasGroup canvasGroup = groupObj.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = groupObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = alpha;
    }

    #region ================= 滑鼠懸停事件 (由 EventTrigger組件 呼叫)(防閃爍緩衝版) =================
    public void OnPointerEnterCenter()
    {
        // 1. 滑鼠進入感應區或按鈕時，立刻停止隱藏倒數
        if (hoverDelayCoroutine != null)
        {
            StopCoroutine(hoverDelayCoroutine);
            hoverDelayCoroutine = null;
        }

        // 2. 顯示 UI
        isHoveringCenter = true;
        UpdateUIState();
    }

    public void OnPointerExitCenter()
    {
        // 滑鼠離開時，不立刻隱藏，而是啟動一個極短的延遲
        hoverDelayCoroutine = StartCoroutine(DelayHideCenter());
    }

    private IEnumerator DelayHideCenter()
    {
        // 等待 0.05 秒。這點時間足夠讓滑鼠從「透明感應區」移動到「按鈕」的判定完成交接
        yield return new WaitForSeconds(0.05f);

        // 如果 0.05 秒後滑鼠沒有進入任何感應區(包含按鈕)，才真正隱藏
        isHoveringCenter = false;
        UpdateUIState();
    }
    #endregion  

    // ================= 核心功能按鈕 =================
    public void StartGame() //
    {
        SceneLoader.Instance.LoadScene("Level1");
    }

    public void OpenSettingPanel() //
    {
        settingPanel.SetActive(true);
        memberPanel.SetActive(false); //保險
        isSettingOpen = true;
        isMemberOpen = false; //保險

        SetFocusForCurrentPanel();
    } 

    public void OpenMemberPanel() //
    {
        memberPanel.SetActive(true);
        settingPanel.SetActive(false); //保險
        isSettingOpen = false; //保險
        isMemberOpen = true;

        SetFocusForCurrentPanel();
    }
    public void ClosePanel() //settingPanel關掉、memberPanel關掉，都通用
    {
        settingPanel.SetActive(false);
        memberPanel.SetActive(false);

        //如果是手柄模式，初始buttons_mainMenuPanel[0]上，切換button依樣用eventSystem自帶的導航
        //if (InputDeviceManager.Instance.CurrentInputType == InputDeviceManager.InputType.Gamepad)
        //{
        //    if(isSettingOpen==true && isMemberOpen==false) //上次打開的是遊戲設定面板
        //    {
        //        EventSystem.current.SetSelectedGameObject(buttons_mainMenuPanel[2].gameObject);
        //    }
        //    else if(isSettingOpen == false && isMemberOpen == true) //上次打開的是人員表面板
        //    {
        //        EventSystem.current.SetSelectedGameObject(buttons_mainMenuPanel[3].gameObject);
        //    }    
        //}

        isSettingOpen = isMemberOpen = false;
        UpdateUIState();
    }

    public void QuitGame() => Application.Quit();

    #region ================= 焦點與設備相容性 (保留手把邏輯) =================
    /// <summary>
    /// 當輸入設備類型改變時被呼叫
    /// </summary>
    private void OnInputDeviceChanged(InputDeviceManager.InputType newType)
    {
        UpdateUIState();
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
        if (settingPanel.activeSelf) EventSystem.current.SetSelectedGameObject(sliders_settingPanel[0].gameObject);
        else if (memberPanel.activeSelf) EventSystem.current.SetSelectedGameObject(exitMemberPanel.gameObject);
        else if (pageActionButtons.Length > currentPageIndex && pageActionButtons[currentPageIndex] != null)
        {
            EventSystem.current.SetSelectedGameObject(pageActionButtons[currentPageIndex].gameObject);
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
    #endregion

    // --- 新增功能對應的函式 ---
    /// <summary>
    /// 根據當前 EventSystem 選擇的物件，更新 Setting Panel 中的提示圖片。
    /// </summary>
    private void HandleSliderHintImages()
    {
        //// 1. 只在 settingPanel 開啟時才執行此邏輯
        //if (!settingPanel.activeSelf)
        //{
        //    // 確保面板關閉時，所有提示都隱藏
        //    for (int i = 0; i < images_hint.Length; i++)
        //    {
        //        if (images_hint[i] != null)
        //        {
        //            images_hint[i].gameObject.SetActive(false);
        //        }
        //    }
        //    return;
        //}

        //// 2. 獲取當前選擇的物件
        //GameObject currentSelected = EventSystem.current.currentSelectedGameObject;

        //// 3. 遍歷所有 slider，更新對應 hint image 的可見性
        //// 假設 sliders_settingPanel 和 images_hint 數量一致
        //for (int i = 0; i < sliders_settingPanel.Length; i++)
        //{
        //    // 進行安全檢查，防止陣列未設定或長度不匹配
        //    if (i < images_hint.Length && sliders_settingPanel[i] != null && images_hint[i] != null)
        //    {
        //        // 檢查當前選擇的物件是否為第 i 個 slider
        //        bool isSelected = (currentSelected == sliders_settingPanel[i].gameObject);

        //        // 根據是否被選中來設置對應 hint image 的 Active 狀態
        //        images_hint[i].gameObject.SetActive(isSelected);
        //    }
        //}

        if (!settingPanel.activeSelf) return;
        GameObject selected = EventSystem.current.currentSelectedGameObject;
        for (int i = 0; i < sliders_settingPanel.Length; i++)
        {
            if (i < images_hint.Length && sliders_settingPanel[i] != null && images_hint[i] != null)
            {
                images_hint[i].gameObject.SetActive(selected == sliders_settingPanel[i].gameObject);
            }
        }
    }
}
