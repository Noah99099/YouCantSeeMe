using UnityEngine;
using UnityEngine.Video;

public class VideoPlayerController : MonoBehaviour
{
    private VideoPlayer videoPlayer;

    void Awake()
    {
        // 嘗試取得 VideoPlayer 元件
        videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer == null)
        {
            // 若沒有則自動加上
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
        }
    }

    /// <summary>
    /// 播放指定影片
    /// </summary>
    /// <param name="videoClip">要播放的影片（VideoClip）</param>
    public void PlayVideo(VideoClip videoClip)
    {
        if (videoClip == null)
        {
            Debug.LogWarning("未指定 VideoClip！");
            return;
        }

        videoPlayer.clip = videoClip;
        videoPlayer.isLooping = false;  // 可自行改成 true
        videoPlayer.Play();
        Debug.Log("影片開始播放！");
    }
}
