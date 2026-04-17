using UnityEngine;
using DG.Tweening; // 必須引入 DOTween 的命名空間

public class BlinkEffect : MonoBehaviour
{
    [Header("眼皮 UI 參考")]
    public RectTransform topLid;
    public RectTransform bottomLid;

    [Header("眨眼設定")]
    public float blinkDuration = 0.15f;    // 單次閉眼/睜眼的時間 (秒)
    public float keepClosedDuration = 0.05f; // 閉眼保持的時間 (秒)
    public float closedYOffset = 540f;     // 閉眼時 Y 軸要移動的距離 (可依螢幕解析度微調)
    public Ease blinkEase = Ease.InOutQuad;  // 動畫曲線 (InOutQuad 讓開始與結束較平滑)

    private float topLidOpenY;
    private float bottomLidOpenY;

    void Start()
    {
        // 紀錄完全睜開時的初始 Y 軸位置 (確保遊戲開始時眼皮在畫外或預設打開的位置)
        topLidOpenY = topLid.anchoredPosition.y;
        bottomLidOpenY = bottomLid.anchoredPosition.y;
    }

    // 呼叫此方法來觸發眨眼
    public void PlayBlink()
    {
        // 1. 防止連續點擊造成動畫衝突，播放前先清掉眼皮上正在進行的 Tween 動畫
        topLid.DOKill();
        bottomLid.DOKill();

        // 2. 建立一個 DOTween 序列 (Sequence) 來串接動作
        Sequence blinkSequence = DOTween.Sequence();

        // 計算閉眼時的目的地 (上眼皮往下，下眼皮往上)
        float topClosedPos = topLidOpenY - closedYOffset;
        float bottomClosedPos = bottomLidOpenY + closedYOffset;

        // 3. 閉眼動作 (Join 表示兩個動畫同時進行)
        blinkSequence.Append(topLid.DOAnchorPosY(topClosedPos, blinkDuration).SetEase(blinkEase))
                     .Join(bottomLid.DOAnchorPosY(bottomClosedPos, blinkDuration).SetEase(blinkEase));

        // 4. 閉眼停留短暫時間
        blinkSequence.AppendInterval(keepClosedDuration);

        // 5. 睜眼動作 (回到初始位置)
        blinkSequence.Append(topLid.DOAnchorPosY(topLidOpenY, blinkDuration).SetEase(blinkEase))
                     .Join(bottomLid.DOAnchorPosY(bottomLidOpenY, blinkDuration).SetEase(blinkEase));
    }
}