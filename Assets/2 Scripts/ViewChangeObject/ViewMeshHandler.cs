using UnityEngine;

public class ViewMeshHandler : MonoBehaviour, IViewInteractable
{
    [Header("Mesh Options")]
    public Mesh yangMesh;
    public Mesh yinMesh;

    [Header("Material Options")]
    public Material yangMaterial;
    public Material yinMaterial;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
    }

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

    public bool IsVisibleIn(ViewType view)
    {
        return true; // 這裡不管互動，只處理顯示
    }

    public bool IsInteractiveIn(ViewType view) => false;

    public void OnViewChanged(ViewType view)
    {
        switch (view)
        {
            case ViewType.Yang:
                meshFilter.sharedMesh = yangMesh;
                meshRenderer.sharedMaterial = yangMaterial;
                break;
            case ViewType.Yin:
                meshFilter.sharedMesh = yinMesh;
                meshRenderer.sharedMaterial = yinMaterial;
                break;
        }
    }
}
