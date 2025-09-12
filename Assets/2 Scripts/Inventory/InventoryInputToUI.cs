using System;
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
    [Tooltip("Inventory Action Map 的 SelectSlot")] public InputActionReference selectSlotAction;
    [Tooltip("Inventory Action Map 的 CloseInventory")] public InputActionReference closeInventoryAction;

    [Header("Input Actions：玩家模式")]
    [Tooltip("Player Action Map 的 OpenInventory")] public InputActionReference openInventoryAction;

    [Header("UI References")]
    //public GameObject inventoryPanel; // 包含所有 Inventory 按鈕的父物件
    public ItemDetailUI itemDetailUI;
    public InventoryUI inventoryUI;

    private List<Button> selectableButtons = new List<Button>();
    private int currentSelectedIndex = -1;

    private void OnEnable()
    {
        // 啟用所有動作
        //背包
        navigateAction?.action.Enable(); //導航按鈕
        selectSlotAction?.action.Enable(); //?
        closeInventoryAction?.action.Enable(); //關背包
        //玩家
        openInventoryAction?.action.Enable(); //開背包

        // 訂閱事件
        //背包
        navigateAction.action.performed += OnNavigate;
        selectSlotAction.action.performed += OnSelectSlot;
        closeInventoryAction.action.performed += OnCloseInventory;
        //玩家
        openInventoryAction.action.performed += OnOpenInventory;
    }

    private void OnDisable()
    {
        //背包
        navigateAction.action.performed -= OnNavigate;
        selectSlotAction.action.performed -= OnSelectSlot;
        closeInventoryAction.action.performed -= OnCloseInventory;
        //玩家
        openInventoryAction.action.performed -= OnOpenInventory;

        // 禁用所有動作
        //背包
        navigateAction?.action.Disable();
        selectSlotAction?.action.Disable();
        closeInventoryAction?.action.Disable();
        //玩家
        openInventoryAction?.action.Disable();     
    }

    #region ===== 輸入處理方法 =====
    private void OnNavigate(InputAction.CallbackContext context)
    {
        Vector2 navigationInput = context.ReadValue<Vector2>();
        if (navigationInput == Vector2.zero) return;

        if (selectableButtons.Count == 0) return;

        // 向上 or 向左 移動選擇
        if (navigationInput.y > 0 || navigationInput.x < 0)
        {
            MoveSelection(-1);
        }
        // 向下 or 向右 移動選擇
        else if (navigationInput.y < 0 || navigationInput.x > 0)
        {
            MoveSelection(1);
        }
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
    private void OnOpenInventory(InputAction.CallbackContext context)
    {
        if (inventoryUI == null) return;
        inventoryUI.ToggleInventory(); // 執行 inventoryUI腳本的 處理開關背包面板 方法
    }

    private void OnCloseInventory(InputAction.CallbackContext context)
    {
        if (itemDetailUI != null && itemDetailUI.detailPanel.activeSelf)
        {
            // 如果有詳情面板打開，先關閉詳情面板
            itemDetailUI.HideItemDetail();
            return;
        }

        if (inventoryUI == null) return;
        inventoryUI.CloseInventory();
    }

    // 找出可選擇的按鈕，排除標籤 "NoSelect" 或導航模式為 None 的按鈕
    private void CacheSelectableButtons()
    {
        selectableButtons.Clear();
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
    private void MoveSelection(int direction)
    {
        if (selectableButtons.Count == 0) return;

        int newIndex = currentSelectedIndex;
        for (int i = 0; i < selectableButtons.Count; i++)
        {
            newIndex += direction;

            // 環繞循環
            if (newIndex < 0)
                newIndex = selectableButtons.Count - 1;
            else if (newIndex >= selectableButtons.Count)
                newIndex = 0;

            Button candidate = selectableButtons[newIndex];
            if (candidate.interactable &&
                candidate.navigation.mode != Navigation.Mode.None &&
                !candidate.gameObject.CompareTag("NoSelect"))
            {
                currentSelectedIndex = newIndex;
                EventSystem.current.SetSelectedGameObject(candidate.gameObject);
                return;
            }
        }
    }
    #endregion
}
