using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AreaTrigger : MonoBehaviour
{
    [Tooltip("要觸發的對話圖形 (請確保其 Trigger Type 已設為 OnAreaEnter)")]
    public DialogueGraph dialogueGraph;

    //private bool hasTriggered = false; // 確保只觸發一次
    // 不需要，我來手動掛腳本刪掉

    private void Awake()
    {
        // 區域觸發器的碰撞體必須設定為 "Is Trigger"
        GetComponent<Collider>().isTrigger = true;
    }

    // 當帶有 Rigidbody 的物體進入觸發區時呼叫
    private void OnTriggerEnter(Collider other)
    {
        // 如果已經觸發過，或進入的不是玩家，就直接返回
        if (!other.CompareTag("Player")) return;
        
        if (dialogueGraph != null && !DialogueManager.Instance.IsDialogueActive())
        {
            Debug.Log("玩家進入觸發區，開始對話。");
            //hasTriggered = true;
            DialogueManager.Instance.StartConversation(dialogueGraph);
        }
    }
}