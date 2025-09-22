using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VoiceItemInteractionManager : MonoBehaviour
{
    public static VoiceItemInteractionManager Instance { get; private set; }

    [Header("UI 參考")]
    public InventoryGridEditor inventoryGrid;
    public GlitchEffectController glitchEffect;
    public TextMeshProUGUI infoText1;
    public TextMeshProUGUI infoText2;

    [Header("右下角模型顯示")]
    public Transform cornerAnchor; // 相機右下角的一個空物件 (child of MainCamera)
    private GameObject spawnedModel;

    private InteractableVoice currentVoice; //聲音物件
    //private bool hasTriggered;
    public InteractableVoice CurrentVoice => currentVoice; // public getter，外部只能讀
    

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void OnInteract(InteractableVoice obj)
    {
        currentVoice = obj;
        //hasTriggered = false;

        // 1. 物件消失
        Destroy(obj.gameObject);

        // 2. 開啟花屏特效
        glitchEffect.SetCurrentVoice(obj);
        Debug.Log("開啟花屏特效");

        // 3. 在右下角生成模型
        if (obj.modelPrefab != null && cornerAnchor != null)
        {
            spawnedModel = Instantiate(obj.modelPrefab, cornerAnchor);
            spawnedModel.transform.localPosition = Vector3.zero;
            spawnedModel.transform.localRotation = Quaternion.identity;
        }

        // 4. ScrollView 第一次變化 (格子換圖)
        if (obj.slotIndex < inventoryGrid.transform.childCount)
        {
            var slot = inventoryGrid.transform.GetChild(obj.slotIndex);
            var icon = slot.Find("ItemIcon")?.GetComponent<Image>();
            if (icon != null && obj.inventoryIcon != null)
            {
                icon.sprite = obj.inventoryIcon;
                icon.enabled = true;
            }
        }
    }

    public void OnEnterTrigger()
    {
        Debug.Log("OnEnterTrigger called"); // 確認方法進來了

        //if (currentVoice == null)
        //{
        //    Debug.LogWarning("currentVoice is null!");
        //    return;
        //}

        //if (hasTriggered)
        //{
        //    Debug.Log("Already triggered!");
        //    return;
        //}

        //hasTriggered = true;

        // 1. 花屏消失
        glitchEffect.HideGlitch();
        Debug.Log("// 1. 花屏消失");

        // 2. 右下角模型消失
        if (spawnedModel != null)
        {
            Destroy(spawnedModel);
            spawnedModel = null;
        }

        // 3. ScrollView 第二次變化 (格子再變)
        if (currentVoice.slotIndex < inventoryGrid.transform.childCount)
        {
            var slot = inventoryGrid.transform.GetChild(currentVoice.slotIndex);
            var icon = slot.Find("ItemIcon")?.GetComponent<Image>();
            if (icon != null)
            {
                // 模擬第二次變化：變成灰色
                icon.color = Color.gray;
            }
        }

        // 4. 更新右側文字框 (根據不同物件)
        if (infoText1 != null) infoText1.text = currentVoice.titleText;
        if (infoText2 != null) infoText2.text = currentVoice.descriptionText;

        //hasTriggered = false;
    }

    public void UpdateGlitchEffect(float distance, float maxDistance)
    {
        float intensity = 1f - Mathf.Clamp01(distance / maxDistance);
        glitchEffect.SetIntensity(intensity);
    }
}
