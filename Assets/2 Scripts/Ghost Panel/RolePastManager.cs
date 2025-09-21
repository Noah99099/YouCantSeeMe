using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RolePastManager : MonoBehaviour
{
    [Header("功能：管理一個角色的多個 Carousel（含文本、大箭頭）")]
    // 一角色一方案，一方案多carousel -> 一個角色有多個carousel
    public RoleData[] allRoles; // 所有角色的資料（包含多個 CarouselData）
    public CarouselController carouselController; //CarouselController腳本
    public TMP_Text text1;
    public TMP_Text text2;

    [Header("大箭頭 UI（切換 CarouselData）")]
    public Button leftArrow; //上一個CarouselData的大箭頭
    public Button rightArrow; //下一個CarouselData的大箭頭

    private RoleData currentRole;
    private int currentCarouselIndex = 0; // 當前角色的第幾個 CarouselData

    private void Start()
    {
        // 一開始就顯示「方案 1」
        if (allRoles != null && allRoles.Length > 0)
        {
            ShowRole(allRoles[0]);
        }

        // 綁定大箭頭的點擊事件
        if (leftArrow != null)
            leftArrow.onClick.AddListener(PreviousCarousel);

        if (rightArrow != null)
            rightArrow.onClick.AddListener(NextCarousel);
    }

    public void ShowRole(RoleData role) //選擇一個角色的方案（由左側按鈕呼叫）
    {
        currentRole = role;
        currentCarouselIndex = 0;
        ShowCurrentCarousel();
    }

    public void NextCarousel() //切換到該角色的下一個 CarouselData（由大箭頭呼叫）
    {
        if (currentRole == null) return;

        currentCarouselIndex = (currentCarouselIndex + 1) % currentRole.carousels.Length;
        ShowCurrentCarousel();
    }

    public void PreviousCarousel() //切換到該角色的上一個 CarouselData（由大箭頭呼叫）
    {
        if (currentRole == null) return;

        currentCarouselIndex = (currentCarouselIndex - 1 + currentRole.carousels.Length) % currentRole.carousels.Length;
        ShowCurrentCarousel();
    }

    private void ShowCurrentCarousel() //顯示當前的 CarouselData（更新圖片與文字）
    {
        if (currentRole == null) return;
        if (currentRole.carousels.Length == 0) return;

        var carouselData = currentRole.carousels[currentCarouselIndex];

        // 更新右側 Carousel 的圖片
        carouselController.SetCarousel(carouselData.images);

        // 更新文本
        text1.text = carouselData.texts.Length > 0 ? carouselData.texts[0] : "";
        text2.text = carouselData.texts.Length > 1 ? carouselData.texts[1] : "";
    }
}
