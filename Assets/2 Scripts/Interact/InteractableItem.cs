// InteractableItem.cs
using UnityEngine;
using UnityEngine.Events;

// 需要一個碰撞器來被射線偵測到
[RequireComponent(typeof(Collider))]
public class InteractableItem : MonoBehaviour
{
    [Header("功能：獲得物品進到背包")]
    public ItemData itemData;

    [Header("拾取事件")] // [新增]
    public UnityEvent onPickUp;

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

    // [新增] 供 PlayerInteraction 呼叫的輔助方法
    public void TriggerPickUpEvent()
    {
        onPickUp?.Invoke();
    }
}