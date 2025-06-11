public interface IViewInteractable
{
    bool IsVisibleIn(ViewType view);
    bool IsInteractiveIn(ViewType view);
    void OnViewChanged(ViewType view);
}