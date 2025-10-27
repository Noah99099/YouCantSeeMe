// SceneDialogueController.cs
using UnityEngine;

[DefaultExecutionOrder(10)] //要比 Level1UIController.cs 慢一點
public class SceneDialogueController : MonoBehaviour
{
    [Tooltip("要在場景開始時觸發的對話 (請確保其 Trigger Type 已設為 OnSceneStart)")]
    public DialogueGraph startDialogue;

    // ***** 需求修改: 新增靜態標記 *****
    /// <summary>
    /// 靜態標記，用於通知其他系統 (如 Level1UIController)
    /// 場景的開場對話是否正在播放。
    /// </summary>
    public static bool IsSceneDialoguePlaying { get; private set; } = false;

    // ***** 需求修改: 新增實例標記 *****
    private bool _isMyDialoguePlaying = false; // 標記是否是這個腳本實例啟動的對話

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

        // ***** 需求修改: 確保取消訂閱 *****
        if (_isMyDialoguePlaying && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnConversationEnd -= HandleMyDialogueEnd;
        }
        IsSceneDialoguePlaying = false; // 離開場景時重置
    }

    // ***** 修改 *****
    // Start() 現在只負責檢查是否能「立即開始」
    void Start()
    {
        // 如果 _sceneTransitionFinished 已經是 true (因為沒有 SceneLoader)，
        // 則在這裡立即嘗試開始對話。
        // 否則，此方法不做任何事，等待 HandleSceneTransitionComplete 被呼叫。
        IsSceneDialoguePlaying = false; //// ***** 需求修改: 重置靜態標記 *****
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
            _isMyDialoguePlaying = true; // 標記是此腳本啟動的
            IsSceneDialoguePlaying = true;

            // ***** 解決方案: 移除這裡的 PushMap *****
            // DialogueManager.Instance.StartConversation 會負責 Push
            /*
            if (InputStackManager.Instance != null)
            {
                InputStackManager.Instance.PushMap(InputActionMaps._Dialouge);
            }
            */

            print("[SceneDialogueController] 開始播放劇情");
            DialogueManager.Instance.StartConversation(startDialogue);

            // ***** 需求修改: 訂閱對話結束事件 *****
            // !!! 假設: 您的 DialogueManager 有一個 OnConversationEnd 事件 !!!
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnConversationEnd += HandleMyDialogueEnd;
            }
        }
    }

    // ***** 需求修改: 新增對話結束後的處理 *****
    /// <summary>
    /// 當 DialogueManager 結束對話時呼叫
    /// </summary>
    private void HandleMyDialogueEnd()
    {
        // 如果不是此腳本啟動的對話，則忽略
        if (!_isMyDialoguePlaying) return;

        _isMyDialoguePlaying = false;
        IsSceneDialoguePlaying = false; // 重置靜態標記

        // 取消訂閱
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnConversationEnd -= HandleMyDialogueEnd;
        }

        // 關鍵: 對話已結束 (DialogueManager 應該已經 PopMap)
        // 棧現在是 [Loading]，我們必須將其切換為 Player
        if (InputStackManager.Instance != null)
        {
            Debug.Log("[SceneDialogueController] 場景對話結束。初始化 Player Map。");
            InputStackManager.Instance.Init(InputActionMaps._Player);
        }
    }
}