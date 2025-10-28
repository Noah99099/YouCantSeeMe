// VoiceItemManager.cs
using System.Collections.Generic;
using UnityEngine;
using System; // 需要引用 System 才能使用 Action
using System.Linq; // 為了 .Exists() 和 .Any()

public class VoiceItemManager : MonoBehaviour
{
    // --- 單例模式 (Singleton) ---
    public static VoiceItemManager Instance { get; private set; }

    // 當聲音內容改變時觸發的事件，UI 會訂閱這個事件來更新顯示
    public event Action OnVoiceChanged;

    [Header("功能：管理聲音物件（只增不減）")]
    // 儲存所有物品資料的 List
    public List<VoiceItemData> items = new List<VoiceItemData>();

    [Header("默認顯示物品 (請在 Inspector 指派一個 VoiceItemData 資產)")]
    // 這是配置數據 (Configuration Data)，不是 UI 狀態，所以保留
    public VoiceItemData defaultVoiceItem;

    // ----- [新需求] 追蹤已使用的物品 -----
    // 使用 HashSet 效率更高 (O(1) 查詢)
    private HashSet<VoiceItemData> usedVoiceItems = new HashSet<VoiceItemData>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 新增物品到聲音面板
    /// </summary>
    /// <param name="item">要新增的物品資料</param>
    public void AddItem(VoiceItemData voiceItem)
    {
        if (voiceItem == null || items.Contains(voiceItem)) return;

        items.Add(voiceItem);
        OnVoiceChanged?.Invoke(); // 只通知，不執行任何 UI 操作
    }

    // ----- [新需求] 標記與檢查使用狀態 -----
    /// <summary>
    /// [新] 標記一個聲音物品為「已使用」
    /// </summary>
    public void MarkItemAsUsed(VoiceItemData item)
    {
        if (item == null || usedVoiceItems.Contains(item)) return;

        Debug.Log($"[VoiceItemManager] 將 {item.itemName} 標記為已使用。");
        usedVoiceItems.Add(item);

        // 觸發事件，強制 UI (VoicePanelUIController) 刷新
        OnVoiceChanged?.Invoke();
    }

    /// <summary>
    /// [新] 檢查一個聲音物品是否已被使用
    /// </summary>
    public bool IsItemUsed(VoiceItemData item)
    {
        if (item == null) return false;
        return usedVoiceItems.Contains(item);
    }

    /// <summary>
    /// [新] (可選) 在遊戲讀檔或重置時，清除使用狀態
    /// </summary>
    public void ResetUsedItems()
    {
        usedVoiceItems.Clear();
        OnVoiceChanged?.Invoke();
    }
}
