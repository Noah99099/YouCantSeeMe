using UnityEngine;

public class InteractableVoice : MonoBehaviour
{
    [Header("右下角顯示")]
    public string objectName; //名稱
    public GameObject modelPrefab; // 用於右下角顯示的聲音模型
    public Sprite inventoryIcon;   // 對應聲音格子的圖片
    public int slotIndex = 0;      // 該物件對應的聲音格子編號 (0 = 第一格)

    [Header("右側文字顯示")]
    public string titleText;       // 顯示在 InfoText1
    [TextArea]
    public string descriptionText; // 顯示在 InfoText2

    public void Interact()
    {
        VoiceItemInteractionManager.Instance.OnInteract(this);
    }
}
