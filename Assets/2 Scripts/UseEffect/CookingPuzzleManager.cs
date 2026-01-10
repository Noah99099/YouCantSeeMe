using UnityEngine;
using System.Collections.Generic;

public class CookingPuzzleManager : MonoBehaviour
{
    public static CookingPuzzleManager Instance { get; private set; }

    [Header("--- 菜單物件 (Menu) ---")]
    public GameObject menu1;
    public GameObject menu2;

    [Header("--- 階段一設定 ---")]
    [Header("判定點 (Spot 1-6)")] // 冷盤*1、熱菜*2、主菜*2、酒水*1
    // 這裡我們只需要知道這幾個點"是否完成"，不需要管它們具體是哪個腳本
    // 稍後會在 Inspector 中透過 Event 綁定來呼叫 Manager 的方法
    public GameObject[] stage1SpotsObjects; // 用於最後清除場景

    [Header("預先配置好的料理物件 (需在Inspector拉入)")]
    public GameObject foodObj_ColdMeal; // (單一物品放置判定點)
    public GameObject foodObj_Drink; // 酒水 特別重要，因為要對它加調味料-安眠藥 (單一物品放置判定點)
    public GameObject[] foodObj_HotMealsAndMainDishs; // 共4個，熱菜*2+主菜*2。用於最後清除 (多物品放置判定點)

    [Header("紙條獎勵 (Stage 1 Complete)")]
    public GameObject infoPaper_1;

    [Header("--- 階段二設定 ---")]
    [Header("判定點 (Spot 7-11)")] // 蔬菜*1、湯品*1、主食*1、甜品*1、水果*1
    public GameObject[] stage2SpotsRoots; // 用於控制 SetActive 和最後清除

    public GameObject foodObj_Soup; // 要加調味料-毒藥
    public GameObject foodObj_Dessert; // 要加調味料-芒果
    public GameObject[] foodObj_VegStableFruits; // 共3個，蔬菜、主食、水果。用於最後清除。

    [Header("紙條獎勵 (Stage 2 Complete)")]
    public GameObject infoPaper_2;
    public GameObject infoPaper_3;

    // ----- 內部狀態追蹤 -----

    // 階段一進度 (需要 6 個料理 + 1 個調味)
    private bool spot1_Done, spot2_Done, spot3_Done, spot4_Done, spot5_Done, spot6_Done;
    private bool foodF_Seasoned;

    // 階段二進度 (需要 5 個料理 + 2 個調味)
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
        // 初始化菜單狀態：階段一 menu1 開, menu2 關
        SetMenuState(menu1, true);
        SetMenuState(menu2, false);

        // 1. 隱藏階段二的判定點
        foreach (var spot in stage2SpotsRoots) spot.SetActive(false);

        // 2. 隱藏所有獎勵紙條
        if (infoPaper_1) infoPaper_1.SetActive(false);
        if (infoPaper_2) infoPaper_2.SetActive(false);
        if (infoPaper_3) infoPaper_3.SetActive(false);

        // 3. 確保預置料理也是隱藏的 (雖然你說場景已經隱藏，但這裡雙重保險)
        // 注意：這裡只確保邏輯上的隱藏，視野腳本會接手剩下的
        if (foodObj_ColdMeal) foodObj_ColdMeal.SetActive(false);
        if (foodObj_Drink) foodObj_Drink.SetActive(false);
        if (foodObj_Soup) foodObj_Soup.SetActive(false);
        if (foodObj_Dessert) foodObj_Dessert.SetActive(false);
    }

    // 封裝一個方法來同時控制 物件顯示 與 視野腳本開關
    private void SetMenuState(GameObject menuObj, bool isActive)
    {
        if (menuObj == null) return;

        menuObj.SetActive(isActive);
        var handler = menuObj.GetComponent<ViewMeshHandler>();
        if (handler != null)
        {
            handler.enabled = isActive;
            // 如果是在執行中切換，手動觸發一次當前視野更新
            if (isActive && ViewManager.Instance != null)
                handler.OnViewChanged(ViewManager.Instance.CurrentView);
        }
    }

    // ==========================================
    // 對外接口：供 Spot 和 Food 的 Event 呼叫
    // ==========================================

    #region 階段一邏輯

    // 當 Spot 1 (InteractableObject) 成功放置 冷盤
    public void OnSpot1Filled()
    {
        spot1_Done = true;
        if (foodObj_ColdMeal) foodObj_ColdMeal.SetActive(true); // 顯示料理-冷盤
        CheckStage1();
    }

    // 當 Spot 2-5 (ItemPlacementSpot) 成功放置 B,C,D,E
    // 這些是生成的，所以不需要我們手動 SetActive，腳本自己會生成
    public void OnSpot2Filled() { spot2_Done = true; CheckStage1(); } // 熱菜1
    public void OnSpot3Filled() { spot3_Done = true; CheckStage1(); } // 熱菜2
    public void OnSpot4Filled() { spot4_Done = true; CheckStage1(); } // 主食1
    public void OnSpot5Filled() { spot5_Done = true; CheckStage1(); } // 主食2

    // 當 Spot 6 (InteractableObject) 成功放置 酒水
    public void OnSpot6Filled()
    {
        print("成功");
        spot6_Done = true;
        if (foodObj_Drink) foodObj_Drink.SetActive(true); // 顯示料理-酒水，讓玩家可以對它調味-安眠藥
        CheckStage1();
    }

    // 當對 料理-酒水 (InteractableObject) 使用 調味料-安眠藥
    public void OnFoodDrinkSeasoned()
    {
        foodF_Seasoned = true;
        Debug.Log("料理 酒水 調味完成！");
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
        Debug.Log("階段一完成！清除場景，生成資訊紙條1");

        // 1. 清除階段一的判定點 (包含新生成的模型 和 舊判定點物件)
        foreach (var spot in stage1SpotsObjects) if (spot) Destroy(spot);

        // 2. 清除預置的料理物件
        if (foodObj_ColdMeal) Destroy(foodObj_ColdMeal);
        if (foodObj_Drink) Destroy(foodObj_Drink); // 即使調味過也要清除
        foreach (var food in foodObj_HotMealsAndMainDishs) if (food) Destroy(food); // 清除那些可能手動放置的殘留物(如果有的話)

        // 3. 顯示資訊紙條1
        if (infoPaper_1) infoPaper_1.SetActive(true);

        // 注意：階段二的開啟是透過「玩家撿起紙條 a」來觸發
        // 你需要在 紙條 a 的 InteractableItem (或類似腳本) 的 Event 中呼叫 StartStage2()
    }

    #endregion

    #region 階段二邏輯

    // 由紙條 a 的拾取事件呼叫
    public void StartStage2()
    {
        // 進入階段二：關閉 menu1, 開啟 menu2
        SetMenuState(menu1, false);
        SetMenuState(menu2, true);

        Debug.Log("開啟階段二！");
        foreach (var spot in stage2SpotsRoots) spot.SetActive(true);
    }

    public void OnSpot7Filled() 
    {
        spot7_Done = true;
        if (foodObj_VegStableFruits[0]) foodObj_VegStableFruits[0].SetActive(true); // 顯示蔬菜
        CheckStage2(); 
    }
    public void OnSpot8Filled()
    {
        spot8_Done = true;
        if (foodObj_Soup) foodObj_Soup.SetActive(true); // 顯示H供調味
        CheckStage2();
    }
    public void OnSpot9Filled() 
    { 
        spot9_Done = true;
        if (foodObj_VegStableFruits[1]) foodObj_VegStableFruits[1].SetActive(true); // 顯示主食
        CheckStage2(); 
    }
    public void OnSpot10Filled()
    {
        spot10_Done = true;
        if (foodObj_Dessert) foodObj_Dessert.SetActive(true); // 顯示J供調味
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
        Debug.Log("階段二完成！清除場景，生成紙條 b, c");

        // 1. 清除階段二的判定點
        foreach (var spot in stage2SpotsRoots) if (spot) Destroy(spot);

        // 2. 清除預置料理
        if (foodObj_Soup) Destroy(foodObj_Soup);
        if (foodObj_Dessert) Destroy(foodObj_Dessert);
        foreach (var food in foodObj_VegStableFruits) if (food) Destroy(food);

        // 3. 顯示資訊紙條2、3
        if (infoPaper_2) infoPaper_2.SetActive(true);
        if (infoPaper_3) infoPaper_3.SetActive(true);
    }

    #endregion
}