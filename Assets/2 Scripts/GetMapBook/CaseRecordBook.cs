// 檔案名稱: CaseRecordBook.cs
using System;
using UnityEngine;

/// <summary>
/// 一個標記組件，用於識別案件紀錄簿這個特殊的交互物件。
/// </summary>
public class CaseRecordBook : MonoBehaviour, IInteractable
{
    // 您可以在這裡添加物品的名稱或描述，以便在UI提示中使用
    public string itemName = "案件紀錄簿";

    // 當此物件被拾取時的事件（不再用單例）
    public static event Action OnCollected;

    #region ** IInteractable要求內容 **
    // 2. 實作提示文字
    public string GetInteractPrompt(bool isGamepad)
    {
        return isGamepad ? $"按 [叉] 與 {itemName} 交互" : $"按 [滑鼠左鍵] 與 {itemName} 交互";
    }

    // 3. 實作互動行為
    public void Interact(PlayerInteraction player)
    {
        Debug.Log($"[CaseRecordBook] 玩家獲得{itemName}");
        Collect(); // 執行它原本的邏輯
    }
    #endregion

    /// <summary>
    /// 呼叫此方法以觸發拾取事件。
    /// </summary>
    public void Collect()
    {
        Debug.Log($"CaseRecordBook: {itemName} 已被拾取！");
        OnCollected?.Invoke();

        // ***** 【關鍵補齊】：原本寫在 PlayerInteraction 的銷毀邏輯，現在要自己負責！ *****
        Destroy(gameObject);
    }
}