using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class DialogueItemPickerUI : MonoBehaviour
{
    public static DialogueItemPickerUI Instance { get; private set; }

    [Header("UI 元件")]
    [SerializeField] private GameObject panelRoot; // 面板根物件
    [SerializeField] private Transform gridContent; // Scroll View 的 Content
    [SerializeField] private Button itemButtonPrefab; // 按鈕預製件 (上面要有 TextMeshProUGUI)
    [SerializeField] private Button closeButton; // 關閉/取消按鈕
    [SerializeField] private DialogueTooltipController tooltipController; // 物品提示控制器

    private List<GameObject> spawnedButtons = new List<GameObject>();

    private void Awake()
    {
        Instance = this;
        panelRoot.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
    }

    /// <summary>
    /// 開啟面板，顯示背包物品
    /// </summary>
    public void Show()
    {
        panelRoot.SetActive(true);
        RefreshItems();
    }

    // 修改 ClosePanel 方法
    public void ClosePanel()
    {
        panelRoot.SetActive(false);
        
        // 【新增】強制關閉 Tooltip
        if (tooltipController != null)
        {
            tooltipController.HideTooltip();
        }

        DialogueManager.Instance.OnItemPicked(""); 
    }

    private void RefreshItems()
    {
        // 清除舊按鈕
        foreach (var btn in spawnedButtons) Destroy(btn);
        spawnedButtons.Clear();

        if (InventoryManager.Instance == null) return;

        // 獲取玩家擁有的物品
        List<ItemData> ownedItems = InventoryManager.Instance.GetOwnedItemsData();

        foreach (ItemData item in ownedItems)
        {
            Button btn = Instantiate(itemButtonPrefab, gridContent);
            
            // --- 設定 Icon ---
            // 1. 尋找名叫 "Icon" 的子物件
            Transform iconTrans = btn.transform.Find("Icon");
            if (iconTrans != null)
            {
                Image iconImg = iconTrans.GetComponent<Image>();
                if (iconImg != null)
                {
                    // 2. 設定圖片 (itemImage 是您 ItemData 裡的變數名稱)
                    iconImg.sprite = item.itemImage; 
                    
                    // 3. (選用) 保持圖片比例，避免拉伸變形
                    iconImg.preserveAspect = true; 
                }
            }
            else
            {
                Debug.LogWarning($"在 Prefab {itemButtonPrefab.name} 中找不到名為 'Icon' 的 Image 物件！");
            }

            // 設定點擊事件
            string itemID = item.itemID;
            btn.onClick.AddListener(() => OnItemClicked(itemID));

            // --- 【新增】設定懸停提示 ---
            // 1. 嘗試獲取按鈕上的 ItemHoverTrigger 元件
            ItemHoverTrigger trigger = btn.GetComponent<ItemHoverTrigger>();
            
            // 2. 如果 Prefab 上沒有掛，就動態加一個 (保險起見)
            if (trigger == null) trigger = btn.gameObject.AddComponent<ItemHoverTrigger>();

            // 3. 初始化數據 (傳入名稱、描述、控制器)
            trigger.Setup(item.itemName, item.description, tooltipController);
            // ---------------------------
            
            spawnedButtons.Add(btn.gameObject);
        }
        
    }

    // 修改 OnItemClicked 方法
    private void OnItemClicked(string itemID)
    {
        panelRoot.SetActive(false);

        // 【新增】強制關閉 Tooltip (因為按鈕消失了，Exit 事件不會觸發)
        if (tooltipController != null)
        {
            tooltipController.HideTooltip();
        }

        DialogueManager.Instance.OnItemPicked(itemID);
    }
}