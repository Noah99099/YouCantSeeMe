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
            // [!!] 核心修改 [!!]
            // 我們不再創建回調，而是在「播放之前」就立刻解鎖

            // 1. 「立刻」觸發 Interact() 來解鎖物品/回憶
            Debug.Log($"[PlayVideo] 影片即將播放，立刻觸發 {targetRole.name} 的 Interact()。");
            targetRole.Interact(); // <-- 核心修改點

            // 2. 【新】創建一個「影片播完後」的回調
            //    這個回調指向新的 DestoryObjectsAfterVideo 函式
            System.Action onFinishedAction = () =>
            {
                Debug.Log($"[PlayVideo] 影片播放完畢，準備觸發 {targetRole.name} 的 DestoryObjectsAfterVideo() (銷毀物件)。");
                targetRole.DestoryObjectsAfterVideo();
            };

            // 3. 呼叫 PlayVideo，並傳入影片和我們剛創建的「銷毀物件」回調
            videoController.PlayVideo(clip, onFinishedAction);
        }
    }
}
