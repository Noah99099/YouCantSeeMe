using UnityEngine;

public class DialogueAudioManager : MonoBehaviour
{
    public static DialogueAudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [Tooltip("用於播放角色語音的 Audio Source")]
    [SerializeField] private AudioSource voiceSource;

    [Tooltip("用於播放音效的 Audio Source")]
    [SerializeField] private AudioSource sfxSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 播放語音
    public void PlayVoiceOver(AudioClip clip)
    {
        if (clip == null) return;
        
        // 播放前先停止當前語音，避免重疊
        voiceSource.Stop();
        voiceSource.clip = clip;
        voiceSource.Play();
    }

    // 播放一次性音效
    public void PlaySoundEffect(AudioClip clip)
    {
        if (clip == null) return;

        // PlayOneShot 允許音效重疊播放，很適合 UI 音效
        sfxSource.PlayOneShot(clip);
    }

    // 停止語音
    public void StopVoiceOver()
    {
        voiceSource.Stop();
    }
}