using System.Collections.Generic;
using System.Linq;
using UnityEngine;
// using UnityEngine.InputSystem; // <-- 這行現在非必要，可以刪除

public class DialogueManager : MonoBehaviour
{
    [Header("全域資源參考")]
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
        //if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        //else { Destroy(gameObject); return; }
        
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
        foreach (var dialogue in managedDialogues)
        {
            if (dialogue == null || dialogue.DialogueContainer == null) continue;
            var runnerGO = new GameObject($"Runner_{dialogue.DialogueContainer.name}");
            runnerGO.transform.SetParent(this.transform);
            var runner = runnerGO.AddComponent<DialogueRunner>();
            runner.SetDialogue(dialogue.DialogueContainer);
            runner.SetDialogueUI(dialogueUI);
            dialogue.Runner = runner;
            _runnerToDialogueMap[runner] = dialogue;

            runner.OnDialogueStart.AddListener(() => OnAnyDialogueStart(runner));
            
            // 【關鍵】確認事件是否被接收的日誌
            runner.OnDialogueEnd.AddListener(() => {
                Debug.Log("<color=aqua>CHAIN_STEP_3: DialogueManager 成功接收到 OnDialogueEnd 事件！準備呼叫 OnAnyDialogueEnd()。</color>");
                OnAnyDialogueEnd(runner);
            });
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
        UIInputManager.Instance?.EnterDialogueMode();
    }

    private void OnAnyDialogueEnd(DialogueRunner dialogueRunner)
    {
        Debug.Log("<color=aqua>CHAIN_STEP_4: OnAnyDialogueEnd() 方法開始執行。</color>");
        if (_runnerToDialogueMap.TryGetValue(dialogueRunner, out ManagedDialogue dialogue))
        {
            if (pauseGameDuringDialogue) Time.timeScale = _originalTimeScale;
            if (gameplayUI != null) gameplayUI.SetActive(true);
            if(dialogueUI != null) dialogueUI.Hide();
            if (dialogue.TriggerOnlyOnce) { dialogue.HasBeenTriggered = true; }
            UIInputManager.Instance?.EnterGameplayMode();
        }
    }
}