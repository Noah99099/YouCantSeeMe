using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using static InputActionMaps; // 【新增】引入 InputActionMaps，以便直接使用 _Loading 變數

public class DemoEndManager : MonoBehaviour
{
    [Header("UI 參考")]
    [Tooltip("掛載 Canvas Group 的那個黑色 Panel")]
    public CanvasGroup endScreenCanvasGroup;

    [Header("轉場設定")]
    [Tooltip("畫面變黑需要幾秒")]
    public float fadeDuration = 2f;
    [Tooltip("全黑並顯示文字後，停留幾秒才切換場景")]
    public float waitBeforeLoad = 3f;
    [Tooltip("主介面的場景名稱 (請確保大小寫完全一致)")]
    public string mainMenuSceneName = "StartScene";

    private void Start()
    {
        // 確保遊戲一開始時，結束畫面是隱藏且透明的
        if (endScreenCanvasGroup != null)
        {
            endScreenCanvasGroup.alpha = 0f;
            endScreenCanvasGroup.gameObject.SetActive(false);
        }
    }

    // 這個方法就是要給 NPC 交互時呼叫的
    public void TriggerDemoEnd()
    {
        Debug.Log("觸發 Demo 結束流程！");

        // ==========================================
        // 【優化核心】：完全禁用玩家操作
        // 利用專案現有的 InputStackManager，將狀態強行切換為 _Loading
        // 這樣 PlayerInteraction 和 SimpleFirstPersonController 就不會再收到任何輸入
        // ==========================================
        if (InputStackManager.Instance != null)
        {
            Debug.Log("[DemoEndManager] 已切換至 Loading 輸入層，封鎖玩家操作。");
            InputStackManager.Instance.PushMap(_Loading);
        }
        else
        {
            // 雙重保險，如果沒抓到 Manager，直接禁用 Player Action Map
            InputProvider.InputActions?.Player.Disable();
        }

        // 開始過場動畫
        StartCoroutine(DemoEndRoutine());
    }

    private IEnumerator DemoEndRoutine()
    {
        // 1. 開啟 UI 物件
        endScreenCanvasGroup.gameObject.SetActive(true);

        // 2. 畫面漸暗 (利用 DOTween 將 Alpha 從 0 漸變到 1)
        endScreenCanvasGroup.DOFade(1f, fadeDuration);

        // 等待漸變動畫完成
        yield return new WaitForSeconds(fadeDuration);

        // 3. 讓玩家看幾秒鐘的「感謝遊玩」
        yield return new WaitForSeconds(waitBeforeLoad);

        // 4. 載入主介面場景
        SceneManager.LoadScene(mainMenuSceneName);
    }
}