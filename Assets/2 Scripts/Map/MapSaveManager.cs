using UnityEngine;
using System.Collections.Generic;
using System.Linq; // 需要這個來轉換 List

public class MapSaveManager : MonoBehaviour
{
    public static MapSaveManager Instance;

    [Header("所有傳送點清單 (自動生成，請勿手動拖曳)")]
    public List<TeleportPointData> allTeleportPoints = new List<TeleportPointData>();

    private void Awake()
    {
        // 單例模式 + 跨場景存活
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 讓這個物件在切換場景時不會死亡
            
            InitializeTeleportData(); // 遊戲啟動時自動載入所有資料
        }
        else
        {
            Destroy(gameObject); // 確保全遊戲只有一個 MapSaveManager
        }
    }

    // 自動從 Resources 抓取所有傳送點，並讀取存檔
    private void InitializeTeleportData()
    {
        // 抓取 Resources/TeleportPoints 資料夾下所有的 TeleportPointData
        TeleportPointData[] loadedPoints = Resources.LoadAll<TeleportPointData>("TeleportPoints");
        
        if (loadedPoints.Length > 0)
        {
            allTeleportPoints = loadedPoints.ToList();
            Debug.Log($"<color=cyan>[MapSaveManager] 成功載入 {allTeleportPoints.Count} 個傳送點資料</color>");
        }
        else
        {
            Debug.LogError("[MapSaveManager] 找不到任何傳送點！請確認它們放在 Resources/TeleportPoints 資料夾下。");
        }

        // 讀取本地解鎖狀態
        LoadMapProgress();
    }

    // 解鎖傳送點並存檔
    public void UnlockPoint(string id)
    {
        TeleportPointData point = allTeleportPoints.Find(p => p.pointID == id);
        if (point != null && !point.isUnlocked)
        {
            point.isUnlocked = true;
            PlayerPrefs.SetInt("Teleport_" + id, 1);
            PlayerPrefs.Save();
            Debug.Log($"<color=green>傳送點 {point.pointName} 已解鎖並存檔</color>");
        }
    }

    // 載入所有傳送點狀態
    public void LoadMapProgress()
    {
        foreach (var point in allTeleportPoints)
        {
            point.isUnlocked = PlayerPrefs.GetInt("Teleport_" + point.pointID, 0) == 1;
        }
    }
}