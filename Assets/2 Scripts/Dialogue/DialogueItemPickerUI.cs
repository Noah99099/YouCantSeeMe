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

    public void ClosePanel()
    {
        panelRoot.SetActive(false);
        // 如果玩家取消選擇，我們可以傳回 null 或空字串
        // 這裡假設取消選擇會繼續對話，但視為 "沒有選任何東西"
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
            
            spawnedButtons.Add(btn.gameObject);
        }
    }

    private void OnItemClicked(string itemID)
    {
        // 1. 隱藏面板
        panelRoot.SetActive(false);
        // 2. 通知 DialogueManager
        DialogueManager.Instance.OnItemPicked(itemID);
    }
}