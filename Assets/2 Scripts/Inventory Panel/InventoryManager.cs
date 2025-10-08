using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System; // 需要引用 System 才能使用 Action
using TMPro;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(-15)] //最早初始化此腳本
public class InventoryManager : MonoBehaviour
{
    // --- 單例模式 (Singleton) ---
    public static InventoryManager Instance { get; private set; }

    // 當背包內容改變時觸發的事件，UI 會訂閱這個事件來更新顯示
    public event Action OnInventoryChanged;

    [Header("功能：管理背包物件的增減，及初始化2個面板")]
    // 儲存所有物品資料的 List
    public List<ItemData> items = new List<ItemData>();

    [Header("物件面板UI設置")]
    public Image itemImage;
    public TMP_Text itemNameText;
    public TMP_Text itemDescriptionText;
    [Header("默認顯示物品 (請在 Inspector 指派一個 ItemData 資產)")]
    public ItemData defaultItem;

    [Header("UI管理")]
    [SerializeField][Tooltip("ItemDetailUI 腳本")] private ItemDetailUI _itemDetailUI; // 現在直接引用組件
    [SerializeField][Tooltip("InventoryUI 腳本")] private InventoryUI _inventoryUI; // 現在直接引用組件

    public ItemDetailUI ItemDetailUI => _itemDetailUI;
    //public InventoryUI InventoryUI => _inventoryUI;

    // 在 InventoryManager 裡，class 成員底下新增：
    public GameObject CurrentSelectedSlot => currentSelectedSlot;

    private GameObject currentSelectedSlot;

    #region ===== 初始化 =====
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 確保 _itemDetailUI 與 _inventoryUI 都有被指派，避免 null reference
        //InitializeInventoryUI();
        InitializeItemDetailUI();

        UpdateInformationPanel(null);
    }
    /// <summary>
    /// 初始化 InventoryUI 組件 10月9日註解
    /// </summary>
    //private void InitializeInventoryUI()
    //{
    //    if (_inventoryUI == null)
    //    {
    //        _inventoryUI = FindObjectOfType<InventoryUI>();
    //        if (_inventoryUI == null)
    //        {
    //            Debug.LogError("找不到 InventoryUI 組件！");
    //            return;
    //        }
    //    }
    //}

    /// <summary>
    /// 初始化 ItemDetailUI 組件
    /// </summary>
    private void InitializeItemDetailUI()
    {
        // 尝试从当前游戏对象获取组件
        _itemDetailUI = GetComponent<ItemDetailUI>();

        if (_itemDetailUI == null)
        {
            // 如果找不到，直接在当前游戏对象上添加组件
            _itemDetailUI = gameObject.AddComponent<ItemDetailUI>();
            Debug.LogWarning("自动添加 ItemDetailUI 组件到 InventoryManager，请配置 UI 元素");
        }

        // 确保 UI 默认关闭
        _itemDetailUI.enabled = false;
    }
    #endregion

    /// <summary>
    /// 新增物品到背包
    /// </summary>
    /// <param name="item">要新增的物品資料</param>
    public void AddItem(ItemData item)
    {
        if (item == null || items.Contains(item)) return;

        items.Add(item);
        OnInventoryChanged?.Invoke();

        // 如果玩家還沒選任何格子 → 自動更新第一格
        if (currentSelectedSlot == null && items.Count > 0)
        {
            var firstSlotGO = InventoryUI.Instance?.GetFirstSelectableSlot();
            if (firstSlotGO != null)
                SelectSlot(firstSlotGO, items[0]);
            else
                UpdateInformationPanel(items[0]); // fallback
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

    // 更新右側面板
    public void UpdateInformationPanel(ItemData item)
    {
        // 如果 item 為 null，就用 defaultItem 代替
        ItemData dataToShow = item ?? defaultItem;

        // 檢查 defaultItem 是否本身為 null，如果 defaultItem 沒設置會報錯
        if (dataToShow == null)
        {
            itemImage?.gameObject.SetActive(false);
            itemNameText.text = "";
            itemDescriptionText.text = "";
            return;
        }

        // 顯示物件圖片
        if (itemImage != null)
        {
            itemImage.sprite = dataToShow.itemImage; // 使用物件圖片 Sprite
            itemImage.enabled = dataToShow.itemImage != null;
        }

        // 顯示物件名稱
        if (itemNameText != null) itemNameText.text = dataToShow.itemName ?? "";

        // 顯示物件描述
        if (itemDescriptionText != null) itemDescriptionText.text = dataToShow.description ?? "";
    }

    // 玩家選擇格子 (滑鼠點擊或手柄選中)
    public void SelectSlot(GameObject slotGO, ItemData item)
    {
        // 如果已經選中同一個格子，不做任何操作，避免重複
        if (currentSelectedSlot == slotGO) return;

        currentSelectedSlot = slotGO;
        UpdateInformationPanel(item);

        // 只在 slotGO 不為 null 且不是已選中物件時，才更新 EventSystem
        if (slotGO != null && EventSystem.current.currentSelectedGameObject != slotGO)
            EventSystem.current.SetSelectedGameObject(slotGO);
    }

    // 玩家取消選擇格子 (手柄/鍵鼠回到第一格自動)
    public void ClearSelectedSlot()
    {
        currentSelectedSlot = null;
        if (items.Count > 0)
            UpdateInformationPanel(items[0]);
        else
            UpdateInformationPanel(null);

        EventSystem.current.SetSelectedGameObject(null);
    }

    public GameObject GetSlotGOByItem(ItemData item)
    {
        return InventoryUI.Instance?.slotManager.GetSlotGOByItem(item);
    }
}