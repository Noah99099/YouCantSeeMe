using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ViewInteractionHandler : MonoBehaviour, IViewInteractable
{
    [Header("互動設定")]
    [Tooltip("陽視野下可否交互")]
    public bool interactiveInYang = true;

    [Tooltip("陰視野下可否交互")]
    public bool interactiveInYin = false;

    private InteractableItem interactableItem;

    void Awake()
    {
        interactableItem = GetComponent<InteractableItem>();
    }

    public bool IsVisibleIn(ViewType view) => true; // 外觀由 ViewMeshHandler 控制

    public bool IsInteractiveIn(ViewType view)
    {
        return (view == ViewType.Yang && interactiveInYang) ||
               (view == ViewType.Yin && interactiveInYin);
    }

    public void OnViewChanged(ViewType view)
    {
        bool canInteract = IsInteractiveIn(view);

        if (interactableItem != null)
        {
            // 核心：根據視野開關交互功能
            interactableItem.SetInteractionEnabled(canInteract);
        }
    }
}
