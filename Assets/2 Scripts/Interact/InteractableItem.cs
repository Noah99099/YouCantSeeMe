// InteractableItem.cs
using UnityEngine;
using UnityEngine.Events;

// 需要一個碰撞器來被射線偵測到
[RequireComponent(typeof(Collider))]
public class InteractableItem : MonoBehaviour, IInteractable
{
    [Header("功能：獲得物品進到背包")]
    public ItemData itemData;

    [Header("拾取事件")] // [新增]
    public UnityEvent onPickUp;

    [HideInInspector] public bool InteractionEnabled = true;

    #region ** IInteractable要求內容 **
    // 2. 實作提示文字
    public string GetInteractPrompt(bool isGamepad)
    {
        return isGamepad ? $"按 [叉] 拾取 {itemData.itemName}" : $"按 [滑鼠左鍵] 拾取 {itemData.itemName}";
    }

    // 3. 實作互動行為
    public void Interact(PlayerInteraction player)
    {
        // 檢查是否允許互動
        if (!InteractionEnabled) return;

        Debug.Log($"[InteractableItem] 玩家拾取了 {itemData.itemName}");

        // 1. 執行它原本的邏輯 (觸發你掛載的 UnityEvent)
        TriggerPickUpEvent();

        // ***** 【關鍵補齊】：原本寫在 PlayerInteraction 的邏輯搬過來 *****
        // 2. 將物品真正加入背包系統
        if (InventoryManager.Instance != null && itemData != null)
        {
            InventoryManager.Instance.AddItem(itemData);
        }
        else
        {
            Debug.LogError($"[InteractableItem] 拾取失敗：InventoryManager 或 ItemData 為空！");
        }

        // 3. 銷毀場景上的 3D 物件
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

    // [新增] 供 PlayerInteraction 呼叫的輔助方法
    public void TriggerPickUpEvent()
    {
        onPickUp?.Invoke();
    }
}