using UnityEngine;
using TMPro;

/// <summary>
/// 管理5個特定物件的交互進度，觸發對話與給予物品
/// </summary>

public class EvidenceMarkerManager : MonoBehaviour
{
    [Header("核心引用")]
    [Tooltip("放入那5個掛有 EvidenceMarker 的物件")]
    [SerializeField] private EvidenceMarker[] specificClues = new EvidenceMarker[5];

    [Tooltip("用來播放提示的 UpdateRightHintText 腳本")]
    [SerializeField] private UpdateRightHintText hintTextManager;

    [Header("對話與獎勵設定")]
    [Tooltip("集齊後要觸發的對話ID")]
    [SerializeField] private string targetDialogueID;

    [Tooltip("對話結束後獲得的物品")]
    [SerializeField] private ItemData rewardItem; // 引用自 ItemData.cs

    // 內部計算用，不顯示給玩家
    private int currentInteractionCount = 0;
    private int requiredCount = 5;

    private void Start()
    {
        requiredCount = specificClues.Length;

        // 訂閱所有線索的第一次交互事件
        foreach (var clue in specificClues)
        {
            if (clue != null)
            {
                clue.OnFirstInteraction += HandleClueInteracted;
            }
            else
            {
                Debug.LogWarning("[Manager] 有線索欄位未填入！");
            }
        }
    }

    private void HandleClueInteracted(EvidenceMarker clue)
    {
        currentInteractionCount++;

        // 取消訂閱，避免重複觸發
        clue.OnFirstInteraction -= HandleClueInteracted;

        // 檢查是否全部收集完畢
        if (currentInteractionCount >= requiredCount)
        {
            TriggerAllCluesFoundEvent();
        }
    }

    private void TriggerAllCluesFoundEvent()
    {
        Debug.Log("[Manager] 5個線索已全數調查完畢！");

        // 1. 跳出新通知 (4)，獲得死因調查結果
        if (hintTextManager != null)
        {
            hintTextManager.AfterEvidenceMarker();
        }

        // 2. 播放對話
        // 假設 DialogueManager 存在於您的專案中
        DialogueManager.Instance.TriggerDialogueByEvent(targetDialogueID);

        // 備註：對話系統通常是非同步的。您需要從您的「對話結束事件」(例如對話節點的最後一步)
        // 呼叫下方這個 OnDialogueFinished() 方法。
        // 如果您的對話系統有提供 C# 事件，可以在這裡訂閱，例如：
        // DialogueManager.Instance.OnDialogueEnd += OnDialogueFinished;
    }

    /// <summary>
    /// 當對話結束時呼叫此方法。
    /// 可以將此方法掛載到對話系統的 UnityEvent 中，或是透過代碼呼叫。
    /// </summary>
    public void OnDialogueFinished()
    {
        // 3. 對話結束後跳出新通知 (5)，解開飯廳的法陣
        if (hintTextManager != null)
        {
            hintTextManager.AfterMarkerConclusion();
        }

        // 4. 獲得 ItemData 類型的物件
        // 假設您的 InventoryManager 擁有 AddItem 方法 (基於 PlayerInteraction.cs 中有 RemoveItem)
        if (rewardItem != null && InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem(rewardItem);
            Debug.Log($"[Manager] 已將物品 {rewardItem.itemName} 加入背包");
        }

        // 5. 腳本結束/掛載腳本的空物件刪除
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // 安全機制：確保物件銷毀時解除所有訂閱，避免 Memory Leak
        foreach (var clue in specificClues)
        {
            if (clue != null)
            {
                clue.OnFirstInteraction -= HandleClueInteracted;
            }
        }
    }
}
