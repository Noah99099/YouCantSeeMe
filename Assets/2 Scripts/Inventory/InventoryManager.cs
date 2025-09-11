using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System; // 需要引用 System 才能使用 Action
using UnityEngine.UIElements;
using TMPro;

[DefaultExecutionOrder(-15)] //更早初始化此腳本
public class InventoryManager : MonoBehaviour
{
    // --- 單例模式 (Singleton) ---
    public static InventoryManager Instance { get; private set; }

    // 當背包內容改變時觸發的事件，UI 會訂閱這個事件來更新顯示
    public event Action OnInventoryChanged;

    // 儲存所有物品資料的 List
    public List<ItemData> items = new List<ItemData>();

    [Header("物件面板UI設置")]
    public UnityEngine.UI.Image itemImage;
    public TMP_Text itemNameText;
    public TMP_Text itemDescriptionText;
    [Header("默認顯示物品 (請在 Inspector 指派一個 ItemData 資產)")]
    public ItemData defaultItem;

    [Header("UI管理")]
    [SerializeField][Tooltip("ItemDetailUI 腳本")] private ItemDetailUI _itemDetailUI; // 現在直接引用組件
    [SerializeField][Tooltip("InventoryUI 腳本")] private InventoryUI _inventoryUI; // 現在直接引用組件

    public ItemDetailUI ItemDetailUI => _itemDetailUI;
    public InventoryUI InventoryUI => _inventoryUI;


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

            // +++ 修改：直接獲取組件 +++
            InitializeItemDetailUI();
            InitializeInventoryUI();
        }
    }
    /// <summary>
    /// 初始化 InventoryUI 組件
    /// </summary>
    private void InitializeInventoryUI()
    {
        if (_inventoryUI == null)
        {
            _inventoryUI = FindObjectOfType<InventoryUI>();
            Debug.LogWarning("自动添加 InventoryUI 组件到 InventoryManager，请配置 UI 元素");
            if (_inventoryUI == null) 
            {
                Debug.LogError("找不到 InventoryUI 組件！");
                return;
            }
        }

        // 關閉背包面板
        if (_inventoryUI.inventoryPanel != null)
        {
            _inventoryUI.inventoryPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("InventoryUI 的 inventoryPanel 未設定！");
        }
    }

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

    /// <summary>
    /// 新增物品到背包
    /// </summary>
    /// <param name="item">要新增的物品資料</param>
    public void AddItem(ItemData item)
    {
        if (item != null && !items.Contains(item)) //新增  && !items.Contains(item)
        {
            items.Add(item);
            Debug.Log($"已將 {item.itemName} 加入背包！");
            OnInventoryChanged?.Invoke(); // 觸發事件，通知所有訂閱者 (例如 UI) 背包已更新
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

    public void UpdateDetailPanel(ItemData item)
    {
        // 如果 item 為 null，就用 defaultItem 代替
        ItemData dataToShow = item ?? defaultItem;

        if (dataToShow == null)
        {
            // 若連 defaultItem 都沒設，就清空
            if (itemImage != null) { itemImage.sprite = null; itemImage.enabled = false; }
            if (itemNameText != null) itemNameText.text = "";
            if (itemDescriptionText != null) itemDescriptionText.text = "";
            return;
        }

        if (itemImage != null)
        {
            itemImage.sprite = dataToShow.icon;
            itemImage.enabled = dataToShow.icon != null;
        }

        if (itemNameText != null) itemNameText.text = dataToShow.itemName ?? "";
        if (itemDescriptionText != null) itemDescriptionText.text = dataToShow.description ?? "";
    }
}