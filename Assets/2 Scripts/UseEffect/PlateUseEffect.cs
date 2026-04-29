using UnityEngine;

[RequireComponent(typeof(AudioSource))] // 確保物件上有 AudioSource
public class PlateUseEffect : MonoBehaviour
{
    // 這裡掛載在 PlateEventManager 物件上
    [System.Serializable]
    public class PlateData
    {
        public GameObject plateObject;
        public MonoBehaviour plateScript;
    }

    [Header("盤子配置")]
    public PlateData[] plates = new PlateData[7];
    public GameObject magicCircle;

    [Header("音效設定")]
    [SerializeField, Tooltip("解謎成功時的音效")]
    private AudioClip successSound;
    [SerializeField, Range(0f, 1f)]
    private float soundVolume = 1f;
    [SerializeField] private AudioSource audioSource;

    private int usedPlateCount = 0;
    private int totalPlates => plates.Length; // 預計需要的總盤數

    // 由每個盤子槽位上掛載的 InteractableObject 事件呼叫此方法
    public void UsePlate(int plateIndex)
    {
        if (plateIndex < 0 || plateIndex >= plates.Length)
        {
            Debug.LogError($"無效的盤子索引: {plateIndex}");
            return;
        }

        var plate = plates[plateIndex];
        if (plate == null) return;

        // 防止重複觸發
        if (plate.plateObject != null && !plate.plateObject.activeInHierarchy)
        {
            plate.plateObject.SetActive(true); // 顯示盤子
            if (plate.plateScript != null)
                plate.plateScript.enabled = true; // 開啟 ViewInteractableObject 腳本

            usedPlateCount++; // 已使用盤子數量 += 1
            CheckCompletion(); // 檢查是否集齊盤子 (例如：3、5、7、8 號位)
        }
    }

    private void CheckCompletion()
    {
        if (usedPlateCount >= totalPlates)
        {
            Debug.Log("所有盤子皆已放置完成！開啟魔法陣。");
            magicCircle.SetActive(true);

            // 通知對話系統播放對話
            DialogueManager.Instance.TriggerDialogueByEvent("FinishPlates");

            // 通知燈光系統解謎完成
            KanWu.Systems.LightSystemManager.Instance.NotifyPuzzleSolved();

            // 處理音效播放與銷毀
            if (successSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(successSound, soundVolume);

                // 為了避免音效被切斷，先關閉此腳本的功能防止重複觸發
                this.enabled = false;

                // 延遲銷毀物件，等待時間為音效的長度
                Destroy(gameObject, successSound.length);
            }
            else
            {
                // 如果沒有設定音效，則直接銷毀
                Destroy(gameObject);
            }
        }
    }

    // 保留原本透過 Inspector 事件綁定的舊方法名稱，以相容現有的 Event 調用
    // 注意：這裡的索引 index 與下方方法名稱的數字可能不一致，請在 Inspector 重新確認
    public void UsePlate_3() => UsePlate(0);
    public void UsePlate_5() => UsePlate(1);
    public void UsePlate_7() => UsePlate(2);
    public void UsePlate_8() => UsePlate(3);
}