using System.Collections.Generic;
using System.Linq;
using UnityEngine;
// using UnityEngine.InputSystem; // <-- 這行現在非必要，可以刪除

public class DialogueManager : MonoBehaviour
{
    [Header("全域資源參考")]
    // 【修改】移除了對 playerControls 的引用，因為 UIInputManager 會處理
    // [SerializeField] private InputActionAsset playerControls; 
    
    [Tooltip("請將場景中掛載了 DialogueUI 腳本的物件拖曳到此處")]
    [SerializeField] private DialogueUI dialogueUI; 

    [Header("統一對話管理")]
    [Tooltip("在此處設定場景中的所有對話及其觸發方式")]
    [SerializeField] private List<ManagedDialogue> managedDialogues = new List<ManagedDialogue>();
    
    [Header("全域設定")]
    [SerializeField] private bool pauseGameDuringDialogue = true;
    [SerializeField] private GameObject gameplayUI;
    
    public static DialogueManager Instance { get; private set; }
    
    private float _originalTimeScale;
    private Dictionary<DialogueRunner, ManagedDialogue> _runnerToDialogueMap = new Dictionary<DialogueRunner, ManagedDialogue>();

    private void Awake()
    {
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
        InitializeRunners();
    }

    private void Start()
    {
        var sceneStartDialogues = managedDialogues.Where(d => d != null && d.TriggerType == DialogueTriggerType.OnSceneStart);
        foreach (var dialogue in sceneStartDialogues)
        {
            StartManagedDialogue(dialogue);
        }
    }

    private void InitializeRunners()
    {
        // 找到場景中的 UIInputManager，以便將 playerControls 傳遞給 Runner
        // 這是為了確保 Runner 和其他系統使用同一個 Input Asset
        var inputManager = FindObjectOfType<UIInputManager>();
        if (inputManager == null || inputManager.playerControls == null)
        {
            Debug.LogError("DialogueManager 找不到 UIInputManager 或其 playerControls 資源！ Runner 將無法接收輸入。", this);
            return;
        }

        foreach (var dialogue in managedDialogues)
        {
            if (dialogue == null || dialogue.DialogueContainer == null) continue;

            var runnerGO = new GameObject($"Runner_{dialogue.DialogueContainer.name}");
            runnerGO.transform.SetParent(this.transform);
            
            var runner = runnerGO.AddComponent<DialogueRunner>();
            runner.SetDialogue(dialogue.DialogueContainer);
            // 將從 UIInputManager 來的共享 PlayerControls 傳給 Runner
            runner.SetPlayerControls(inputManager.playerControls);
            runner.SetDialogueUI(dialogueUI);
            
            dialogue.Runner = runner;
            _runnerToDialogueMap[runner] = dialogue;

            runner.OnDialogueStart.AddListener(() => OnAnyDialogueStart(runner));
            runner.OnDialogueEnd.AddListener(() => OnAnyDialogueEnd(runner));
        }
    }

    public void HandleInteraction(GameObject interactedObject)
    {
        var dialogue = managedDialogues.FirstOrDefault(d => d != null && d.TriggerType == DialogueTriggerType.OnInteraction && d.InteractionTarget == interactedObject);
        if (dialogue != null)
        {
            StartManagedDialogue(dialogue);
        }
    }

    public void HandleZoneEnter(Collider zoneCollider)
    {
        var dialogue = managedDialogues.FirstOrDefault(d => d != null && d.TriggerType == DialogueTriggerType.OnZoneEnter && d.ZoneTarget == zoneCollider);
        if (dialogue != null)
        {
            StartManagedDialogue(dialogue);
        }
    }

    public void HandleEvent(GameEvent gameEvent)
    {
        var dialogue = managedDialogues.FirstOrDefault(d => d != null && d.TriggerType == DialogueTriggerType.OnEvent && d.EventToListenFor == gameEvent);
        if (dialogue != null)
        {
            StartManagedDialogue(dialogue);
        }
    }

    private void StartManagedDialogue(ManagedDialogue dialogue)
    {
        if (dialogue == null) return;
        if (dialogue.TriggerOnlyOnce && dialogue.HasBeenTriggered) return;
        
        if (dialogue.Runner != null)
        {
            dialogue.Runner.StartDialogue();
        }
        else
        {
            Debug.LogError($"對話 '{dialogue.Name}' 缺少對應的 DialogueRunner！", this);
        }
    }
    
    private void OnAnyDialogueStart(DialogueRunner dialogueRunner)
    {
        if (pauseGameDuringDialogue) Time.timeScale = 0f;
        if (gameplayUI != null) gameplayUI.SetActive(false);

        // 【最終修改】指揮 UIInputManager 進入 UI 模式
        UIInputManager.Instance?.EnterUIMode();
    }

    private void OnAnyDialogueEnd(DialogueRunner dialogueRunner)
    {
        if (_runnerToDialogueMap.TryGetValue(dialogueRunner, out ManagedDialogue dialogue))
        {
            if (pauseGameDuringDialogue) Time.timeScale = _originalTimeScale;
            if (gameplayUI != null) gameplayUI.SetActive(true);
            
            if (dialogue.TriggerOnlyOnce)
            {
                dialogue.HasBeenTriggered = true;
            }
            
            Debug.Log($"對話結束：{dialogue.Name}");

            // 【最終修改】指揮 UIInputManager 回到遊戲模式
            UIInputManager.Instance?.EnterGameplayMode();
        }
    }
}