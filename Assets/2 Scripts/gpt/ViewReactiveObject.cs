using UnityEngine;

public class ViewReactiveObject : MonoBehaviour
{
    [Tooltip("����������ܪ�����")]
    public GameObject yinViewModel;

    [Tooltip("����������ܪ�����")]
    public GameObject yangViewModel;

    [Tooltip("�O�_�b����������ܪ���")]
    public bool visibleInYin = true;

    [Tooltip("�O�_�b����������ܪ���")]
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

        // �Y�u�Τ@�Ӫ��󱱨���ܻP�_�]�D�ҫ������^�A�]�i�H�o�˰��G
        if (yinViewModel == null && yangViewModel == null)
        {
            gameObject.SetActive(
                (currentView == ViewType.Yin && visibleInYin) ||
                (currentView == ViewType.Yang && visibleYang)
            );
        }
    }
}