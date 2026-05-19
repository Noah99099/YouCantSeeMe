using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 結合 ViewDependentImageObject 與 InteractableItem 的功能 (可重複查看版)。
/// 流程：
/// 首次點擊 -> 獲得物品進背包 -> 開啟陰陽圖片面板 -> 關閉面板後觸發提示 (僅限首次) -> 不銷毀場景物件
/// 再次點擊 -> 不再次獲得物品 -> 開啟陰陽圖片面板 -> 關閉面板不觸發提示 -> 不銷毀場景物件
/// </summary>
[RequireComponent(typeof(Collider))]
public class ReusableViewDependentItem : MonoBehaviour, IInteractable
{
    [Header("圖片設定 (陰陽視野)")]
    [Tooltip("在陽視野 (閉眼) 下看到的圖片")]
    public Sprite yangImage;
    [Tooltip("在陰視野 (張眼) 下看到的圖片")]
    public Sprite yinImage;

    [Header("獲得物品設定")]
    public ItemData itemData;
    [Tooltip("第一次點擊時顯示的互動提示")]
    public string firstTimePromptText = "查看並拾取";
    [Tooltip("物品已經在背包後，再次點擊顯示的互動提示")]
    public string inspectPromptText = "再次查看";

    [Header("事件觸發")]
    [Tooltip("『首次』拾取物品當下觸發的事件")]
    public UnityEvent onPickUp;

    [Tooltip("『每次』關閉圖片面板後觸發的事件 (請將 UpdateRightHintText 對應的方法拖曳至此)")]
    public UnityEvent onPanelClosed;

    [HideInInspector] public bool InteractionEnabled = true;

    private bool isInteracting = false;
    private bool hasPickedUp = false; // 紀錄是否已經將物品加入背包
    private bool hasTriggeredPanelClosed = false; // 紀錄是否已經觸發過關閉面板事件

    // 實作 IInteractable 要求的介面方法
    public string GetInteractPrompt(bool isGamepad)
    {
        // 根據是否已經撿過物品，切換不同的提示文字
        string currentPrompt = hasPickedUp ? inspectPromptText : firstTimePromptText;
        return isGamepad ? $"按 [叉] {currentPrompt} {itemData?.itemName}" : $"按 [滑鼠左鍵] {currentPrompt} {itemData?.itemName}";
    }

    // 實作 IInteractable 要求的介面方法
    public void Interact(PlayerInteraction player)
    {
        if (!InteractionEnabled || isInteracting) return;
        isInteracting = true; // 防呆，避免連點造成開多次面板或協程

        if (!hasPickedUp)
        {
            Debug.Log($"[ReusableViewDependentItem] 玩家首次拾取了 {itemData?.itemName} 並查看圖片");

            // 1. 觸發原始的拾取事件
            onPickUp?.Invoke();

            // 2. 將物品真正加入背包系統
            if (InventoryManager.Instance != null && itemData != null)
            {
                InventoryManager.Instance.AddItem(itemData);
            }
            else
            {
                Debug.LogError($"[ReusableViewDependentItem] 拾取失敗：InventoryManager 或 ItemData 為空！");
            }

            hasPickedUp = true; // 標記為已拾取，下次點擊就不會再進這段邏輯
        }
        else
        {
            Debug.Log($"[ReusableViewDependentItem] 玩家再次查看了 {itemData?.itemName} 的圖片");
        }

        // 3. 呼叫 UI 控制器，把兩張圖片傳過去開啟面板
        if (ViewImagePanelController.Instance != null)
        {
            ViewImagePanelController.Instance.OpenPanel(yangImage, yinImage);
        }
        else
        {
            Debug.LogError("[ReusableViewDependentItem] 找不到 ViewImagePanelController 實例！");
        }

        // 4. 開啟協程，等待玩家關閉圖片面板後再執行後續邏輯 (不隱藏、不銷毀物件)
        StartCoroutine(WaitForPanelClose());
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

        // 依照需求：只在第一次關閉面板時觸發事件
        if (!hasTriggeredPanelClosed)
        {
            Debug.Log($"[ReusableViewDependentItem] 圖片面板首次關閉，觸發後續提示。");
            onPanelClosed?.Invoke();
            hasTriggeredPanelClosed = true; // 標記為已觸發，以後不再執行
        }
        else
        {
            Debug.Log($"[ReusableViewDependentItem] 圖片面板再次關閉，不再觸發提示。");
        }

        // 將互動狀態解除，允許玩家進行下一次的點擊查看
        isInteracting = false;
    }
}