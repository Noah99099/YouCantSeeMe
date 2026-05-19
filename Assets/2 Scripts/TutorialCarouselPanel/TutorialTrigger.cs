// TutorialTrigger.cs
using System.Collections;
using UnityEngine;

/// <summary>
/// 這是一個觸發教學指示腳本。
/// 外部調用 TutorialTrigger 的 public 方法實現。
/// </summary>
public class TutorialTrigger : MonoBehaviour
{
    // 1. 在 Inspector 中，將你的 "TutorialCarouselPanel" 物件拖到這裡
    public TutorialCarouselManager _tutorialManager;

    // 2. 在 Inspector 中，將你建立的 所有指示 拖到這裡
    public TutorialData[] tutorialToShow;

    public void MoveLookInteract() // 移動、旋轉相機、交互（純文字、標示）
    {
        StartCoroutine(ShowTutorialAfterDelay(tutorialToShow[0]));
    }

    public void SwitchView() // 切換陰陽視野
    {
        StartCoroutine(ShowTutorialAfterDelay(tutorialToShow[1]));

        // 呼叫右上提示
        RightHintManager.Instance.ShowHint();
    }

    public void CaseRecordBook() // 使用紀錄簿（物、鬼、聲、組合） + 平面圖
    {
        // 目前規劃是先顯示紀錄簿後接著平面圖
        // 只能寫在一起
        StartCoroutine(ShowTutorialAfterDelay(tutorialToShow[2]));
    }

    public void AfterGetVoiceItem() // 獲得聲音物品後
    {
        // 目前規劃是先顯示紀錄簿後接著平面圖
        // 只能寫在一起
        StartCoroutine(ShowTutorialAfterDelay(tutorialToShow[3]));
    }

    /// <summary>
    /// 啟動一個協程 (Coroutine)，等待一幀後再顯示教學。
    /// </summary>
    /// <param name="tutorialData">要顯示的教學資料</param>
    private IEnumerator ShowTutorialAfterDelay(TutorialData tutorialData)
    {
        // --- 核心 ---
        // 在這裡暫停，等到下一幀再繼續執行下面的程式碼
        yield return null;
        yield return null;

        // (如果你想等待更多幀，例如 3 幀，就在這裡寫 3 次 yield return null;)
        // (如果想等到該幀所有渲染都完成後才顯示，可以用 yield return new WaitForEndOfFrame();)

        // 在下一幀，才真正呼叫 ShowTutorial 方法
        _tutorialManager.ShowTutorial(tutorialData);
    }
}