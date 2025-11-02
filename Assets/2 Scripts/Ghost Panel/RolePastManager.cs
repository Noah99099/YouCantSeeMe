// RolePastManager.cs
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

    [Header("右側 UI 父物件")]
    public GameObject rightInformation; // 控制整個右側的開關

    private RoleData currentRole;
    private int currentCarouselIndex = 0; // 當前角色的第幾個 CarouselData

    private void Start()
    {
        //// 一開始就顯示「方案 1」
        //if (allRoles != null && allRoles.Length > 0)
        //{
        //    ShowRole(allRoles[0]);
        //}

        if (rightInformation == null)
        {
            Debug.LogError("RightInformation 沒有在 Inspector 綁定！");
        }

        // 預設隱藏右側，因為沒有一個Role有Carousel
        rightInformation.SetActive(false);

        // 這裡加檢查按鈕 1 對應的角色
        if (allRoles != null && allRoles.Length > 0)
        {
            RoleData role1 = allRoles[0];
            if (role1 != null && role1.carousels != null && role1.carousels.Length > 0)
            {
                // 如果 Role1 已經有 Carousels，就直接顯示右側資訊
                rightInformation.SetActive(true);
                ShowRole(role1);
            }
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
        if (currentRole == null || currentRole.carousels.Length == 0) return;

        currentCarouselIndex = (currentCarouselIndex + 1) % currentRole.carousels.Length;
        ShowCurrentCarousel();
    }

    public void PreviousCarousel() //切換到該角色的上一個 CarouselData（由大箭頭呼叫）
    {
        if (currentRole == null || currentRole.carousels.Length == 0) return;

        currentCarouselIndex = (currentCarouselIndex - 1 + currentRole.carousels.Length) % currentRole.carousels.Length;
        ShowCurrentCarousel();
    }

    private void ShowCurrentCarousel() //顯示當前的 CarouselData（更新圖片與文字）
    {
        if (currentRole == null || currentRole.carousels.Length == 0)
        {
            rightInformation.SetActive(false); // 沒有資料 → 隱藏右側
            return;
        }

        rightInformation.SetActive(true); // 有資料 → 顯示右側
        var carouselData = currentRole.carousels[currentCarouselIndex];

        // 更新圖片
        carouselController.SetCarousel(carouselData.images);

        // 更新文本
        text1.text = carouselData.texts.Length > 0 ? carouselData.texts[0] : "";
        text2.text = carouselData.texts.Length > 1 ? carouselData.texts[1] : "";
    }

    /// <summary> 當互動物體解鎖新的 Carousel 時呼叫 </summary>
    public void AddCarouselToRole(RoleData role, CarouselData newCarousel)
    {
        role.AddCarousel(newCarousel);

        // 如果剛好是當前顯示的角色，刷新 UI
        if (role == currentRole)
        {
            ShowCurrentCarousel();
        }
    }
}
