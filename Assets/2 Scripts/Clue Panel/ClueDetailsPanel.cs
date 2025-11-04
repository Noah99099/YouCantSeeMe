using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 管理左下方顯示標題、描述和「使用物品」按鈕的面板
/// </summary>
public class ClueDetailsPanel : MonoBehaviour
{
    public GameObject panelContainer;
    public TMP_Text titleText;
    public TMP_Text descriptionText;
    public Button useButton;

    private System.Action _onUseCallback;

    void Awake()
    {
        useButton.onClick.AddListener(OnUseClick);
        Hide();
    }

    public void Show(string title, string description, System.Action onUseCallback)
    {
        titleText.text = title;
        descriptionText.text = description;
        _onUseCallback = onUseCallback;
        panelContainer.SetActive(true);
        useButton.gameObject.SetActive(true);
    }

    private void OnUseClick()
    {
        _onUseCallback?.Invoke();
    }

    public void Hide()
    {
        panelContainer.SetActive(false);
        useButton.gameObject.SetActive(false);
    }
}

