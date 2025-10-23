using UnityEngine;

// 這個元件需要一個碰撞體才能被點擊
[RequireComponent(typeof(Collider))]
public class InteractionTrigger : MonoBehaviour
{
    [Tooltip("要觸發的對話圖形 (請確保其 Trigger Type 已設為 OnInteract)")]
    public DialogueGraph dialogueGraph;

    // OnMouseDown 是 Unity 內建的方法，當滑鼠點擊到物件的 Collider 時會被呼叫
    private void OnMouseDown()
    {
        // 確保有指定對話圖形，並且當前沒有其他對話正在進行
        if (dialogueGraph != null && !DialogueManager.Instance.IsDialogueActive())
        {
            DialogueManager.Instance.StartConversation(dialogueGraph);
        }
    }
}