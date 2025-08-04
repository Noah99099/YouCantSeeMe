using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]

public class ViewInteractableObject : MonoBehaviour, IViewInteractable
{
    [Header("未來保存/加載場景時可用")]
    [SerializeField] private string objectID = "type me"; // 每個物件要設唯一ID
    [Header("可以在什麼視野出現?")]
    [SerializeField] private bool visibleInYang = true;
    [SerializeField] private bool visibleInYin = false;
    [Header("可以在什麼視野進行交互?")]
    [SerializeField] private bool interactiveInYang = true;
    [SerializeField] private bool interactiveInYin = false;
    [Header("模型切換")] //改成用mesh 和 collider去進行模型切換
    //public GameObject yangModel;
    //public GameObject yinModel;
    [Tooltip("陽視野模型")] public Mesh yangMesh;
    [Tooltip("陰視野模型")] public Mesh yinMesh;
    [Tooltip("陽視野碰撞體")] public Collider yangCollider;
    [Tooltip("陰視野碰撞體")] public Collider yinCollider;
    [Tooltip("陽視野材質")] public Material yangMaterial;
    [Tooltip("陰視野材質")] public Material yinMaterial;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Collider mainCollider;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
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

        // 設定 Mesh 顯示與替換
        meshRenderer.enabled = isVisible; //mesh開不開取決於是否可在該視野下可見
        meshFilter.mesh = isYang ? yangMesh : yinMesh;

        //材質切換
         if (meshRenderer != null)
            meshRenderer.material = isYang ? yangMaterial : yinMaterial;

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
}
