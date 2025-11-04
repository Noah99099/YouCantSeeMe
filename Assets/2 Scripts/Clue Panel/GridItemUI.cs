using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [這是一個新檔案]
/// 管理左側單個物品格的 UI 元素
/// </summary>
public class GridItemUI : MonoBehaviour
{
    public Image icon;
    public Image border; // 用於顯示外框 (黃/紅/藍)
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
        icon.sprite = clue.ClueIcon;
        border.color = borderColor;
        button.onClick.RemoveAllListeners(); // 移除舊的監聽
        button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        _onClickCallback?.Invoke(_clue);
    }
}
