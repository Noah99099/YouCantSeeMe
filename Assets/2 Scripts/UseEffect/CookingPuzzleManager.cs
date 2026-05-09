using UnityEngine;
using System.Collections.Generic;

public class CookingPuzzleManager : MonoBehaviour
{
    public static CookingPuzzleManager Instance { get; private set; }

    [Header("--- 菜單狀態 (Menu) ---")]
    public GameObject menu1;
    public GameObject menu2;

    [Header("--- 第一階段設定 ---")]
    [Header("判定點 (Spot 1-6)")] // 冷盤*1、熱菜*2、主食*2、飲料*1
    // 這裡我們只需要得知這些點"是否填滿"，不需要管其內容是什麼。
    // 邏輯會在 Inspector 中透過 Event 綁定來呼叫 Manager 的方法。
    public GameObject[] stage1SpotsObjects; // 用於最後清除使用

    [Header("預先放置的料理模型 (需在 Inspector 拖入)")]
    public GameObject foodObj_ColdMeal; // (冷盤模型判定點)
    public GameObject foodObj_Drink; // 飲料 特別提出是因為需要對其進行加工-調味 (冷盤模型判定點)
    public GameObject[] foodObj_HotMealsAndMainDishs; // 共4個，熱菜*2+主食*2。用於最後清除 (多個模型判定點)

    [Header("完成後給予 (Stage 1 Complete)")]
    public GameObject infoPaper_1;

    [Header("--- 第二階段設定 ---")]
    [Header("判定點 (Spot 7-11)")] // 濃湯*1、甜點*1、主餐*1、配菜*1、水果*1
    public GameObject[] stage2SpotsRoots; // 用於開啟 SetActive 與最後清除

    public GameObject foodObj_Soup; // 需要加工-撒鹽
    public GameObject foodObj_Dessert; // 需要加工-淋醬
    public GameObject[] foodObj_VegStableFruits; // 共3個，青菜、主餐、水果。用於最後清除。

    [Header("完成後給予 (Stage 2 Complete)")]
    public GameObject infoPaper_2;
    public GameObject infoPaper_3;

    // ----- 內部狀態變數 -----

    // 階段一進度 (需要 6 個物理 + 1 個加工)
    private bool spot1_Done, spot2_Done, spot3_Done, spot4_Done, spot5_Done, spot6_Done;
    private bool foodF_Seasoned;

    // 階段二進度 (需要 5 個物理 + 2 個加工)
    private bool spot7_Done, spot8_Done, spot9_Done, spot10_Done, spot11_Done;
    private bool foodH_Seasoned, foodJ_Seasoned;

    // 階段狀態
    private bool isStage1Complete = false;
    private bool isStage2Complete = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        InitializePuzzle();
    }

    private void InitializePuzzle()
    {
        // 初始化菜單狀態：開啟第一階段 menu1，關閉 menu2
        SetMenuState(menu1, true);
        SetMenuState(menu2, false);

        // 1. 隱藏第二階段的所有判定點
        foreach (var spot in stage2SpotsRoots) spot.SetActive(false);

        // 2. 隱藏所有資訊紙條
        if (infoPaper_1) infoPaper_1.SetActive(false);
        if (infoPaper_2) infoPaper_2.SetActive(false);
        if (infoPaper_3) infoPaper_3.SetActive(false);

        // 3. 確保預設料理模型是隱藏的 (雖然在編輯器可能已經隱藏，但這裡做保險)
        // 注意：這裡只確保邏輯上的隱藏，實際開啟會由事件觸發
        if (foodObj_ColdMeal) foodObj_ColdMeal.SetActive(false);
        if (foodObj_Drink) foodObj_Drink.SetActive(false);
        if (foodObj_Soup) foodObj_Soup.SetActive(false);
        if (foodObj_Dessert) foodObj_Dessert.SetActive(false);
    }

    // 封裝一個方法來同時控制 遊戲物件 與 視圖處理器 開關
    private void SetMenuState(GameObject menuObj, bool isActive)
    {
        if (menuObj == null) return;

        menuObj.SetActive(isActive);
        var handler = menuObj.GetComponent<ViewMeshHandler>();
        if (handler != null)
        {
            handler.enabled = isActive;
            // 如果是在遊戲中切換，手動觸發一次視圖更新
            if (isActive && ViewManager.Instance != null)
                handler.OnViewChanged(ViewManager.Instance.CurrentView);
        }
    }

    // ==========================================
    // 外部事件對接：由 Spot 與 Food 的 Event 呼叫
    // ==========================================

    #region 第一階段邏輯

    // 當 Spot 1 (InteractableObject) 成功放置 冷盤
    public void OnSpot1Filled()
    {
        spot1_Done = true;
        if (foodObj_ColdMeal) foodObj_ColdMeal.SetActive(true); // 顯示料理-冷盤
        CheckStage1();
    }

    // 當 Spot 2-5 (ItemPlacementSpot) 成功放置 B,C,D,E
    // 這些是生成物件，所以不需要我們 SetActive，物件自己會生成
    public void OnSpot2Filled() { spot2_Done = true; CheckStage1(); } // 熱菜1
    public void OnSpot3Filled() { spot3_Done = true; CheckStage1(); } // 熱菜2
    public void OnSpot4Filled() { spot4_Done = true; CheckStage1(); } // 主食1
    public void OnSpot5Filled() { spot5_Done = true; CheckStage1(); } // 主食2

    // 當 Spot 6 (InteractableObject) 成功放置 飲料
    public void OnSpot6Filled()
    {
        print("放置成功");
        spot6_Done = true;
        if (foodObj_Drink) foodObj_Drink.SetActive(true); // 顯示料理-飲料，此時玩家可以對其進行加工-淋糖漿
        CheckStage1();
    }

    // 對 料理-飲料 (InteractableObject) 使用 加工物件-糖漿後
    public void OnFoodDrinkSeasoned()
    {
        foodF_Seasoned = true;
        Debug.Log("料理 飲料 加工完成！");
        CheckStage1();
    }

    private void CheckStage1()
    {
        if (isStage1Complete) return;

        if (spot1_Done && spot2_Done && spot3_Done && spot4_Done && spot5_Done && spot6_Done && foodF_Seasoned)
        {
            CompleteStage1();
        }
    }

    private void CompleteStage1()
    {
        isStage1Complete = true;
        Debug.Log("階段一完成！清除物件，生成資訊紙條1");

        // 1. 清除階段一的所有判定點 (包括產生的模型 與 觸發判定點物件)
        foreach (var spot in stage1SpotsObjects) if (spot) Destroy(spot);

        // 2. 清除預設料理模型
        if (foodObj_ColdMeal) Destroy(foodObj_ColdMeal);
        if (foodObj_Drink) Destroy(foodObj_Drink); // 就算加工過也要清除
        foreach (var food in foodObj_HotMealsAndMainDishs) if (food) Destroy(food); // 清除那些原本在預置位子上的模型(如果有)

        // 3. 顯示資訊紙條1
        if (infoPaper_1) infoPaper_1.SetActive(true);

        // 注意：階段二的開啟是透過玩家撿起「紙條 a」後觸發
        // 你需要在 紙條 a 的 InteractableItem (拾起事件) 的 Event 中呼叫 StartStage2()
    }

    #endregion

    #region 第二階段邏輯

    // 由紙條 a 的拾起事件呼叫
    public void StartStage2()
    {
        // 進入階段二：關閉 menu1, 開啟 menu2
        SetMenuState(menu1, false);
        SetMenuState(menu2, true);

        Debug.Log("啟動階段二！");
        foreach (var spot in stage2SpotsRoots) spot.SetActive(true);
    }

    public void OnSpot7Filled() 
    {
        spot7_Done = true;
        if (foodObj_VegStableFruits[0]) foodObj_VegStableFruits[0].SetActive(true); // 顯示青菜
        CheckStage2(); 
    }
    public void OnSpot8Filled()
    {
        spot8_Done = true;
        if (foodObj_Soup) foodObj_Soup.SetActive(true); // 顯示後供加工
        CheckStage2();
    }
    public void OnSpot9Filled() 
    { 
        spot9_Done = true;
        if (foodObj_VegStableFruits[1]) foodObj_VegStableFruits[1].SetActive(true); // 顯示主餐
        CheckStage2(); 
    }
    public void OnSpot10Filled()
    {
        spot10_Done = true;
        if (foodObj_Dessert) foodObj_Dessert.SetActive(true); // 顯示後供加工
        CheckStage2();
    }
    public void OnSpot11Filled() 
    {
        spot11_Done = true;
        if (foodObj_VegStableFruits[2]) foodObj_VegStableFruits[2].SetActive(true); // 顯示水果
        CheckStage2(); 
    }

    public void OnFoodSoupSeasoned() { foodH_Seasoned = true; CheckStage2(); }
    public void OnFoodDessertSeasoned() { foodJ_Seasoned = true; CheckStage2(); }

    private void CheckStage2()
    {
        if (isStage2Complete) return;

        if (spot7_Done && spot8_Done && spot9_Done && spot10_Done && spot11_Done && foodH_Seasoned && foodJ_Seasoned)
        {
            CompleteStage2();
        }
    }

    private void CompleteStage2()
    {
        isStage2Complete = true;
        Debug.Log("階段二完成！清除物件，生成紙條 b, c");

        // 1. 清除階段二的所有判定點
        foreach (var spot in stage2SpotsRoots) if (spot) Destroy(spot);

        // 2. 清除料理模型
        if (foodObj_Soup) Destroy(foodObj_Soup);
        if (foodObj_Dessert) Destroy(foodObj_Dessert);
        foreach (var food in foodObj_VegStableFruits) if (food) Destroy(food);

        // 3. 顯示資訊紙條2、3
        if (infoPaper_2) infoPaper_2.SetActive(true);
        if (infoPaper_3) infoPaper_3.SetActive(true);
        
        // 觸發解謎成功的燈光或環境回饋
        KanWu.Systems.LightSystemManager.Instance.NotifyPuzzleSolved();

        // 【新增】解除對應空氣牆
        KanWu.Systems.WallSystemManager.Instance.NotifyPuzzleSolved();
    }

    #endregion
}