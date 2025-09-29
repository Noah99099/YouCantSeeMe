using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class MusicManager : MonoBehaviour
{
    [System.Serializable]
    public class SceneMusic
    {
        public string sceneName;
        public AudioSource audioSource;
    }

    [Header("場景音樂列表")]
    public List<SceneMusic> sceneMusics = new List<SceneMusic>();

    [Header("BGM最大音量 (0~100)")]
    [Range(0f, 100f)]
    public float masterVolume = 100f; // 預設音量

    private Dictionary<string, AudioSource> musicMap = new Dictionary<string, AudioSource>();
    //【修改】將 instance 改為公開的靜態屬性，這是更標準的單例寫法
    public static MusicManager Instance { get; private set; }
    private string currentSceneName = "";
    private AudioSource currentAudioSource; // 【新增】追蹤當前正在播放的 AudioSource

    private const string VOLUME_KEY = "MusicVolume"; // 【新增】用於 PlayerPrefs 的鍵

    private void Awake()
    {
        // 單例模式
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 【新增】從 PlayerPrefs 讀取已儲存的音量，如果沒有則預設為 100
        masterVolume = PlayerPrefs.GetFloat(VOLUME_KEY, 100f);

        // 初始化字典
        foreach (var sm in sceneMusics)
        {
            if (sm.audioSource != null && !musicMap.ContainsKey(sm.sceneName))
            {
                sm.audioSource.volume = 0f; // 起始靜音
                musicMap.Add(sm.sceneName, sm.audioSource);
            }
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    //【新增】當物件被銷毀時，取消訂閱事件，避免記憶體洩漏
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string newScene = scene.name;

        // 【修改】淡入淡出的目標音量使用 masterVolume
        if (musicMap.TryGetValue(newScene, out AudioSource targetSource))
        {
            // 淡入新場景的音樂
            // 【關鍵修改】淡入的目標音量需要轉換為 0-1 的範圍
            StartCoroutine(FadeMusic(targetSource, 1f, masterVolume/100f));
            currentAudioSource = targetSource;
        }

        // 淡出舊場景的音樂
        if (!string.IsNullOrEmpty(currentSceneName) && currentSceneName != newScene)
        {
            if (musicMap.TryGetValue(currentSceneName, out AudioSource oldSource))
            {
                StartCoroutine(FadeMusic(oldSource, 1f, 0f));
            }
        }

        currentSceneName = newScene;
    }

    /// <summary>
    /// 【新增】設定並儲存主音量
    /// </summary>
    /// <param name="volume">新的音量值 (0-1)</param>
    public void SetMasterVolume(float volume)
    {
        // 【關鍵修改】確保音量在 0-100 之間
        masterVolume = Mathf.Clamp(volume,0f,100f); // 確保音量在 0-1 之間

        // 立即更新當前播放音樂的音量
        if (currentAudioSource != null && currentAudioSource.isPlaying)
        {
            // 【關鍵修改】將 0-100 的值轉換為 AudioSource 需要的 0-1 範圍
            currentAudioSource.volume = masterVolume/100f;
        }

        // 儲存設定
        PlayerPrefs.SetFloat(VOLUME_KEY, masterVolume);
        PlayerPrefs.Save();
        Debug.Log($"音量已設定為: {masterVolume}");
    }

    private IEnumerator FadeMusic(AudioSource source, float duration, float targetVolume)
    {
        float currentTime = 0f;
        float startVolume = source.volume;

        if (!source.isPlaying && targetVolume > 0f) 
        {
            source.Play();
            print("播放BGM成功");
        }

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, targetVolume, currentTime / duration);
            yield return null;
        }

        source.volume = targetVolume;

        if (targetVolume == 0f)
            source.Stop();
    }
}
