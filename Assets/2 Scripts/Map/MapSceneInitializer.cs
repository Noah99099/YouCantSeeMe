using UnityEngine;

public class MapSceneInitializer : MonoBehaviour
{
    public Transform mySceneMin;
    public Transform mySceneMax;

    void Start() 
    {
        // 1. 檢查單例是否存在
        if (BigMapController.Instance == null)
        {
            Debug.LogError("[地圖初始化] 失敗：找不到 BigMapController.Instance！");
            return;
        }

        // 2. 取得玩家的 Transform
        // 優先使用 BigMapController 已經綁定好的 Player (因為看你的截圖2，你已經綁定 PlayerCameraRoot 了)
        Transform targetPlayer = BigMapController.Instance.playerTransform;
        
        // 如果沒綁定，再嘗試用 Tag 尋找
        if (targetPlayer == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) 
            {
                targetPlayer = playerObj.transform;
            }
            else
            {
                Debug.LogError("[地圖初始化] 失敗：場景中找不到標籤為 'Player' 的物件！");
                return;
            }
        }

        // 3. 註冊資料到 BigMapController
        BigMapController.Instance.RegisterMapBounds(mySceneMin, mySceneMax, targetPlayer);
        Debug.Log("<color=green>[地圖初始化] 成功註冊場景邊界與玩家！地圖開始運作。</color>");
    }
}