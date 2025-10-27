using UnityEngine;
using UnityEngine.UI;

public class ItemDetailUI : MonoBehaviour
{
    [Header("功能：實際控制 3D 模型預覽面板（開啟/關閉、生成模型等）")]
    [Header("物件面板UI設置")]
    public GameObject modelPreviewPanel;
    public Text nameText;
    public RawImage modelPreview;
    public RenderTexture renderTexture;

    [Header("物件面板相機設置")]
    public Transform modelSpawnPoint; // 放置模型的位置
    public Camera previewCamera; // 專門拍攝模型的相機

    [Header("控制預覽物件腳本")]
    public ItemPreviewController previewController; // 記得在 Inspector 指定

    // ShowItemDetail：只更新文字、icon，不生成模型
    public void ShowItemDetail(ItemData item)
    {
        ItemData data = item ?? InventoryManager.Instance?.defaultItem;
        nameText.text = data?.itemName ?? "";
        modelPreviewPanel?.SetActive(false); // 不生成模型
    }

    // ShowModelPreview：玩家明確操作才生成模型
    public void ShowModelPreview(ItemData item)
    {
        if (item == null || item.modelPrefab == null) return;
        if (previewController == null)
        {
            Debug.LogWarning("[ItemDetailUI] previewController 未綁定，無法顯示模型預覽");
            return;
        }

        previewController.ResetPreview(item.modelPrefab);
        modelPreviewPanel?.SetActive(true);
    }

    /// <summary>
    /// 隱藏物品詳情與模型
    /// </summary>
    public void HideItemDetail()
    {
        modelPreviewPanel?.SetActive(false);
    }

    /// <summary>
    /// 清空當前模型預覽（刪除所有帶 PreviewModelTag 的子物件）
    /// </summary>
    public void ClearPreview()
    {
        if (previewController?.modelRoot == null) return;

        foreach (Transform child in previewController.modelRoot)
        {
            if (child.GetComponent<PreviewModelTag>() != null)
                Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// 從模型預覽返回 Inventory
    /// </summary>
    public void ClosePreviewAndReturnToInventory()
    {
        Debug.Log("[ItemDetailUI] ClosePreviewAndReturnToInventory called");

        HideItemDetail();
        ClearPreview();
    }
}
