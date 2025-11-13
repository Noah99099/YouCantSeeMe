// InventoryManager.cs (升級版)
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

[DefaultExecutionOrder(-15)]
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }
    public event Action OnInventoryChanged;

    [Header("功能：管理背包物件的增減")]
    // 【修改】使用 Dictionary 來追蹤 物品ID 和 對應的數量
    private Dictionary<string, int> itemQuantities = new Dictionary<string, int>();

    // 【新增】一個列表，用於儲存 "獲得過的" 物品的 ItemData 實例
    // 這樣 UI 才能知道要顯示什麼圖標和名稱
    private Dictionary<string, ItemData> itemDatabase = new Dictionary<string, ItemData>();

    [Header("默認顯示物品")]
    public ItemData defaultItem;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 【修改】新增物品到背包 (現在會處理數量)
    /// </summary>
    public void AddItem(ItemData item)
    {
        if (item == null) return;
        string itemID = item.itemID; // 假設您的 ItemData 中有 itemID

        // 1. 如果這是第一次獲得，先存入 "資料庫"
        if (!itemDatabase.ContainsKey(itemID))
        {
            itemDatabase[itemID] = item;
        }

        // 2. 增加數量
        if (itemQuantities.ContainsKey(itemID))
        {
            itemQuantities[itemID]++; // 數量+1
        }
        else
        {
            itemQuantities[itemID] = 1; // 第一次獲得，數量設為1
        }
        
        Debug.Log($"[InventoryManager] 已新增: {item.itemName} (ID: {itemID})。目前總數: {itemQuantities[itemID]}");
        OnInventoryChanged?.Invoke(); // 通知UI更新

        if (item.isClueItem)
        {
            ClueCombinationManager.Instance?.CheckForNewPuzzleUnlocks();
        }
    }

    /// <summary>
    /// 【修改】從背包移除物品 (現在會處理數量)
    /// </summary>
    public void RemoveItem(ItemData item)
    {
        if (item == null) return;
        string itemID = item.itemID;

        if (itemQuantities.ContainsKey(itemID) && itemQuantities[itemID] > 0)
        {
            itemQuantities[itemID]--; // 數量-1
            Debug.Log($"[InventoryManager] 已移除: {item.itemName} (ID: {itemID})。剩餘數量: {itemQuantities[itemID]}");

            // (可選) 如果數量歸零了，您甚至可以從字典中移除它
            // if (itemQuantities[itemID] == 0)
            // {
            //     itemQuantities.Remove(itemID);
            // }

            OnInventoryChanged?.Invoke();
        }
        else
        {
            Debug.LogWarning($"[InventoryManager] 嘗試移除不存在或數量為0的物品: {item.itemName}");
        }
    }

    /// <summary>
    /// 【修改】檢查背包中是否 "至少有1個" 指定 ID 的物品
    /// </summary>
    public bool HasItem(string itemIDToCheck)
    {
        // 檢查字典中是否有這個ID，並且其數量 > 0
        return itemQuantities.ContainsKey(itemIDToCheck) && itemQuantities[itemIDToCheck] > 0;
    }

    /// <summary>
    /// 【新功能】獲取指定物品 ID 的確切數量
    /// </summary>
    public int GetItemCount(string itemIDToCheck)
    {
        if (itemQuantities.ContainsKey(itemIDToCheck))
        {
            return itemQuantities[itemIDToCheck];
        }
        return 0; // 如果背包中沒有這個物品，返回 0
    }

    /// <summary>
    /// 【新功能】(可選，供UI使用) 獲取所有已獲得物品的 ItemData 列表
    /// </summary>
    public List<ItemData> GetOwnedItemsData()
    {
        // 返回所有 "當前數量 > 0" 的物品的 ItemData 實例
        return itemDatabase
            .Where(pair => itemQuantities.ContainsKey(pair.Key) && itemQuantities[pair.Key] > 0)
            .Select(pair => pair.Value)
            .ToList();
    }
}