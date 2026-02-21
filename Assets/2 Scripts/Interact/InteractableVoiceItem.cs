using UnityEngine;

// 需要一個碰撞器來被射線偵測到
[RequireComponent(typeof(Collider))]
public class InteractableVoiceItem : MonoBehaviour, IInteractable
{
    [Header("功能：獲得 聲音物品 進到 聲音面板")]
    // 在 Inspector 中，將你為這個物件建立的 VoiceItemData 資源檔拖曳到這裡
    public VoiceItemData voiceItemData;

    [HideInInspector] public bool InteractionEnabled = true;

    #region ** IInteractable要求內容 **
    // 2. 實作提示文字
    public string GetInteractPrompt(bool isGamepad)
    {
        return isGamepad ? $"按 [叉] 拾取 {voiceItemData.itemName}" : $"按 [滑鼠左鍵] 拾取 {voiceItemData.itemName}";
    }

    // 3. 實作互動行為
    public void Interact(PlayerInteraction player)
    {
        if (!InteractionEnabled || voiceItemData == null) return;

        Debug.Log($"[InteractableVoiceItem] 玩家與{voiceItemData.itemName}交互");

        // 第 1 步：觸發花屏特效與雜音 (呼叫玩家身上的公開協程)
        player.StartCoroutine(player.PlayGlitchEffectOnce());

        // 第 2 步：加進聲音面板
        if (VoiceItemManager.Instance != null)
        {
            VoiceItemManager.Instance.AddItem(voiceItemData);
        }

        // 第 3 步：觸發對應的對話 (完美解耦寫法！)
        // 只要 voiceItemID 有填寫內容，就直接拿它當作事件名稱去呼叫
        if (DialogueManager.Instance != null && !string.IsNullOrEmpty(voiceItemData.voiceItemID))
        {
            DialogueManager.Instance.TriggerDialogueByEvent(voiceItemData.voiceItemID);
        }

        // 第 4 步：銷毀場景上的 3D 物件
        Destroy(gameObject);
    }
    #endregion

    public void SetInteractionEnabled(bool enabled)
    {
        InteractionEnabled = enabled;
        this.enabled = enabled; // 如果外部用 component.enabled 檢查也能同步
    }

    public bool TryInteract()
    {
        if (!InteractionEnabled) return false;
        // 執行撿取或互動邏輯
        return true;
    }
}
