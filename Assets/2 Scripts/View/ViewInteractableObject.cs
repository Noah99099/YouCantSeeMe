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
    [Tooltip("陽視野下的模型")] public Mesh yangMesh;
    [Tooltip("陰視野下的模型")] public Mesh yinMesh;
    [Tooltip("陽視野下的碰撞體")] public Collider yangCollider;
    [Tooltip("陰視野下的碰撞體")] public Collider yinCollider;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Collider mainCollider;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        mainCollider = GetComponent<Collider>();
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

        meshRenderer.enabled = isVisible; //mesh開不開取決於是否可在該視野下可見
        mainCollider.enabled = isVisible || isInteractive; //不懂
        meshFilter.mesh = isYang ? yangMesh : yinMesh;

        if (yangCollider != null) yangCollider.enabled = isYang && isVisible;
        if (yinCollider != null) yinCollider.enabled = !isYang && isVisible;

        string state = isVisible ? "在" : "不在";
        Debug.Log($"{gameObject.name} {state} {(isYang ? "陽" : "陰")}視野顯示");

        //未給gpt精簡
        //if (yangMesh != null && yangCollider != null) //針對陽模型的判斷
        //{
        //    if(view == ViewType.Yang) //當前陽視野
        //    {
        //        switch (visibleInYang) 
        //        {
        //            case true: //可以在陽顯示
        //                GetComponent<MeshRenderer>().enabled = true; // 開啟 mesh 顯示
        //                GetComponent<Collider>().enabled = true; // 開啟碰撞
        //                meshFilter.mesh = yangMesh;
        //                yangCollider.enabled = true;
        //                yinCollider.enabled = false;
        //                print(gameObject.name + "在陽視野顯示");
        //                break;
        //            case false: //不可以在陽顯示
        //                GetComponent<MeshRenderer>().enabled = false; // 關閉 mesh 顯示
        //                GetComponent<Collider>().enabled = false; // 關閉碰撞
        //                print(gameObject.name + "不在陽視野顯示");
        //                break;
        //        }
        //    }
        //    else //當前陰視野
        //    {
        //        switch (visibleInYin) 
        //        {
        //            case true: //可以在陰顯示
        //                print("沒有" + gameObject.name + "在陰視野顯示的可能性！");
        //                break;
        //            case false: //不可以在陰顯示
        //                GetComponent<MeshRenderer>().enabled = false; // 關閉 mesh 顯示
        //                GetComponent<Collider>().enabled = false; // 關閉碰撞
        //                print(gameObject.name + "不在陰視野顯示");
        //                break;
        //        }
        //    }
        //}

        //if (yinMesh != null && yinCollider != null) //針對陰模型的判斷
        //{
        //    if (view == ViewType.Yang) //當前陽視野
        //    {
        //        switch (visibleInYang)
        //        {
        //            case true: //不可以在陽顯示
        //                GetComponent<MeshRenderer>().enabled = false; // 關閉 mesh 顯示
        //                GetComponent<Collider>().enabled = false; // 關閉碰撞
        //                print(gameObject.name + "不在陽視野顯示");
        //                break;
        //            case false: //可以在陽顯示
        //                print("沒有" + gameObject.name + "在陽視野顯示的可能性！");
        //                break;
        //        }
        //    }
        //    else //當前陰視野
        //    {
        //        switch (visibleInYin)
        //        {
        //            case true: //可以在陰顯示
        //                GetComponent<MeshRenderer>().enabled = true; // 開啟 mesh 顯示
        //                GetComponent<Collider>().enabled = true; // 開啟碰撞
        //                meshFilter.mesh = yinMesh;
        //                yangCollider.enabled = false;
        //                yinCollider.enabled = true;
        //                print(gameObject.name + "在陰視野顯示");
        //                break;
        //            case false: //不可以在陰顯示
        //                GetComponent<MeshRenderer>().enabled = false; // 關閉 mesh 顯示
        //                GetComponent<Collider>().enabled = false; // 關閉碰撞
        //                print(gameObject.name + "不在陰視野顯示");
        //                break;
        //        }
        //    }
        //}

        //    // 交互功能切換（例如拖動或點擊）
        //    Collider col = GetComponent<Collider>();
        //if (col != null)
        //    col.enabled = IsInteractiveIn(view);
    }

    private void OnDestroy()
    {
        if (ViewManager.Instance != null)
        {
            ViewManager.OnViewChanged -= OnViewChanged;
        }
    }
}
