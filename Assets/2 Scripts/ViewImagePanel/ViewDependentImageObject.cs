using UnityEngine;

/// <summary>
/// 掛載在場景物件上，Layer 需設為 Interactable
/// </summary>
public class ViewDependentImageObject : MonoBehaviour, IInteractable
{
    [Header("圖片設定")]
    [Tooltip("在陽視野 (閉眼) 下看到的圖片")]
    public Sprite yangImage;
    [Tooltip("在陰視野 (張眼) 下看到的圖片")]
    public Sprite yinImage;

    [Header("互動提示")]
    public string promptText = "查看物品";

    // 實作 IInteractable 要求的介面方法
    public void Interact(PlayerInteraction player)
    {
        if (ViewImagePanelController.Instance != null)
        {
            // 呼叫 UI 控制器，把兩張圖片傳過去
            ViewImagePanelController.Instance.OpenPanel(yangImage, yinImage);
        }
        else
        {
            Debug.LogError("[ViewDependentImageObject] 找不到 ViewImagePanelController 實例！");
        }
    }

    // 實作 IInteractable 要求的介面方法
    public string GetInteractPrompt(bool isGamepad)
    {
        // 若有支援手把，可以在這裡切換提示文字 (例如: "[E] 查看" vs "[A] 查看")
        return promptText;
    }
}