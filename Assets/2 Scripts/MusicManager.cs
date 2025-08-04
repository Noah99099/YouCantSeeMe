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

    [Header("BGM最大音量 (0~1)")]
    [Range(0f, 1f)]
    public float maxVolume = 1f; // 這是BGM最大音量，方便在Inspector調整

    private Dictionary<string, AudioSource> musicMap = new Dictionary<string, AudioSource>();
    private static MusicManager instance;
    private string currentSceneName = "";

    private void Awake()
    {
        // 單例模式
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

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

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string newScene = scene.name;

        foreach (var pair in musicMap)
        {
            if (pair.Key == newScene)
            {
                StartCoroutine(FadeMusic(pair.Value, 1f, maxVolume)); // 淡入目前場景音樂
            }
            else
            {
                StartCoroutine(FadeMusic(pair.Value, maxVolume, 0f)); // 淡出其他場景音樂
            }
        }

        currentSceneName = newScene;
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
