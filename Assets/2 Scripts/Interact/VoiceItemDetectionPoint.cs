using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Collider))]
public class VoiceItemDetectionPoint : MonoBehaviour
{
    [Header("聲音物品")]
    [SerializeField] private VoiceItemData requiredVoiceItem;

    [Header("雜音")]
    [SerializeField] private AudioSource staticNoiseSource;

    [Header("距離設置")]
    [SerializeField] private float maxDetectionDistance = 20.0f; // 這是效果開始產生的最遠距離
    [SerializeField] private float minDetectionDistance = 1.0f; // 這是效果達到最大值（100% 強度）的距離

    [Header("影片和刪除物品")]
    [SerializeField] private PlayVideo playVideo;
    [SerializeField] private GameObject destroyObj;

    private Transform playerTransform;
    private bool isActivated = false;
    private ScreenGlitchEffect glitchController;

    // [!! 新增 !!] 波形圖控制器
    private WaveformVisualizer waveformController;

    // ----- [!! 新增 !!] -----
    // 我們現在需要直接存取 FilmGrain "參數" 本身
    private FilmGrain filmGrainEffect;
    private ChromaticAberration chromaticAberrationEffect;
    // ----- [!! 結束新增 !!] -----

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col == null || !col.isTrigger)
        {
            Debug.LogWarning($"[VoiceItemDetectionPoint] {gameObject.name} �ݭn�@�� 'Is Trigger' = true �� Collider�C", this);
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogError($"[VoiceItemDetectionPoint] �䤣�� Tag �� 'Player' ������I", this);
        }

        // 從 PlayerInteraction 獲取全局參照
        if (PlayerInteraction.Instance != null && PlayerInteraction.Instance.glitchController != null)
        {
            // 1. 獲取 Glitch Controller
            this.glitchController = PlayerInteraction.Instance.glitchController;
            // 2. [!! 新增 !!] 獲取 UI 波形圖控制器
            this.waveformController = PlayerInteraction.Instance.waveformUI;
            // 確保遊戲開始時 UI 是關閉的
            if (this.waveformController != null)
            {
                this.waveformController.gameObject.SetActive(false);
            }
            // 3. 獲取 Volume Profile 參數
            // 既然拿到了 Controller，就從它的 Volume Profile 裡預先抓出 FilmGrain 參數
            if (this.glitchController.glitchVolume != null && this.glitchController.glitchVolume.profile != null)
            {
                // 1. 抓取 FilmGrain
                if (!this.glitchController.glitchVolume.profile.TryGet(out filmGrainEffect))
                {
                    Debug.LogError($"[VoiceItemDetectionPoint]  {this.glitchController.glitchVolume.name}  Profile 䤣 FilmGrainI");
                }
                
                // 2. 抓取 ChromaticAberration (色差) [!! 新增 !!]
                if (!this.glitchController.glitchVolume.profile.TryGet(out chromaticAberrationEffect))
                {
                    Debug.LogError($"[VoiceItemDetectionPoint]  {this.glitchController.glitchVolume.name}  Profile 䤣 ChromaticAberrationI");
                }
            }
            else
            {
                Debug.LogError("[VoiceItemDetectionPoint] Glitch Controller  'glitchVolume' wBoO Profile wI");
            }
            // ----- [!! 結束新增 !!] -----
        }
        else
        {
            Debug.LogWarning($"[VoiceItemDetectionPoint] �L�k�q PlayerInteraction.Instance ��� Glitch Controller�I", this);
        }

        if (staticNoiseSource != null)
        {
            staticNoiseSource.volume = 0;
            staticNoiseSource.Stop();
        }
    }

    /// <summary>
    /// 打開判定點
    /// </summary>
    /// <param name="item">聲音物品</param>
    /// <returns></returns>
    public bool ActivatePoint(VoiceItemData item)
    {
        if (item == requiredVoiceItem)
        {
            isActivated = true;
            Debug.Log($"[VoiceItemDetectionPoint] {gameObject.name} �w�Q�E���C");

            if (staticNoiseSource != null && !staticNoiseSource.isPlaying)
            {
                staticNoiseSource.Play();
            }

            // [!! 新增 !!] 打開波形圖 UI
            if (waveformController != null)
            {
                waveformController.gameObject.SetActive(true);
                waveformController.ResetWave(); // 確保從平穩開始
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

        // 距離越近，強度越高 (0 ~ 1)
        float intensity = Mathf.InverseLerp(maxDetectionDistance, minDetectionDistance, distance);
        intensity = Mathf.Clamp01(intensity);

        // ----- [!! 新增 !!] -----
        // 而是直接修改 FilmGrain 參數的 .value
        // 因為 GlitchVolume (P=20, W=1) 會覆蓋 YinVolume (P=10, W=1)
        // 所以這裡的修改會 100% 顯示出來
        // 1. 應用到 FilmGrain
        if (filmGrainEffect != null) filmGrainEffect.intensity.value = intensity;

        // 2. 應用到 ChromaticAberration [!! 新增 !!]
        if (chromaticAberrationEffect != null) chromaticAberrationEffect.intensity.value = intensity;

        // 3. 應用到聲音
        if (staticNoiseSource != null) staticNoiseSource.volume = intensity;

        // 4. [!! 新增 !!] 應用到波形圖 UI
        if (waveformController != null) waveformController.SetIntensity(intensity);
    }

    /// <summary>
    /// 玩家走進判定點
    /// </summary>
    /// <param name="other">玩家的碰撞體</param>
    private void OnTriggerEnter(Collider other)
    {
        if (isActivated && other.CompareTag("Player"))
        {
            Debug.Log($"[VoiceItemDetectionPoint] 玩家已進入 {gameObject.name} 觸發範圍");
            isActivated = false;

            // 1. 關閉所有效果
            if (glitchController != null)
            {
                glitchController.StopGlitch(); // (這會將 Weight 設為 0)
            }
            // 順手將參數也歸零，保持乾淨
            if (filmGrainEffect != null)
            {
                filmGrainEffect.intensity.value = 0f;
            }
            if (chromaticAberrationEffect != null) // [!! 新增 !!]
            {
                chromaticAberrationEffect.intensity.value = 0f;
            }
            if (staticNoiseSource != null)
            {
                staticNoiseSource.Stop();
            }

            // [!! 新增 !!] 關閉波形圖 UI
            if (waveformController != null)
            {
                waveformController.SetIntensity(0f); // 歸零
                waveformController.gameObject.SetActive(false); // 隱藏物件
            }

            // 2. 播放影片/死亡邏輯
            if (playVideo != null)
            {
                // [!! 核心修改點 !!] 傳入一個「影片播完後」的回調 Action
                System.Action onVideoFinishedAction = () =>
                {
                    Debug.Log($"[VoiceItemDetectionPoint] 影片播放完畢，執行銷毀物件和關閉偵測點。");

                    // 3. 完成聲音物品使用 (這步可以移到回調裡，確保發生在影片結束後)
                    PlayerInteraction.Instance.CompleteVoiceItemUsage(requiredVoiceItem);

                    // 4. [!! 核心修改點 !!] 影片播完後，刪除指定物件
                    if (destroyObj != null)
                    {
                        Destroy(destroyObj);
                    }
                    else
                    {
                        Debug.LogWarning("[VoiceItemDetectionPoint] destroyObj 為空，跳過銷毀步驟。");
                    }

                    // 5. 關掉後刪除 (通常是偵測點本身)
                    gameObject.SetActive(false);

                    // 備註: 如果您是想 Destroy(gameObject)，請使用 Destroy(gameObject);
                };

                // [!! 調用新的 PlayWithoutRole 方法 !!]
                playVideo.PlayWithoutRole(onVideoFinishedAction); //開始播放影片
            }
            else
            {
                // 如果沒有影片，則立即執行銷毀邏輯 (與舊邏輯一致)
                if (PlayerInteraction.Instance != null)
                {
                    PlayerInteraction.Instance.CompleteVoiceItemUsage(requiredVoiceItem);
                }
                if (destroyObj != null)
                {
                    Destroy(destroyObj);
                }
                gameObject.SetActive(false);
            }
        }
    }
}