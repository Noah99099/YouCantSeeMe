using UnityEngine;

public class ItemPreviewController : MonoBehaviour
{
    public Transform modelRoot; // 動態載入模型的 parent
    public Camera previewCamera; // 指向你的 RenderTexture 專用相機
    public float rotationSpeed = 5f;
    public float zoomSpeed = 5f;
    public float minZoom = 0.5f;
    public float maxZoom = 3f;

    private Vector3 initialScale;
    private bool isDragging = false;



    void Start()
    {
        if (modelRoot != null)
            initialScale = modelRoot.localScale;
    }

    void Update()
    {
        if (modelRoot == null) return;

        // 滑鼠拖曳旋轉
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        if (isDragging)
        {
            float rotX = Input.GetAxis("Mouse X") * rotationSpeed;
            modelRoot.Rotate(Vector3.up, -rotX, Space.World);
        }

        // 滾輪縮放
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            Vector3 newScale = modelRoot.localScale + Vector3.one * scroll * zoomSpeed;
            float scaleFactor = newScale.magnitude / initialScale.magnitude;

            if (scaleFactor >= minZoom && scaleFactor <= maxZoom)
            {
                modelRoot.localScale = newScale;
            }
        }
    }

public void ResetPreview(GameObject newModel)
{
    foreach (Transform child in modelRoot)
    {
        Destroy(child.gameObject);
    }

    if (newModel == null)
    {
        Debug.LogWarning("未指定模型 prefab！");
        return;
    }

    GameObject instance = Instantiate(newModel, modelRoot);
    instance.transform.localPosition = Vector3.zero;
    instance.transform.localRotation = Quaternion.identity;

    //自動設 Layer
    SetLayerRecursively(instance, LayerMask.NameToLayer("ItemPreview"));

    // 將模型擺在相機正前方、居中
    Bounds bounds = CalculateBounds(instance);
    Vector3 centerOffset = bounds.center;
    instance.transform.localPosition = -centerOffset;

    float radius = bounds.extents.magnitude;
    float fov = previewCamera.fieldOfView;
    float distance = radius / Mathf.Tan(Mathf.Deg2Rad * fov * 0.5f) * 0.8f; // 加倍率

    Vector3 forward = previewCamera.transform.forward;
    modelRoot.position = previewCamera.transform.position + forward * distance;
    modelRoot.LookAt(previewCamera.transform);
    modelRoot.rotation = Quaternion.Euler(0, modelRoot.eulerAngles.y, 0);
    modelRoot.localScale = initialScale;

    Debug.Log($"模型已自動調整距離：{distance:F2}");
}


    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;

        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            if (child == null) continue;
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    private Bounds CalculateBounds(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
            return new Bounds(go.transform.position, Vector3.zero);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        return bounds;
    }


}
