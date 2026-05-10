using UnityEngine;
using System.Collections;

/// <summary>
/// 負責控管 Level1 的首次載入與繞道邏輯。
/// 營造出 StartScene -> Level0 -> Level1 的遊玩錯覺。
/// </summary>

public class CrossSceneManager : MonoBehaviour
{
    [Header("場景狀態追蹤 (全局靜態變數)")]
    [Tooltip("記錄玩家是否已經通關/去過 Level0")]
    public static bool hasVisitedLevel0 = false;

    [Header("物件狀態控制")]
    [Tooltip("第一次進 Level1 時要隱藏，從 Level0 回來時要打開的物件")]
    public GameObject targetObjectToToggle;

    private void Start()
    {
        // 判斷是否是第一次來到 Level1
        if (!hasVisitedLevel0)
        {
            // 1. 隱藏特定物件 (避免在轉場瞬間露餡)
            if (targetObjectToToggle != null)
            {
                targetObjectToToggle.SetActive(false);
            }

            // 2. 訂閱轉場完成事件：等 SceneLoader 黑幕淡出結束、鎖定解除後，立刻跳轉 Level0
            SceneLoader.Instance.OnSceneTransitionComplete += JumpToLevel0;

            Debug.Log("[Level1_BypassManager] 偵測到首次進入 Level1，準備繞道至 Level0...");
        }
        else
        {
            // 玩家已經從 Level0 回來了
            if (targetObjectToToggle != null)
            {
                targetObjectToToggle.SetActive(true);
            }

            Debug.Log("[Level1_BypassManager] 已完成 Level0 流程，正常啟動 Level1。");
        }
    }

    private void JumpToLevel0()
    {
        // 記得取消訂閱，防止重複觸發
        SceneLoader.Instance.OnSceneTransitionComplete -= JumpToLevel0;

        // 標記為已經去過 Level0
        hasVisitedLevel0 = true;

        // 開啟協程，稍等一下再跳轉
        StartCoroutine(WaitAndLoadLevel0());
    }

    private IEnumerator WaitAndLoadLevel0()
    {
        // 關鍵：等待一幀。
        // 這會讓 Unity 先把 SceneLoader 裡剩下的程式碼（包含 _isLoading = false）執行完畢。
        yield return null;

        // 現在 _isLoading 已經是 false 了，可以安全呼叫過場
        SceneLoader.Instance.LoadScene("Level0");
    }
}
