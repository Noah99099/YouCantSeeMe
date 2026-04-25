using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TeleportListButton : MonoBehaviour
{
    public TeleportPointData pointData; // 拖入對應的資料
    public TextMeshProUGUI buttonText; // 指向按鈕上的 Text
    public Button myButton;            // 指向按鈕組件

    // 當面板開啟時呼叫此方法刷新狀態
    public void RefreshStatus()
    {
        if (pointData == null) return;

        if (pointData.isUnlocked)
        {
            buttonText.text = pointData.pointName;
            myButton.interactable = true;
        }
        else
        {
            buttonText.text = "未解鎖";
            myButton.interactable = false; // 未解鎖則無法點擊
        }
    }

    // 按鈕點擊事件 (在 Inspector 連結)
    public void OnClickTeleport()
    {
        // --- 新增 Debug Log ---
        Debug.Log($"[Teleport] 點擊了按鈕，PointData 是否為空: {pointData == null}");
        
        if (pointData != null)
        {
            Debug.Log($"[Teleport] 傳送點解鎖狀態: {pointData.isUnlocked}");
            Debug.Log($"[Teleport] BigMapController 實例是否存在: {BigMapController.Instance != null}");
        }

        if (pointData != null && pointData.isUnlocked && BigMapController.Instance != null)
        {
            BigMapController.Instance.TeleportToPoint(pointData);
            Debug.Log("<color=cyan>[Teleport] 成功呼叫傳送方法！</color>");
        }
    }
}