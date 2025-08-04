using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    //public static SceneLoader Instance;

    [Header("黑幕與轉場設定")]
    public GameObject loadingPanel;
    public CanvasGroup loadingCanvasGroup;
    public float fadeDuration = 1f;
    public float minLoadingTime = 2f;

    [Header("要加載的場景名稱（已加入 Build Settings）")]
    public string sceneToLoad = "type me";

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void LoadScene()
    {
        StartCoroutine(LoadSceneRoutine(sceneToLoad));
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

        // 等場景真的切過去
        yield return null;
        yield return new WaitForSeconds(0.1f);

        yield return StartCoroutine(FadeOut());
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
        
        Destroy(this.gameObject); // 刪除自己
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 放你每次載入場景都要重置的邏輯
        // 比如重設一些單例狀態
        Debug.Log("場景載入完成：" + scene.name);
    }

    // 8/5暫用
    public void RestartGame(string startSceneName)
    {
        StartCoroutine(RestartGameRoutine(startSceneName));
    }

    private IEnumerator RestartGameRoutine(string startSceneName)
    {
        loadingPanel.SetActive(true);
        yield return StartCoroutine(FadeIn());

        // 重設時間與遊戲狀態（可視情況新增）

        // 等待最小時間
        float timer = 0f;
        while (timer < minLoadingTime)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        // 直接使用 LoadScene（會清除整個場景內容）
        SceneManager.LoadScene(startSceneName);

        // 注意！因為這是重新進入場景，因此 SceneLoader 必須能在新場景中重建！

        yield return null;
    }
}
