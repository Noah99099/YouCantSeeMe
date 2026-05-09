using UnityEngine;
using TMPro;
using System.Collections; // 為了使用協程 IEnumerator

/// <summary>
/// 結合了圖片查看與線索進度推進的腳本。
/// 掛載於特定的 5 個線索物件上。必須在 Interactable Layer。
/// </summary>
public class EvidenceMarker : MonoBehaviour, IInteractable
{
    [Header("圖片設定")]
    [Tooltip("要顯示的調查圖片 (不分陰陽視野，皆顯示此張)")]
    [SerializeField] private Sprite evidenceImage;

    [Header("互動提示")]
    [SerializeField] private string promptText = "調查";

    [Header("文本設定")]
    [Tooltip("這個物件要更新 Panel 上的第幾個文本？(對應陣列編號，從 0 開始。例如填 0, 1, 2, 3, 4)")]
    [SerializeField] private int clueIndex = 0;

    [Tooltip("交互後要顯示的文本內容")]
    [TextArea]
    [SerializeField] private string newTextContent;

    [Header("狀態 (唯讀觀察)")]
    public bool hasBeenInteracted = false;
    private bool isWaitingForPanelClose = false; // 防止玩家在面板開啟時狂按，造成重複觸發

    // 委派事件：當玩家第一次與此物件交互時觸發，讓腳本2(Manager)可以監聽
    public System.Action<EvidenceMarker> OnFirstInteraction;

    /// <summary>
    /// 提供給 PlayerInteraction 顯示 UI 提示
    /// </summary>
    public string GetInteractPrompt(bool isGamepad)
    {
        // 如果已經調查過，顯示「已調查」，否則顯示自訂的 promptText
        return hasBeenInteracted ? "已調查證物標示牌"+ (clueIndex+1) : promptText;
    }

    /// <summary>
    /// 當玩家點擊交互時由 PlayerInteraction 呼叫
    /// </summary>
    public void Interact(PlayerInteraction player)
    {
        // 防呆：如果已經在等待面板關閉中，就不做任何事
        if (isWaitingForPanelClose) return;

        // --- 功能 1: 打開圖片面板 ---
        if (ViewImagePanelController.Instance != null)
        {
            // 將同一張 evidenceImage 傳入兩次，這樣無論陰陽視野都顯示相同的圖片
            ViewImagePanelController.Instance.OpenPanel(evidenceImage, evidenceImage);
        }
        else
        {
            Debug.LogError("[EvidenceMarker] 找不到 ViewImagePanelController 實例！");
        }

        // --- 功能 2: 更新 Scene 1 的文字面板 ---
        if (GhostPanelTextUpdateManager.Instance != null)
        {
            GhostPanelTextUpdateManager.Instance.UpdateGhostText(clueIndex, newTextContent);
            Debug.LogWarning("[EvidenceMarker] 更新文本成功");
        }
        else
        {
            Debug.LogWarning("[EvidenceMarker] 找不到 GhostPanelTextUpdateManager！請確認 Scene 1 的 Panel 是否有成功跨場景帶過來。");
        }

        // --- 功能 3: 推進任務進度 (改為等待面板關閉後執行) ---
        if (!hasBeenInteracted)
        {
            StartCoroutine(WaitForPanelCloseAndRecord());
        }
    }

    /// <summary>
    /// 協程：等待圖片面板關閉後，才記錄交互並通知 Manager
    /// </summary>
    private IEnumerator WaitForPanelCloseAndRecord()
    {
        isWaitingForPanelClose = true;

        // 先等待一幀，確保面板已經完全開啟，避免瞬間誤判為關閉狀態
        yield return null;

        // --- 【關鍵判定：等待面板關閉】 ---
        // 這裡預設使用檢查 gameObject 是否隱藏的方式。
        // 如果您的面板關閉是將 ViewImagePanelController 物件 SetActive(false)，請用這行：
        //yield return new WaitUntil(() => !ViewImagePanelController.Instance.gameObject.activeInHierarchy);

        // 備用方案：
        // 根據您 PlayerInteraction.cs 裡的寫法，如果您開啟圖片面板時會更改 InputMap，
        // 關閉時會切回 _Player，您可以註解掉上面那行，改用下面這行來判定玩家是否回到遊玩狀態：
        yield return new WaitUntil(() => InputStackManager.Instance.CurrentMap == InputActionMaps._Player);

        // 面板確認關閉後，才真正推進進度
        hasBeenInteracted = true;
        isWaitingForPanelClose = false;

        Debug.Log($"[EvidenceMarker] 面板已關閉，玩家首次調查了 {gameObject.name}");

        // 通知 Scene 2 的 Manager 計數 +1
        OnFirstInteraction?.Invoke(this);
    }
}
