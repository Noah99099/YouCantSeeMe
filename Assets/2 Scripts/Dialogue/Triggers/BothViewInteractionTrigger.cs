using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 這個元件需要一個碰撞體才能被點擊
[RequireComponent(typeof(Collider))]
public class BothViewInteractionTrigger : MonoBehaviour
{
    [Header("陰陽視野下要觸發的對話圖形")]
    [Tooltip("當 ViewManager 處於 'Yang' 視野時要觸發的對話")]
    public DialogueGraph yangDialogueGraph;

    [Tooltip("當 ViewManager 處於 'Yin' 視野時要觸發的對話")]
    public DialogueGraph yinDialogueGraph;

    /// <summary>
    /// 這個方法可以被例如點擊事件 (OnMouseDown) 或其他互動系統呼叫。
    /// </summary>
    public void Interact()
    {
        // 檢查 DialogueManager 是否存在且當前沒有對話在播放
        if (DialogueManager.Instance == null || DialogueManager.Instance.IsDialogueActive())
        {
            return;
        }

        // 檢查 ViewManager 是否存在
        if (ViewManager.Instance == null)
        {
            Debug.LogError("ViewManager.Instance 未找到，無法判斷視野類型。");
            return;
        }

        // 根據當前的 ViewType 選擇要播放的 DialogueGraph
        DialogueGraph graphToPlay = null;
        ViewType currentView = ViewManager.Instance.CurrentView;

        if (currentView == ViewType.Yang)
        {
            graphToPlay = yangDialogueGraph;
        }
        else // (currentView == ViewType.Yin)
        {
            graphToPlay = yinDialogueGraph;
        }

        // 播放選擇的對話
        if (graphToPlay != null)
        {
            DialogueManager.Instance.StartConversation(graphToPlay);
        }
        else
        {
            // 可選：如果對應視野的 dialogueGraph 沒有設置，可以在 Console 提示
            Debug.LogWarning($"在 {gameObject.name} 上，目前視野 ({currentView}) 沒有指定對應的 DialogueGraph。");
        }
    }
}
