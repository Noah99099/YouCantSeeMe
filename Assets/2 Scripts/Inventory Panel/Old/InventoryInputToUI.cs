using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryInputToUI : MonoBehaviour
{
    [Header("功能：透過偵測的輸入(Input Actions)，調用{InventoryUI 腳本}的方法")]
    [Header("Input Actions：背包模式")]
    [Tooltip("Inventory Action Map 的 Navigate")] public InputActionReference navigateAction;
    [Tooltip("Inventory Action Map 的 CloseInventory")] public InputActionReference closeInventoryAction;
    [Tooltip("Inventory Action Map 的 OpenModelPreview")] public InputActionReference openModelPreviewAction;
    [Tooltip("Inventory Action Map 的 UseItem")] public InputActionReference useItemAction;

    [Header("Input Actions：玩家模式")]
    [Tooltip("Player Action Map 的 OpenInventory")] public InputActionReference openInventoryAction;

    [Header("與 事件紀錄簿-物件 相關")]
    [SerializeField] private int columnsCount = 4; // 每行格子數，可在 Inspector 設定

    [Header("UI References")]
    public ItemDetailUI itemDetailUI;
    public InventoryUI inventoryUI;
    [SerializeField] private InventorySlotManager slotManager;

    private List<Button> selectableButtons = new List<Button>();
    private int currentSelectedIndex = -1;

    // 防抖設定
    private bool canToggleInventory = true;
    private float toggleCooldown = 0.15f;
    private bool canMoveSelection = true;


    #region ===== 初始化 =====
    private void OnEnable()
    {
        // 初始化時緩存可選擇的按鈕
        CacheSelectableButtons();

        // 確保所有引用不為空
        if (navigateAction == null || closeInventoryAction == null || openInventoryAction == null || openModelPreviewAction == null)
        {
            Debug.LogError("InventoryInputToUI: 有些 InputActionReference 未設置!");
            return;
        }

        // 啟用除 OpenInventory 外的背包相關動作
        navigateAction?.action.Enable(); //導航按鈕
        closeInventoryAction?.action.Enable(); //關背包
        //openModelPreviewAction?.action.Enable(); //開模型預覽
        useItemAction?.action.Enable(); //使用物品

        // 背包相關訂閱事件
        navigateAction.action.performed += OnNavigate;
        closeInventoryAction.action.performed += OnCloseInventory;
        //openModelPreviewAction.action.performed += OnOpenModelPreview;
        useItemAction.action.performed += OnUseItem;

        //新增
        if (openModelPreviewAction != null)
            openModelPreviewAction.action.performed += OnOpenModelPreview;

        // 手柄模式 → 打開背包時自動選中第一個格子
        if (InputDeviceManager.Instance != null &&
            InputDeviceManager.Instance.CurrentInputType == InputDeviceManager.InputType.Gamepad)
        {
            var firstSlot = slotManager?.GetFirstSlot();
            if (firstSlot != null && InventorySelection.Instance != null)
            {
                //InventorySelection.Instance.SetSelected(firstSlot.gameObject);
            }
        }
        else
        {
            // 鍵鼠模式 → 確保沒有強制選中
            InventorySelection.Instance?.ClearSelection();
        }
    }

    private void OnDisable()
    {
        // 確保所有引用不為空
        if (navigateAction == null || closeInventoryAction == null || openInventoryAction == null || openModelPreviewAction == null)
            return;

        //移除訂閱
        navigateAction.action.performed -= OnNavigate;
        closeInventoryAction.action.performed -= OnCloseInventory;
        //openModelPreviewAction.action.performed -= OnOpenModelPreview;
        useItemAction.action.performed -= OnUseItem;

        // 禁用所有動作
        //背包
        navigateAction?.action.Disable();
        closeInventoryAction?.action.Disable();
        //openModelPreviewAction?.action.Disable();
        useItemAction?.action.Disable();

        //新增
        if (openModelPreviewAction != null)
            openModelPreviewAction.action.performed -= OnOpenModelPreview;

        // === 修正：避免 NullReferenceException ===
        if (InventorySelection.Instance != null)
        {
            InventorySelection.Instance.ClearSelection();
        }
    }

    #endregion

    /// <summary>
    /// 導航 事件紀錄簿-物品：左側的40個格子按鈕
    /// </summary>
    /// <param name="context"></param>
    private void OnNavigate(InputAction.CallbackContext context)
    {
        Vector2 navigationInput = context.ReadValue<Vector2>();
        if (navigationInput == Vector2.zero) return;

        if (selectableButtons.Count == 0)
        {
            CacheSelectableButtons();
            if (selectableButtons.Count == 0) return;
        }

        if (navigationInput.y > 0) // 上
            MoveSelection(-1, true);
        else if (navigationInput.y < 0) // 下
            MoveSelection(1, true);
        else if (navigationInput.x < 0) // 左
            MoveSelection(-1, false);
        else if (navigationInput.x > 0) // 右
            MoveSelection(1, false);
    }

    #region ===== 玩家模式開關背包 =====
    public void BindOpenInventory(bool bind)
    {
        if (openInventoryAction == null) return;

        if (bind)
        {
            openInventoryAction.action.Enable();
            openInventoryAction.action.performed += OnOpenInventory;
            Debug.Log("[InventoryInputToUI] 綁定 OpenInventory");
        }
        else
        {
            openInventoryAction.action.performed -= OnOpenInventory;
            openInventoryAction.action.Disable();
            Debug.Log("[InventoryInputToUI] 解除綁定 OpenInventory");
        }
    }

    private void OnOpenInventory(InputAction.CallbackContext context)
    {
        if (!canToggleInventory) return;

        canToggleInventory = false;

        if (inventoryUI != null)
            inventoryUI.ToggleInventory();

        // 防抖
        Invoke(nameof(ResetToggleInventoryCooldown), toggleCooldown);
    }

    private void ResetToggleInventoryCooldown()
    {
        canToggleInventory = true;
    }

    private void OnCloseInventory(InputAction.CallbackContext context) //0924修改
    {
        Debug.Log($"[InventoryInputToUI] OnCloseInventory called - InventoryVisible: {inventoryUI?.isInventoryVisible}, PreviewActive: {itemDetailUI?.modelPreviewPanel?.activeSelf}");
        //if (itemDetailUI != null && itemDetailUI.modelPreviewPanel.activeSelf)
        //{
        //    itemDetailUI.HideItemDetail();
        //    return;
        //}
        //if (inventoryUI == null) return;

        //inventoryUI.CloseInventory();
        //UIInputManager.Instance?.EnterGameplayMode();

        // 理論上不該出現的情況1：預覽面板開啟時，關閉預覽面板
        if (itemDetailUI != null && itemDetailUI.modelPreviewPanel != null &&
            itemDetailUI.modelPreviewPanel.activeSelf)
        {
            itemDetailUI.ClosePreviewAndReturnToInventory();
            UIInputManager.Instance?.EnterInventoryMode(); // 確保回到背包模式
            return;
        }

        // 情況2：只有背包面板開啟時，關閉整個背包
        if (inventoryUI != null && inventoryUI.isInventoryVisible)
        {
            inventoryUI.CloseInventory();
            // 不要在這裡調用 EnterGameplayMode，讓InventoryUI腳本的CloseInventory 統一處理
        }
    }
    #endregion

    private void OnOpenModelPreview(InputAction.CallbackContext context) // 物品預覽面板
    {
        //if (inventoryUI == null || itemDetailUI == null) return;

        //if (!inventoryUI.isInventoryVisible) return; // <-- 只有當背包面板開啟時，手柄按鈕才有效

        //// 取得選中物品
        //ItemData item = inventoryUI.CurrentSelectedItem;
        //if (item == null) return;

        //itemDetailUI.ShowItemDetail(item);
        if (!InventoryUI.Instance.isInventoryVisible) return;

        var item = InventoryUI.Instance.CurrentSelectedItem;
        if (item == null) return;

        //var slotUI = slotManager.GetSlotByItem(item); //0924 這一句是讓預覽面板跳出的兇手
        //if (slotUI == null) return;

        InventoryManager.Instance.ItemDetailUI.ShowModelPreview(item);
        UIInputManager.Instance?.EnterModelPreviewMode();
    }

    private void OnUseItem(InputAction.CallbackContext context) // 使用背包物品
    {
        if (inventoryUI == null) return;
        ItemData item = inventoryUI.CurrentSelectedItem;
        if (item != null)
            PlayerInteraction.Instance?.OnItemUsed(item);
    }

    #region ===== UI 輔助方法 =====
    // 找出可選擇的按鈕，排除標籤 "NoSelect" 或導航模式為 None 的按鈕
    private void CacheSelectableButtons()
    {
        selectableButtons.Clear();
        if (inventoryUI == null || inventoryUI.inventoryPanel == null) return;

        Button[] buttons = inventoryUI.inventoryPanel.GetComponentsInChildren<Button>(true);

        foreach (var btn in buttons)
        {
            bool isNoSelectTag = btn.gameObject.CompareTag("NoSelect");
            bool navNone = btn.navigation.mode == Navigation.Mode.None;
            bool interactable = btn.interactable;

            if (!isNoSelectTag && !navNone && interactable)
            {
                selectableButtons.Add(btn);
            }
        }

        // 確保按鈕按照正確的順序排列（按照格子索引）
        selectableButtons.Sort((a, b) => {
            int indexA = GetSlotIndex(a.transform);
            int indexB = GetSlotIndex(b.transform);
            return indexA.CompareTo(indexB);
        });
    }
    //private IEnumerator SelectFirstButtonNextFrame()
    //{
    //    yield return null; // 等待一幀確保UI佈局完成
    //    SelectFirstValidButton();
    //}

    private int GetSlotIndex(Transform slotTransform)
    {
        // 從格子名稱中提取索引（例如 "Slot_0" -> 0）
        string name = slotTransform.name;
        if (name.StartsWith("Slot_"))
        {
            if (int.TryParse(name.Substring(5), out int index))
                return index;
        }

        // 如果無法從名稱中提取，使用 sibling index
        return slotTransform.GetSiblingIndex();
    }

    private void SelectFirstValidButtonNextFrame()
    {
        StartCoroutine(SelectFirstButtonCoroutine());
    }

    private IEnumerator SelectFirstButtonCoroutine()
    {
        yield return null; // 延遲一幀，避免 EventSystem 報錯

        if (selectableButtons.Count > 0)
        {
            currentSelectedIndex = 0;
            //InventorySelection.Instance.SetSelected(selectableButtons[currentSelectedIndex].gameObject);

            // 更新右側詳情
            var slotUI = selectableButtons[currentSelectedIndex].GetComponent<InventorySlotUI>();
            if (slotUI != null)
            {
                inventoryUI.SetCurrentSelectedItem(slotUI.BoundItem);
                inventoryUI.UpdateItemDetail(slotUI.BoundItem, true);
            }

            inventoryUI.EnsureSlotVisible(selectableButtons[currentSelectedIndex].transform);
        }
        else
        {
            currentSelectedIndex = -1;
            InventorySelection.Instance.ClearSelection();
        }
    }

    //private void SelectFirstValidButton()
    //{
    //    if (selectableButtons.Count > 0)
    //    {
    //        currentSelectedIndex = 0;

    //        // 改用 UISelectionManager
    //        InventorySelection.Instance.SetSelected(selectableButtons[currentSelectedIndex].gameObject);
    //    }
    //    else
    //    {
    //        currentSelectedIndex = -1;
    //        InventorySelection.Instance.ClearSelection();
    //    }
    //}

    // 移動選擇焦點，跳過不可選按鈕
    private void MoveSelection(int direction, bool isVertical)
    {
        if (selectableButtons.Count == 0) return;

        int newIndex = currentSelectedIndex;

        if (isVertical)
            newIndex += direction * columnsCount; // 上下跳一行
        else
            newIndex += direction; // 左右跳一格

        // 邊界限制
        newIndex = Mathf.Clamp(newIndex, 0, selectableButtons.Count - 1);

        // 跳過不可互動格子
        while (!selectableButtons[newIndex].interactable)
        {
            newIndex += isVertical ? direction * columnsCount : direction;
            if (newIndex < 0 || newIndex >= selectableButtons.Count)
            {
                newIndex = currentSelectedIndex;
                break;
            }
        }

        StartCoroutine(SelectSlotNextFrame(newIndex));
    }

    // 添加獲取當前選中索引的方法
    private IEnumerator SelectSlotNextFrame(int index)
    {
        yield return null; // 延遲一幀

        currentSelectedIndex = index;
        var slotGO = selectableButtons[currentSelectedIndex].gameObject;

        //InventorySelection.Instance.SetSelected(slotGO);

        var slotUI = slotGO.GetComponent<InventorySlotUI>();
        if (slotUI != null)
        {
            inventoryUI.SetCurrentSelectedItem(slotUI.BoundItem);
            inventoryUI.UpdateItemDetail(slotUI.BoundItem, true);
        }

        inventoryUI.EnsureSlotVisible(slotGO.transform);
    }
    #endregion
}

