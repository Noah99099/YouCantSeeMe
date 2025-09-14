using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    [Header("功能：使用物件進行場景交互的設置腳本")]
    [Header("交互物件設置")]
    public string objectName = "物件"; // 物件名稱
    public ItemData requiredItem; // 需要的物品

    [Header("事件")]
    public UnityEngine.Events.UnityEvent onCorrectItemUsed; // 正確使用物品時觸發
    public UnityEngine.Events.UnityEvent onWrongItemUsed; // 錯誤使用物品時觸發

    /// <summary>
    /// 當正確使用物品時調用
    /// </summary>
    public void OnCorrectItemUsed()
    {
        Debug.Log($"正確物品被使用於 {objectName}");
        onCorrectItemUsed.Invoke();

        // 可以在這裡添加其他邏輯，例如禁用物件、播放動畫等
    }

    /// <summary>
    /// 當錯誤使用物品時調用
    /// </summary>
    public void OnWrongItemUsed()
    {
        Debug.Log($"錯誤物品被使用於 {objectName}");
        onWrongItemUsed.Invoke();

        // 可以在這裡添加其他邏輯，例如顯示錯誤訊息、播放聲音等
    }
}