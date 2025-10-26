// SceneDialogueController.cs
using UnityEngine;

[DefaultExecutionOrder(10)] //要比 Level1UIController.cs 慢一點
public class SceneDialogueController : MonoBehaviour
{
    [Tooltip("要在場景開始時觸發的對話 (請確保其 Trigger Type 已設為 OnSceneStart)")]
    public DialogueGraph startDialogue;

    // ***** 新增 *****
    // 標記場景轉場是否已完成
    private bool _sceneTransitionFinished = false;
    // 標記對話是否已開始 (防止重複觸發)
    private bool _dialogueStarted = false;

    // ***** 修改 *****
    // 使用 OnEnable 來訂閱事件
    private void OnEnable()
    {
        // 檢查 SceneLoader 是否存在
        if (SceneLoader.Instance != null)
        {
            // 訂閱「轉場完成」事件
            SceneLoader.Instance.OnSceneTransitionComplete += HandleSceneTransitionComplete;
        }
        else
        {
            // 如果沒有 SceneLoader (例如直接在編輯器中啟動此場景)，
            // 我們就當作轉場已「完成」，可以立即開始
            _sceneTransitionFinished = true;
        }
    }

    // ***** 新增 *****
    // 務必在 OnDisable 取消訂閱
    private void OnDisable()
    {
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.OnSceneTransitionComplete -= HandleSceneTransitionComplete;
        }
    }

    // ***** 修改 *****
    // Start() 現在只負責檢查是否能「立即開始」
    void Start()
    {
        // 如果 _sceneTransitionFinished 已經是 true (因為沒有 SceneLoader)，
        // 則在這裡立即嘗試開始對話。
        // 否則，此方法不做任何事，等待 HandleSceneTransitionComplete 被呼叫。
        TryStartDialogue();
    }

    // ***** 新增 *****
    /// <summary>
    /// 當 SceneLoader 觸發 OnSceneTransitionComplete 事件時，此方法會被呼叫。
    /// </summary>
    private void HandleSceneTransitionComplete()
    {
        _sceneTransitionFinished = true;
        TryStartDialogue();
    }

    // ***** 新增 *****
    /// <summary>
    /// 嘗試開始對話。
    /// </summary>
    private void TryStartDialogue()
    {
        // 必須滿足所有條件：
        // 1. 有 startDialogue
        // 2. 場景轉場已完成 (無論是事件通知的，還是因為沒有SceneLoader)
        // 3. 對話尚未開始過
        if (startDialogue != null && _sceneTransitionFinished && !_dialogueStarted)
        {
            // 立刻標記為已開始，防止重複執行
            _dialogueStarted = true;

            print("[SceneDialogueController] 開始播放劇情");
            DialogueManager.Instance.StartConversation(startDialogue);
        }
    }
}