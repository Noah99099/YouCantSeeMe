using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems; // 必須引入

// 實作 IPointerClickHandler 接口
public class TeleportListButton : MonoBehaviour, IPointerClickHandler
{
    public TeleportPointData pointData;
    public TextMeshProUGUI buttonText;
    public Button myButton;

    public void RefreshStatus()
    {
        if (pointData == null) return;
        if (pointData.isUnlocked)
        {
            buttonText.text = pointData.pointName;
            if(myButton != null) myButton.interactable = true;
        }
        else
        {
            buttonText.text = "未解鎖";
            if(myButton != null) myButton.interactable = false;
        }
    }

    // 這是底層的點擊事件，繞過 Button 組件的 OnClick
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"<color=white>[Pointer] 底層點擊偵測！按鈕：{gameObject.name}</color>");

        if (pointData != null && pointData.isUnlocked && BigMapController.Instance != null)
        {
            BigMapController.Instance.TeleportToPoint(pointData);
            Debug.Log("<color=cyan>[Teleport] 成功發出傳送指令！</color>");
        }
        else
        {
            Debug.LogWarning($"[Pointer] 點擊無效：解鎖={pointData?.isUnlocked}，Manager={BigMapController.Instance != null}");
        }
    }

    // 保留原本的 OnClickTeleport 給 Unity Event 使用 (選配)
    public void OnClickTeleport() { } 
}