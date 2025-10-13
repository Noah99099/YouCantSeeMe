using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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

            // 訂閱場景加載事件
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        // 取消訂閱避免記憶體洩漏
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    // 在 GameManager.cs 中添加這些方法
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"場景加載完成: {scene.name}");
        // 重要修正：每次加載新場景時，自動使用該場景的默認重生點
        StartCoroutine(DelayedRespawn());

        //ResetToSceneDefaultSpawnPoint();
    }

    public void SetSpawnPoint(string newSpawnPointID)
    {
        currentSpawnPointID = newSpawnPointID;
        Debug.Log($"重生點設置為: {newSpawnPointID}");
    }

    private IEnumerator DelayedRespawn()
    {
        // 延遲 1~2 幀，確保其他 Start() 腳本都執行完
        yield return null;
        yield return null;

        ResetToSceneDefaultSpawnPoint();
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
