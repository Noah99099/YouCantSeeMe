using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class ItemDetailUI : MonoBehaviour
{
    [Header("功能：查看3D物件")]
    [Header("物件面板UI設置")]
    public GameObject detailPanel;
    public Text nameText;
    public Text descriptionText;
    public RawImage modelPreview;
    public RenderTexture renderTexture;

    [Header("物件面板相機設置")]
    public Transform modelSpawnPoint; // 放置模型的位置
    public Camera previewCamera; // 專門拍攝模型的相機

    [Header("控制預覽物件腳本")]
    public ItemPreviewController previewController; // 記得在 Inspector 指定

    public void ShowItemDetail(ItemData item)
    {
        detailPanel.SetActive(true);
        nameText.text = item.itemName;
        descriptionText.text = item.description;

        if (previewController != null && item.modelPrefab != null)
        {
            previewController.ResetPreview(item.modelPrefab);
        }
        else
        {
            Debug.LogWarning("模型無法預覽，請確認 item 與 Controller 是否指定");
        }
    }

    public void HideItemDetail()
    {
        detailPanel.SetActive(false);
    }

    public void ClearPreview()
    {
        if (previewController == null || previewController.modelRoot == null)
            return;

        foreach (Transform child in previewController.modelRoot)
        {
            if (child.GetComponent<PreviewModelTag>() != null)
            {
                Destroy(child.gameObject);
            }
        }
    }
}
