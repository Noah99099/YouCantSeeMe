using System.Collections;
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
        if (Input.GetMouseButtonDown(0)) isDragging = true;
        if (Input.GetMouseButtonUp(0)) isDragging = false;

        if (isDragging)
        {
            float rotX = Input.GetAxis("Mouse X") * rotationSpeed;
            float rotY = Input.GetAxis("Mouse Y") * rotationSpeed;

            // 自由旋轉（使用 localRotation 控制）
            modelRoot.Rotate(Vector3.up, -rotX, Space.World);   // 水平滑動旋轉 Y 軸
            modelRoot.Rotate(Vector3.right, rotY, Space.Self);  // 垂直滑動旋轉 X 軸
        }

        // ➤ 滾輪縮放（限制範圍）
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            float scaleMultiplier = 1 + scroll * zoomSpeed;
            Vector3 newScale = modelRoot.localScale * scaleMultiplier;

            float factor = newScale.magnitude / initialScale.magnitude;

            if (factor >= minZoom && factor <= maxZoom)
            {
                modelRoot.localScale = newScale;
            }
        }
    }

    public void ResetPreview(GameObject newModel)
    {
        // 清除舊的模型（只刪除有 PreviewModelTag 的）
        foreach (Transform child in modelRoot)
        {
            if (child.GetComponent<PreviewModelTag>() != null)
            {
                Destroy(child.gameObject);
            }
        }

        StartCoroutine(DelayedInstantiate(newModel));
    }

    private IEnumerator DelayedInstantiate(GameObject newModel)
    {
        yield return null; // 等待一幀，確保 Destroy() 完成

        if (newModel == null) yield break;

        GameObject instance = Instantiate(newModel, modelRoot);
        instance.AddComponent<PreviewModelTag>();

        // 自動對齊模型至攝影機前方
        Bounds bounds = CalculateBounds(instance);
        float modelSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);

        float distance = modelSize * 2f; // 控制與相機距離，調整比例可放大縮小

        Vector3 camForward = previewCamera.transform.forward;
        Vector3 spawnPos = previewCamera.transform.position + camForward * distance;

        modelRoot.position = spawnPos;

        // 將模型重置到 modelRoot 中心
        instance.transform.localPosition = -bounds.center;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;
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
        // 本地空間的中心
        Vector3 localCenter = go.transform.InverseTransformPoint(bounds.center);
        bounds.center = localCenter;

        return bounds;
    }

}
