using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 遊戲流程快捷鍵腳本
/// 按下數字 1 鍵，自動取得紀錄簿、平面圖，並放置盤子3、5、7
/// </summary>

public class Level2_BypassManager : MonoBehaviour
{
    [Header("=== 階段 1：核心功能物件 (Level 2) ===")]
    [Tooltip("場景中掛載 Map 腳本的物件")]
    public Map mapItem;
    [Tooltip("場景中掛載 CaseRecordBook 腳本的物件")]
    public CaseRecordBook caseRecordBookItem;

    [Header("=== 階段 2：盤子解謎物件 (Level 2) ===")]
    [Tooltip("負責管理盤子顯示的 PlateUseEffect")]
    public PlateUseEffect plateManager;

    [Tooltip("場景中還在地面上的盤子 3, 5, 7 實體物件 (用於銷毀)")]
    public GameObject[] plateItemsInScene;

    [Tooltip("場景中對應盤子 3, 5, 7 的放置判定點 (InteractableObject，用於銷毀)")]
    public GameObject[] platePlacementSpots;

    private void Update()
    {
        // 偵測鍵盤數字鍵 1 是否被按下
        if (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            ExecuteShortcut();
        }
    }

    private void ExecuteShortcut()
    {
        Debug.Log("<color=green>[GameFlowShortcut] 觸發快捷流程：一鍵取得地圖、紀錄簿、並放置盤子 3, 5, 7</color>");

        // 1. 執行地圖與紀錄簿的拾取
        // 呼叫 Collect() 會觸發 Map.GetMap 與 CaseRecordBook.OnCollected 事件
        // GetMapBookManager (Level 1) 收到後會自動呼叫 GetTwoThings.ExecuteActivation()
        if (mapItem != null)
        {
            mapItem.Collect(); // 觸發獲得事件
            Destroy(mapItem.gameObject); // 【明確刪除】平面圖
        }
        if (caseRecordBookItem != null)
        {
            caseRecordBookItem.Collect(); // 觸發獲得事件
            Destroy(caseRecordBookItem.gameObject); // 【明確刪除】紀錄簿
        }

        // 2. 執行盤子 3, 5, 7 的放置邏輯
        if (plateManager != null)
        {
            // 繞過背包介面，直接調用 PlateUseEffect 的放置方法
            plateManager.UsePlate_3();
            plateManager.UsePlate_5();
            plateManager.UsePlate_7();
        }

        // 3. 場景清理：刪除多餘的道具與判定點
        // 刪除地面上的盤子道具
        foreach (var plate in plateItemsInScene)
        {
            if (plate != null) Destroy(plate);
        }

        // 刪除判定點（防止玩家重複與空位置交互）
        foreach (var spot in platePlacementSpots)
        {
            if (spot != null) Destroy(spot);
        }

        Debug.Log("<color=green>[Shortcut] 快捷流程完成：已模擬拾取與盤子放置，教學與提示應已自動觸發。</color>");
    }
}
