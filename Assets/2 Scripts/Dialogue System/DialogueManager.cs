using System.Collections.Generic;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    [Header("對話管理")]
    [SerializeField] private List<DialogueRunner> availableDialogues = new List<DialogueRunner>();
    
    [Header("全域設定")]
    [SerializeField] private bool pauseGameDuringDialogue = true;
    [SerializeField] private GameObject gameplayUI; // 遊戲時要隱藏的 UI
    
    public static DialogueManager Instance { get; private set; }
    
    private DialogueRunner _currentActiveDialogue;
    private float _originalTimeScale;

    private void Awake()
    {
        // 單例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        _originalTimeScale = Time.timeScale;
        SetupDialogueListeners();
    }

    private void SetupDialogueListeners()
    {
        foreach (var dialogue in availableDialogues)
        {
            if (dialogue != null)
            {
                dialogue.OnDialogueStart.AddListener(() => OnAnyDialogueStart(dialogue));
                dialogue.OnDialogueEnd.AddListener(() => OnAnyDialogueEnd(dialogue));
            }
        }
    }

    private void OnAnyDialogueStart(DialogueRunner dialogueRunner)
    {
        _currentActiveDialogue = dialogueRunner;
        
        if (pauseGameDuringDialogue)
        {
            Time.timeScale = 0f;
        }
        
        if (gameplayUI != null)
        {
            gameplayUI.SetActive(false);
        }
        
        Debug.Log($"對話開始：{dialogueRunner.gameObject.name}");
    }

    private void OnAnyDialogueEnd(DialogueRunner dialogueRunner)
    {
        if (_currentActiveDialogue == dialogueRunner)
        {
            _currentActiveDialogue = null;
        }
        
        if (pauseGameDuringDialogue)
        {
            Time.timeScale = _originalTimeScale;
        }
        
        if (gameplayUI != null)
        {
            gameplayUI.SetActive(true);
        }
        
        Debug.Log($"對話結束：{dialogueRunner.gameObject.name}");
    }

    // 公開方法
    public bool IsAnyDialogueActive()
    {
        return _currentActiveDialogue != null;
    }

    public void StopCurrentDialogue()
    {
        if (_currentActiveDialogue != null)
        {
            _currentActiveDialogue.GetComponent<DialogueUI>()?.Hide();
            OnAnyDialogueEnd(_currentActiveDialogue);
        }
    }

    public void StartDialogueByName(string dialogueName)
    {
        var dialogue = availableDialogues.Find(d => d.gameObject.name == dialogueName);
        if (dialogue != null)
        {
            dialogue.StartDialogue();
        }
        else
        {
            Debug.LogWarning($"找不到名稱為 '{dialogueName}' 的對話！");
        }
    }
}