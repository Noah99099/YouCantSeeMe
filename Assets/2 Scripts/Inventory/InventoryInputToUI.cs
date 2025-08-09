using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryInputToUI : MonoBehaviour
{
    [Header("Input Actions")]
    //public InputActionReference navigateAction;    // 指向 Inventory Action Map 的 Navigate
    public InputActionReference selectSlotAction;  // 指向 Inventory Action Map 的 SelectSlot

    [Header("Buttons Parent")]
    public GameObject inventoryPanel;    // 包含所有 Inventory 按鈕的父物件

    private List<Button> selectableButtons = new List<Button>();
    private int currentSelectedIndex = -1;

    private void OnEnable()
    {
        //navigateAction.action.Enable();
        selectSlotAction.action.Enable();

        //navigateAction.action.performed += OnNavigate;
        selectSlotAction.action.performed += OnSelectSlot;

        CacheSelectableButtons();

        // 初始化選擇第一個可選按鈕
        SelectFirstValidButton();
    }

    private void OnDisable()
    {
        //navigateAction.action.performed -= OnNavigate;
        selectSlotAction.action.performed -= OnSelectSlot;

        //navigateAction.action.Disable();
        selectSlotAction.action.Disable();
    }

    // 找出可選擇的按鈕，排除標籤 "NoSelect" 或導航模式為 None 的按鈕
    private void CacheSelectableButtons()
    {
        selectableButtons.Clear();
        Button[] buttons = inventoryPanel.GetComponentsInChildren<Button>(true);

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
}
