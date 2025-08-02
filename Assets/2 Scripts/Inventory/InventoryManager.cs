using System.Collections.Generic;
using UnityEngine;
using System; // 需要引用 System 才能使用 Action

public class InventoryManager : MonoBehaviour
{
    // --- 單例模式 (Singleton) ---
    public static InventoryManager Instance { get; private set; }

    // 當背包內容改變時觸發的事件，UI 會訂閱這個事件來更新顯示
    public event Action OnInventoryChanged;

    // 儲存所有物品資料的 List
    public List<ItemData> items = new List<ItemData>();

    private void Awake()
    {
        // 如果場景中已經有一個 InventoryManager，就摧毀自己，確保永遠只有一個存在
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // 標記這個物件在載入場景時不要被銷毀
            DontDestroyOnLoad(gameObject);
        }
    }

    /// <summary>
    /// 新增物品到背包
    /// </summary>
    /// <param name="item">要新增的物品資料</param>
    public void AddItem(ItemData item)
    {
        if (item != null)
        {
            items.Add(item);
            Debug.Log($"已將 {item.itemName} 加入背包！");

            // 觸發事件，通知所有訂閱者 (例如 UI) 背包已更新
            OnInventoryChanged?.Invoke();
        }
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
            Debug.Log($"已將 {item.itemName} 從背包移除！");

            // 同樣觸發更新事件
            OnInventoryChanged?.Invoke();
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