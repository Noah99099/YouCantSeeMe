using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    [Header("功能：使用物件進行場景交互的設置腳本")]
    [Header("交互物件設置")]
    public string objectName = "物件"; // 物件名稱
    public ItemData requiredItem; // 需要的物品

    [Header("可以在什麼視野出現?")]
    [SerializeField] private bool visibleInYang = true;
    [SerializeField] private bool visibleInYin = false;
    [Header("可以在什麼視野進行交互?")]
    [SerializeField] private bool interactiveInYang = true;
    [SerializeField] private bool interactiveInYin = false;

    [Header("模型切換")]
    [Tooltip("陽視野碰撞體")] public Collider yangCollider;
    [Tooltip("陰視野碰撞體")] public Collider yinCollider;

    [Header("成功和失敗事件")]
    public UnityEngine.Events.UnityEvent onCorrectItemUsed;
    public UnityEngine.Events.UnityEvent onWrongItemUsed;

    private Collider mainCollider;

    private void Awake()
    {
        mainCollider = GetComponent<Collider>();

        if (yangCollider != null) yangCollider.enabled = false;
        if (yinCollider != null) yinCollider.enabled = false;

        DontDestroyOnLoad(gameObject); //該遊戲物件不銷毀
    }
    private void Start()
    {
        if (ViewManager.Instance != null) //初始化接收當前視野
        {
            ViewManager.OnViewChanged += OnViewChanged;
            OnViewChanged(ViewManager.Instance.CurrentView);
        }
    }
    public bool IsVisibleIn(ViewType view) =>
        view == ViewType.Yang ? visibleInYang : visibleInYin; //當前視野陽嗎? 是:visibleInYang，不是:visibleInYin
    public bool IsInteractiveIn(ViewType view) =>
        view == ViewType.Yang ? interactiveInYang : interactiveInYin;

    public void OnViewChanged(ViewType view) //注意！！！就算設定視野不顯示模型也要套模型，不能null放著
    {
        bool isYang = view == ViewType.Yang; //isYang接收TF
        bool isVisible = IsVisibleIn(view);
        bool isInteractive = IsInteractiveIn(view);

        // 主 Collider：在可見或可互動時啟用
        mainCollider.enabled = isVisible || isInteractive;
        // 子 Collider：只有當視野正確且可見時啟用
        if (yangCollider != null) yangCollider.enabled = isYang && isVisible;
        if (yinCollider != null) yinCollider.enabled = !isYang && isVisible;

        string state = isVisible ? "在" : "不在";
        Debug.Log($"{gameObject.name} {state} {(isYang ? "陽" : "陰")}視野顯示");
    }

    private void OnDestroy()
    {
        if (ViewManager.Instance != null)
        {
            ViewManager.OnViewChanged -= OnViewChanged;
        }
    }

    /// <summary>
    /// 嘗試使用物品
    /// </summary>
    public bool UseItem(ItemData item)
    {
        if (item == null) return false;

        if (item == requiredItem)
        {
            Debug.Log($"[InteractableObject] 使用了正確物品 {item.itemName} 於 {objectName}");
            onCorrectItemUsed?.Invoke();
            Destroy(gameObject); // 物件消失
            return true; // 使用成功
        }
        else
        {
            Debug.Log($"[InteractableObject] 使用了錯誤物品 {item.itemName} 於 {objectName}");
            onWrongItemUsed?.Invoke();
            return false; // 使用失敗
        }
    }
}