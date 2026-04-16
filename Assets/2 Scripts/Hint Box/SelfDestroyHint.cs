using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections;

public class SelfDestroyHint : MonoBehaviour
{
    [Header("UI 參照")]
    [Tooltip("要移動的面板 (拖入 Hint Panel)")]
    [SerializeField] private RectTransform uiPanel;
    [SerializeField] private TextMeshProUGUI hintText;

    [Header("動畫定位點")]
    [Tooltip("滑入的目標位置 (拖入 HintTrans)")]
    [SerializeField] private RectTransform targetTransform;

    [Header("動畫參數")]
    public float slideInDuration = 0.5f;
    [Tooltip("提示框維持在畫面上的時間")]
    public float displayDuration = 2.0f; // <--- 新增這行
    public float slideOutDuration = 0.5f;
    public Ease easeIn = Ease.OutQuad;
    public Ease easeOut = Ease.InQuad;

    private Vector2 startPos;
    private Vector2 targetPos;
    private Sequence currentSequence;

    /// <summary>
    /// 初始化文字並開始生命週期
    /// </summary>
    public void InitAndShow(string message)
    {
        if (hintText != null) hintText.text = message;

        if (uiPanel == null || targetTransform == null)
        {
            Debug.LogError("SelfDestroyHint: 尚未綁定 Hint Panel 或 HintTrans！");
            return;
        }

        // 1. 動態抓取座標
        // 抓取 Hint Panel 目前的位置 (你可以在 Prefab 裡把它先擺在畫面外)
        startPos = uiPanel.anchoredPosition;
        // 抓取 HintTrans 所在的座標當作終點
        targetPos = targetTransform.anchoredPosition;

        // 2. 建立 DOTween 序列
        currentSequence = DOTween.Sequence();

        // 滑入 (從外部移到 HintTrans 的位置)
        currentSequence.Append(uiPanel.DOAnchorPos(targetPos, slideInDuration).SetEase(easeIn));

        // 停留
        // 直接使用 Inspector 設定的時間
        currentSequence.AppendInterval(displayDuration); // <--- 修改這裡

        // 滑出 (從 HintTrans 的位置退回一開始的外部位置)
        currentSequence.Append(uiPanel.DOAnchorPos(startPos, slideOutDuration).SetEase(easeOut));

        // 3. 播完後銷毀
        currentSequence.OnComplete(() =>
        {
            // 注意：這裡是銷毀 gameObject，也就是整個 RightHintPanel (根物件)
            Destroy(gameObject);
        });
    }

    private void OnDestroy()
    {
        // 防呆機制
        if (currentSequence != null && currentSequence.IsActive())
        {
            currentSequence.Kill();
        }
    }
}