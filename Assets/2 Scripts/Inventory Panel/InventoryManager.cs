// InventoryManager.cs
using System.Collections.Generic;
using UnityEngine;
using System; // 需要引用 System 才能使用 Action
using System.Linq; // 為了 .Exists() 和 .Any()

[DefaultExecutionOrder(-15)] //最早初始化此腳本
public class InventoryManager : MonoBehaviour
{
    // --- 單例模式 (Singleton) ---
    public static InventoryManager Instance { get; private set; }

    // 當背包內容改變時觸發的事件，UI 會訂閱這個事件來更新顯示
    public event Action OnInventoryChanged;

    [Header("功能：管理背包物件的增減")]
    // 儲存所有物品資料的 List
    public List<ItemData> items = new List<ItemData>();

    [Header("默認顯示物品 (請在 Inspector 指派一個 ItemData 資產)")]
    // 這是配置數據 (Configuration Data)，不是 UI 狀態，所以保留
    public ItemData defaultItem;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 新增物品到背包
    /// </summary>
    /// <param name="item">要新增的物品資料</param>
    public void AddItem(ItemData item)
    {
        if (item == null || items.Contains(item)) return;

        items.Add(item);
        OnInventoryChanged?.Invoke(); // 只通知，不執行任何 UI 操作
    }

    /// <summary>
    /// 從背包移除物品 (可選，未來可能會用到)
    /// </summary>
    /// <param name="item">要移除的物品資料</param>
    public void RemoveItem(ItemData item)
    {
        if (items.Contains(item))
        {
            items.Remove(item);
            OnInventoryChanged?.Invoke(); // 通知UI更新
            Debug.Log($"[InventoryManager] 已移除物品: {item.itemName}");
        }
        else
        {
            Debug.LogWarning($"[InventoryManager] 嘗試移除不存在的物品: {item.itemName}");
        }
    }

    /// <summary>
    /// 檢查背包中是否含有指定名稱的物品
    /// </summary>
    /// <param name="itemNameToCheck">要檢查的物品名稱</param>
    /// <returns>如果找到返回 true，否則返回 false</returns>
    public bool HasItem(string itemNameToCheck)
    {
        // 使用 System.Linq 的 Any 方法，可以很有效率地檢查 List 中是否有符合條件的項目
        // 這行程式碼的意思是：「在 items 這個 List 中，是否有任何一個 item 的 itemName 等於我們要檢查的名稱？」
        return items.Exists(item => item.itemName == itemNameToCheck);
    }
}