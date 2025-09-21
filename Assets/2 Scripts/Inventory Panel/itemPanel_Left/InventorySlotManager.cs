using Spine;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotManager : MonoBehaviour
{
    [Header("功能：管理背包格子的更新與顯示，確保使用物品後格子自動遞補")]
    //private Queue<GameObject> itemSlotPool = new Queue<GameObject>();
    //private List<GameObject> allSlots = new List<GameObject>(); // 核心背包格子列表。暫時註解，已用 slotUIs 替代
    //private List<GameObject> activeSlots = new List<GameObject>(); // 當前有物品的格子。暫時註解，已用 slotUIs 過濾

    [Tooltip("背包格子容器 / 放置所有格子的父物件")]
    [SerializeField] private Transform itemsContainer;

    // 緩存每個格子的 InventorySlotUI，避免 GetComponent
    private List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();
    private int totalSlots => slotUIs.Count;

    //初始化格子
    public void Initialize(Transform container)
    {
        itemsContainer = container;
        //InitializeAllSlots();
        slotUIs.Clear();

        foreach (Transform child in itemsContainer)
        {
            InventorySlotUI slotUI = child.GetComponent<InventorySlotUI>();
            if (slotUI != null)
            {
                slotUI.ClearSlot(); // 初始清空
                slotUIs.Add(slotUI);
            }
        }
    }

    /// <summary>
    /// 更新背包格子顯示，使用物品或新增物品後呼叫
    /// </summary>
    /// <param name="items">當前背包物品列表</param>
    /// <param name="onClickAction">Slot 點擊回調</param>
    public void UpdateSlots(List<ItemData> items, System.Action<ItemData> onClickAction)
    {
        for (int i = 0; i < totalSlots; i++)
        {
            var slotUI = slotUIs[i];

            //if (i < items.Count && items[i] != null)
            //{
            //    // 有物品 → 顯示並綁定
            //    slotUI.SetItem(items[i], onClickAction);
            //}
            //else
            //{
            //    // 空格子 → 清空
            //    slotUI.ClearSlot();
            //}

            // 即使是 defaultItem 也要呼叫 SetItem 來保持按鈕可點
            ItemData item = i < items.Count ? items[i] : InventoryManager.Instance.defaultItem;
            slotUI.SetItem(item, onClickAction);
        }
    }

    /// <summary>
    /// 設置單個格子
    /// </summary>
    private void SetupSlot(InventorySlotUI slotUI, ItemData item, System.Action<ItemData> onClickAction)
    {
        if (slotUI == null) return;

        // 綁定資料
        slotUI.Bind(item);

        // 設定顯示與按鈕事件
        slotUI.SetItem(item, onClickAction);
    }

    /// <summary>
    /// 使用物品後，背包自動遞補
    /// </summary>
    /// <param name="usedIndex">被使用物品在背包列表的索引</param>
    /// <param name="items">背包當前物品列表</param>
    /// <param name="onClickAction">Slot 點擊回調</param>
    public void RemoveAndShift(int usedIndex, List<ItemData> items, System.Action<ItemData> onClickAction)
    {
        // 從列表移除已使用物品
        items.RemoveAt(usedIndex);

        // 重新刷新格子
        UpdateSlots(items, onClickAction);
    }

    /// <summary>
    /// 根據 ItemData 找到對應 Slot GameObject
    /// </summary>
    /// <param name="item">目標物品</param>
    /// <returns>對應 Slot GameObject 或 null</returns>
    public GameObject GetSlotGOByItem(ItemData item)
    {
        if (itemsContainer == null || item == null) return null;

        foreach (Transform child in itemsContainer)
        {
            var slotUI = child.GetComponent<InventorySlotUI>();
            if (slotUI != null && slotUI.BoundItem == item)
                return child.gameObject;
        }

        return null;
    }

    /// <summary>
    /// 取得第一個可選 Slot，用於手柄模式自動選中
    /// </summary>
    public InventorySlotUI GetFirstSlot()
    {
        return slotUIs.Find(slot => slot.BoundItem != null && slot.BoundItem != InventoryManager.Instance.defaultItem);
    }

    /// <summary>
    /// 取得格子對應的 InventorySlotUI
    /// </summary>
    public InventorySlotUI GetSlotUI(int index)
    {
        if (index >= 0 && index < slotUIs.Count)
            return slotUIs[index];
        return null;
    }

    /// <summary>
    /// 根據 ItemData 找到對應 SlotUI
    /// </summary>
    public InventorySlotUI GetSlotByItem(ItemData item)
    {
        if (item == null) return null;
        return slotUIs.Find(slot => slot.BoundItem == item);
    }

    // 用來明確取 slot_0(第一個格子)
    public InventorySlotUI GetSlotByIndex(int index)
    {
        if (index >= 0 && index < slotUIs.Count)
            return slotUIs[index];
        return null;
    }

}