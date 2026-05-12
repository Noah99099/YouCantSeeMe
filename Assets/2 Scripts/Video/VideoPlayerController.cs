using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System; // 【新】為了使用 System.Action
using System.Collections;

public class VideoPlayerController : MonoBehaviour
{
    public RenderTexture renderTexture;
    public RawImage targetImage;

    [Header("過場效果")]
    public BlinkEffect blinkEffect; // 【新增】拖曳 BlinkEffect 腳本到此

    private VideoPlayer videoPlayer;

    // 儲存影片播完後要執行的回調動作
    private Action onPlaybackComplete;

    private bool triggerBlinkForCurrentVideo = false; // 【新增】紀錄當前影片是否需要眨眼

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

    // 【新增】Update 偵測數字鍵 1 跳過
    void Update()
    {
        // 確保影片正在播放中才允許跳過
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            // 內部人員用：按下數字鍵 1 觸發跳過
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SkipVideo();
            }
        }
    }

    // 【新增】跳過影片的核心邏輯
    private void SkipVideo()
    {
        Debug.Log("[VideoPlayerController] 內部指令：跳過影片！");

        // 1. 停止影片播放 (這不會自動觸發 loopPointReached)
        videoPlayer.Stop();

        // 2. 手動呼叫結束邏輯，確保所有回調與後續事件無縫接軌
        OnVideoFinished(videoPlayer);
    }

    /// <summary>
    /// 播放指定影片，並可選擇性地傳入一個播放完畢後的回調
    /// </summary>
    /// <param name="videoClip">要播放的影片（VideoClip）</param>
    /// <param name="onFinishedCallback">【新】影片播放完畢後要執行的動作</param>
    public void PlayVideo(VideoClip videoClip, Action onFinishedCallback = null, bool triggerBlink = false)
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

        this.triggerBlinkForCurrentVideo = triggerBlink; // 【新增】儲存是否眨眼

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
    /// 當影片播放結束 (或被手動跳過) 時觸發
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

        // 【新增】判斷是否需要插入眨眼效果
        if (triggerBlinkForCurrentVideo && blinkEffect != null)
        {
            Debug.Log("[VideoPlayerController] 觸發眨眼過場，等待眨眼結束後恢復操作...");
            blinkEffect.BlinkAfterVideo(() =>
            {
                RestoreAndFinish();
            });
        }
        else
        {
            // 不需眨眼，直接恢復操作
            RestoreAndFinish();
        }
    }

    // 【新增】將原本恢復操作與觸發回調的邏輯獨立出來
    private void RestoreAndFinish()
    {
        // 2. 播放完畢後，恢復玩家輸入
        try
        {
            InputStackManager.Instance.PopMap();
            Debug.Log("[VideoPlayerController] 已恢復玩家輸入");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[VideoPlayerController] InputStackManager.PopMap 失敗: {e.Message}");
        }

        // 影片結束後，恢復背景音樂音量 (1秒淡入)
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetVideoMute(false, 1.0f);
        }

        // 3. 執行傳入的回調動作 (包含情況1/2的物件銷毀與對話延遲)
        if (onPlaybackComplete != null)
        {
            onPlaybackComplete.Invoke();
            onPlaybackComplete = null;
        }

        // 4. 發送「全局信號」給動畫管理器
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
