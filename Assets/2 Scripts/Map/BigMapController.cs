using UnityEngine;
using UnityEngine.UI; // 用於 Image
using System.Collections.Generic; // 用於 List

[System.Serializable]
public class FloorData
{
    public string floorName;     // 樓層名稱
    public float yMin;           // 該樓層的最低 Y 值
    public float yMax;           // 該樓層的最高 Y 值
    public Sprite floorSprite;   // 對應的地圖圖片
}

public class BigMapController : MonoBehaviour
{
    public static BigMapController Instance; // 單例

    [Header("場景參考點 (由場景註冊器動態傳入)")]
    public Transform sceneMinPoint;
    public Transform sceneMaxPoint;
    public Transform playerTransform;

    [Header("UI 顯示元件")]
    public Image mapDisplayImage;      // 顯示地圖底圖的 Image 元件
    public RectTransform mapRect;      // 大地圖的 UI 容器 (用來計算寬高比例)
    public RectTransform playerIcon;   // 玩家在 UI 上的箭頭圖示

    [Header("微調設定")]
    [Tooltip("用來修正箭頭圖示的初始朝向。如果倒過來，請嘗試輸入 180、90 或 -90")]
    public float iconRotationOffset = 180f; // 加上這個變數

    [Header("樓層數據")]
    public List<FloorData> floors; 
    private FloorData currentFloor;

    /// <summary>
    /// 將地圖視角強制對焦在玩家圖示上
    /// </summary>
    public void CenterMapOnPlayer()
    {
        // 防呆：確保地圖與玩家參考點都存在
        if (playerIcon == null || mapRect == null || playerTransform == null) return;

        // 1. 強制更新一次玩家在地圖上的最新座標，避免圖示位置還沒算好
        UpdatePlayerMarker(); 

        // 2. 核心邏輯：把 Content (地圖) 的位置設為玩家圖示位置的「反向」
        mapRect.anchoredPosition = -playerIcon.anchoredPosition;
    }

    void Awake() 
    {
        // 更嚴謹的單例模式寫法
        if (Instance == null)
        {
            Instance = this;
            // 如果你的 UI 是放在持久層 (不會隨場景銷毀)，可以解除下行註解
            // DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject); // 確保場景中只有一個地圖控制器
        }
    }

    // 提供一個方法讓新場景來註冊座標
    public void RegisterMapBounds(Transform min, Transform max, Transform player) 
    {
        sceneMinPoint = min;
        sceneMaxPoint = max;
        playerTransform = player;
        
        // 註冊完立刻強制更新一次樓層，避免一開始顯示錯誤
        UpdateFloor(); 
    }

    void Update()
    {
        // 防呆機制：如果還沒有載入玩家或場景參考點，就先不要執行
        if (playerTransform == null || sceneMinPoint == null || sceneMaxPoint == null) return;

        UpdateFloor();        // 1. 先判斷樓層並切換底圖
        UpdatePlayerMarker(); // 2. 根據當前場景參考點，更新玩家座標
    }

    void UpdateFloor()
    {
        float playerY = playerTransform.position.y;

        foreach (var floor in floors)
        {
            // 判斷玩家在哪個 Y 軸區間內
            if (playerY >= floor.yMin && playerY <= floor.yMax)
            {
                if (currentFloor != floor)
                {
                    currentFloor = floor;
                    mapDisplayImage.sprite = floor.floorSprite; // 切換地圖底圖
                    Debug.Log($"目前切換至: {floor.floorName}");
                }
                break; // 找到對應樓層就跳出迴圈
            }
        }
    }

    // 實作遺漏的座標轉換邏輯
    void UpdatePlayerMarker()
    {
        // 1. 計算場景的總寬度與總深度
        float totalWidth = sceneMaxPoint.position.x - sceneMinPoint.position.x;
        float totalHeight = sceneMaxPoint.position.z - sceneMinPoint.position.z;

        // 避免除以零的錯誤 (防呆)
        if (totalWidth == 0 || totalHeight == 0) return;

        // 2. 計算玩家在 3D 場景中的比例 (0 ~ 1)
        float normalizedX = (playerTransform.position.x - sceneMinPoint.position.x) / totalWidth;
        float normalizedY = (playerTransform.position.z - sceneMinPoint.position.z) / totalHeight;

        // 3. 將比例轉換為 UI 的本地座標
        // 假設 mapRect 的 Pivot 是在正中心 (0.5, 0.5)
        float uiX = (normalizedX - 0.5f) * mapRect.rect.width;
        float uiY = (normalizedY - 0.5f) * mapRect.rect.height;

        playerIcon.anchoredPosition = new Vector2(uiX, uiY);

        // 4. 同步旋轉 (加上 offset 微調)
        // 原本是：playerIcon.localRotation = Quaternion.Euler(0, 0, -playerTransform.eulerAngles.y);
        // 請改成：
        playerIcon.localRotation = Quaternion.Euler(0, 0, -playerTransform.eulerAngles.y + iconRotationOffset);
    }
    // 在 BigMapController.cs 結尾處修正
    public void TeleportToPoint(TeleportPointData targetData)
    {
        if (targetData == null || playerTransform == null) return;

        // 1. 處理 CharacterController
        CharacterController cc = playerTransform.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // 2. 執行傳送
        playerTransform.position = targetData.targetPosition;
        Physics.SyncTransforms();

        if (cc != null) cc.enabled = true;

        // 3. 更新樓層狀態
        UpdateFloor(); 
        
        // 4. 【完美融入團隊架構】：呼叫專屬的關閉方法
        CloseMapProperly();

        Debug.Log($"<color=green>[Teleport] 傳送成功：{targetData.pointName}</color>");
    }

    /// <summary>
    /// 負責與組員的系統串接，安全地關閉地圖與切換輸入狀態
    /// </summary>
    private void CloseMapProperly()
    {
        // 步驟 A：利用組員的 InputStackManager 退出地圖模式
        // 當 PopMap 執行後，組員的 UpdateCursorStateBasedOnTopMap() 就會自動把滑鼠隱藏並鎖定了！
        if (InputStackManager.Instance != null && InputStackManager.Instance.CurrentMap == InputActionMaps._Map)
        {
            InputStackManager.Instance.PopMap();
        }

        // 步驟 B：呼叫負責關閉地圖的 UI 邏輯
        // 【請根據你們專案的實際情況，選擇以下其中一種做法】
        // 做法 3：觸發組員寫在 Player 身上或 Manager 身上的 "ToggleMap" 邏輯
        // 如果你們是按 M 鍵開關地圖，可以找找負責按 M 鍵的那個腳本，去呼叫它的關閉函式。
    }
}