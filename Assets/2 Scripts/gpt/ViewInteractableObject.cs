using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]

public class ViewInteractableObject : MonoBehaviour, IViewInteractable
{
    [Header("可視化視野")]
    [SerializeField] private bool visibleInYang = true;
    [SerializeField] private bool visibleInYin = false;
    [Header("可交互視野")]
    [SerializeField] private bool interactiveInYang = true;
    [SerializeField] private bool interactiveInYin = false;
    [Header("模型切換（可選）")]
    public GameObject yangModel;
    public GameObject yinModel;

    public bool IsVisibleIn(ViewType view) =>
        view == ViewType.Yang ? visibleInYang : visibleInYin;
    public bool IsInteractiveIn(ViewType view) =>
        view == ViewType.Yang ? interactiveInYang : interactiveInYin;

    public void OnViewChanged(ViewType view)
    {
        // 模型可視狀態切換（不關掉整個物件！）
        if (yangModel != null)
            yangModel.SetActive(view == ViewType.Yang && visibleInYang);

        if (yinModel != null)
            yinModel.SetActive(view == ViewType.Yin && visibleInYin);

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
