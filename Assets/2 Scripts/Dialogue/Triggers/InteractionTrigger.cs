using UnityEngine;

// 這個元件需要一個碰撞體才能被點擊
[RequireComponent(typeof(Collider))]
public class InteractionTrigger : MonoBehaviour, IInteractable
{
    [Tooltip("名稱，目前用到的有子彈")]
    public string itemName = "";

    [Tooltip("要觸發的對話圖形 (請確保其 Trigger Type 已設為 OnInteract)")]
    public DialogueGraph dialogueGraph;

    #region ** IInteractable要求內容 **
    // 2. 實作提示文字
    public string GetInteractPrompt(bool isGamepad)
    {
        return isGamepad ? $"按 [叉] 與 {itemName} 交互" : $"按 [滑鼠左鍵] 與 {itemName} 交互";
    }

    // 3. 實作互動行為
    public void Interact(PlayerInteraction player)
    {
        Debug.Log($"[InteractionTrigger] 玩家與{itemName}交互");
        Interact(); // 執行它原本的邏輯
    }
    #endregion

    /// <summary>
    /// 執行純對話邏輯 (由 IInteractable 介面觸發)
    /// </summary>
    public void Interact()
    {
        if (dialogueGraph != null && DialogueManager.Instance != null && !DialogueManager.Instance.IsDialogueActive())
        {
            DialogueManager.Instance.StartConversation(dialogueGraph);
        }
    }
}