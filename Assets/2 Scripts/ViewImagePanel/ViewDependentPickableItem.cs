using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 結合 ViewDependentImageObject 與 InteractableItem 的功能。
/// 流程：點擊 -> 獲得物品進背包 -> 開啟陰陽圖片面板 -> 關閉面板後觸發提示 (UpdateRightHintText) -> 銷毀場景物件
/// </summary>
[RequireComponent(typeof(Collider))]
public class ViewDependentPickableItem : MonoBehaviour, IInteractable
{
    [Header("圖片設定 (陰陽視野)")]
    [Tooltip("在陽視野 (閉眼) 下看到的圖片")]
    public Sprite yangImage;
    [Tooltip("在陰視野 (張眼) 下看到的圖片")]
    public Sprite yinImage;

    [Header("獲得物品設定")]
    public ItemData itemData;
    public string promptText = "查看並拾取物品";

    [Header("事件觸發")]
    [Tooltip("拾取物品『當下』觸發的事件")]
    public UnityEvent onPickUp;

    [Tooltip("『關閉圖片面板後』觸發的事件 (請將 UpdateRightHintText 對應的方法拖曳至此)")]
    public UnityEvent onPanelClosed;

    [HideInInspector] public bool InteractionEnabled = true;
    private bool isInteracting = false;

    // 實作 IInteractable 要求的介面方法
    public string GetInteractPrompt(bool isGamepad)
    {
        return isGamepad ? $"按 [叉] {promptText} {itemData?.itemName}" : $"按 [滑鼠左鍵] {promptText} {itemData?.itemName}";
    }

    // 實作 IInteractable 要求的介面方法
    public void Interact(PlayerInteraction player)
    {
        if (!InteractionEnabled || isInteracting) return;
        isInteracting = true; // 防呆，避免連點

        Debug.Log($"[ViewDependentPickableItem] 玩家拾取了 {itemData?.itemName} 並查看圖片");

        // 1. 觸發原始的拾取事件
        onPickUp?.Invoke();

        // 2. 將物品真正加入背包系統
        if (InventoryManager.Instance != null && itemData != null)
        {
            InventoryManager.Instance.AddItem(itemData);
        }
        else
        {
            Debug.LogError($"[ViewDependentPickableItem] 拾取失敗：InventoryManager 或 ItemData 為空！");
        }

        // 3. 呼叫 UI 控制器，把兩張圖片傳過去開啟面板
        if (ViewImagePanelController.Instance != null)
        {
            ViewImagePanelController.Instance.OpenPanel(yangImage, yinImage);
        }
        else
        {
            Debug.LogError("[ViewDependentPickableItem] 找不到 ViewImagePanelController 實例！");
        }

        // 4. 隱藏場景上的 3D 物件 (先不銷毀，因為還要跑協程等待 UI 關閉)
        // HideObject();

        // 5. 開啟協程，等待玩家關閉圖片面板後再執行後續邏輯
        StartCoroutine(WaitForPanelCloseAndDestroy());
    }

    /// <summary>
    /// 隱藏物件的渲染與碰撞，讓玩家感覺物品已被撿走
    /// </summary>
    private void HideObject()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        MeshRenderer mesh = GetComponent<MeshRenderer>();
        if (mesh != null) mesh.enabled = false;

        // 若您的物件有多個子物件 Mesh，可以考慮使用 gameObject.SetActive(false)
        // 但注意 SetActive(false) 會強制中斷此腳本的協程！
        // 所以這裡僅關閉 Collider 和 MeshRenderer 是最安全的做法。
    }

    /// <summary>
    /// 等待圖片面板關閉的協程
    /// </summary>
    private IEnumerator WaitForPanelCloseAndDestroy()
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

        // 當迴圈結束，代表面板已經關閉
        Debug.Log($"[ViewDependentPickableItem] 圖片面板已關閉，觸發後續提示並銷毀物件。");

        // 觸發自訂事件 (例如呼叫 UpdateRightHintText 的 GetTwoThings 等方法)
        onPanelClosed?.Invoke();

        // 最終銷毀場景上的物件
        Destroy(gameObject);
    }
}