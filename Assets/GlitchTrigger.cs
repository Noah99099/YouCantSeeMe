using UnityEngine;

public class GlitchTrigger : MonoBehaviour
{
    public string voiceObjectName;
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // 取得玩家拿到的物件
        var manager = VoiceItemInteractionManager.Instance;
        if (manager == null) return;

        Debug.Log($"獲得物件名稱： {manager.CurrentVoice.objectName} ，另一個名稱：{voiceObjectName}");

        //if (manager.CurrentVoice != null)
        //{
        //    // 玩家已經拿到 InteractableVoice，且這個 Trigger 對應它
        //    // 可以用 trigger 對應物件或 ID 判斷
        //    if (manager.CurrentVoice.objectName == voiceObjectName) // 或其他對應方式
        //    {
        //        manager.OnEnterTrigger();
        //        Debug.Log($"Player 進到 {gameObject.name} 範圍，觸發 VoiceItemInteractionManager");
        //    }
        //}
        // 玩家已經拿到 InteractableVoice，且這個 Trigger 對應它
        // 可以用 trigger 對應物件或 ID 判斷
        if (manager.CurrentVoice.objectName == voiceObjectName) // 或其他對應方式
        {
            manager.OnEnterTrigger();
            Debug.Log($"Player 進到 {gameObject.name} 範圍，觸發 VoiceItemInteractionManager");
        }

        Destroy(gameObject);
    }
}
