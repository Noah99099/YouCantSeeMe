// 檔案名稱: VoiceItemDetectionPoint.cs

using UnityEngine;

[RequireComponent(typeof(Collider))]
public class VoiceItemDetectionPoint : MonoBehaviour
{
    [Header("判定點設定")]
    [Tooltip("此判定點需要哪個聲音物品才能觸發")]
    [SerializeField] private VoiceItemData requiredVoiceItem;

    [Header("特效與聲音 (可選)")]
    [Tooltip("對應的雜音 AudioSource")]
    [SerializeField] private AudioSource staticNoiseSource;

    [Header("判定範圍")]
    [Tooltip("開始偵測玩家並產生特效的最遠距離")]
    [SerializeField] private float maxDetectionDistance = 20.0f;
    [Tooltip("特效最強 (音量最大) 的距離")]
    [SerializeField] private float minDetectionDistance = 1.0f;

    [Header("觸發設定")]
    [Tooltip("觸發動畫的影片")]
    [SerializeField] private PlayVideo playVideo;
    [Tooltip("最後要刪除的物件")]
    [SerializeField] private GameObject destroyObj;

    private Transform playerTransform;
    private bool isActivated = false; // 判定點是否被激活
    private ScreenGlitchEffect glitchController; // 花屏特效控制器

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col == null || !col.isTrigger)
        {
            Debug.LogWarning($"[VoiceItemDetectionPoint] {gameObject.name} 需要一個 'Is Trigger' = true 的 Collider。", this);
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogError($"[VoiceItemDetectionPoint] 找不到 Tag 為 'Player' 的物件！", this);
        }

        // [修改] 嘗試獲取全局的花屏控制器 (從 PlayerInteraction 單例獲取)
        if (PlayerInteraction.Instance != null && PlayerInteraction.Instance.glitchController != null)
        {
            // 獲取 PlayerInteraction 上的 Glitch Controller 引用
            this.glitchController = PlayerInteraction.Instance.glitchController;
        }
        else
        {
            Debug.LogWarning($"[VoiceItemDetectionPoint] 無法從 PlayerInteraction.Instance 獲取 Glitch Controller！", this);
        }

        if (staticNoiseSource != null)
        {
            staticNoiseSource.volume = 0;
            staticNoiseSource.Stop();
        }
    }

    /// <summary>
    /// 由 PlayerInteraction 呼叫，嘗試激活這個判定點
    /// </summary>
    public bool ActivatePoint(VoiceItemData item)
    {
        if (item == requiredVoiceItem)
        {
            isActivated = true;
            Debug.Log($"[VoiceItemDetectionPoint] {gameObject.name} 已被激活。");

            if (staticNoiseSource != null && !staticNoiseSource.isPlaying)
            {
                staticNoiseSource.Play(); // 開始播放雜音
            }
            return true;
        }
        return false;
    }

    private void Update()
    {
        if (!isActivated || playerTransform == null)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        // 核心邏輯：距離越近，強度 (intensity) 越接近 1.0
        float intensity = Mathf.InverseLerp(maxDetectionDistance, minDetectionDistance, distance);
        intensity = Mathf.Clamp01(intensity);

        // 1. 控制花屏特效
        if (glitchController != null)
        {
            glitchController.SetGlitchIntensity(intensity);
        }

        // 2. 控制雜音音效
        if (staticNoiseSource != null)
        {
            staticNoiseSource.volume = intensity;
        }
    }

    /// <summary>
    /// 當玩家走進判定點時
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // 必須是激活狀態，且進入的是玩家
        if (isActivated && other.CompareTag("Player"))
        {
            Debug.Log($"[VoiceItemDetectionPoint] 玩家已進入 {gameObject.name} 觸發區域。");
            isActivated = false;

            // 1. 停止特效和聲音
            if (glitchController != null)
            {
                glitchController.StopGlitch(); // 呼叫停止
            }
            if (staticNoiseSource != null)
            {
                staticNoiseSource.Stop();
            }

            // 2. 播放動畫 (您已架設好)
            if (playVideo != null)
            {
                playVideo.PlayForDeceased();
            }
            Debug.Log("[VoiceItemDetectionPoint] 播放動畫/影片...");

            // 3. 通知 PlayerInteraction 流程結束
            PlayerInteraction.Instance.CompleteVoiceItemUsage(requiredVoiceItem);

            // 4. 禁用此判定點物件
            gameObject.SetActive(false);

            // 5. 刪掉判定點
            Destroy(destroyObj);
        }
    }
}