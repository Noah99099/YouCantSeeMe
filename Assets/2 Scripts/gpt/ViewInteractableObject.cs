using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]

public class ViewInteractableObject : MonoBehaviour, IViewInteractable
{
    [Header("�i���Ƶ���")]
    [SerializeField] private bool visibleInYang = true;
    [SerializeField] private bool visibleInYin = false;
    [Header("�i�椬����")]
    [SerializeField] private bool interactiveInYang = true;
    [SerializeField] private bool interactiveInYin = false;
    [Header("�ҫ������]�i��^")]
    public GameObject yangModel;
    public GameObject yinModel;

    public bool IsVisibleIn(ViewType view) =>
        view == ViewType.Yang ? visibleInYang : visibleInYin;
    public bool IsInteractiveIn(ViewType view) =>
        view == ViewType.Yang ? interactiveInYang : interactiveInYin;

    public void OnViewChanged(ViewType view)
    {
        // �ҫ��i�����A�����]��������Ӫ���I�^
        if (yangModel != null)
            yangModel.SetActive(view == ViewType.Yang && visibleInYang);

        if (yinModel != null)
            yinModel.SetActive(view == ViewType.Yin && visibleInYin);

        // �椬�\������]�Ҧp��ʩ��I���^
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
