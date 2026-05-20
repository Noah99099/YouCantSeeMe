using UnityEngine;

public class InfoCollectorManager : MonoBehaviour
{
    [Header("收集狀態 (僅供觀察)")]
    [SerializeField] private bool hasInfo2 = false;
    [SerializeField] private bool hasInfo3 = false;

    /// <summary>
    /// 給 L2_KR_Info2 的 ViewDependentPickableItem 在 onPanelClosed 時呼叫
    /// </summary>
    public void OnCollectInfo2()
    {
        if (hasInfo2) return; // 避免重複觸發

        hasInfo2 = true;
        Debug.Log("[InfoCollectorManager] 已閱讀並關閉 L2_KR_Info2 面板");
        CheckAndTriggerDialogue();
    }

    /// <summary>
    /// 給 L2_KR_Info3 的 ViewDependentPickableItem 在 onPanelClosed 時呼叫
    /// </summary>
    public void OnCollectInfo3()
    {
        if (hasInfo3) return; // 避免重複觸發

        hasInfo3 = true;
        Debug.Log("[InfoCollectorManager] 已閱讀並關閉 L2_KR_Info3 面板");
        CheckAndTriggerDialogue();
    }

    /// <summary>
    /// 檢查是否兩者都已收集，是則觸發對話
    /// </summary>
    private void CheckAndTriggerDialogue()
    {
        if (hasInfo2 && hasInfo3)
        {
            Debug.Log("[InfoCollectorManager] 兩個線索皆已閱讀完畢，觸發對話 Round2_3！");

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.TriggerDialogueByEvent("Round2_3");
            }
            else
            {
                Debug.LogError("[InfoCollectorManager] 找不到 DialogueManager 實例，無法觸發對話！");
            }
        }
    }
}