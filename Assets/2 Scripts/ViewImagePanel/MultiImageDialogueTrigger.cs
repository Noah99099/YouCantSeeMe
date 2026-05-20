using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 監聽多個 ViewDependentImageObject，當所有物品都觸發過一次 onPanelClosed 後，執行指定的對話事件。
/// </summary>
public class MultiImageDialogueTrigger : MonoBehaviour
{
    [Header("觸發條件設定")]
    [Tooltip("需要交互且關閉面板的物品清單")]
    public List<ViewDependentImageObject> targetImages;

    [Header("對話設定")]
    [Tooltip("所有物品都交互完畢後，要觸發的對話事件 ID")]
    public string dialogueEventID = "B1_3Painting";

    private int completedCount = 0;
    private bool hasTriggeredDialogue = false;

    private void Start()
    {
        // 在遊戲開始時，自動為清單中的每個物品註冊事件
        // 這樣就不需要在 Inspector 裡手動為每個物品拉 UnityEvent 的線了
        foreach (var imageObj in targetImages)
        {
            if (imageObj != null)
            {
                imageObj.onPanelClosed.AddListener(OnSinglePanelClosed);
            }
            else
            {
                Debug.LogWarning("[MultiImageDialogueTrigger] targetImages 清單中有空缺的物件，請檢查 Inspector！");
            }
        }
    }

    /// <summary>
    /// 當清單中的任何一個物品觸發了 onPanelClosed 時，就會呼叫此方法
    /// </summary>
    private void OnSinglePanelClosed()
    {
        // 為了確保對話只會被觸發一次
        if (hasTriggeredDialogue) return;

        completedCount++;
        Debug.Log($"[MultiImageDialogueTrigger] 物品已查看進度: {completedCount} / {targetImages.Count}");

        // 檢查是否所有物品都已經交互完畢
        if (completedCount >= targetImages.Count)
        {
            TriggerTargetDialogue();
        }
    }

    private void TriggerTargetDialogue()
    {
        hasTriggeredDialogue = true;

        if (DialogueManager.Instance != null)
        {
            Debug.Log($"[MultiImageDialogueTrigger] 條件達成！準備觸發對話事件: {dialogueEventID}");
            DialogueManager.Instance.TriggerDialogueByEvent(dialogueEventID);
        }
        else
        {
            Debug.LogError("[MultiImageDialogueTrigger] 找不到 DialogueManager.Instance，無法觸發對話！");
        }
    }
}