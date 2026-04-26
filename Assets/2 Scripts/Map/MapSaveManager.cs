using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MapSaveManager : MonoBehaviour
{
    public static MapSaveManager Instance;

    [Header("所有傳送點清單 (自動生成，請勿手動拖曳)")]
    public List<TeleportPointData> allTeleportPoints = new List<TeleportPointData>();

    private void Awake()
    {
        // 因為都在同一個場景，移除 DontDestroyOnLoad
        if (Instance == null)
        {
            Instance = this;
            InitializeTeleportData(); // 遊戲啟動時自動載入所有資料
        }
        else
        {
            Destroy(gameObject); 
        }
    }

    // 自動從 Resources 抓取所有傳送點，並強制初始化狀態
    private void InitializeTeleportData()
    {
        TeleportPointData[] loadedPoints = Resources.LoadAll<TeleportPointData>("TeleportPoints");
        
        if (loadedPoints.Length > 0)
        {
            allTeleportPoints = loadedPoints.ToList();
            Debug.Log($"<color=cyan>[MapSaveManager] 成功載入 {allTeleportPoints.Count} 個傳送點資料</color>");
            
            // 【關鍵修改】：每次遊戲啟動時，強制把所有 ScriptableObject 的狀態重置為 false
            ResetAllProgress(); 
        }
        else
        {
            Debug.LogError("[MapSaveManager] 找不到任何傳送點！請確認它們放在 Resources/TeleportPoints 資料夾下。");
        }
    }

    // 解鎖傳送點 (移除了 PlayerPrefs，只改記憶體中的狀態)
    public void UnlockPoint(string id)
    {
        TeleportPointData point = allTeleportPoints.Find(p => p.pointID == id);
        if (point != null && !point.isUnlocked)
        {
            point.isUnlocked = true;
            Debug.Log($"<color=green>傳送點 {point.pointName} 已解鎖 (僅限本次遊戲)</color>");
        }
    }

    // 將所有傳送點重置為未解鎖 (避免編輯器殘留資料)
    private void ResetAllProgress()
    {
        foreach (var point in allTeleportPoints)
        {
            point.isUnlocked = false;
        }
        Debug.Log("[MapSaveManager] 遊戲開始，所有傳送點狀態已重置為未解鎖。");
    }
}