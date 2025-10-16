using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SwitchInventoryPageButton : MonoBehaviour
{
    [SerializeField] private Button[] buttons; // 四個按鈕
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite[] selectedSprites;    // 每個按鈕的選中顏色
    [SerializeField] private GameObject[] panels; // 這裡放 案件紀錄簿的所有面板

    private int currentPanelIndex = 0; // 0 = 物品, 1 = 鬼, 2 = 聲音, 3 = 組合線索
    // ***** 新增: 用於存儲父級控制器的引用 *****
    private InventoryPanelUIController _panelController;

    private void Awake()
    {
        // 在 Awake 時，向上查找父物件中的控制器。
        // 這樣做比單例模式更靈活，且不需要在 Inspector 中手動拖曳。
        _panelController = GetComponentInParent<InventoryPanelUIController>();

        if (_panelController == null)
        {
            Debug.LogError("SwitchInventoryPageButton 找不到父級的 InventoryPanelUIController！", this.gameObject);
        }
    }

    private void OnEnable()
    {
        // ***** 修改: 訂閱來自 _panelController 的新事件 *****
        if (_panelController != null)
        {
            _panelController.OnPanelOpened += HandleInventoryOpened;
            _panelController.OnPanelClosed += HandleInventoryClosed;

            // 防呆：如果啟用時，父面板已經是打開狀態，立即刷新按鈕
            if (_panelController.IsInventoryPanelOpen)
            {
                HandleInventoryOpened();
            }
        }
    }

    private void OnDisable()
    {
        // ***** 修改: 取消訂閱來自 _panelController 的事件 *****
        if (_panelController != null)
        {
            _panelController.OnPanelOpened -= HandleInventoryOpened;
            _panelController.OnPanelClosed -= HandleInventoryClosed;
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
            // 預設顯示 index 0，也就是物品
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
