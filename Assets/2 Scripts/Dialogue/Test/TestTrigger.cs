using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestTrigger : MonoBehaviour
{
    // 將你剛才建立的 TestConversation 這個 Asset 拖到這裡
    public DialogueGraph dialogueToStart;

    // 為了測試方便，我們在遊戲開始時直接觸發
    void Start()
    {
        if (dialogueToStart != null)
        {
            DialogueManager.Instance.StartConversation(dialogueToStart);
        }
    }
}
