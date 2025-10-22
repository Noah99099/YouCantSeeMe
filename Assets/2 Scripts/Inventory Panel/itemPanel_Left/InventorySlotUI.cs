// 檔案名稱: InventorySlotUI.cs
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using System.Collections;

/// <summary>
/// 負責單一背包格子的 UI 行為：
/// - 綁定資料
/// - 處理選中（手柄）
/// - 處理點擊（滑鼠）
/// </summary>

// 必須掛載在有 Button 的物件上
[RequireComponent(typeof(Button))]
public class InventorySlotUI : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
{
    [Header("UI 引用 (在 Prefab 中設定)")]
    public Image iconImage; // 格子中的物品圖示
    public Button buttonComponent;
    //[SerializeField] private GameObject selectedIndicator; // (可選) 一個用於手把選中的外框

    //public Button ButtonComponent { get; private set; }
    public ItemData CurrentItemData { get; private set; }

    // 回調：當此格子被選中時，通知總管 (InventoryPanelUIController)
    private Action<InventorySlotUI> _onSlotSelectedCallback;

    private void Awake()
    {
        // 若未手動指定，嘗試自動取得（保險機制）
        if (buttonComponent == null)
            buttonComponent = GetComponent<Button>();

        if (buttonComponent == null)
        {
            Debug.LogError($"[InventorySlotUI] 錯誤：在 '{gameObject.name}' 上的 Button 是 null！請在 Inspector 手動指定。", this.gameObject);
            return;
        }
        buttonComponent.onClick.AddListener(OnSlotClicked);
        //ButtonComponent = GetComponent<Button>();
        //ButtonComponent.onClick.AddListener(OnSlotClicked);
        //if (selectedIndicator != null)
        //    selectedIndicator.SetActive(false);
    }

    /// <summary>
    /// 由總管呼叫，用來設定這個格子的內容
    /// </summary>
    public void Setup(ItemData data, ItemData defaultItem, Action<InventorySlotUI> onSelectCallback)
    {
        _onSlotSelectedCallback = onSelectCallback;

        // 決定要顯示的資料 (如果 data 為 null，就使用 defaultItem)
        ItemData dataToShow = data ?? defaultItem;
        CurrentItemData = dataToShow;

        // ----- 防彈檢查 -----
        if (iconImage == null)
        {
            Debug.LogError($"[InventorySlotUI] 錯誤：在 '{gameObject.name}' 上的 iconImage 欄位是 null！請檢查 Prefab 和場景中的實例。", this.gameObject);
            if (buttonComponent != null) buttonComponent.interactable = false;
            return; // 直接返回，防止崩潰
        }

        if (buttonComponent == null)
        {
            Debug.LogError($"[InventorySlotUI] 錯誤：在 '{gameObject.name}' 上的 ButtonComponent 是 null！", this.gameObject);
            return; // 直接返回，防止崩潰
        }
        // ----- 檢查結束 -----

        if (dataToShow != null)
        {
            iconImage.sprite = dataToShow.icon;
            iconImage.enabled = (dataToShow.icon != null);
            buttonComponent.interactable = true; // 讓格子可以被點擊/選中
        }
        else
        {
            // 如果連 defaultItem 都沒有，就徹底禁用
            iconImage.sprite = null; // 1022新加
            iconImage.enabled = false;
            buttonComponent.interactable = true; // 即使是空格子，也應該可以被選中（根據您的需求）
        }
    }

    /// <summary>
    /// 鍵鼠：當格子被「點擊」時
    /// </summary>
    private void OnSlotClicked()
    {
        _onSlotSelectedCallback?.Invoke(this);
    }

    /// <summary>
    /// 手把：當格子被「導航選中」時
    /// </summary>
    public void OnSelect(BaseEventData eventData)
    {
        _onSlotSelectedCallback?.Invoke(this);
        //if (selectedIndicator != null)
        //    selectedIndicator.SetActive(true);
    }

    /// <summary>
    /// 手把：當格子「失去選中」時
    /// </summary>
    public void OnDeselect(BaseEventData eventData)
    {
        //if (selectedIndicator != null)
        //    selectedIndicator.SetActive(false);
    }

    /// <summary>
    /// 鍵鼠：當滑鼠「懸停」在格子上時 (實現懸停預覽)
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 只有在鍵鼠模式下，懸停才觸發選中
        if (InputDeviceManager.Instance != null &&
            InputDeviceManager.Instance.CurrentInputType == InputDeviceManager.InputType.KeyboardMouse)
        {
            // ***** 修改 *****
            // 移除 _onSlotSelectedCallback?.Invoke(this);
            // 我們不再希望懸停時觸發右側面板更新。

            // (可選) 我們仍然可以讓滑鼠懸停時選中該按鈕，這樣鍵盤就可以接管
            // 但這可能會導致手把和鍵鼠的焦點衝突
            // 為了實現您「僅點擊」的需求，最好的方式是讓這個方法保持空白

            // 為了實現 "懸停時按鈕高亮"，我們可以使用 Button 內建的高亮功能
            // 如果您需要 "懸停時選中" (以便鍵盤可以接管)，請取消註解下面這行
            // buttonComponent.Select();
        }
    }
}
