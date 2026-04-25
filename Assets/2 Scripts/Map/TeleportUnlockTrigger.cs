using UnityEngine;

public class TeleportUnlockTrigger : MonoBehaviour
{
    public TeleportPointData pointData;

    private void OnTriggerEnter(Collider other)
    {
        // 判定玩家進入且該點尚未解鎖 
        if (other.CompareTag("Player") && pointData != null && !pointData.isUnlocked)
        {
            // 1. 執行存檔邏輯
            if (MapSaveManager.Instance != null)
            {
                MapSaveManager.Instance.UnlockPoint(pointData.pointID);
            }

            // 2. 呼叫提示面板控制 (請確保 TeleportHintManager 檔案名稱與類別名一致)
            if (TeleportHintManager.Instance != null)
            {
                TeleportHintManager.Instance.ShowUnlockHint(pointData.pointName);
            }

            // 3. 停用此觸發器，避免重複觸發
            this.enabled = false; 
        }
    }
}