using UnityEngine;
using DG.Tweening; // [!!] 記得要 using DOTween [!!]
using UnityEngine.UI; // 如果您的 uiPanel 包含 Image/Text
using TMPro; // [!!] 新增：為了使用 TextMeshPro
using System.Collections.Generic; // [!!] 新增：為了使用 Queue

public class PuzzleUnlockAnimator : MonoBehaviour
{
    public static PuzzleUnlockAnimator Instance { get; private set; }

    [Header("動畫設定")]
    [Tooltip("要移動的UI面板 (將它放在 Canvas 外的起始位置)")]
    public RectTransform uiPanel; // 要移動的 UI

    [Tooltip("您在 Canvas 內設定的『trans』空物件")]
    public RectTransform targetPosition; // TO (目標位置)

    // [!!] 新增：您在 uiPanel 上的 Text 物件 [!!]
    [Header("文字設定")]
    [Tooltip("在 uiPanel 上用於顯示標題的 TMP_Text")]
    public TMP_Text titleText;

    [Header("動畫參數")]
    public float slideInDuration = 0.5f; // 滑入時間
    public float displayDuration = 1.0f; // 顯示停留時間 (您要求的1秒)
    public float slideOutDuration = 0.5f; // 滑出時間
    public Ease easeIn = Ease.OutQuad; // 滑入的動畫曲線
    public Ease easeOut = Ease.InQuad; // 滑出的動畫曲線

    private Vector2 startPosition; // FROM (起始位置)
    private Vector2 targetPos;
    private Sequence currentSequence; // 用於管理當前的動畫

    // [!!] 核心修改：動畫佇列 [!!]
    private Queue<ClueCombinationPuzzle> puzzleAnimationQueue = new Queue<ClueCombinationPuzzle>();
    private bool isProcessingQueue = false; // 防止動畫重疊
    private bool isWaitingForVideo = false; // 是否正在等待影片信號

    void Awake()
    {
        // --- 設定 Singleton ---
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // (您可以選擇是否要 DontDestroyOnLoad)
            // DontDestroyOnLoad(gameObject);
        }

        // --- 儲存位置 ---
        // [重要!] 
        // 1. 確保 uiPanel 在 Awake 時是 active (可見) 狀態
        // 2. 確保 uiPanel 在編輯器中已放在「Canvas 外」的起始位置
        // 3. 確保 targetPosition 已放在「Canvas 內」的目標位置
        if (uiPanel == null || targetPosition == null)
        {
            Debug.LogError("[PuzzleUnlockAnimator] UI 面板或目標位置未設定！");
            return;
        }

        // 讀取 UI 的 RectTransform.anchoredPosition
        startPosition = uiPanel.anchoredPosition;
        targetPos = targetPosition.anchoredPosition;

        // 讀取完位置後，立刻隱藏 uiPanel
        uiPanel.gameObject.SetActive(false);
    }

    /// <summary>
    /// [!!] 新增：公開的入口函式 [!!]
    /// (由 CCM 呼叫) 
    /// </summary>
    /// <param name="puzzles">新解鎖謎題的「列表」</param>
    /// <param name="unlockType">觸發解鎖的類型</param>
    public void QueueNewUnlocks(List<ClueCombinationPuzzle> puzzles, EClueType unlockType)
    {
        // 1. 將所有新謎題加入佇列
        foreach (var p in puzzles)
        {
            puzzleAnimationQueue.Enqueue(p);
        }

        Debug.Log($"[PUA] 已將 {puzzles.Count} 個新謎題動畫加入佇列。");

        // 2. 決定是否立刻播放
        bool isVideoUnlock = (unlockType == EClueType.Memory || unlockType == EClueType.Sound);

        if (isVideoUnlock)
        {
            // 是回憶/聲音：設置「等待」標記
            isWaitingForVideo = true;
            Debug.Log("[PUA] 設置為等待影片信號...");
        }
        else
        {
            // 是物品：立刻開始處理佇列
            Debug.Log("[PUA] 立刻開始處理佇列...");
            ProcessQueue();
        }
    }

    /// <summary>
    /// [!!] 新增：處理佇列中的下一個動畫 [!!]
    /// </summary>
    private void ProcessQueue()
    {
        // 如果「正在播動畫」或「佇列是空的」，就返回
        if (isProcessingQueue || puzzleAnimationQueue.Count == 0)
        {
            return;
        }

        // 標記為「正在處理」
        isProcessingQueue = true;

        // 從佇列中取出「下一個」謎題
        ClueCombinationPuzzle puzzle = puzzleAnimationQueue.Dequeue();

        // 播放這個謎題的動畫
        PlaySingleAnimation(puzzle);
    }

    /// <summary>
    /// [!!] 修改：舊的 PlayUnlockAnimation [!!]
    /// 現在是私有的，並且播放「單一」謎題的動畫
    /// </summary>
    private void PlaySingleAnimation(ClueCombinationPuzzle puzzle)
    {
        // 1. [!!] 設定您要的文字 [!!]
        titleText.text = $"已更新資訊組合—[ {puzzle.puzzleTitle} ]";

        // 2. 重置並顯示面板
        uiPanel.anchoredPosition = startPosition;
        uiPanel.gameObject.SetActive(true);

        // 3. 建立動畫序列
        currentSequence = DOTween.Sequence();
        currentSequence.Append(uiPanel.DOAnchorPos(targetPos, slideInDuration).SetEase(easeIn));
        currentSequence.AppendInterval(displayDuration);
        currentSequence.Append(uiPanel.DOAnchorPos(startPosition, slideOutDuration).SetEase(easeOut));

        // 4. [!!] 關鍵 [!!] 動畫播完後...
        currentSequence.OnComplete(() =>
        {
            uiPanel.gameObject.SetActive(false);
            isProcessingQueue = false; // 解除「正在處理」標記

            // [!!] 呼叫 ProcessQueue() 來播放「下一個」動畫 (如果有的話)
            ProcessQueue();
        });
    }

    /// <summary>
    /// [!!] 新增 [!!] 
    /// 接收來自 VideoPlayerController 的「影片播畢」信號
    /// </summary>
    public void OnVideoPlaybackFinished()
    {
        Debug.Log("[PUA] 收到影片結束信號。");

        if (isWaitingForVideo)
        {
            // 如果是在等待影片
            isWaitingForVideo = false;
            // 現在開始處理佇列
            ProcessQueue();
        }
    }
}