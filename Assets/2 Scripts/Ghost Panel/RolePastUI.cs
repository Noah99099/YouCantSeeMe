using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

// [!!] 這是「純 UI」顯示器 [!!]
// 把它放在您的 "案件紀錄簿" Prefab 上
public class RolePastUI : MonoBehaviour
{
    // [!!] 這是您所有的 UI 引用 [!!]
    [Header("UI 引用")]
    public CarouselController carouselController;
    public TMP_Text text1;
    public TMP_Text text2;

    [Header("大箭頭 UI（切換 CarouselData）")]
    public Button leftArrow;
    public Button rightArrow;

    [Header("右側 UI 父物件")]
    public GameObject rightInformation;

    // --- 私有狀態 (僅供 UI 使用) ---
    private RoleData currentRole;
    private int currentCarouselIndex = 0;
    private List<CarouselData> _currentRoleUnlockedList = new List<CarouselData>();

    // [!!] 重要 [!!]
    // 您的「角色按鈕」(例如 Role1, Role2) 的 OnClick() 事件
    // 現在應該呼叫這個函式
    public void ShowRole(RoleData role)
    {
        Debug.Log($"[RolePastUI] 按鈕點擊，要求顯示 '{role.name}'");
        currentRole = role;
        currentCarouselIndex = 0;

        // 刷新 UI 用的列表
        UpdateUnlockedListForCurrentRole();

        // 顯示 UI
        ShowCurrentCarousel();
    }

    /// <summary>
    /// 刷新 _currentRoleUnlockedList
    /// </summary>
    private void UpdateUnlockedListForCurrentRole()
    {
        _currentRoleUnlockedList.Clear();

        if (currentRole == null) return;

        // [!!] 核心 [!!] 
        // 向「純邏輯」管理器「讀取」資料
        if (RolePastManager.Instance != null && RolePastManager.Instance.unlockedMemories.ContainsKey(currentRole))
        {
            Debug.Log($"[RolePastUI] 成功從 RPM.Instance 讀取到 '{currentRole.name}' 的資料。");
            _currentRoleUnlockedList = RolePastManager.Instance.unlockedMemories[currentRole].ToList();
        }
        else
        {
            Debug.LogWarning($"[RolePastUI] 在 RPM.Instance 中找不到 Key '{currentRole.name}'！");
        }
    }

    public void NextCarousel()
    {
        if (_currentRoleUnlockedList.Count == 0) return;
        currentCarouselIndex = (currentCarouselIndex + 1) % _currentRoleUnlockedList.Count;
        ShowCurrentCarousel();
    }

    public void PreviousCarousel()
    {
        if (_currentRoleUnlockedList.Count == 0) return;
        currentCarouselIndex = (currentCarouselIndex - 1 + _currentRoleUnlockedList.Count) % _currentRoleUnlockedList.Count;
        ShowCurrentCarousel();
    }

    private void ShowCurrentCarousel()
    {
        if (_currentRoleUnlockedList.Count == 0)
        {
            rightInformation.SetActive(false); // 沒有資料 → 隱藏右側
            return;
        }

        rightInformation.SetActive(true); // 有資料 → 顯示右側
        var carouselData = _currentRoleUnlockedList[currentCarouselIndex];

        carouselController.SetCarousel(carouselData.images);
        text1.text = carouselData.texts.Length > 0 ? carouselData.texts[0] : "";
        text2.text = carouselData.texts.Length > 1 ? carouselData.texts[1] : "";

        bool showArrows = _currentRoleUnlockedList.Count > 1;
        if (leftArrow != null) leftArrow.gameObject.SetActive(showArrows);
        if (rightArrow != null) rightArrow.gameObject.SetActive(showArrows);
    }
}