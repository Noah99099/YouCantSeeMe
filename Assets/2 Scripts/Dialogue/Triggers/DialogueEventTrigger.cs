using UnityEngine;

/// <summary>
/// 【新腳本】
/// 將一個 "遊戲事件ID" 與一個 "對話圖形" 綁定。
/// 其他腳本可以透過呼叫 DialogueManager.Instance.TriggerDialogueByEvent(eventID) 來啟動這個對話。
/// </summary>
public class DialogueEventTrigger : MonoBehaviour
{
    [Header("事件ID")]
    [Tooltip("用於觸發此對話的唯一字串 ID")]
    public string eventID;

    [Header("要觸發的對話")]
    [Tooltip("當此 eventID 被呼叫時，要啟動的對話圖形")]
    public DialogueGraph graphToTrigger;

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(eventID) || graphToTrigger == null)
        {
            Debug.LogWarning($"在 {gameObject.name} 上的 DialogueEventTrigger 沒有設定 eventID 或 graphToTrigger。", this);
            return;
        }

        // 向 DialogueManager 註冊自己
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.RegisterDialogueEvent(eventID, graphToTrigger);
        }
    }

    private void OnDisable()
    {
        if (string.IsNullOrEmpty(eventID)) return;

        // 向 DialogueManager 取消註冊自己
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.UnregisterDialogueEvent(eventID);
        }
    }
}