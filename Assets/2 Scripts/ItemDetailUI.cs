using UnityEngine;
using UnityEngine.UI;

public class ItemDetailUI : MonoBehaviour
{
    public GameObject detailPanel;
    public Text nameText;
    public Text descriptionText;
    public RawImage modelPreview;
    public RenderTexture renderTexture;

    public Transform modelSpawnPoint; // 放置模型的位置
    public Camera previewCamera; // 專門拍攝模型的相機

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

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

}
