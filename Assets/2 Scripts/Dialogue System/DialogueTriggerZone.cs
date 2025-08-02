using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DialogueTriggerZone : MonoBehaviour
{
    [Header("觸發區域設定")]
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private bool triggerOnlyOnce = true;
    [SerializeField] private bool requirePlayerTag = true;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float triggerDelay = 0f; // 進入觸發區後延遲多久開始對話
    
    [Header("視覺提示")]
    [SerializeField] private GameObject triggerEffectPrefab; // 觸發時的特效
    [SerializeField] private AudioClip triggerSound; // 觸發時的音效
    
    private bool _hasTriggered = false;
    private bool _playerInZone = false;
    private AudioSource _audioSource;

    private void Awake()
    {
        // 確保觸發器設定正確
        var collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }

        // 設定音效組件
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null && triggerSound != null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 檢查是否為玩家
        if (requirePlayerTag && !other.CompareTag(playerTag))
            return;

        if (_hasTriggered && triggerOnlyOnce)
            return;

        if (dialogueRunner == null)
        {
            Debug.LogError("DialogueRunner 未設定！", this);
            return;
        }

        _playerInZone = true;
        
        // 播放觸發音效
        PlayTriggerSound();
        
        // 顯示觸發特效
        ShowTriggerEffect();
        
        // 開始對話（可能有延遲）
        if (triggerDelay > 0)
        {
            Invoke(nameof(StartTriggeredDialogue), triggerDelay);
        }
        else
        {
            StartTriggeredDialogue();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (requirePlayerTag && !other.CompareTag(playerTag))
            return;

        _playerInZone = false;
        
        // 如果設定了延遲但玩家已經離開，取消觸發
        CancelInvoke(nameof(StartTriggeredDialogue));
    }

    private void StartTriggeredDialogue()
    {
        if (_playerInZone && (!_hasTriggered || !triggerOnlyOnce))
        {
            dialogueRunner.StartDialogue();
            _hasTriggered = true;
            
            Debug.Log($"觸發區域對話已開始：{gameObject.name}");
        }
    }

    private void PlayTriggerSound()
    {
        if (_audioSource != null && triggerSound != null)
        {
            _audioSource.PlayOneShot(triggerSound);
        }
    }

    private void ShowTriggerEffect()
    {
        if (triggerEffectPrefab != null)
        {
            var effect = Instantiate(triggerEffectPrefab, transform.position, transform.rotation);
            // 自動銷毀特效（假設特效持續 3 秒）
            Destroy(effect, 3f);
        }
    }

    // 公開方法，允許重置觸發狀態
    public void ResetTrigger()
    {
        _hasTriggered = false;
    }

    // 手動觸發對話的方法
    public void ManualTrigger()
    {
        if (!_hasTriggered || !triggerOnlyOnce)
        {
            StartTriggeredDialogue();
        }
    }

    // 在場景視圖中顯示觸發範圍
    private void OnDrawGizmos()
    {
        var collider = GetComponent<Collider>();
        if (collider != null)
        {
            Gizmos.color = _hasTriggered ? Color.gray : Color.green;
            Gizmos.matrix = transform.localToWorldMatrix;
            
            if (collider is BoxCollider boxCollider)
            {
                Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
            }
            else if (collider is SphereCollider sphereCollider)
            {
                Gizmos.DrawWireSphere(sphereCollider.center, sphereCollider.radius);
            }
            else if (collider is CapsuleCollider capsuleCollider)
            {
                Gizmos.DrawWireCube(capsuleCollider.center, new Vector3(capsuleCollider.radius * 2, capsuleCollider.height, capsuleCollider.radius * 2));
            }
        }

        // 顯示觸發器名稱
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up, gameObject.name);
        #endif
    }
}