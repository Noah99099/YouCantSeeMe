using UnityEngine;
using UnityEngine.EventSystems; // 必須引用這兩個

public class ItemHoverTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private string _itemName;
    private string _itemDesc;
    private DialogueTooltipController _tooltipController;

    public void Setup(string name, string desc, DialogueTooltipController controller)
    {
        _itemName = name;
        _itemDesc = desc;
        _tooltipController = controller;
    }

    // 當滑鼠進入按鈕範圍
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_tooltipController != null)
        {
            _tooltipController.ShowTooltip(_itemName, _itemDesc);
        }
    }

    // 當滑鼠離開按鈕範圍
    public void OnPointerExit(PointerEventData eventData)
    {
        if (_tooltipController != null)
        {
            _tooltipController.HideTooltip();
        }
    }
}