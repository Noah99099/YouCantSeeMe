using UnityEngine;

public class AfterGetFridgeNote : MonoBehaviour
{
    [Header("事件ID")]
    [Tooltip("用於觸發此對話的唯一字串 ID")]
    public string eventID;

    public void FridgeNote()
    {
        DialogueManager.Instance.TriggerDialogueByEvent(eventID);
    }
}
