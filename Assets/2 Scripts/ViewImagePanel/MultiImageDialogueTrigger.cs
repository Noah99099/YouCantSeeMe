using UnityEngine;
using System.Collections.Generic;
using System.Collections;

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

    // 使用 HashSet 紀錄已看過的物件，避免玩家重複觀看同一個物件導致計數錯誤
    private HashSet<ViewDependentImageObject> completedImages = new HashSet<ViewDependentImageObject>();
    private bool hasTriggeredDialogue = false;

    private void Start()
    {
        // 在遊戲開始時，自動為清單中的每個物品註冊事件
        // 這樣就不需要在 Inspector 裡手動為每個物品拉 UnityEvent 的線了
        foreach (var imageObj in targetImages)
        {
            if (imageObj != null)
            {
                // 使用 Lambda 傳遞是哪一個物件觸發了關閉
                imageObj.onPanelClosed.AddListener(() => OnSinglePanelClosed(imageObj));
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
    private void OnSinglePanelClosed(ViewDependentImageObject triggeredObj)
    {
        // 確保最終對話只會被觸發一次邏輯
        if (hasTriggeredDialogue) return;

        // 如果這個物件之前沒被看過，加入進度
        if (!completedImages.Contains(triggeredObj))
        {
            completedImages.Add(triggeredObj);
            Debug.Log($"[MultiImageDialogueTrigger] 物品已查看進度: {completedImages.Count} / {targetImages.Count}");

            // 當 3 個物品都至少看過一次
            if (completedImages.Count >= targetImages.Count)
            {
                // 開啟協程，等待第3個物品自己的對話播完
                StartCoroutine(WaitAndTriggerTargetDialogue());
            }
        }
    }

    private IEnumerator WaitAndTriggerTargetDialogue()
    {
        hasTriggeredDialogue = true; // 鎖住狀態，防止後續重複觸發協程

        // 1. 先等待一幀！確保第 3 個物件在 onPanelClosed 中觸發的對話有時間啟動並讓群組變數更新
        yield return null;

        // 2. 對接 DialogueManager 的 API，持續檢查對話是否還在進行
        if (DialogueManager.Instance != null)
        {
            // 如果當前還有對話正在播放（例如第3個物件的對話），就持續等待
            while (DialogueManager.Instance.IsDialogueActive())
            {
                yield return null; // 每幀檢查一次
            }
            Debug.Log("[MultiImageDialogueTrigger] 偵測到前置對話已徹底結束。");
        }

        // 3. 前置對話全部清空、徹底結束後，才觸發最終的整合對話
        if (DialogueManager.Instance != null)
        {
            Debug.Log($"[MultiImageDialogueTrigger] 條件達成！準備觸發最終對話事件: {dialogueEventID}");
            DialogueManager.Instance.TriggerDialogueByEvent(dialogueEventID);
        }
        else
        {
            Debug.LogError("[MultiImageDialogueTrigger] 找不到 DialogueManager.Instance，無法觸發對話！");
        }
    }
}