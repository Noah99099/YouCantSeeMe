using UnityEngine;
using UnityEngine.Video;
using System; // 確保有引用 System

public class PlayVideo : MonoBehaviour
{
    [Header("影片控制器與影片")]
    public VideoPlayerController videoController;
    public VideoClip clip;

    [Header("影片播放完畢後要觸發的角色")]
    [Tooltip("指定播放完畢後要執行 Interact() 的 InteractableRole 物件")]
    public InteractableRole targetRole;

    /// <summary>
    /// 【供 Inspector/外部調用 - 情況 1】
    /// 播放影片，並在播放前後觸發 targetRole 的邏輯。
    /// 此方法無參數，可被 Inspector 的 UnityEvent 調用。
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
    /// 播放影片，只執行單純播放邏輯，並在影片結束後執行傳入的回調。
    /// 此方法通常用於沒有 targetRole 或是銷毀邏輯在外部的情況。
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

        Action combinedFinishAction = onFinishedAction; // 預設加入外部回調

        // 處理 targetRole 邏輯
        if (triggerRole && targetRole != null)
        {
            // 1. 「立刻」觸發 Interact() 來解鎖物品/回憶
            Debug.Log($"[PlayVideo] 影片即將播放，立刻觸發 {targetRole.name} 的 Interact()。");
            targetRole.Interact();

            // 2. 將 targetRole 的 DestoryObjectsAfterVideo() 加入回調鏈 (影片結束後執行)
            // C# 的 += 允許我們將多個 Action 合併
            combinedFinishAction += targetRole.DestoryObjectsAfterVideo;
        }
        else if (triggerRole && targetRole == null)
        {
            Debug.LogWarning("[PlayVideo] 已設定要觸發 Role，但 targetRole 為空！");
        }

        // 3. 呼叫 PlayVideo
        if (combinedFinishAction == null)
        {
            Debug.LogWarning($"[PlayVideo] {gameObject.name} 沒有任何後續回調，只播放影片。");
        }

        videoController.PlayVideo(clip, combinedFinishAction);
    }
}
