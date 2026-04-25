using UnityEngine;

public class TeleportUnlockTrigger : MonoBehaviour
{
    public TeleportPointData pointData;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && pointData != null && !pointData.isUnlocked)
        {
            // 1. 解鎖
            if (MapSaveManager.Instance != null)
                MapSaveManager.Instance.UnlockPoint(pointData.pointID);

            // 2. 檢查 Manager 是否存在
            Debug.Log($"[Hint] 準備顯示提示。RightHintManager.Instance 是否存在: {RightHintManager.Instance != null}");

            if (RightHintManager.Instance != null)
            {
                RightHintManager.Instance.ShowHint($"已解鎖{pointData.pointName}傳送點，地圖即可查看");
            }
            else
            {
                // 如果這裡是 Null，代表你場景中的 RightHintManager 物件沒打開，或是腳本沒掛好
                Debug.LogError("[Hint] 找不到 RightHintManager！請檢查 Hierarchy 中該物件是否為啟用狀態。");
            }

            this.enabled = false; 
        }
    }
}