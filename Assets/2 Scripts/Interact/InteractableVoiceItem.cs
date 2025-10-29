using UnityEngine;

// 需要一個碰撞器來被射線偵測到
[RequireComponent(typeof(Collider))]
public class InteractableVoiceItem : MonoBehaviour
{
    [Header("功能：獲得 聲音物品 進到 聲音面板")]
    // 在 Inspector 中，將你為這個物件建立的 VoiceItemData 資源檔拖曳到這裡
    public VoiceItemData voiceItemData;

    [HideInInspector] public bool InteractionEnabled = true;

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
