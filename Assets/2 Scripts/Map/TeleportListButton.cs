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
        if (pointData.isUnlocked && BigMapController.Instance != null)
        {
            BigMapController.Instance.TeleportToPoint(pointData);
            // 傳送後通常會關閉地圖面板
            // TeleportListPanelManager.Instance.TogglePanel(false); 
        }
    }
}