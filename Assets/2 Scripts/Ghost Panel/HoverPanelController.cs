using UnityEngine;
using UnityEngine.EventSystems; // 必須引用，用於偵測滑鼠事件
using System.Collections;

public class HoverPanelController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI 設定")]
    [Tooltip("請將此按鈕對應的 Panel 拉到這裡")]
    public GameObject targetPanel;

    [Header("時間設定")]
    [Tooltip("滑鼠離開後延遲幾秒關閉")]
    public float delayTime = 1.0f;

    // --- 新增：靜態變數，所有按鈕共享，記錄目前誰是「老大」 ---
    private static HoverPanelController currentActiveBtn;

    // 用來儲存目前的計時器，以便需要時可以取消它
    private Coroutine closeCoroutine;

    // 當滑鼠進入按鈕範圍時觸發
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 1. 檢查是否有「其他的」按鈕正在開啟或倒數中
        if (currentActiveBtn != null && currentActiveBtn != this)
        {
            // 強制關閉上一個按鈕的 Panel，不管它倒數完了沒
            currentActiveBtn.ForceClose();
        }

        // 2. 設定自己為當前活動按鈕
        currentActiveBtn = this;

        // 3. 取消自己的關閉倒數 (防閃爍) 並打開 Panel
        if (closeCoroutine != null)
        {
            StopCoroutine(closeCoroutine);
            closeCoroutine = null;
        }

        if (targetPanel != null)
        {
            targetPanel.SetActive(true);
        }
    }

    // 當滑鼠離開按鈕範圍時觸發
    public void OnPointerExit(PointerEventData eventData)
    {
        // 4. 開始執行延遲關閉的計時器
        if (this.gameObject.activeInHierarchy) // 確保物件啟用中才能跑 Coroutine
        {
            closeCoroutine = StartCoroutine(ClosePanelDelay());
        }
    }

    // 延遲關閉的邏輯
    private IEnumerator ClosePanelDelay()
    {
        yield return new WaitForSeconds(delayTime); // 等待設定的秒數

        ForceClose(); // 時間到，執行關閉
    }

    // --- 新增：被強制關閉的邏輯 (供外部或自己呼叫) ---
    public void ForceClose()
    {
        // 停止倒數
        if (closeCoroutine != null)
        {
            StopCoroutine(closeCoroutine);
            closeCoroutine = null;
        }

        // 關閉 Panel
        if (targetPanel != null)
        {
            targetPanel.SetActive(false);
        }

        // 如果我是當前的紀錄保持者，清空紀錄
        if (currentActiveBtn == this)
        {
            currentActiveBtn = null;
        }
    }

    // 額外保險：如果按鈕本身被關閉或隱藏，確保 Panel 也重置 (視需求而定)
    private void OnDisable()
    {
        if (targetPanel != null) targetPanel.SetActive(false);
    }
}