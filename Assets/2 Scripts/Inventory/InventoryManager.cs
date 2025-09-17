using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System; // 需要引用 System 才能使用 Action
using TMPro;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(-15)] //更早初始化此腳本
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
    public InventoryUI InventoryUI => _inventoryUI;

    private GameObject currentSelectedSlot;

    private void Awake()
    {
        // 如果場景中已經有一個 InventoryManager，就摧毀自己，確保永遠只有一個存在
        //if (Instance != null && Instance != this)
        //{
        //    Destroy(gameObject);
        //}
        //else
        //{
        //    Instance = this;
        //    // 標記這個物件在載入場景時不要被銷毀
        //    DontDestroyOnLoad(gameObject);

        //    // +++ 修改：直接獲取組件 +++
        //    InitializeItemDetailUI();
        //    InitializeInventoryUI();

        //    // === 新增：初始化時就顯示 defaultItem ===
        //    UpdateInformationPanel(null);
        //}
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        UpdateInformationPanel(null);
    }
    /// <summary>
    /// 初始化 InventoryUI 組件
    /// </summary>
    private void InitializeInventoryUI()
    {
        if (_inventoryUI == null)
        {
            _inventoryUI = FindObjectOfType<InventoryUI>();
            if (_inventoryUI == null)
            {
                Debug.LogError("找不到 InventoryUI 組件！");
                return;
            }
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
        //if (item != null && !items.Contains(item)) //新增  && !items.Contains(item)
        //{
        //    items.Add(item);
        //    Debug.Log($"已將 {item.itemName} 加入背包！");
        //    OnInventoryChanged?.Invoke(); // 觸發事件，通知所有訂閱者 (例如 UI) 背包已更新
        //}
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
        if (item == null || !items.Contains(item)) return;

        items.Remove(item);
        OnInventoryChanged?.Invoke();

        // 更新面板
        if (currentSelectedSlot == null)
        {
            UpdateInformationPanel(items.Count > 0 ? items[0] : null);
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

        //if (dataToShow == null)
        //{
        //    // 若連 defaultItem 都沒設，就清空 UI
        //    if (itemImage != null) { itemImage.sprite = null; itemImage.enabled = false; }
        //    if (itemNameText != null) itemNameText.text = "";
        //    if (itemDescriptionText != null) itemDescriptionText.text = "";
        //    return;
        //}
        if (itemImage != null)
        {
            itemImage.sprite = dataToShow?.itemImage;
            itemImage.enabled = dataToShow?.itemImage != null;
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
    // 將3D物件與覽的名稱一律改成［ModelPreview］，因為叫［DetailPanel］太容易出錯了
}