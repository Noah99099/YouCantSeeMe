using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

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

        
        yield return null;
        yield return new WaitForSeconds(0.1f);

        yield return StartCoroutine(FadeOut());
        loadingPanel.SetActive(false);
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
