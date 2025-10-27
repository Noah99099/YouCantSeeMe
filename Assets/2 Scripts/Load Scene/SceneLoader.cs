// 檔案名稱: SceneLoader.cs，是prefab
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

public class SceneLoader : MonoBehaviour
{
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
    public event Action OnSceneTransitionStart; // <--- 新增事件

    // ***** 新增: 用於通知場景「轉場已100%完成」的事件 *****
    public event Action OnSceneTransitionComplete;

    // ***** 新增: 防止重複加載 *****
    private bool _isLoading = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (loadingPanel != null)
                loadingPanel.SetActive(false);

            // ***** 解決方案 1: 將訂閱移至 Awake (僅在 Singleton 創建時) *****
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            // ***** 解決方案 2: 增加 return *****
            // 確保這個即將被銷毀的物件不會執行後續的 OnEnable/Start
            return;
        }
    }

    private void OnEnable()
    {
        // ***** 解決方案 1: 移除這裡的訂閱 *****
        // SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // ***** 解決方案 1: 移除這裡的取消訂閱 *****
        // SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        // ***** 解決方案 1: 在 OnDestroy 中取消訂閱 *****
        // 確保 Instance 存在時才移除 (雖然理論上 OnDestroy 時 Instance 應該是 self)
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    public void LoadScene(string sceneName) 
    {
        // ***** 新增: 檢查是否已在加載中 *****
        if (_isLoading)
        {
            Debug.LogWarning($"[SceneLoader] 正在加載中，忽略重複的 '{sceneName}' 加載請求。");
            return;
        }
        // ***** 結束 *****

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
        // ***** 新增: 在協程開始時立即設置旗標 *****
        _isLoading = true;
        OnSceneTransitionStart?.Invoke(); // <--- 在協程開始時廣播

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

        // ==因為還是修不好，所以嘗試直接拔掉Init看看==
        // ***** 新的 (正確的) 順序 *****
        // 1. 先重置輸入棧
        //if (InputStackManager.Instance != null)
        //{
        //    InputStackManager.Instance.Init("Loading");
        //}
        // ====
        // 好像修好了，反正目前測下來沒有bug

        // 2. 再廣播「轉場完成」事件，讓 SDC 和 L1UI 執行
        OnSceneTransitionComplete?.Invoke();

        // 3. 最後才印出日誌
        // (日誌訊息現在有點誤導，因為 SDC 可能已經 PUSH 了 "Dialogue"，但沒關係)
        Debug.Log($"[SceneLoader] 淡出結束，已廣播 OnSceneTransitionComplete。輸入棧在廣播前已 Init 為 'Loading'。");

        // ***** 新Do: 在協程的最後重置旗標 *****
        _isLoading = false;
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
        LoadScene(startSceneName);
    }
}
