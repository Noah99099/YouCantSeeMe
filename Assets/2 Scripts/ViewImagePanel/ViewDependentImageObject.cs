using System.Collections;
using UnityEngine;
using UnityEngine.Events;

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

    [Header("事件觸發")]
    [Tooltip("『首次』交互當下觸發的事件")]
    public UnityEvent onPickUp;
    [Tooltip("『首次』關閉圖片面板後觸發的事件")]
    public UnityEvent onPanelClosed;

    private bool isInteracting = false;
    private bool hasPickedUp = false; // 紀錄是否已經觸發過首次交互

    // 實作 IInteractable 要求的介面方法
    public void Interact(PlayerInteraction player)
    {
        if (isInteracting) return;
        isInteracting = true; // 防呆，避免連點造成開多次面板或協程

        // 1. 觸發首次交互事件
        if (!hasPickedUp)
        {
            Debug.Log($"[ViewDependentImageObject] 玩家首次查看了物品");
            onPickUp?.Invoke();
            hasPickedUp = true;
        }

        // 2. 呼叫 UI 控制器，把兩張圖片傳過去開啟面板
        if (ViewImagePanelController.Instance != null)
        {
            ViewImagePanelController.Instance.OpenPanel(yangImage, yinImage);
            // 3. 開啟協程，等待玩家關閉圖片面板後再執行後續邏輯
            StartCoroutine(WaitForPanelClose());
        }
        else
        {
            Debug.LogError("[ViewDependentImageObject] 找不到 ViewImagePanelController 實例！");
            isInteracting = false; // 若出錯則解除鎖定
        }
    }

    // 實作 IInteractable 要求的介面方法
    public string GetInteractPrompt(bool isGamepad)
    {
        // 若有支援手把，可以在這裡切換提示文字 (例如: "[E] 查看" vs "[A] 查看")
        return promptText;
    }

    /// <summary>
    /// 等待圖片面板關閉的協程
    /// </summary>
    private IEnumerator WaitForPanelClose()
    {
        // 確認 Controller 與 panelRoot 存在
        if (ViewImagePanelController.Instance != null && ViewImagePanelController.Instance.panelRoot != null)
        {
            // 等待一幀，確保面板狀態已經切換為 Active
            yield return null;

            // 持續檢查，直到 panelRoot 被關閉為止
            while (ViewImagePanelController.Instance.panelRoot.activeSelf)
            {
                yield return null;
            }
        }

        // 每次關閉面板都會觸發事件
        Debug.Log($"[ViewDependentImageObject] 圖片面板關閉，觸發 onPanelClosed。");
        onPanelClosed?.Invoke();

        // 將互動狀態解除，允許玩家進行下一次的點擊查看
        isInteracting = false;
    }
}