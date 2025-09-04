using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Spine.Unity;

public enum ViewType { Yang, Yin }

public class ViewManager : MonoBehaviour
{
    [Header("視野UI提示")]
    public GameObject yangUI;
    public GameObject yinUI;

    [Header("Spine動畫控制")]
    public SkeletonGraphic spineUI;

    // 腳本設置：單例、接收 ViewType
    public static ViewManager Instance { get; private set; }
    public static event Action<ViewType> OnViewChanged;
    public ViewType CurrentView { get; private set; } = ViewType.Yin; // 初始為陰視野：等待玩家按下切換，變回陽視野

    // 輸入系統
    private InputAction viewAction;
    private InputAction startGameAction;

    // Spine動畫名稱常量
    private const string BLINK_IDLE_ANIM = "blink_idle";
    private const string BLINK_ANIM = "blink";
    private const string OPEN_ANIM = "open";
    private const string OPEN_IDLE_ANIM = "open_idle";

    // 新增: 防止動畫重複執行
    private bool isAnimating = false;

    void Awake()
    {
        if (Instance != null && Instance != this) // 如果 Instance 已存在且不是自己
        {
            Destroy(gameObject); // 則銷毀這個重複的物件
            return; // 結束 Awake
        }
        Instance = this; // 將自己設為唯一的 Instance
        DontDestroyOnLoad(gameObject); // 確保切換場景時物件不被銷毀

        //yangUI.SetActive(true);
        //yinUI.SetActive(false);

        // 初始化Spine動畫 - 遊戲開始前處於open_idle狀態
        if (spineUI != null)
        {
            spineUI.AnimationState.SetAnimation(0, OPEN_IDLE_ANIM, true);
        }
    }

    void Start()
    {
        UIInputManager inputManager = FindObjectOfType<UIInputManager>();
        if (inputManager != null && inputManager.PlayerControls != null) // 【核心修正 #1】使用大寫的 'PlayerControls'
        {
            // 【核心修正 #2】直接存取 Player Action Map 和 View Action
            // 獲取視圖切換操作
            viewAction = inputManager.PlayerControls.Player.View;
            
            if (viewAction != null)
            {
                viewAction.performed += OnViewPerformed;
            }

            // 獲取開始遊戲操作
            startGameAction = inputManager.PlayerControls.Startup.StartGame;
            if (startGameAction != null)
            {
                startGameAction.performed += OnStartGamePerformed;
            }

            // 初始時只啟用 StartGame 操作
            viewAction.Disable();
        }
        else
        {
            Debug.LogError("在 ViewManager 中找不到 UIInputManager 或其 PlayerControls！", this);
        }
    }

    private void OnDestroy() // 【新增】當物件被銷毀時，取消訂閱
    {
        if (viewAction != null)
        {
            viewAction.performed -= OnViewPerformed;
        }
        if (startGameAction != null)
        {
            startGameAction.performed -= OnStartGamePerformed;
        }
    }

    // 開始遊戲的輸入處理
    private void OnStartGamePerformed(InputAction.CallbackContext context)
    {
        if (!UIInputManager.Instance.IsGameStarted)
        {
            StartGame();
        }
    }

    private void OnViewPerformed(InputAction.CallbackContext context)
    {
        if (UIInputManager.Instance.IsGameStarted && UIInputManager.Instance.IsInPlayerMode)
        {
            ToggleView();
        }
    }

    // 開始遊戲的方法
    public void StartGame()
    {
        UIInputManager.Instance.StartGame();

        // 啟用 View 操作
        if (viewAction != null)
        {
            viewAction.Enable();
        }

        // 從陰視圖切換回陽視圖
        StartCoroutine(SwitchToYangView());

        Debug.Log("遊戲開始，玩家現在可以行動了");
    }

    void ToggleView()
    {
        if (isAnimating) return;

        if (CurrentView == ViewType.Yang)
        {
            StartCoroutine(SwitchToYinView());
        }
        else
        {
            StartCoroutine(SwitchToYangView());
        }
    }

    // 切換到陰視圖的協程
    private IEnumerator SwitchToYinView()
    {
        isAnimating = true;
        // 播放陽視圖到陰視圖的過渡動畫
        if (spineUI != null)
        {
            var track = spineUI.AnimationState.SetAnimation(0, OPEN_ANIM, false);
            yield return new WaitForSpineAnimationComplete(track);

            spineUI.AnimationState.SetAnimation(0, OPEN_IDLE_ANIM, true);
        }

        CurrentView = ViewType.Yin;
        OnViewChanged?.Invoke(CurrentView);

        Debug.Log($"Switched to view: {CurrentView}");
        isAnimating = false;
    }

    // 切換到陽視圖的協程
    private IEnumerator SwitchToYangView()
    {
        isAnimating = true;

        // 播放陰視圖到陽視圖的過渡動畫
        if (spineUI != null)
        {
            var track = spineUI.AnimationState.SetAnimation(0, BLINK_ANIM, false);
            yield return new WaitForSpineAnimationComplete(track);

            spineUI.AnimationState.SetAnimation(0, BLINK_IDLE_ANIM, true);
        }

        CurrentView = ViewType.Yang;
        OnViewChanged?.Invoke(CurrentView);

        Debug.Log($"Switched to view: {CurrentView}");
        isAnimating = false;
    }

    // 等待Spine動畫完成的輔助類
    public class WaitForSpineAnimationComplete : CustomYieldInstruction
    {
        private Spine.TrackEntry trackEntry;

        public WaitForSpineAnimationComplete(Spine.TrackEntry trackEntry)
        {
            this.trackEntry = trackEntry;
        }

        public override bool keepWaiting
        {
            get
            {
                return trackEntry != null && !trackEntry.IsComplete;
            }
        }
    }
}