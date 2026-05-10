using UnityEngine;
using UnityEngine.Video;
using System; // 確保有引用 System

public class PlayVideo : MonoBehaviour
{
    [Header("影片控制器與影片")]
    public VideoPlayerController videoController;
    public VideoClip clip;

    [Header("情況 1：影片播放完畢後要觸發的角色")]
    [Tooltip("指定播放完畢後要執行 Interact() 的 InteractableRole 物件")]
    public InteractableRole targetRole;

    [Header("通用對話設定 (情況 1 & 2 共用)")]
    [Tooltip("影片播完後是否觸發對話？")]
    public bool triggerDialogueAfterVideo = false;
    [Tooltip("對話系統中的事件 ID")]
    public string dialogueID = "";
    [Tooltip("影片結束後幾秒開始對話？")]
    public float delayBeforeDialogue = 1.0f;

    /// <summary>
    /// 【供 Inspector/外部調用 - 情況 1】
    /// 供 Inspector 或外部調用，包含角色 Interact 與 DestoryObjects 邏輯。
    /// </summary>
    public void PlayWithRole()
    {
        // 情況 1: 有 targetRole。
        // 由於是在 Inspector 調用，我們不知道外部是否有其他銷毀邏輯，
        // 所以這裡只處理 targetRole 的相關邏輯。
        StartPlayback(null, true);
    }

    /// <summary>
    /// 【供 VoiceItemDetectionPoint 調用 - 情況 2/通用】
    /// 供 VoiceItemDetectionPoint 等調用，僅包含傳入的自定義回調 (如銷毀偵測點)。
    /// </summary>
    /// <param name="onFinishedAction">影片播放完畢後要執行的動作，例如銷毀物件。</param>
    public void PlayWithoutRole(Action onFinishedAction = null)
    {
        // 情況 2: 沒有 targetRole，只有銷毀物件邏輯 (來自 onFinishedAction)。
        StartPlayback(onFinishedAction, false);
    }

    // --- 核心播放邏輯重構 ---

    /// <summary>
    /// 內部核心播放邏輯。
    /// </summary>
    /// <param name="onFinishedAction">外部傳入的額外回調 (如銷毀物件)</param>
    /// <param name="triggerRole">是否包含 targetRole 的 Interact 邏輯</param>
    private void StartPlayback(Action onFinishedAction, bool triggerRole)
    {
        print($"[PlayVideo] 播放影片: {clip.name} (Role: {triggerRole})");

        if (videoController == null)
        {
            Debug.LogError("[PlayVideo] VideoPlayerController 未指定！");
            return;
        }

        Action combinedFinishAction = onFinishedAction; // 預設加入外部回調，初始化回調鏈

        // 處理 targetRole 邏輯，情況 1
        if (triggerRole && targetRole != null)
        {
            // 1. 「立刻」觸發 Interact() 來解鎖物品/回憶
            Debug.Log($"[PlayVideo] 影片即將播放，立刻觸發 {targetRole.name} 的 Interact()。");
            targetRole.Interact();

            // 2. 將 targetRole 的 DestoryObjectsAfterVideo() 加入回調鏈 (影片結束後執行)
            // C# 的 += 允許我們將多個 Action 合併
            combinedFinishAction += targetRole.DestroyObjectsAfterVideo;
        }

        // --- 處理通用對話邏輯 ---
        if (triggerDialogueAfterVideo && !string.IsNullOrEmpty(dialogueID))
        {
            // 關鍵：將對話任務交給 videoController 執行。
            // 即使本物件 (PlayVideo 所在的 GameObject) 被 Destroy，對話依然會計時並彈出。
            combinedFinishAction += () => videoController.StartDelayedDialogue(delayBeforeDialogue, dialogueID);
        }
        videoController.PlayVideo(clip, combinedFinishAction);
    }
}
