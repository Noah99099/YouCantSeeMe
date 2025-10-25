using UnityEngine;
using UnityEngine.Video;

public class PlayVideo : MonoBehaviour
{
    public VideoPlayerController videoController;
    public VideoClip clip;

    //void Start()
    //{
    //    videoController.PlayVideo(clip);
    //}

    public void PlayForDeceased() 
    {
        videoController.PlayVideo(clip);
    }
}
