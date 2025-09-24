using UnityEngine;

public class ViewColliderHandler : MonoBehaviour, IViewInteractable
{
    public Collider sharedCollider;
    public Collider yangCollider;
    public Collider yinCollider;

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

    public bool IsInteractiveIn(ViewType view)
    {
        //return view == ViewType.Yang; // 例如只在陽可互動
        return true; // 這裡不管互動，只處理顯示
    }

    public void OnViewChanged(ViewType view)
    {
        if (sharedCollider) sharedCollider.enabled = true;
        if (yangCollider) yangCollider.enabled = (view == ViewType.Yang);
        if (yinCollider) yinCollider.enabled = (view == ViewType.Yin);
    }
}
