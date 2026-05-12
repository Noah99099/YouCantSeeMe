using UnityEngine;
using System.Collections;
using System;
using Spine.Unity; // 確保有匯入 Spine Unity API

public class TestShow : MonoBehaviour
{
    [Header("UI 元件參考")]
    [Tooltip("包含 Spine 動畫的最上層 CanvasGroup，用來控制整體透明度")]
    public CanvasGroup targetCanvasGroup;
    [Tooltip("Spine 動畫的 RectTransform，用來控制位移")]
    public RectTransform spineUIRect;
    [Tooltip("Spine UI 動畫元件")]
    public SkeletonGraphic spineGraphic;

    [Header("位移設定")]
    [Tooltip("一開始在畫面外的位置")]
    public Vector2 offScreenPosition = new Vector2(0, -1500f);
    [Tooltip("上升到畫面正中間的位置")]
    public Vector2 centerPosition = Vector2.zero;
    public float moveDuration = 2.0f;

    [Header("動畫名稱設定 (請輸入對應的 Spine 動畫名稱)")]
    [SpineAnimation(dataField: "spineGraphic")] public string anim1Name = "Anim1";
    [SpineAnimation(dataField: "spineGraphic")] public string anim2Name = "Anim2";
    [SpineAnimation(dataField: "spineGraphic")] public string anim3Name = "Anim3";

    [Header("其他設定")]
    public float fadeOutDuration = 1.0f;

    // 用來記錄對話是否結束的標記
    private bool isDialogueFinished = false;

    private void Start()
    {
        // 遊戲開始即啟動展演流程
        StartCoroutine(ShowSequenceCoroutine());
    }

    private IEnumerator ShowSequenceCoroutine()
    {
        // ==========================================
        // 初始狀態設定
        // ==========================================
        targetCanvasGroup.alpha = 1f;
        targetCanvasGroup.gameObject.SetActive(true);
        spineUIRect.anchoredPosition = offScreenPosition;

        // 1. 一開始讓 map 從 Player 到 Loading
        if (InputStackManager.Instance != null)
        {
            InputStackManager.Instance.PushMap(InputActionMaps._Loading);
            Debug.Log("[TestShow] Map 已經切換至 Loading");
        }

        // ==========================================
        // 第一階段：進場與動畫切換
        // ==========================================
        // Spine UI 素材(動畫1) 循環播放 (為配合緩緩上升)
        spineGraphic.AnimationState.SetAnimation(0, anim1Name, true);

        // 緩緩上升到正中間
        yield return StartCoroutine(MoveRectPosition(spineUIRect, offScreenPosition, centerPosition, moveDuration));

        // 接著 Spine UI 素材切換成動畫2，循環播放
        spineGraphic.AnimationState.SetAnimation(0, anim2Name, true);

        // ==========================================
        // 第二階段：進行 TestShow1 對話
        // ==========================================
        // 註冊對話結束事件
        DialogueManager.Instance.OnConversationEnd += OnDialogueEnded;
        isDialogueFinished = false;

        // 呼叫 TestShow1 (DialogueManager 內部會自動 push "Dialogue" map)
        DialogueManager.Instance.TriggerDialogueByEvent("TestShow1");

        // 等待對話結束 (結束時 DialogueManager 內部會自動 pop 掉 "Dialogue"，回到 "Loading")
        yield return new WaitUntil(() => isDialogueFinished);
        DialogueManager.Instance.OnConversationEnd -= OnDialogueEnded;

        // ==========================================
        // 第三階段：對話結束後，等待動畫銜接
        // ==========================================
        // (目前 Map 自然退回至 Loading，確保玩家無法操作)

        // 等待動畫2(不循環)播放至結尾
        Spine.TrackEntry track2 = spineGraphic.AnimationState.SetAnimation(0, anim2Name, false);
        yield return new WaitForSeconds(track2.Animation.Duration);

        // 動畫1銜接(不循環)播放至結尾
        Spine.TrackEntry track1 = spineGraphic.AnimationState.SetAnimation(0, anim1Name, false);
        yield return new WaitForSeconds(track1.Animation.Duration);

        // 銜接動畫3，循環播放
        spineGraphic.AnimationState.SetAnimation(0, anim3Name, true);

        // ==========================================
        // 第四階段：進行 TestShow2 對話
        // ==========================================
        DialogueManager.Instance.OnConversationEnd += OnDialogueEnded;
        isDialogueFinished = false;

        DialogueManager.Instance.TriggerDialogueByEvent("TestShow2");

        yield return new WaitUntil(() => isDialogueFinished);
        DialogueManager.Instance.OnConversationEnd -= OnDialogueEnded;

        // ==========================================
        // 第五階段：對話結束後，切換 map 到 Dialogue，等待最後的動畫
        // ==========================================
        // 強制鎖定 Map 在 Dialogue 模式 (根據您的需求描述)
        if (InputStackManager.Instance != null)
        {
            InputStackManager.Instance.PushMap(InputActionMaps._Dialogue);
        }

        // 等待動畫3(不循環)播放至結尾
        Spine.TrackEntry track3 = spineGraphic.AnimationState.SetAnimation(0, anim3Name, false);
        yield return new WaitForSeconds(track3.Animation.Duration);

        // 動畫1銜接(不循環)播放至結尾
        track1 = spineGraphic.AnimationState.SetAnimation(0, anim1Name, false);
        yield return new WaitForSeconds(track1.Animation.Duration);

        // ==========================================
        // 第六階段：退場與淡出
        // ==========================================
        // 動畫1下降至一開始畫面外的地方
        yield return StartCoroutine(MoveRectPosition(spineUIRect, centerPosition, offScreenPosition, moveDuration));

        // Canva 的 alpha 逐漸從 1 降至 0 消失
        yield return StartCoroutine(FadeCanvasGroup(targetCanvasGroup, 1f, 0f, fadeOutDuration));

        // 最後關掉 Canva
        targetCanvasGroup.gameObject.SetActive(false);

        // ==========================================
        // 結尾處理與觸發正式遊戲
        // ==========================================
        // 由於我們前面手動 Push 了一層 Loading，後來又 Push 了一層 Dialogue，現在要全部 Pop 掉清空，歸還控制權
        if (InputStackManager.Instance != null)
        {
            InputStackManager.Instance.PopMap(); // Pop 掉剛才強制加上的 _Dialogue
            InputStackManager.Instance.PopMap(); // Pop 掉一開始的 _Loading
        }

        // 通過 DialogueEventTrigger 的邏輯呼叫 "StartGame" 對話
        DialogueManager.Instance.TriggerDialogueByEvent("StartGame");
    }

    /// <summary>
    /// 事件回調：用來改變 isDialogueFinished 狀態
    /// </summary>
    private void OnDialogueEnded()
    {
        isDialogueFinished = true;
    }

    /// <summary>
    /// UI 座標平滑移動的輔助協程
    /// </summary>
    private IEnumerator MoveRectPosition(RectTransform rect, Vector2 start, Vector2 end, float duration)
    {
        float timeElapsed = 0f;
        rect.anchoredPosition = start;

        while (timeElapsed < duration)
        {
            timeElapsed += Time.deltaTime;
            // 使用 SmoothStep 讓起步和結尾更平滑
            float t = Mathf.Clamp01(timeElapsed / duration);
            t = t * t * (3f - 2f * t);

            rect.anchoredPosition = Vector2.Lerp(start, end, t);
            yield return null;
        }

        rect.anchoredPosition = end;
    }

    /// <summary>
    /// CanvasGroup 透明度漸變的輔助協程
    /// </summary>
    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float startAlpha, float endAlpha, float duration)
    {
        float timeElapsed = 0f;
        cg.alpha = startAlpha;

        while (timeElapsed < duration)
        {
            timeElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(timeElapsed / duration);
            cg.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            yield return null;
        }

        cg.alpha = endAlpha;
    }
}