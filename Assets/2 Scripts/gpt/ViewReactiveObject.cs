using UnityEngine;

public class ViewReactiveObject : MonoBehaviour
{
    [Tooltip("陰視野時顯示的物件")]
    public GameObject yinViewModel;

    [Tooltip("陽視野時顯示的物件")]
    public GameObject yangViewModel;

    [Tooltip("是否在陰視野時顯示物件")]
    public bool visibleInYin = true;

    [Tooltip("是否在陽視野時顯示物件")]
    public bool visibleYang = true;

    private void OnEnable()
    {
        ViewManager.OnViewChanged += UpdateVisibility;
        UpdateVisibility(ViewManager.Instance.CurrentView);
    }

    private void OnDisable()
    {
        ViewManager.OnViewChanged -= UpdateVisibility;
    }

    private void UpdateVisibility(ViewType currentView)
    {
        if (yinViewModel != null)
        {
            yinViewModel.SetActive(currentView == ViewType.Yin && visibleInYin);
        }

        if (yangViewModel != null)
        {
            yangViewModel.SetActive(currentView == ViewType.Yang && visibleYang);
        }

        // 若只用一個物件控制顯示與否（非模型替換），也可以這樣做：
        if (yinViewModel == null && yangViewModel == null)
        {
            gameObject.SetActive(
                (currentView == ViewType.Yin && visibleInYin) ||
                (currentView == ViewType.Yang && visibleYang)
            );
        }
    }
}