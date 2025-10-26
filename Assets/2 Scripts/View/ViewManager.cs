// ViewManager.cs
using System;
using System.Collections;
using UnityEngine;
using Spine.Unity;

/// <summary>
/// UIInputManager 相關的內容不用
/// 要重寫，代替UIInputManager
/// 改好了
/// </summary>
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
    public ViewType CurrentView { get; private set; } = ViewType.Yang; // 初始為陽視野(閉眼)，玩家按下切換，變成陰視野(張眼)

    // 輸入系統
    //private InputAction viewAction;
    //private InputAction startGameAction;

    // Spine動畫名稱常量
    private const string BLINK_IDLE_ANIM = "blink_idle";
    private const string BLINK_ANIM = "blink";
    private const string OPEN_ANIM = "open";
    private const string OPEN_IDLE_ANIM = "open_idle";

    // 新增: 防止動畫重複執行
    private bool isAnimating = false;
    // 新增: 標記是否已初始化
    //private bool isInitialized = false;

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

        // 初始化Spine動畫為陽視野(閉眼)
        if (spineUI != null)
        {
            spineUI.AnimationState.SetAnimation(0, BLINK_IDLE_ANIM, true);
        }
    }

    public void ToggleView()
    {
        if (isAnimating) return;

        if (CurrentView == ViewType.Yang) //當前陽視野(閉眼)
        {
            StartCoroutine(SwitchToYinView()); //閉眼到張眼
        }
        else  //當前陰視野(張眼)
        {
            StartCoroutine(SwitchToYangView()); //張眼到閉眼
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