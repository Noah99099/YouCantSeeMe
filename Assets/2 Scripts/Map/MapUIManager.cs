using UnityEngine;
using TMPro;
using DG.Tweening;

public class MapUIManager : MonoBehaviour
{
    public static MapUIManager Instance;

    [Header("UI 參照 (地圖專用)")]
    [SerializeField] private RectTransform uiPanel;
    [SerializeField] private TextMeshProUGUI hintText;
    [Tooltip("滑入的目標位置 (空物件的 RectTransform)")]
    [SerializeField] private RectTransform targetTransform;

    [Header("動畫參數")]
    public float slideInDuration = 0.5f;
    public float displayDuration = 2.0f; 
    public float slideOutDuration = 0.5f;
    public Ease easeIn = Ease.OutQuad;
    public Ease easeOut = Ease.InQuad;

    private Vector2 startPos;
    private Vector2 targetPos;
    private Sequence currentSequence;

    private void Awake()
    {
        // 遵循場景內聚：每個場景有自己的 MapUIManager，不跨場景
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (uiPanel != null && targetTransform != null)
        {
            startPos = uiPanel.anchoredPosition;
            targetPos = targetTransform.anchoredPosition;
            uiPanel.gameObject.SetActive(false); // 初始隱藏
        }
    }

    public void ShowHint(string message)
    {
        if (uiPanel == null || targetTransform == null) return;

        uiPanel.gameObject.SetActive(true);
        if (hintText != null) hintText.text = message;

        if (currentSequence != null && currentSequence.IsActive())
        {
            currentSequence.Kill();
        }

        targetPos = targetTransform.anchoredPosition;
        uiPanel.anchoredPosition = startPos;

        currentSequence = DOTween.Sequence();
        currentSequence.Append(uiPanel.DOAnchorPos(targetPos, slideInDuration).SetEase(easeIn));
        currentSequence.AppendInterval(displayDuration);
        currentSequence.Append(uiPanel.DOAnchorPos(startPos, slideOutDuration).SetEase(easeOut));
        
        currentSequence.OnComplete(() => uiPanel.gameObject.SetActive(false));
    }
}