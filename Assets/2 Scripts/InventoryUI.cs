using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("UI 元件")]
    public GameObject inventoryPanel; // 整個背包 UI 的面板
    public Transform itemsContainer;  // 用來放置所有物品格子的容器
    public GameObject itemSlotPrefab; // 單一物品格子的 Prefab
    [SerializeField] private ItemDetailUI itemDetailUI; // 在 Inspector 指定

    void Start()
    {
        // 訂閱 InventoryManager 的事件。當事件觸發時，呼叫 UpdateUI 方法
        InventoryManager.Instance.OnInventoryChanged += UpdateUI;

        inventoryPanel.SetActive(false); // 遊戲開始時預設關閉背包

        if (itemDetailUI == null)
        {
            itemDetailUI = FindObjectOfType<ItemDetailUI>();
            Debug.LogWarning("用程式動態綁定 ItemDetailUI");
        }

        InventoryManager.Instance.OnInventoryChanged += UpdateUI;
        inventoryPanel.SetActive(false);
    }

    void OnDestroy()
    {
        // 當物件被摧毀時，取消訂閱，避免記憶體洩漏
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= UpdateUI;
        }
    }

    void Update()
    {
        // 按下 B 鍵來開關背包
        if (Input.GetKeyDown(KeyCode.B))
        {
            ToggleInventory();
        }
    }

    /// <summary>
    /// 開關背包 UI
    /// </summary>
    public void ToggleInventory()
    {
        bool isActive = !inventoryPanel.activeSelf;
        inventoryPanel.SetActive(isActive);

        // --- 這裡就是關鍵 ---
        if (isActive)
        {
            // 如果是打開背包，就進入 UI 模式
            CursorManager.EnterUIMode();
            UpdateUI(); // 更新 UI 顯示
        }
        else
        {
            // 如果是關閉背包，就回到遊戲模式
            CursorManager.EnterGameplayMode();
        }
    }

    /// <summary>
    /// 更新背包 UI 顯示
    /// </summary>
    private void UpdateUI()
    {
        // 1. 清空所有舊的格子
        foreach (Transform child in itemsContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. 根據 InventoryManager 中的物品列表，重新生成所有格子
        foreach (ItemData item in InventoryManager.Instance.items)
        {
            // 實例化一個物品格子 Prefab
            GameObject slotInstance = Instantiate(itemSlotPrefab, itemsContainer);

            // 找到格子中的 Image 元件來設定圖示
            // 假設 Prefab 中代表圖示的 Image 元件被命名為 "ItemIcon"
            Image itemIcon = slotInstance.transform.Find("ItemIcon")?.GetComponent<Image>();

            if (itemIcon != null)
            {
                itemIcon.sprite = item.icon;
                itemIcon.enabled = true;
            }
            //讓格子支援滑鼠點擊
            Button button = slotInstance.GetComponent<Button>();
            if (button != null)
            {
                ItemData capturedItem = item; // 避免閉包錯誤
                button.onClick.AddListener(() => ShowItemDetail(capturedItem));
                Debug.Log($"[綁定點擊] {capturedItem.itemName}");
            }
        }
    }
    //顯示物件細節
        private void ShowItemDetail(ItemData item)
    {
        if (itemDetailUI != null)
        {
            itemDetailUI.ShowItemDetail(item);
        }
        else
        {
            Debug.LogWarning("ItemDetailUI 尚未指定！");
        }
    }
}