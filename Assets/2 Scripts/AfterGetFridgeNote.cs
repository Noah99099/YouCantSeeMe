using UnityEngine;

/// <summary>
/// 專門接收 Close Panel 後要觸發的對話圖形
/// </summary>
public class AfterGetFridgeNote : MonoBehaviour
{
    [Header("事件ID")]
    [Tooltip("用於觸發此對話的唯一字串 ID")]
    public string eventID;
    public string ID_B1_Paper;
    public string ID_B1_Painting1;
    public string ID_B1_Painting2;
    public string ID_B1_Painting3;

    public void FridgeNote()
    {
        DialogueManager.Instance.TriggerDialogueByEvent(eventID);
    }

    public void B1Paper()
    {
        DialogueManager.Instance.TriggerDialogueByEvent(ID_B1_Paper);
    }
    
    public void B1Painting_1()
    {
        DialogueManager.Instance.TriggerDialogueByEvent(ID_B1_Painting1);
    }

    public void B1Painting_2()
    {
        DialogueManager.Instance.TriggerDialogueByEvent(ID_B1_Painting2);
    }

    public void B1Painting_3()
    {
        DialogueManager.Instance.TriggerDialogueByEvent(ID_B1_Painting3);
    }
}
