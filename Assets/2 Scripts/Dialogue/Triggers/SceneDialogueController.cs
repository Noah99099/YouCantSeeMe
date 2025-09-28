// SceneDialogueController.cs
using UnityEngine;

public class SceneDialogueController : MonoBehaviour
{
    [Tooltip("要在場景開始時觸發的對話 (請確保其 Trigger Type 已設為 OnSceneStart)")]
    public DialogueGraph startDialogue;

    void Start()
    {
        if (startDialogue != null)
        {
            DialogueManager.Instance.StartConversation(startDialogue);
        }
    }
}