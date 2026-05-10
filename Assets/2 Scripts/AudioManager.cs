using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [System.Serializable]
    public class SceneMusic
    {
        public string sceneName; // 例如 "StartScene", "Level1"
        public AudioClip bgmClip; // 該場景對應的音檔
    }

    [Header("場景 BGM 設定")]
    public List<SceneMusic> sceneMusicList = new List<SceneMusic>();

    [Header("預設音量 (0~100)")]
    [Range(0f, 100f)]
    public float defaultMasterVolume = 100f;

    [Header("切換場景的淡入淡出時間")]
    public float fadeDuration = 1.0f;

    private AudioSource bgmSource;
    private const string VOLUME_PREF_KEY = "MasterBGMVolume";

    // 真正的玩家設定音量 (0~100)
    private float masterVolume;

    // 系統淡入淡出用的乘數 (0.0 ~ 1.0)
    private float fadeMultiplier = 1f;

    private Coroutine fadeCoroutine;

    // 影片靜音用的乘數 (0.0 ~ 1.0)
    private float videoMuteMultiplier = 1f;
    private Coroutine videoFadeCoroutine;

    private void Awake()
    {
        // 確保全遊戲只有一個 AudioManager
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 初始化 AudioSource
        bgmSource = GetComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;

        // 讀取玩家存檔的音量 (若無存檔則使用預設值)
        masterVolume = PlayerPrefs.GetFloat(VOLUME_PREF_KEY, defaultMasterVolume);
        UpdateAudioSourceVolume();

        // 註冊場景載入事件
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayBGMForScene(scene.name);
    }

    /// <summary>
    /// 根據場景名稱播放對應的 BGM
    /// </summary>
    private void PlayBGMForScene(string sceneName)
    {
        SceneMusic match = sceneMusicList.Find(x => x.sceneName == sceneName);

        if (match != null && match.bgmClip != null)
        {
            // 如果目前已經在播這首歌，就不要重頭播
            if (bgmSource.clip == match.bgmClip && bgmSource.isPlaying) return;

            // 執行淡出換歌再淡入
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(CrossFadeBGM(match.bgmClip));
        }
    }

    /// <summary>
    /// 供外部 Slider 呼叫，設定玩家音量
    /// </summary>
    public void SetMasterVolume(float volume0to100)
    {
        masterVolume = Mathf.Clamp(volume0to100, 0f, 100f);
        PlayerPrefs.SetFloat(VOLUME_PREF_KEY, masterVolume);
        PlayerPrefs.Save();

        UpdateAudioSourceVolume();
    }

    /// <summary>
    /// 供外部 Slider 讀取初始音量
    /// </summary>
    public float GetMasterVolume()
    {
        return masterVolume;
    }

    /// <summary>
    /// 核心公式：最終音量 = (玩家設定音量) * (系統淡入淡出比例)
    /// </summary>
    private void UpdateAudioSourceVolume()
    {
        // 最終音量 = (玩家設定音量) * (換場淡入淡出) * (影片靜音切換)
        bgmSource.volume = (masterVolume / 100f) * fadeMultiplier * videoMuteMultiplier;
    }

    /// <summary>
    /// 淡入淡出切換音樂的協程
    /// </summary>
    private IEnumerator CrossFadeBGM(AudioClip newClip)
    {
        // 1. 先淡出舊音樂
        if (bgmSource.clip != null && bgmSource.isPlaying)
        {
            float startMultiplier = fadeMultiplier;
            for (float t = 0; t < fadeDuration; t += Time.deltaTime)
            {
                fadeMultiplier = Mathf.Lerp(startMultiplier, 0f, t / fadeDuration);
                UpdateAudioSourceVolume();
                yield return null;
            }
        }

        // 2. 換音樂並播放
        fadeMultiplier = 0f;
        UpdateAudioSourceVolume();
        bgmSource.clip = newClip;
        bgmSource.Play();

        // 3. 淡入新音樂
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            fadeMultiplier = Mathf.Lerp(0f, 1f, t / fadeDuration);
            UpdateAudioSourceVolume();
            yield return null;
        }

        fadeMultiplier = 1f;
        UpdateAudioSourceVolume();
    }

    /// <summary>
    /// 供 VideoPlayerController 呼叫，設定影片模式的靜音狀態
    /// </summary>
    public void SetVideoMute(bool isMuted, float fadeDuration = 0.5f)
    {
        if (videoFadeCoroutine != null) StopCoroutine(videoFadeCoroutine);
        videoFadeCoroutine = StartCoroutine(FadeVideoMuteOnly(isMuted, fadeDuration));
    }

    private IEnumerator FadeVideoMuteOnly(bool isMuted, float duration)
    {
        float startMultiplier = videoMuteMultiplier;
        float targetMultiplier = isMuted ? 0f : 1f;
        float currentTime = 0f;

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            videoMuteMultiplier = Mathf.Lerp(startMultiplier, targetMultiplier, currentTime / duration);
            UpdateAudioSourceVolume();
            yield return null;
        }

        videoMuteMultiplier = targetMultiplier;
        UpdateAudioSourceVolume();
    }
}