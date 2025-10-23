// ViewActiveSelfHandler.cs
using UnityEngine;

/// <summary>
/// 功能：被掛載的遊戲物件在陰陽視野下 SetActive(true/false)
/// </summary>
public class ViewActiveSelfHandler : MonoBehaviour, IViewInteractable
{
    public enum ActiveMode { ShowInYang, ShowInYin }

    [Header("Active Mode Setting")]
    [Tooltip("選擇此物件在哪個視野下啟用")]
    public ActiveMode mode = ActiveMode.ShowInYang;

    void Start()
    {
        if (ViewManager.Instance != null)
        {
            ViewManager.OnViewChanged += OnViewChanged;
            OnViewChanged(ViewManager.Instance.CurrentView); // 初始化狀態
        }
    }

    void OnDestroy()
    {
        if (ViewManager.Instance != null)
            ViewManager.OnViewChanged -= OnViewChanged;
    }

    public bool IsVisibleIn(ViewType view) => true;
    public bool IsInteractiveIn(ViewType view) => false;

    public void OnViewChanged(ViewType view)
    {
        bool shouldBeActive =
            (mode == ActiveMode.ShowInYang && view == ViewType.Yang) ||
            (mode == ActiveMode.ShowInYin && view == ViewType.Yin);

        gameObject.SetActive(shouldBeActive);
    }
}
