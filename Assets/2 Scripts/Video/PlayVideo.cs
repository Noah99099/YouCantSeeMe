using UnityEngine;
using UnityEngine.Video;

public class PlayVideo : MonoBehaviour
{
    [Header("影片控制器與影片")]
    public VideoPlayerController videoController;
    public VideoClip clip;

    [Header("【新】影片播放完畢後要觸發的角色")]
    [Tooltip("指定播放完畢後要執行 Interact() 的 InteractableRole 物件")]
    public InteractableRole targetRole;

    public void PlayForDeceased() 
    {
        print($"[PlayVideo] 播放影片: {clip.name}");

        if (videoController == null)
        {
            Debug.LogError("[PlayVideo] VideoPlayerController 未指定！");
            return;
        }

        // 檢查是否有指定 targetRole
        if (targetRole == null)
        {
            // 如果沒有指定角色，就只播放影片，不執行任何回調
            Debug.LogWarning($"[PlayVideo] {gameObject.name} 未指定 targetRole，將只播放影片。");
            videoController.PlayVideo(clip, null);
        }
        else
        {
            // 【核心】創建一個回調 (Callback)
            // 我們使用 Lambda 運算式來定義這個 "動作"
            // 這個動作就是去呼叫 targetRole.Interact()
            System.Action onFinishedAction = () =>
            {
                Debug.Log($"[PlayVideo] 影片播放完畢，準備觸發 {targetRole.name} 的 Interact()。");
                targetRole.Interact();
            };

            // 呼叫 PlayVideo，並傳入影片和我們剛創建的回調
            videoController.PlayVideo(clip, onFinishedAction);
        }
    }
}
