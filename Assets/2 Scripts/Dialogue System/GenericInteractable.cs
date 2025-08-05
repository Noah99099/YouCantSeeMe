using UnityEngine;
using UnityEngine.InputSystem;

// 掛載在任何可互動的物件上，例如 NPC
public class GenericInteractable : MonoBehaviour
{
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private GameObject interactionPromptUI; // "按 E 互動" 的提示
    private Transform _playerTransform;
    private bool _playerInRange = false;

    private void Start()
    {
        _playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        if (interactionPromptUI != null) interactionPromptUI.SetActive(false);
    }

    private void Update()
    {
        if (_playerTransform == null) return;

        _playerInRange = Vector3.Distance(transform.position, _playerTransform.position) <= interactionDistance;
        if (interactionPromptUI != null) interactionPromptUI.SetActive(_playerInRange);

        if (_playerInRange && Keyboard.current.eKey.wasPressedThisFrame) // 簡化輸入檢測
        {
            // 通知 DialogueManager
            DialogueManager.Instance?.HandleInteraction(this.gameObject);
        }
    }
}