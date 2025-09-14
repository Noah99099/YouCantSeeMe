using UnityEngine;
using UnityEngine.EventSystems;

public class InventorySlotUI : MonoBehaviour, ISelectHandler, IPointerClickHandler
{
    private ItemData boundItem;

    public void Bind(ItemData item)
    {
        boundItem = item;
    }

    // 手柄 / 鍵盤選中 → 更新右側資訊 (selected 行為)
    public void OnSelect(BaseEventData eventData)
    {
        InventoryManager.Instance?.UpdateInformationPanel(boundItem);
        InventoryUI.Instance?.SetCurrentSelectedItem(boundItem);
    }

    // 滑鼠點擊 → 視為「選中 / 點擊格子」(但不直接開模型面板)
    public void OnPointerClick(PointerEventData eventData)
    {
        InventoryUI.Instance?.OnSlotClicked(boundItem);
    }
}
