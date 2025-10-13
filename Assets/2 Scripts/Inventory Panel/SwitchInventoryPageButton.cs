using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SwitchInventoryPageButton : MonoBehaviour
{
    [SerializeField] private Button[] buttons; // 三個按鈕
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite[] selectedSprites;    // 每個按鈕的選中顏色
    [SerializeField] private GameObject[] panels; // 這裡放 A, B, C 面板

    private int currentPanelIndex = 0; // 0 = 物品, 1 = 鬼, 2 = 聲音

    //private void Start()
    //{
    //    // 一開始隱藏三個按鈕
    //    SetButtonsActive(false);
    //}

    private void OnEnable() //要修改的部分，因為用了InventoryUI
    {
        // 訂閱事件
        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.OnInventoryOpened += HandleInventoryOpened;
            InventoryUI.Instance.OnInventoryClosed += HandleInventoryClosed;
        }

        // 也可以防呆：如果此時背包已經開啟，立即刷新狀態
        if (InventoryUI.Instance != null && InventoryUI.Instance.isInventoryVisible)
        {
            HandleInventoryOpened();
        }
        else
        {
            SetButtonsActive(false);
        }
    }

    private void OnDisable()
    {
        // 取消訂閱
        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.OnInventoryOpened -= HandleInventoryOpened;
            InventoryUI.Instance.OnInventoryClosed -= HandleInventoryClosed;
        }
    }

    private void HandleInventoryOpened()
    {
        SetButtonsActive(true);
    }

    private void HandleInventoryClosed()
    {
        SetButtonsActive(false);
    }

    private void SetButtonsActive(bool isActive)
    {
        foreach (var btn in buttons)
            btn.gameObject.SetActive(isActive);

        foreach (var panel in panels)
            panel.SetActive(false);

        if (isActive)
        {
            // 預設顯示 index 0
            ShowPanel(0);
        }
    }

    public void OnButtonClicked(int index)
    {
        ShowPanel(index);

        //如果之後按鈕還想加更多效果（像是音效或動畫），也能直接在 OnButtonClicked 或事件訂閱裡擴充，非常方便。
    }

    private void ShowPanel(int index)
    {
        // 切換按鈕狀態
        for (int i = 0; i < buttons.Length; i++)
        {
            var img = buttons[i].GetComponent<Image>();
            if (i == index)
            {
                img.sprite = selectedSprites[i];
                buttons[i].interactable = false;
            }
            else
            {
                img.sprite = normalSprite;
                buttons[i].interactable = true;
            }
        }

        // 切換面板顯示
        for (int i = 0; i < panels.Length; i++)
        {
            panels[i].SetActive(i == index);
        }

        currentPanelIndex = index;
    }
}
