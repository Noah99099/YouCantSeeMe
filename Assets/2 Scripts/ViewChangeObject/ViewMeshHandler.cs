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
            // 只有在腳本啟用的情況下才初始化
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
        // 關鍵：如果腳本被關閉(例如在非所屬階段)，則不執行切換邏輯
        if (!this.enabled) return;

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
