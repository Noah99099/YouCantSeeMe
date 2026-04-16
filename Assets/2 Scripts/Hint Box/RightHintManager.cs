using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections;
using UnityEngine.SceneManagement; // 必須引用場景管理

public class RightHintManager : MonoBehaviour
{
    public static RightHintManager Instance;

    [Header("UI 參照 (Scene 1 專用)")]
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

    private void Awake()
    {
        // 單純的單例模式，移除跨場景保留，避免到 Scene 2 報錯
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (uiPanel != null && targetTransform != null)
        {
            // 初始狀態：紀錄「編輯器中擺放的位置」作為起始點，紀錄「HintTrans」作為目標點
            startPos = uiPanel.anchoredPosition;
            targetPos = targetTransform.anchoredPosition;

            // 初始先隱藏，避免擋住畫面
            uiPanel.gameObject.SetActive(false);
        }
    }

    // 將第二個參數加上預設值 -1，代表「如果沒特別指定，就用 Inspector 設定的時間」
    public void ShowHint(string message, float customDuration = -1f)
    {
        if (uiPanel == null || targetTransform == null) return;

        // 顯示物件並更新文字
        uiPanel.gameObject.SetActive(true);
        if (hintText != null) hintText.text = message;

        // 如果先前有動畫正在播，先停掉重來
        if (currentSequence != null && currentSequence.IsActive())
        {
            currentSequence.Kill();
        }

        // 每次播放前重新確認座標 (預防解析度改變或 UI 縮放)
        targetPos = targetTransform.anchoredPosition;
        uiPanel.anchoredPosition = startPos;

        // 決定這次要停留多久
        float waitTime = (customDuration < 0f) ? displayDuration : customDuration;

        // 建立動畫序列
        currentSequence = DOTween.Sequence();

        // 1. 滑入目標點
        currentSequence.Append(uiPanel.DOAnchorPos(targetPos, slideInDuration).SetEase(easeIn));

        // 2. 停留
        // 使用計算好的停留時間
        currentSequence.AppendInterval(waitTime); // <--- 修改這裡

        // 3. 滑出回起始點
        currentSequence.Append(uiPanel.DOAnchorPos(startPos, slideOutDuration).SetEase(easeOut));

        // 4. 完成後關閉
        currentSequence.OnComplete(() =>
        {
            uiPanel.gameObject.SetActive(false);
        });
    }
}
