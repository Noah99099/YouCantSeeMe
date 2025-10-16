// PlayerRespawnManager.cs
using UnityEngine;

public class PlayerRespawnManager : MonoBehaviour
{
    public static PlayerRespawnManager Instance { get; private set; }

    [Header("重生系統")]
    public string currentSpawnPointID = "Start";

    void Awake()
    {
        // 單例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ***** 新增: 在 Start 中訂閱 SceneLoader 的事件 *****
    // 使用 Start 是為了確保 SceneLoader.Instance 已經在它的 Awake 中被賦值
    private void Start()
    {
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.OnSceneLoadComplete += HandleSceneLoadComplete;
        }
        else
        {
            Debug.LogError("PlayerRespawnManager 找不到 SceneLoader 的實例！");
        }
    }

    void OnDestroy()
    {
        // ***** 修改: 取消訂閱我們自己的事件 *****
        if (Instance == this)
        {
            // SceneManager.sceneLoaded -= OnSceneLoaded;
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.OnSceneLoadComplete -= HandleSceneLoadComplete;
            }
        }
    }

    // ***** 修改: 這是新的事件處理方法 *****
    // 問題所在: 不知道(string sceneName)
    void HandleSceneLoadComplete(string sceneName)
    {
        Debug.Log($"[PlayerRespawnManager] Received OnSceneLoadComplete event for scene: {sceneName}. Now respawning player.");

        // 現在呼叫重生邏輯是 100% 安全的，因為轉場已結束
        // 也不再需要延遲協程了
        ResetToSceneDefaultSpawnPoint();
    }

    public void SetSpawnPoint(string newSpawnPointID)
    {
        currentSpawnPointID = newSpawnPointID;
        Debug.Log($"重生點設置為: {newSpawnPointID}");
    }


    // 新增方法：自動尋找並使用場景的默認重生點
    void ResetToSceneDefaultSpawnPoint()
    {
        SpawnPoint[] allSpawnPoints = FindObjectsOfType<SpawnPoint>();

        if (allSpawnPoints.Length == 0)
        {
            Debug.LogError("場景中沒有任何重生點！");
            return;
        }

        // 優先尋找默認重生點
        SpawnPoint defaultSpawnPoint = null;
        foreach (SpawnPoint point in allSpawnPoints)
        {
            if (point.isDefault)
            {
                defaultSpawnPoint = point;
                break;
            }
        }

        // 如果沒有默認點，使用第一個找到的點
        if (defaultSpawnPoint == null)
        {
            defaultSpawnPoint = allSpawnPoints[0];
            Debug.LogWarning("場景沒有默認重生點，使用第一個找到的重生點");
        }

        // 更新為新場景的重生點
        currentSpawnPointID = defaultSpawnPoint.pointID;

        // 執行重生
        SpawnPlayerAtPoint(currentSpawnPointID);
    }

    void SpawnPlayerAtPoint(string spawnPointID)
    {
        // 尋找玩家
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("找不到 Player 物件！");
            return;
        }

        // 尋找所有重生點
        SpawnPoint[] allSpawnPoints = FindObjectsOfType<SpawnPoint>();
        SpawnPoint targetSpawnPoint = null;

        // 優先尋找指定ID的重生點
        foreach (SpawnPoint point in allSpawnPoints)
        {
            if (point.pointID == spawnPointID)
            {
                targetSpawnPoint = point;
                break;
            }
        }

        // 如果沒找到指定ID，尋找默認重生點
        if (targetSpawnPoint == null)
        {
            foreach (SpawnPoint point in allSpawnPoints)
            {
                if (point.isDefault)
                {
                    targetSpawnPoint = point;
                    Debug.LogWarning($"找不到重生點 {spawnPointID}，使用默認重生點");
                    break;
                }
            }
        }

        // 如果還是沒找到，使用第一個找到的重生點
        if (targetSpawnPoint == null && allSpawnPoints.Length > 0)
        {
            targetSpawnPoint = allSpawnPoints[0];
            Debug.LogWarning("使用第一個找到的重生點");
        }

        // 移動玩家到重生點
        if (targetSpawnPoint != null)
        {
            player.transform.position = targetSpawnPoint.transform.position;
            player.transform.rotation = targetSpawnPoint.transform.rotation;
            Debug.Log($"玩家已重生在: {targetSpawnPoint.pointID}");
        }
        else
        {
            Debug.LogError("場景中沒有任何重生點！");
        }
    }
}
