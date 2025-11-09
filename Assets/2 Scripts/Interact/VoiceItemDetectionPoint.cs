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
    [SerializeField] private float maxDetectionDistance = 20.0f;
    [SerializeField] private float minDetectionDistance = 1.0f;

    [Header("影片和刪除物品")]
    [SerializeField] private PlayVideo playVideo;
    [SerializeField] private GameObject destroyObj;

    private Transform playerTransform;
    private bool isActivated = false; // �P�w�I�O�_�Q�E��
    private ScreenGlitchEffect glitchController; // ��̯S�ı��

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

        // [�ק�] ���������������̱�� (�q PlayerInteraction ������)
        if (PlayerInteraction.Instance != null && PlayerInteraction.Instance.glitchController != null)
        {
            // ��� PlayerInteraction �W�� Glitch Controller �ޥ�
            this.glitchController = PlayerInteraction.Instance.glitchController;
            // ----- [!! 新增 !!] -----
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
    /// �� PlayerInteraction �I�s�A���տE���o�ӧP�w�I
    /// </summary>
    public bool ActivatePoint(VoiceItemData item)
    {
        if (item == requiredVoiceItem)
        {
            isActivated = true;
            Debug.Log($"[VoiceItemDetectionPoint] {gameObject.name} �w�Q�E���C");

            if (staticNoiseSource != null && !staticNoiseSource.isPlaying)
            {
                staticNoiseSource.Play(); // �}�l��������
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

        // �֤��޿�G�Z���V��A�j�� (intensity) �V���� 1.0
        float intensity = Mathf.InverseLerp(maxDetectionDistance, minDetectionDistance, distance);
        intensity = Mathf.Clamp01(intensity);

        // ----- [!! 新增 !!] -----
        // 而是直接修改 FilmGrain 參數的 .value
        // 因為 GlitchVolume (P=20, W=1) 會覆蓋 YinVolume (P=10, W=1)
        // 所以這裡的修改會 100% 顯示出來
        // 1. 應用到 FilmGrain
        if (filmGrainEffect != null)
        {
            filmGrainEffect.intensity.value = intensity;
        }
        
        // 2. 應用到 ChromaticAberration [!! 新增 !!]
        if (chromaticAberrationEffect != null)
        {
            chromaticAberrationEffect.intensity.value = intensity;
        }
        // ----- [!! 結束新增 !!] -----

        // 2. ������������
        if (staticNoiseSource != null)
        {
            staticNoiseSource.volume = intensity;
        }
    }

    /// <summary>
    /// �����a���i�P�w�I��
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // �����O�E�����A�A�B�i�J���O���a
        if (isActivated && other.CompareTag("Player"))
        {
            Debug.Log($"[VoiceItemDetectionPoint] ���a�w�i�J {gameObject.name} Ĳ�o�ϰ�C");
            isActivated = false;

            // 1. ����S�ĩM�n��
            if (glitchController != null)
            {
                glitchController.StopGlitch(); // (這會將 Weight 設為 0)
            }
            // ----- [!! 新增 !!] -----
            // 順手將參數也歸零，保持乾淨
            if (filmGrainEffect != null)
            {
                filmGrainEffect.intensity.value = 0f;
            }
            if (chromaticAberrationEffect != null) // [!! 新增 !!]
            {
                chromaticAberrationEffect.intensity.value = 0f;
            }
            // ----- [!! 結束新增 !!] -----
            if (staticNoiseSource != null)
            {
                staticNoiseSource.Stop();
            }

            // 2. ����ʵe (�z�w�[�]�n)
            if (playVideo != null)
            {
                playVideo.PlayForDeceased();
            }
            Debug.Log("[VoiceItemDetectionPoint] ����ʵe/�v��...");

            // 3. �q�� PlayerInteraction �y�{����
            PlayerInteraction.Instance.CompleteVoiceItemUsage(requiredVoiceItem);

            // 4. �T�Φ��P�w�I����
            gameObject.SetActive(false);

            // 5. �R���P�w�I
            Destroy(destroyObj);
        }
    }
}