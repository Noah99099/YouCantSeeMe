using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]

public class ViewInteractableObject : MonoBehaviour, IViewInteractable
{
    [Header("可以在什麼視野出現?")]
    [SerializeField] private bool visibleInYang = true;
    [SerializeField] private bool visibleInYin = false;
    [Header("可以在什麼視野進行交互?")]
    [SerializeField] private bool interactiveInYang = true;
    [SerializeField] private bool interactiveInYin = false;
    [Header("模型切換（可選）")]
    public GameObject yangModel;
    public GameObject yinModel;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject); //該遊戲物件不銷毀
    }
    public bool IsVisibleIn(ViewType view) =>
        view == ViewType.Yang ? visibleInYang : visibleInYin;
    public bool IsInteractiveIn(ViewType view) =>
        view == ViewType.Yang ? interactiveInYang : interactiveInYin;

    public void OnViewChanged(ViewType view)
    {
        if (yangModel != null)
            yangModel.SetActive(view == ViewType.Yang && visibleInYang); //當前陽視野 且 可以在陽看到

        if (yinModel != null)
            yinModel.SetActive(view == ViewType.Yin && visibleInYin); //當前陰視野 且 可以在陰看到

        if (yangModel != null && view == ViewType.Yin) //陽模型不為空 且 當前陰視野
            yangModel.SetActive(false);

        if(yinModel != null && view == ViewType.Yang) //陰模型不為空 且 當前陽視野
            yinModel.SetActive(false);

            // 交互功能切換（例如拖動或點擊）
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = IsInteractiveIn(view);
    }

    void OnEnable()
    {
        if (ViewManager.Instance != null) 
        {
            ViewManager.OnViewChanged += OnViewChanged;
            OnViewChanged(ViewManager.Instance.CurrentView);
        }
    }

    void OnDisable()
    {
        if (ViewManager.Instance != null)
            ViewManager.OnViewChanged -= OnViewChanged;
    }
}
