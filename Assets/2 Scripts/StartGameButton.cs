using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGameButton : MonoBehaviour
{
    [Header("場景設定")]
    [Tooltip("要加載的場景名稱（必須已加入 Build Settings）")]
    public string sceneToLoad;

    public void StartGame()
    {
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogWarning("請在 Inspector 中指定 sceneToLoad 的場景名稱！");
        }
    }
}
