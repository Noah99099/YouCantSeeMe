// 檔案名稱: BookRequirementActivator.cs
using UnityEngine;

/// <summary>
/// 監聽 CaseRecordBook.OnCollected 事件。
/// 當事件觸發時 (玩家拾取案件紀錄簿)，
/// 才會啟用指定的目標遊戲物件 (targetObjects)。
/// </summary>
public class BookRequirementActivator : MonoBehaviour
{
    [Header("需要案件紀錄簿才能啟用的物件")]
    [Tooltip("將那些需要先拿到紀錄簿才能交互的物件拖到這裡")]
    [SerializeField]
    private GameObject[] targetObjects;

    void Start()
    {
        // 1. 遊戲開始時，預設禁用所有目標物件
        SetObjectsActive(false);

        // 2. 訂閱“獲得案件紀錄簿”事件
        CaseRecordBook.OnCollected += EnableTargetObjects;
    }

    private void OnDestroy()
    {
        // 3. 在此腳本物件銷毀時，取消訂閱，防止記憶體洩漏
        CaseRecordBook.OnCollected -= EnableTargetObjects;
    }

    /// <summary>
    /// 當 CaseRecordBook.OnCollected 事件被觸發時，此方法會被呼叫。
    /// </summary>
    private void EnableTargetObjects()
    {
        Debug.Log("[BookRequirementActivator] 收到通知：已獲得案件紀錄簿。正在啟用特殊物件...");

        // 4. 啟用所有目標物件
        SetObjectsActive(true);

        // 5. 事件只會觸發一次，立即取消訂閱
        // (這一步也確保了 Level1UIController 中的取消訂閱不會互相影響，
        // 每個監聽者都應該管理好自己的訂閱狀態)
        CaseRecordBook.OnCollected -= EnableTargetObjects;
    }

    /// <summary>
    /// 輔助方法：統一設置所有目標物件的啟用狀態
    /// </summary>
    /// <param name="isActive">是否啟用</param>
    private void SetObjectsActive(bool isActive)
    {
        if (targetObjects == null || targetObjects.Length == 0)
        {
            Debug.LogWarning($"[BookRequirementActivator] 在 {this.name} 上沒有指定任何 targetObjects。");
            return;
        }

        foreach (GameObject obj in targetObjects)
        {
            if (obj != null)
            {
                obj.SetActive(isActive);
            }
            else
            {
                Debug.LogWarning($"[BookRequirementActivator] targetObjects 列表存在 null 的項目。");
            }
        }
    }
}