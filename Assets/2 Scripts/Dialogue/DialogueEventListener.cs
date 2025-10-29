using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic; // 1. 引用 List
using System.Linq; // 2. 引用 Linq (方便查找)

// ------------------------------------------------------------------
// 1. 定義一個「事件項目」的 Helper Class
// [System.Serializable] 讓我們能在 Inspector 中看到並編輯它
// ------------------------------------------------------------------
[System.Serializable]
public class DialogueEventEntry
{
    [Tooltip("必須與 InvokeEventNode 中的 ID 完全匹配")]
    public string eventID;
    [Tooltip("當此 ID 被觸發時，要執行的事件")]
    public UnityEvent onEventTriggered;
}

// ------------------------------------------------------------------
// 2. 修改 DialogueEventListener 主體
// ------------------------------------------------------------------
public class DialogueEventListener : MonoBehaviour
{
    [Header("監聽的事件列表")]
    [Tooltip("此組件可以監聽的多個事件")]
    public List<DialogueEventEntry> eventEntries = new List<DialogueEventEntry>(); // 3. 從單一字串改成列表

    private void OnEnable()
    {
        if (DialogueManager.Instance != null)
        {
            // 4. 【重要】改為呼叫新的 "批量註冊" 方法
            DialogueManager.Instance.RegisterListener(this);
        }
    }

    private void OnDisable()
    {
        if (DialogueManager.Instance != null)
        {
            // 5. 【重要】改為呼叫新的 "批量取消註冊" 方法
            DialogueManager.Instance.UnregisterListener(this);
        }
    }

    /// <summary>
    /// 【重要】
    /// 此方法現在由 DialogueManager 呼叫，並傳入被觸發的 ID
    /// </summary>
    /// <param name="triggeredID">DialogueManager 傳來的事件 ID</param>
    public void TriggerEvent(string triggeredID)
    {
        // 6. 在我們的列表中尋找是哪一個 ID 被觸發了
        DialogueEventEntry entry = eventEntries.FirstOrDefault(e => e.eventID.Trim() == triggeredID);

        if (entry != null)
        {
            // 7. 找到後，只執行 "那一個" 綁定的 UnityEvent
            Debug.Log($"[EventListener] {gameObject.name} 接收到事件: {triggeredID}，並觸發綁定事件。");
            entry.onEventTriggered?.Invoke();
        }
        else
        {
            Debug.LogWarning($"[EventListener] {gameObject.name} 接收到事件 {triggeredID}，但在其列表中找不到匹配的項目。");
        }
    }
}