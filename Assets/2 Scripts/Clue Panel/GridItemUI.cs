using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [這是一個新檔案]
/// 管理左側單個物品格的 UI 元素
/// </summary>
public class GridItemUI : MonoBehaviour
{
    public Image icon;
    public Image border; // 現在直接在 Inspector 指定素材 A/B/C，不再動態改顏色
    public Button button;

    private IClue _clue;
    private System.Action<IClue> _onClickCallback;

    /// <summary>
    /// 設置此格子的內容
    /// </summary>
    public void Setup(IClue clue, Color borderColor, System.Action<IClue> onClickCallback)
    {
        _clue = clue;
        _onClickCallback = onClickCallback;

        // 設置圖標
        if (icon != null)
        {
            icon.sprite = clue.ClueIcon;
        }
        // border.color = borderColor;

        // 設置按鈕點擊事件
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }
    }

    private void OnClick()
    {
        _onClickCallback?.Invoke(_clue);
    }
}
