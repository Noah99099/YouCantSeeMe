using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class AutoScrollController : MonoBehaviour
{
    [Header("組件引用")]
    public ScrollRect scrollRect;
    public GameObject arrowUp;
    public GameObject arrowDown;
    public CanvasGroup contentCanvasGroup; // 用於控制內容消失

    [Header("時間設定")]
    public float waitAtTopTime = 2f;    // 頂部停留
    public float scrollSpeed = 0.1f;    // 滾動速度
    public float waitAtBottomTime = 2f; // 底部停留
    public float fadeDuration = 1f;      // 淡入淡出的持續時間

    private Coroutine mainRoutine; // 用來追蹤當前的協程

    private void OnEnable()
    {
        // 強制重置所有 UI 狀態，防止被上次關閉時的狀態干擾
        StopAllCoroutines();

        contentCanvasGroup.alpha = 0f;
        arrowUp.SetActive(false);
        arrowDown.SetActive(false);
        scrollRect.verticalNormalizedPosition = 1f;

        // 啟動主循環
        mainRoutine = StartCoroutine(ScrollRoutine());
    }

    private void OnDisable()
    {
        // 物件關閉時強制停止，避免後台出錯
        StopAllCoroutines();
    }

    IEnumerator ScrollRoutine()
    {
        // 關鍵：等待 0.1 秒或 1 幀，確保 Layout 組件已經計算出正確的 Content 高度
        // 確保 Layout 已經計算出正確的高度（非常重要！）
        yield return new WaitForEndOfFrame();

        while (true)
        {
            // --- 判定：內容是否超出可視範圍？ ---
            // content 總高度 <= scrollRect 視窗高度
            if (scrollRect.content.rect.height <= scrollRect.viewport.rect.height)
            {
                // 情況 A: 內容沒超出範圍，直接顯示後待命
                scrollRect.verticalNormalizedPosition = 1f;
                arrowUp.SetActive(false);
                arrowDown.SetActive(false);

                // 如果你希望沒超出時也要有淡入效果，保留這行
                yield return StartCoroutine(FadeCanvas(0f, 1f, fadeDuration));
                contentCanvasGroup.alpha = 1f;

                // 在這裡「卡住」協程，直到內容變動或物件重啟
                // 或者你可以選擇 yield break 結束協程
                yield return new WaitUntil(() => scrollRect.content.rect.height > scrollRect.viewport.rect.height);
                // 一旦內容變多了，會跳出 WaitUntil 繼續往下執行循環
            }

            // --- 情況 B: 內容超出範圍，執行原有的滾動邏輯 ---

            // 1. --- 初始位置歸位 (隱藏狀態下先回到頂部) ---
            scrollRect.verticalNormalizedPosition = 1f;

            // 2. --- 淡入 (0 -> 1) ---
            // 在淡入開始時，先根據位置設定好初始箭頭（此時應只有下箭頭）
            UpdateArrows(1f);
            yield return StartCoroutine(FadeCanvas(0f, 1f, fadeDuration));
            contentCanvasGroup.alpha = 1f;

            // 3. --- 頂部停留 ---
            yield return new WaitForSeconds(waitAtTopTime);

            // 4. --- 自動滾動過程 ---
            // 修改判斷條件：改用 >= 0 且手動限制範圍，防止數值溢出
            while (scrollRect.verticalNormalizedPosition > 0.001f)
            {
                scrollRect.verticalNormalizedPosition -= scrollSpeed * Time.deltaTime;
                UpdateArrows(scrollRect.verticalNormalizedPosition);
                yield return null;
            }
            scrollRect.verticalNormalizedPosition = 0f;
            UpdateArrows(0f);

            // 5. --- 底部停留 ---
            yield return new WaitForSeconds(waitAtBottomTime);

            // 6. --- 淡出 (1 -> 0) ---
            // 淡出的同時關閉箭頭，視覺上更乾淨
            yield return StartCoroutine(FadeCanvas(1f, 0f, fadeDuration));
            contentCanvasGroup.alpha = 0f; // 強制設為 0

            arrowUp.SetActive(false);
            arrowDown.SetActive(false);

            // 7. --- 循環間隔 (完全消失後等幾秒) ---
            yield return new WaitForSeconds(1f);
        }
    }

    // 專門處理淡入淡出的協程
    IEnumerator FadeCanvas(float startAlpha, float endAlpha, float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            // 使用更穩定的平滑插值
            float t = Mathf.Clamp01(elapsedTime / duration);
            contentCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            yield return null;
        }
        contentCanvasGroup.alpha = endAlpha;
    }

    void UpdateArrows(float normPos)
    {
        // 如果內容沒超出範圍，不顯示任何箭頭
        if (scrollRect.content.rect.height <= scrollRect.viewport.rect.height)
        {
            arrowUp.SetActive(false);
            arrowDown.SetActive(false);
            return;
        }

        // 如果內容正在淡出（Alpha 變低），我們可以選擇讓箭頭跟著消失
        // 或者單純在 FadeCanvas 結束後統一關閉
        // 增加容錯區間
        bool isAtTop = normPos >= 0.95f;
        bool isAtBottom = normPos <= 0.05f;

        // 頂部 (接近 1)
        if (isAtTop)
        {
            arrowUp.SetActive(false);
            arrowDown.SetActive(true);
        }
        // 底部 (接近 0)
        else if (isAtBottom)
        {
            arrowUp.SetActive(true);
            arrowDown.SetActive(false);
        }
        // 過程中
        else
        {
            arrowUp.SetActive(true);
            arrowDown.SetActive(true);
        }
    }
}