using UnityEngine;
using UnityEngine.UI;
using TMPro; // 如果你使用的是 TextMeshPro
using DG.Tweening; // 使用 DOTween [cite: 10]

public class TeleportHintManager : MonoBehaviour
{
    public static TeleportHintManager Instance;

    [Header("UI 元件")]
    public Text hintText; // 如果是 TMP 請改成 public TextMeshProUGUI hintText;
    public CanvasGroup canvasGroup; // 建議給 Hint Panel 加一個 CanvasGroup 方便控制透明度

    private void Awake()
    {
        Instance = this;
        // 初始隱藏
        if (canvasGroup != null) canvasGroup.alpha = 0;
        gameObject.SetActive(false);
    }

    public void ShowUnlockHint(string pointName)
    {
        gameObject.SetActive(true);
        hintText.text = $"已解鎖{pointName}傳送點，地圖即可查看";

        // DOTween 動畫邏輯：淡入 -> 停留 -> 淡出
        Sequence hintSeq = DOTween.Sequence();
        hintSeq.Append(canvasGroup.DOFade(1, 0.5f))
               .AppendInterval(2.0f) // 停留 2 秒
               .Append(canvasGroup.DOFade(0, 0.5f))
               .OnComplete(() => gameObject.SetActive(false));
    }
}