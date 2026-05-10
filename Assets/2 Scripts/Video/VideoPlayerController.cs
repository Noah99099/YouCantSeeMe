using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System; // 【新】為了使用 System.Action
using System.Collections;

public class VideoPlayerController : MonoBehaviour
{
    public RenderTexture renderTexture;
    public RawImage targetImage;

    private VideoPlayer videoPlayer;

    // 【新】儲存影片播完後要執行的回調動作
    private Action onPlaybackComplete;

    void Awake()
    {
        // 嘗試取得 VideoPlayer 元件
        videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer == null)
        {
            // 若沒有則自動加上
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
        }

        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;
        if (targetImage != null)
            targetImage.texture = renderTexture;

        // 【重要】訂閱影片播放結束事件
        videoPlayer.loopPointReached += OnVideoFinished;
    }

    void OnDestroy()
    {
        // 【重要】取消訂閱事件，避免記憶體洩漏
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
    }

    /// <summary>
    /// 播放指定影片，並可選擇性地傳入一個播放完畢後的回調
    /// </summary>
    /// <param name="videoClip">要播放的影片（VideoClip）</param>
    /// <param name="onFinishedCallback">【新】影片播放完畢後要執行的動作</param>
    public void PlayVideo(VideoClip videoClip, Action onFinishedCallback = null)
    {
        if (videoClip == null)
        {
            Debug.LogWarning("未指定 VideoClip！");
            return;
        }

        videoPlayer.clip = videoClip;
        videoPlayer.isLooping = false;

        // 【新】儲存這個回調，以便在 OnVideoFinished 中使用
        this.onPlaybackComplete = onFinishedCallback;

        if (targetImage != null)
            targetImage.gameObject.SetActive(true);

        // 播放開始時，鎖定玩家輸入
        try
        {
            InputStackManager.Instance.PushMap(InputActionMaps._Loading);
        }
        catch (Exception e)
        {
            Debug.LogError($"[VideoPlayerController] InputStackManager.PushMap 失敗: {e.Message}");
        }

        // 【新增】開始播放影片時，將背景音樂靜音 (0.5秒淡出)
        // 【修改】MusicManager -> AudioManager
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetVideoMute(true, 0.5f);
        }

        videoPlayer.Play();
        Debug.Log("[VideoPlayerController] 影片開始播放！");
    }

    /// <summary>
    /// 當影片播放結束時觸發
    /// </summary>
    /// <param name="vp">觸發此事件的 VideoPlayer</param>
    private void OnVideoFinished(VideoPlayer vp)
    {
        Debug.Log("[VideoPlayerController] 影片播放完畢！");

        // 1. 播放完畢後，隱藏圖片
        if (targetImage != null)
            targetImage.gameObject.SetActive(false);

        // 【新增】清空 RenderTexture，避免下一支影片播放前出現殘影
        if (renderTexture != null)
        {
            renderTexture.Release();
        }

        // 2. 播放完畢後，恢復玩家輸入
        try
        {
            InputStackManager.Instance.PopMap();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[VideoPlayerController] InputStackManager.PopMap 失敗: {e.Message}");
        }

        // 【新增】影片結束後，恢復背景音樂音量 (1秒淡入)
        // 【修改】MusicManager -> AudioManager
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetVideoMute(false, 1.0f);
        }

        // 3. 【新】執行傳入的回調動作
        if (onPlaybackComplete != null)
        {
            onPlaybackComplete.Invoke();
            // 清除回調，確保它只被執行一次
            onPlaybackComplete = null;
        }

        // 4. [!!] 新增 [!!]
        //    發送「全局信號」給動畫管理器
        if (PuzzleUnlockAnimator.Instance != null)
        {
            PuzzleUnlockAnimator.Instance.OnVideoPlaybackFinished();
        }
    }

    /// <summary>
    /// 提供給外部調用，由 Controller 宿主代為執行對話延遲，避免調用者被銷毀時協程中斷。
    /// </summary>
    public void StartDelayedDialogue(float delay, string dialogueID)
    {
        StartCoroutine(DelayedDialogueRoutine(delay, dialogueID));
    }

    private IEnumerator DelayedDialogueRoutine(float delay, string dialogueID)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);

        if (DialogueManager.Instance != null && !string.IsNullOrEmpty(dialogueID))
        {
            Debug.Log($"[VideoPlayerController] 延遲結束，觸發對話: {dialogueID}");
            DialogueManager.Instance.TriggerDialogueByEvent(dialogueID);
        }
    }
}
