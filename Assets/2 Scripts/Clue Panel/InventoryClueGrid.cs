using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// [已更新] 管理左側「物品一覽」的 3x3 格子 (使用 Scroll View 和物件池)
/// </summary>
public class InventoryClueGrid : MonoBehaviour
{
    public GameObject gridContainer; // 整個 Scroll View 物件

    // [!!] 這個列表由您的 InventoryGridEditor.cs 填充
    // [!!] 它應該包含您預先實例化的 27 個 GridItemUI
    public List<GridItemUI> gridSlots = new List<GridItemUI>();

    // [移除] 不再需要 gridParent，因為 Scroll View 的 Content 就是 Parent
    // [移除] 不再需要 gridItemPrefab，因為格子是預製的

    /// <summary>
    /// 顯示並填充格子 (新邏輯)
    /// </summary>
    public void Show(List<IClue> clues, EClueType clueType, System.Action<IClue> onGridItemClicked)
    {
        gridContainer.SetActive(true);

        // 1. 決定外框顏色
        Color borderColor = Color.white;
        switch (clueType)
        {
            case EClueType.Item: borderColor = Color.yellow; break;
            case EClueType.Memory: borderColor = Color.red; break;
            case EClueType.Sound: borderColor = Color.blue; break;
        }

        // 2. 遍歷所有 27 個格子
        for (int i = 0; i < gridSlots.Count; i++)
        {
            GridItemUI slot = gridSlots[i];

            // 3. 如果我們的線索 (clues) 列表還有東西，就填充這個格子
            if (i < clues.Count)
            {
                slot.Setup(clues[i], borderColor, onGridItemClicked);
                slot.gameObject.SetActive(true); // 顯示格子
            }
            else
            {
                // 4. 如果線索不夠填滿 27 格，就把多餘的格子隱藏
                slot.gameObject.SetActive(false);
            }
        }

        // (可選) 每次打開時，將 Scroll View 滾動回頂部
        // GetComponent<ScrollRect>().normalizedPosition = new Vector2(0, 1);
    }

    public void Hide()
    {
        gridContainer.SetActive(false);
    }
}

