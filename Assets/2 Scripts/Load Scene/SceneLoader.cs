// 檔案名稱: SceneLoader.cs，是prefab
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

public class SceneLoader : MonoBehaviour
{
    //public static SceneLoader Instance;
    public static SceneLoader Instance { get; private set; } 

    [Header("轉場物件")]
    public GameObject loadingPanel;
    public CanvasGroup loadingCanvasGroup;
    public float fadeDuration = 1f;
    public float minLoadingTime = 2f;

    [Header("下一場景名稱")]
    public string sceneToLoad = "type me";

    // ***** 自訂的場景加載完成事件 *****
    // ***** 保持這個事件: 用於在螢幕全黑時定位玩家 *****
    public event Action<string> OnSceneLoadComplete;

    // ***** 新增: 用於通知場景「轉場已100%完成」的事件 *****
    public event Action OnSceneTransitionComplete;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (loadingPanel != null)
                loadingPanel.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void LoadScene(string sceneName) 
    {
        if (Instance != null)
        {
            Instance.StartCoroutine(Instance.LoadSceneRoutine(sceneName));
        }
        else
        { 
            CreateInstance();
            Instance.StartCoroutine(Instance.LoadSceneRoutine(sceneName)); 
        }
    }

    private static void CreateInstance()
    { 
        GameObject loaderPrefab = Resources.Load<GameObject>("SceneLoader");
        if (loaderPrefab != null)
        {
            Instantiate(loaderPrefab);
        }
        else
        {          
            GameObject go = new GameObject("SceneLoader");
            go.AddComponent<SceneLoader>();
        }
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        // ***** 新增: 轉場開始，鎖定所有遊戲輸入 *****
        if (InputStackManager.Instance != null)
        {
            InputStackManager.Instance.PushMap("Loading"); // 使用您新建的空 Map 名稱
        }

        loadingPanel.SetActive(true);
        yield return StartCoroutine(FadeIn());

        float timer = 0f;
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        while (asyncLoad.progress < 0.9f)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        while (timer < minLoadingTime)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        asyncLoad.allowSceneActivation = true;

        // 等待一幀，確保新場景的 Awake() 都已執行
        yield return null;
        yield return null; //再多一幀看有沒有解決 -> 解決了

        // ***** 關鍵順序調整 *****
        // 1. 在畫面還是黑色的時候，觸發事件，讓 PlayerRespawnManager 定位玩家
        Debug.Log($"[SceneLoader] 螢幕全黑，準備定位玩家...");
        OnSceneLoadComplete?.Invoke(sceneName);

        // ***** 在這裡才執行淡出 *****
        yield return StartCoroutine(FadeOut());
        loadingPanel.SetActive(false);

        // ***** 關鍵 *****
        // 在淡出後，廣播場景加載完成事件 (我們下一步會添加)
        // 並且重置輸入棧
        if (InputStackManager.Instance != null)
        {
            // Init 會清空舊棧並設置 Player 為基礎，讓新場景的控制器接管
            InputStackManager.Instance.Init("Player");
        }

        // 在所有步驟都完成後，廣播「轉場已徹底完成」事件
        OnSceneTransitionComplete?.Invoke();
        Debug.Log($"[SceneLoader] 淡出結束");
    }

    private IEnumerator FadeIn()
    {
        float time = 0f;
        loadingCanvasGroup.alpha = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            loadingCanvasGroup.alpha = Mathf.Clamp01(time / fadeDuration);
            yield return null;
        }

        loadingCanvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOut()
    {
        float time = 0f;
        loadingCanvasGroup.alpha = 1f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            loadingCanvasGroup.alpha = 1f - Mathf.Clamp01(time / fadeDuration);
            yield return null;
        }

        loadingCanvasGroup.alpha = 0f;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("[SceneLoader] " + scene.name);
    }

    public void RestartGame(string startSceneName)
    {
        StartCoroutine(RestartGameRoutine(startSceneName));
    }

    private IEnumerator RestartGameRoutine(string startSceneName)
    {
        loadingPanel.SetActive(true);
        yield return StartCoroutine(FadeIn());

        float timer = 0f;
        while (timer < minLoadingTime)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        yield return StartCoroutine(FadeOut());
        loadingPanel.SetActive(false); //新加

        SceneManager.LoadScene(startSceneName);

        yield return null;
    }
}
