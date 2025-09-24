using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    //public static SceneLoader Instance;
    public static SceneLoader Instance { get; private set; }

    [Header("�¹��P����]�w")]
    public GameObject loadingPanel;
    public CanvasGroup loadingCanvasGroup;
    public float fadeDuration = 1f;
    public float minLoadingTime = 2f;

    [Header("�n�[���������W�١]�w�[�J Build Settings�^")]
    public string sceneToLoad = "type me";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // ������O�s

            // ��l�Ƴ]�w
            if (loadingPanel != null)
                loadingPanel.SetActive(false);
        }
        else
        {
            Destroy(gameObject); // �p�G�w�g�s�b�A�P�����ƪ�
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
    // �[�W OnDestroy �T�O�M�z
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void LoadScene(string sceneName) //// 0924 �R�A��k��K�I�s
    {
        if (Instance != null)
        {
            Instance.StartCoroutine(Instance.LoadSceneRoutine(sceneName));
        }
        else
        {
            // �p�G Instance ���s�b�A�۰ʫإ�
            CreateInstance();
            Instance.StartCoroutine(Instance.LoadSceneRoutine(sceneName)); // �ץ����j���D
        }
    }

    private static void CreateInstance()
    {
        // �q Resources ���J�w�s��
        GameObject loaderPrefab = Resources.Load<GameObject>("SceneLoader");
        if (loaderPrefab != null)
        {
            Instantiate(loaderPrefab);
        }
        else
        {
            // �Ϊ̰ʺA�إ�
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

        // �������u�����L�h
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
        
        //Destroy(this.gameObject); // �R���ۤv
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // ��A�C�����J�������n���m���޿�
        // ��p���]�@�ǳ�Ҫ��A
        Debug.Log("�������J�����G" + scene.name);
    }

    // 8/5�ȥ�
    public void RestartGame(string startSceneName)
    {
        StartCoroutine(RestartGameRoutine(startSceneName));
    }

    private IEnumerator RestartGameRoutine(string startSceneName)
    {
        loadingPanel.SetActive(true);
        yield return StartCoroutine(FadeIn());

        // ���]�ɶ��P�C�����A�]�i�����p�s�W�^

        // ���ݳ̤p�ɶ�
        float timer = 0f;
        while (timer < minLoadingTime)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        yield return StartCoroutine(FadeOut()); // �[�J�H�X 0924
        loadingPanel.SetActive(false); //新加
        // �����ϥ� LoadScene�]�|�M����ӳ������e�^
        SceneManager.LoadScene(startSceneName);

        // �`�N�I�]���o�O���s�i�J�����A�]�� SceneLoader ������b�s���������ءI

        yield return null;
    }
}
