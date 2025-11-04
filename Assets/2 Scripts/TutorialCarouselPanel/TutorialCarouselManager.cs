// TutorialCarouselManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro; // 記得引入 TextMeshPro
using System.Collections; // 為了 IEnumator (雖然這個版本沒用到，但備用方案會用到)

/// <summary>
/// 專門管理教學指示 (Tutorial) 的 Carousel 腳本。
/// 獨立於 CarouselController 和 RolePastManager。
/// </summary>
public class TutorialCarouselManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("要顯示/隱藏的教學面板根物件")]
    public GameObject tutorialPanelObject;
    public TMP_Text titleText;           // 標題文本 (教學1, 教學2...)
    public TMP_Text contentText;         // 內容文本 (內容文本1-1, 1-2...)
    public ScrollRect scrollRect;
    public RectTransform contentRect;    // ScrollRect 裡的 Content
    public Button leftArrow;
    public Button rightArrow;
    public Transform paginationPanel;    // 小圈圈 (dot) 的父物件
    public Button closeButton;           // 關閉按鈕 (強烈建議要有)

    [Header("Prefabs")]
    public GameObject dotPrefab;         // 小圈圈的 Prefab
    [Tooltip("一個包含 Image 元件的 Prefab，用來在 ScrollRect 中實例化")]
    public GameObject imagePrefab;       // 要在 Content 中生成的圖片 Prefab

    [Header("Pagination Colors")]
    public Color dotActiveColor;
    public Color dotInactiveColor;

    private TutorialData currentTutorial;
    private Image[] dots;
    private int currentIndex = 0;

    void Awake()
    {
        // 綁定按鈕事件
        leftArrow.onClick.AddListener(() => Move(-1));
        rightArrow.onClick.AddListener(() => Move(1));

        if (closeButton != null)
            closeButton.onClick.AddListener(HideTutorial);

        // 確保面板一開始是隱藏的
        if (tutorialPanelObject == null)
            tutorialPanelObject = this.gameObject;

        tutorialPanelObject.SetActive(false);
    }

    /// <summary>
    /// [Public] 外部呼叫此方法來顯示並載入指定的教學內容。
    /// </summary>
    public void ShowTutorial(TutorialData tutorialData)
    {
        InputStackManager.Instance.PushMap(InputActionMaps._Tutorial);

        if (tutorialData == null || tutorialData.slides == null)
        {
            Debug.LogWarning("傳入的 TutorialData 為空或沒有任何 slides。");
            return;
        }

        currentTutorial = tutorialData;
        tutorialPanelObject.SetActive(true); // 顯示面板

        SetupCarouselUI(); // 設定 UI 內容
    }

    /// <summary>
    /// [Public] 隱藏教學面板。
    /// </summary>
    public void HideTutorial()
    {
        InputStackManager.Instance.Init(InputActionMaps._Player);
        tutorialPanelObject.SetActive(false);
    }

    /// <summary>
    /// 根據 currentTutorial 的資料，完整設定 Carousel 的所有 UI 元件。
    /// </summary>
    private void SetupCarouselUI()
    {
        // 1. 設定標題
        titleText.text = currentTutorial.title;

        // 2. 清空舊的圖片和 Dots
        foreach (Transform child in contentRect) Destroy(child.gameObject);
        foreach (Transform child in paginationPanel) Destroy(child.gameObject);

        // 檢查是否有 slides
        if (currentTutorial.slides.Length == 0)
        {
            contentText.text = ""; // 清空內文
            dots = new Image[0];   // 建立空陣列
            UpdateUIForCurrentIndex(); // 更新 UI (會隱藏箭頭)
            return;
        }

        // 3. 根據 slides 建立新的圖片
        foreach (var slide in currentTutorial.slides)
        {
            // 你需要一個簡單的 Prefab (GameObject + Image)
            GameObject imgObj = Instantiate(imagePrefab, contentRect);
            Image img = imgObj.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = slide.image;
                img.preserveAspect = true; // 保持圖片比例
                // img.SetNativeSize(); // 或者使用這個
            }
        }

        // 建議: 在 contentRect (Content 物件) 上掛載一個 HorizontalLayoutGroup
        // 並設定 Child Alignment, Spacing, Padding，這樣圖片會自動排好。

        // --- 解決方案：在這裡加入 ---
        // 強制 contentRect 立即重新計算其排版和大小
        // (記得你的腳本頂部要有 using UnityEngine.UI;)
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        // --- 結束 ---

        // 4. 建立新的 Dots
        dots = new Image[currentTutorial.slides.Length];
        for (int i = 0; i < currentTutorial.slides.Length; i++)
        {
            GameObject dot = Instantiate(dotPrefab, paginationPanel); //報錯
            //dots[i] = dot.GetComponent<Image>();

            Image dotImage = dot.GetComponent<Image>(); //因為沒有正確打開所以改這樣

            if (dotImage != null)
            {
                dotImage.enabled = true; // <-- 在這裡強制啟用
                dots[i] = dotImage;
            }
            else
            {
                // 如果 prefab 錯誤，dots[i] 會是 null，並在這裡報錯
                Debug.LogError("dotPrefab 缺少 Image 元件！", dot);
            }

        }

        // 5. 初始化到第一頁
        currentIndex = 0;
        scrollRect.horizontalNormalizedPosition = 0; // 捲動到最左邊
        UpdateUIForCurrentIndex(); // 更新文本、Dots 和箭頭
    }

    /// <summary>
    /// 處理左右箭頭點擊事件
    /// </summary>
    private void Move(int direction)
    {
        if (dots == null || dots.Length == 0) return;

        int newIndex = currentIndex + direction;

        // 確保索引在範圍內
        newIndex = Mathf.Clamp(newIndex, 0, dots.Length - 1);

        if (newIndex == currentIndex) return; // 已經在最邊緣，不動作

        currentIndex = newIndex;

        // [核心] 滾動 ScrollRect 到目標位置
        // 演算法：(float)currentIndex / (總數 - 1)
        float targetPos = (dots.Length == 1) ? 0 : (float)currentIndex / (dots.Length - 1);

        // 為了平滑捲動，我們可以使用 Coroutine (可選，但效果更好)
        // scrollRect.horizontalNormalizedPosition = targetPos; // 這是瞬間移動
        StopAllCoroutines(); // 停止任何正在進行的滾動
        StartCoroutine(SmoothScrollTo(targetPos));

        // 更新 UI
        UpdateUIForCurrentIndex();
    }

    /// <summary>
    /// 平滑滾動到目標位置
    /// </summary>
    private System.Collections.IEnumerator SmoothScrollTo(float targetPos)
    {
        float startPos = scrollRect.horizontalNormalizedPosition;
        float timer = 0f;
        float duration = 0.25f; // 滾動動畫時間 (秒)

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime; // 使用 unscaledDeltaTime 避免受 Time.timeScale 影響
            float t = Mathf.Clamp01(timer / duration);
            t = 1f - (1f - t) * (1f - t); // Ease Out 效果
            scrollRect.horizontalNormalizedPosition = Mathf.Lerp(startPos, targetPos, t);
            yield return null;
        }
        scrollRect.horizontalNormalizedPosition = targetPos;
    }


    /// <summary>
    /// 根據 currentIndex 更新「內容文本」、「Dot 顏色」和「箭頭可否點擊」。
    /// </summary>
    private void UpdateUIForCurrentIndex()
    {
        // 處理沒有 slides 的邊界情況
        if (dots == null || dots.Length == 0 || currentTutorial == null)
        {
            contentText.text = "";
            leftArrow.interactable = false;
            rightArrow.interactable = false;
            return;
        }

        // 確保 currentIndex 總是在合法範圍
        if (currentIndex < 0 || currentIndex >= currentTutorial.slides.Length)
        {
            Debug.LogError($"教學系統錯誤：CurrentIndex ({currentIndex}) 超出範圍！");
            return;
        }

        // 1. [核心] 更新內容文本
        contentText.text = currentTutorial.slides[currentIndex].contentText;

        // 2. 更新 Dots 顏色
        for (int i = 0; i < dots.Length; i++)
        {
            // --- [健壯性修改] ---
            // 增加 Null 檢查，避免 prefab 錯誤時導致報錯
            if (dots[i] != null)
            {
                dots[i].color = (i == currentIndex) ? dotActiveColor : dotInactiveColor;
            }
            // --- 結束修改 ---
        }

        // 3. 更新箭頭可否點擊 (在第一頁不能按左，在最後一頁不能按右)
        leftArrow.interactable = (currentIndex > 0);
        rightArrow.interactable = (currentIndex < dots.Length - 1);
    }
}