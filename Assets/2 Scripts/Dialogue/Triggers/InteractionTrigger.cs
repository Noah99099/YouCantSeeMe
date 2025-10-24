using UnityEngine;

// 這個元件需要一個碰撞體才能被點擊
[RequireComponent(typeof(Collider))]
public class InteractionTrigger : MonoBehaviour
{
    [Tooltip("要觸發的對話圖形 (請確保其 Trigger Type 已設為 OnInteract)")]
    public DialogueGraph dialogueGraph;

    // OnMouseDown 是 Unity 內建的方法，當滑鼠點擊到物件的 Collider 時會被呼叫
    public void Interact()
    {
        if (dialogueGraph != null && DialogueManager.Instance != null && !DialogueManager.Instance.IsDialogueActive())
        {
            DialogueManager.Instance.StartConversation(dialogueGraph);
        }
    }
}