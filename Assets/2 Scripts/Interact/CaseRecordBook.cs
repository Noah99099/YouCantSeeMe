// 檔案名稱: CaseRecordBook.cs
using System;
using UnityEngine;

/// <summary>
/// 一個標記組件，用於識別案件紀錄簿這個特殊的交互物件。
/// </summary>
public class CaseRecordBook : MonoBehaviour
{
    // 您可以在這裡添加物品的名稱或描述，以便在UI提示中使用
    public string itemName = "案件紀錄簿";

    // 當此物件被拾取時的事件（不再用單例）
    public static event Action OnCollected;

    /// <summary>
    /// 呼叫此方法以觸發拾取事件。
    /// </summary>
    public void Collect()
    {
        Debug.Log($"CaseRecordBook: {itemName} 已被拾取！");
        OnCollected?.Invoke();
    }
}