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
    [Tooltip("Inventory Action Map 的 OpenItemDetail")] public InputActionReference openItemDetailAction;

    [Header("Input Actions：玩家模式")]
    [Tooltip("Player Action Map 的 OpenInventory")] public InputActionReference openInventoryAction;

    [Header("與 事件紀錄簿-物件 相關")]
    [SerializeField] private int columnsCount = 4; // 每行格子數，可在 Inspector 設定

    [Header("UI References")]
    public ItemDetailUI itemDetailUI;
    public InventoryUI inventoryUI;

    private List<Button> selectableButtons = new List<Button>();
    private int currentSelectedIndex = -1;

    // 防抖設定
    private bool canToggleInventory = true;
    private float toggleCooldown = 0.15f;

    //處理第一次開背包的瞬間 ToggleInventory 被呼叫兩次，導致 UI 被立即關閉


    #region ===== 初始化 =====
    private void Start()
    {
        // 初始化時緩存可選擇的按鈕
        CacheSelectableButtons();
    }

    private void OnEnable()
    {
        // 確保所有引用不為空
        if (navigateAction == null || closeInventoryAction == null || openInventoryAction == null || openItemDetailAction == null)
        {
            Debug.LogError("InventoryInputToUI: 有些 InputActionReference 未設置!");
            return;
        }

        // 背包相關動作始終啟用
        navigateAction?.action.Enable(); //導航按鈕
        closeInventoryAction?.action.Enable(); //關背包
        openItemDetailAction?.action.Enable(); //開模型預覽

        // 背包相關訂閱事件
        navigateAction.action.performed += OnNavigate;
        closeInventoryAction.action.performed += OnCloseInventory;
        openItemDetailAction.action.performed += OnCloseInventory;         
    }

    private void OnDisable()
    {
        // 確保所有引用不為空
        if (navigateAction == null || closeInventoryAction == null || openInventoryAction == null || openItemDetailAction == null)
            return;

        //背包
        navigateAction.action.performed -= OnNavigate;
        closeInventoryAction.action.performed -= OnCloseInventory;
        openItemDetailAction.action.performed -= OnCloseInventory;

        // 禁用所有動作
        //背包
        navigateAction?.action.Disable();
        closeInventoryAction?.action.Disable();
        openItemDetailAction?.action.Disable();    
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

    private void OnSelectSlot(InputAction.CallbackContext context)
    {
        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;
        if (currentSelected == null) return;

        Button btn = currentSelected.GetComponent<Button>();
        if (btn == null || !btn.interactable) return;

        // 正確呼叫 Button 的 OnSubmit，必須帶 BaseEventData 參數
        BaseEventData eventData = new BaseEventData(EventSystem.current);
        currentSelected.SendMessage("OnSubmit", eventData, SendMessageOptions.DontRequireReceiver);
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

        StartCoroutine(ToggleInventoryWithCooldown());
    }

    private IEnumerator ToggleInventoryWithCooldown()
    {
        canToggleInventory = false;

        if (inventoryUI == null) yield break;

        // 切換到 Inventory 模式
        UIInputManager.Instance?.EnterInventoryMode();

        // 打開背包 UI
        inventoryUI.ToggleInventory();

        // 緩存按鈕並選第一個
        CacheSelectableButtons();
        if (selectableButtons.Count > 0) yield return SelectFirstButtonNextFrame();

        // 等待 cooldown
        yield return new WaitForSeconds(toggleCooldown);
        canToggleInventory = true;
    }
    #endregion

    private void OnCloseInventory(InputAction.CallbackContext context)
    {
        if (itemDetailUI != null && itemDetailUI.detailPanel.activeSelf)
        {
            itemDetailUI.HideItemDetail();
            return;
        }

        if (inventoryUI == null) return;

        inventoryUI.CloseInventory();

        UIInputManager.Instance?.EnterGameplayMode();
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
    private IEnumerator SelectFirstButtonNextFrame()
    {
        yield return null; // 等待一幀確保UI佈局完成
        SelectFirstValidButton();
    }

    private int GetSlotIndex(Transform slotTransform)
    {
        // 從格子名稱中提取索引（例如 "Slot_0" -> 0）
        string name = slotTransform.name;
        if (name.StartsWith("Slot_"))
        {
            string indexStr = name.Substring(5);
            int index;
            if (int.TryParse(indexStr, out index))
            {
                return index;
            }
        }

        // 如果無法從名稱中提取，使用 sibling index
        return slotTransform.GetSiblingIndex();
    }

    private void SelectFirstValidButton()
    {
        if (selectableButtons.Count > 0)
        {
            currentSelectedIndex = 0;
            EventSystem.current.SetSelectedGameObject(selectableButtons[currentSelectedIndex].gameObject);
        }
        else
        {
            currentSelectedIndex = -1;
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    // 移動選擇焦點，跳過不可選按鈕
    private void MoveSelection(int direction, bool isVertical)
    {
        if (selectableButtons.Count == 0)
        {
            CacheSelectableButtons();
            if (selectableButtons.Count == 0) return;
        }

        // 獲取當前選中的按鈕索引
        int currentIndex = GetCurrentSelectedIndex();
        if (currentIndex == -1)
        {
            // 如果沒有選中任何按鈕，選擇第一個
            SelectFirstValidButton();
            return;
        }

        int newIndex = currentIndex;
        int attempts = 0;
        int maxAttempts = selectableButtons.Count; // 防止無限循環

        do
        {
            if (isVertical)
            {
                newIndex += direction * columnsCount; // 上下跳一行
            }
            else
            {
                newIndex += direction; // 左右跳一格
            }

            // 環繞循環
            if (newIndex < 0)
                newIndex = selectableButtons.Count - 1;
            else if (newIndex >= selectableButtons.Count)
                newIndex = 0;

            attempts++;

            Button candidate = selectableButtons[newIndex];
            if (candidate.interactable &&
                candidate.navigation.mode != Navigation.Mode.None &&
                !candidate.gameObject.CompareTag("NoSelect"))
            {
                EventSystem.current.SetSelectedGameObject(candidate.gameObject);
                if (inventoryUI != null)
                    inventoryUI.EnsureSlotVisible(candidate.transform);
                return;
            }
        } while (attempts < maxAttempts);
    }

    // 添加獲取當前選中索引的方法
    private int GetCurrentSelectedIndex()
    {
        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;
        if (currentSelected == null) return -1;

        for (int i = 0; i < selectableButtons.Count; i++)
        {
            if (selectableButtons[i].gameObject == currentSelected)
            {
                return i;
            }
        }

        return -1;
    }
    #endregion
}

