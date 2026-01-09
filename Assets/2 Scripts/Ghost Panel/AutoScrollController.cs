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

    private void Start()
    {
        StartCoroutine(ScrollRoutine());
    }

    IEnumerator ScrollRoutine()
    {
        // 初始狀態確保是隱藏的
        contentCanvasGroup.alpha = 0f;
        arrowUp.SetActive(false);
        arrowDown.SetActive(false);

        while (true)
        {
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
            while (scrollRect.verticalNormalizedPosition > 0f)
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
            // 使用 Lerp 根據時間流逝計算當前的 alpha 值
            contentCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            yield return null;
        }
        contentCanvasGroup.alpha = endAlpha;
    }

    void UpdateArrows(float normPos)
    {
        // 如果內容正在淡出（Alpha 變低），我們可以選擇讓箭頭跟著消失
        // 或者單純在 FadeCanvas 結束後統一關閉

        // 頂部 (接近 1)
        if (normPos >= 0.99f)
        {
            arrowUp.SetActive(false);
            arrowDown.SetActive(true);
        }
        // 底部 (接近 0)
        else if (normPos <= 0.01f)
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