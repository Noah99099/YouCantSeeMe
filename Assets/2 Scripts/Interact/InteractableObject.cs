using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    [Header("功能：使用物件進行場景交互的設置腳本")]
    [Header("交互物件設置")]
    public string objectName = "物件"; // 物件名稱
    public ItemData requiredItem; // 需要的物品

    [Header("事件")]
    public UnityEngine.Events.UnityEvent onCorrectItemUsed;
    public UnityEngine.Events.UnityEvent onWrongItemUsed;

    /// <summary>
    /// 嘗試使用物品
    /// </summary>
    public bool UseItem(ItemData item)
    {
        if (item == null) return false;

        if (item == requiredItem)
        {
            Debug.Log($"[InteractableObject] 使用了正確物品 {item.itemName} 於 {objectName}");
            onCorrectItemUsed?.Invoke();
            Destroy(gameObject); // 物件消失
            return true; // 使用成功
        }
        else
        {
            Debug.Log($"[InteractableObject] 使用了錯誤物品 {item.itemName} 於 {objectName}");
            onWrongItemUsed?.Invoke();
            return false; // 使用失敗
        }
    }
}