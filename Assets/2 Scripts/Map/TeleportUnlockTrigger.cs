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

            // 2. 呼叫地圖專用的 UI 管理器
            if (MapUIManager.Instance != null)
            {
                MapUIManager.Instance.ShowHint($"已解鎖{pointData.pointName}傳送點，地圖即可查看");
            }

            this.enabled = false; 
        }
    }
}