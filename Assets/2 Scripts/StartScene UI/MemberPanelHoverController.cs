using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// 控制 Panel 的滑鼠懸停事件 (配合 Event Trigger 使用)：
/// 滑鼠進入 Panel 範圍時顯示輔助物件（如退出按鈕），移出時延遲隱藏。
/// </summary>
public class MemberPanelHoverController : MonoBehaviour
{
    [Header("輔助物件")]
    [Tooltip("滑鼠懸停時要顯示的退出按鈕或其他輔助 UI")]
    public GameObject exitButton;

    [Header("緩衝設定")]
    [Tooltip("滑鼠移出後的隱藏延遲時間 (秒)，用來防止滑鼠在 UI 縫隙間移動時閃爍")]
    public float hideDelay = 0.05f;

    private Coroutine hideCoroutine;

    private void Start()
    {
        // 初始狀態下隱藏退出按鈕
        if (exitButton != null)
        {
            exitButton.SetActive(false);
        }
    }

    /// <summary>
    /// 給 Event Trigger 的 PointerEnter 呼叫
    /// </summary>
    public void OnPointerEnterPanel()
    {
        // 1. 停止隱藏倒數
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        // 2. 顯示輔助物件
        exitButton.SetActive(true);
    }

    /// <summary>
    /// 給 Event Trigger 的 PointerExit 呼叫
    /// </summary>
    public void OnPointerExitPanel()
    {
        // 啟動延遲隱藏
        if (gameObject.activeInHierarchy)
        {
            hideCoroutine = StartCoroutine(DelayHide());
        }
    }

    /// <summary>
    /// 延遲隱藏協程
    /// </summary>
    private IEnumerator DelayHide()
    {
        // 等待緩衝時間 (與原腳本相同的 0.05 秒概念)
        yield return new WaitForSeconds(hideDelay);

        // 緩衝時間結束後，確實隱藏按鈕
        if (exitButton != null)
        {
            exitButton.SetActive(false);
        }

        hideCoroutine = null;
    }

    private void OnDisable()
    {
        // 安全機制：當這個 Panel 被關閉 (SetActive(false)) 時，
        // 確保協程被清空，且退出按鈕重置為隱藏狀態
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        if (exitButton != null)
        {
            exitButton.SetActive(false);
        }
    }
}
