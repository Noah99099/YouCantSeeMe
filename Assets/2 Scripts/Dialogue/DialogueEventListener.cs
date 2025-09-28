// DialogueEventListener.cs (最終偵錯版)
using UnityEngine;
using UnityEngine.Events;

public class DialogueEventListener : MonoBehaviour
{
    public string eventID;
    public UnityEvent onEventTriggered;

    private void OnEnable()
    {
        if (DialogueManager.Instance != null)
        {
            if (!string.IsNullOrEmpty(eventID))
            {
                // --- 新增日誌 ---
                Debug.Log($"[EventListener] 監聽器 '{gameObject.name}' 正在嘗試用 ID 註冊: \"{eventID.Trim()}\"");
                DialogueManager.Instance.RegisterListener(this);
            }
        }
    }

    private void OnDisable()
    {
        if (DialogueManager.Instance != null)
        {
            if (!string.IsNullOrEmpty(eventID))
            {
                DialogueManager.Instance.UnregisterListener(this);
            }
        }
    }

    public void TriggerEvent()
    {
        onEventTriggered?.Invoke();
    }
}