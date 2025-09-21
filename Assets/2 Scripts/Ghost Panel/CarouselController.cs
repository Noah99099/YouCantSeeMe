using UnityEngine;
using UnityEngine.UI;

public class CarouselController : MonoBehaviour
{
    [Header("功能：管理單一Carousel UI的腳本（僅圖片、小圈圈、圖片箭頭）")]
    public ScrollRect scrollRect; //用來平移圖片
    public RectTransform content; // ScrollRect 裡的 Content
    public Button leftArrow; //圖片往左的按鈕
    public Button rightArrow; //圖片往右的按鈕
    public Transform paginationPanel; // 小圈圈父物件
    public GameObject dotPrefab;      // 小圈圈Prefab

    [HideInInspector]
    public int currentIndex = 0; // 當前顯示圖片的索引

    private Image[] dots; // 用來儲存小圈圈 Image，方便改顏色

    void Start()
    {
        //綁上箭頭鍵的onClick
        leftArrow.onClick.AddListener(() => Move(-1));
        rightArrow.onClick.AddListener(() => Move(1));
    }

    public void SetCarousel(Sprite[] images) //設定一個新的 CarouselData（更新圖片與小圈圈）
    {
        // 清空舊圖片
        foreach (Transform child in content) Destroy(child.gameObject);

        // 清空舊小圈圈
        foreach (Transform child in paginationPanel) Destroy(child.gameObject);

        // 建立新圖片
        foreach (var img in images)
        {
            GameObject go = new GameObject("Image", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(content);
            var image = go.GetComponent<Image>();
            image.sprite = img;
            image.SetNativeSize();
        }

        // 建立小圈圈
        dots = new Image[images.Length];
        for (int i = 0; i < images.Length; i++)
        {
            GameObject dot = Instantiate(dotPrefab, paginationPanel);
            dots[i] = dot.GetComponent<Image>();
        }

        // 初始化顯示第一張圖片
        currentIndex = 0;
        UpdateDots();
        scrollRect.horizontalNormalizedPosition = 0;
    }

    void Move(int direction) //左右箭頭切換圖片的邏輯，移動到上一張或下一張圖片
    {
        if (dots == null || dots.Length == 0) return;

        currentIndex += direction;
        currentIndex = Mathf.Clamp(currentIndex, 0, dots.Length - 1);

        float targetPos = (dots.Length == 1) ? 0 : (float)currentIndex / (dots.Length - 1);
        scrollRect.horizontalNormalizedPosition = targetPos;

        UpdateDots();
    }

    void UpdateDots() //更新小圈圈顏色（白色 = 當前，灰色 = 其他）
    {
        if (dots == null) return;

        for (int i = 0; i < dots.Length; i++)
            dots[i].color = (i == currentIndex) ? Color.white : Color.gray;
    }
}
