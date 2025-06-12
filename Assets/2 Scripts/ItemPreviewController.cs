using UnityEngine;

public class ItemPreviewController : MonoBehaviour
{
    public Transform modelRoot; // 動態載入模型的 parent
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
        // 清除舊模型
        foreach (Transform child in modelRoot)
        {
            Destroy(child.gameObject);
        }

        if (newModel != null)
        {
            GameObject instance = Instantiate(newModel, modelRoot);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;

            // 重設縮放
            modelRoot.localScale = initialScale;
        }
    }
}
