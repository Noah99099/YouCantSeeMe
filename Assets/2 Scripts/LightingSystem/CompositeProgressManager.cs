using System;
using System.Collections;
using UnityEngine;

namespace KanWu.Systems
{
    /// <summary>
    /// 綜合進度管理器：負責追蹤多種不同類型的進度 (謎題、聲音物品、視角物品)
    /// 並在達到特定複合門檻時觸發最終對話
    /// </summary>
    public class CompositeProgressManager : MonoBehaviour
    {
        [Header("當前進度追蹤 (唯讀)")]
        [SerializeField] private int completedPuzzles = 0;
        [SerializeField] private int collectedVoiceItems = 0;
        [SerializeField] private int collectedViewItems = 0;

        [Header("觸發門檻設定")]
        [Tooltip("需要完成多少個謎題？")]
        public int requiredPuzzles = 2;
        [Tooltip("需要收集多少個 VoiceItem？")]
        public int requiredVoiceItems = 3;
        [Tooltip("需要查看/收集多少個 ViewDependentItem？")]
        public int requiredViewItems = 5;

        [Header("事件設定")]
        [Tooltip("達標後要觸發的對話 ID")]
        public string targetDialogueID = "Floor1_End";

        // 確保只觸發一次
        private bool isTriggered = false;

        public static CompositeProgressManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        /// <summary>
        /// 當完成謎題時呼叫
        /// </summary>
        public void NotifyPuzzleSolved()
        {
            completedPuzzles++;
            Debug.Log($"[CompositeProgress] 謎題進度：{completedPuzzles}/{requiredPuzzles}");
            CheckThresholds();
        }

        /// <summary>
        /// 當拾取聲音物品時呼叫
        /// </summary>
        public void NotifyVoiceItemCollected()
        {
            collectedVoiceItems++;
            Debug.Log($"[CompositeProgress] 聲音物品進度：{collectedVoiceItems}/{requiredVoiceItems}");
            CheckThresholds();
        }

        /// <summary>
        /// 當首次查看/拾取陰陽視角物品時呼叫
        /// </summary>
        public void NotifyViewItemCollected()
        {
            collectedViewItems++;
            Debug.Log($"[CompositeProgress] 視角物品進度：{collectedViewItems}/{requiredViewItems}");
            CheckThresholds();
        }

        /// <summary>
        /// 檢查是否三個條件都達標
        /// </summary>
        private void CheckThresholds()
        {
            if (isTriggered) return;

            if (completedPuzzles >= requiredPuzzles &&
                collectedVoiceItems >= requiredVoiceItems &&
                collectedViewItems >= requiredViewItems)
            {
                isTriggered = true;
                Debug.Log($"[CompositeProgress] 所有條件皆已達成！準備播放 {targetDialogueID}");
                StartCoroutine(WaitAndPlayFinalDialogue());
            }
        }

        private IEnumerator WaitAndPlayFinalDialogue()
        {
            // 等待一幀確保其他系統先跑完
            yield return null;

            // 1. 等待對話結束 (處理拾取 VoiceItem 等情況帶來的對話)
            if (DialogueManager.Instance != null)
            {
                while (DialogueManager.Instance.IsDialogueActive())
                {
                    yield return null;
                }
            }

            // 2. [新增] 等待陰陽圖片面板關閉 (處理 ReusableViewDependentItem)
            // 透過檢查 ViewImagePanelController 的 panelRoot 是否顯示中來判斷
            if (ViewImagePanelController.Instance != null && ViewImagePanelController.Instance.panelRoot != null)
            {
                while (ViewImagePanelController.Instance.panelRoot.activeSelf)
                {
                    yield return null;
                }
            }

            // 3. 雙重保險：如果在關閉面板的瞬間又觸發了什麼對話，再等一次對話結束
            if (DialogueManager.Instance != null)
            {
                while (DialogueManager.Instance.IsDialogueActive())
                {
                    yield return null;
                }
            }

            // 等到畫面乾淨 (無對話、無圖片面板) 後，觸發指定的結尾對話
            if (DialogueManager.Instance != null && !string.IsNullOrEmpty(targetDialogueID))
            {
                DialogueManager.Instance.TriggerDialogueByEvent(targetDialogueID);
                Debug.Log($"[CompositeProgress] 已觸發結尾對話：{targetDialogueID}");
            }
            else
            {
                Debug.LogError("[CompositeProgress] 找不到 DialogueManager，或未設定 DialogueID！");
            }
        }
    }
}