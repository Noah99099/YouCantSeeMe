using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

[Serializable]
public class DialogueEvent : UnityEvent<string> { }

public class DialogueRunner : MonoBehaviour
{
    [Header("對話資料")]
    [SerializeField] private DialogueContainerSO dialogueContainer;

    [Header("執行元件")]
    private DialogueUI dialogueUI;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private bool autoPlayEnabled = false;
    [SerializeField] private float autoPlayDelay = 1.5f;

    [Header("事件")]
    public UnityEvent OnDialogueStart = new UnityEvent();
    public UnityEvent OnDialogueEnd = new UnityEvent();
    public DialogueEvent OnVariableChanged = new DialogueEvent();

    private InputActionAsset playerControls;
    
    private InputAction dialogueAdvanceAction;
    private InputAction autoPlayAction;
    private InputAction skipAction;

    private const string UI_ACTION_MAP_NAME = "UI";
    private const string ADVANCE_DIALOGUE_ACTION_NAME = "AdvanceDialogue";
    private DialogueNodeData _currentNode;
    private bool _isWaitingForContinue = false;
    private NodeLinkData _pendingContinueLink;
    private bool _isPlayingAudio = false;
    private Coroutine _typewriterCoroutine;
    private bool _isTyping = false;
    private Dictionary<string, object> _dialogueVariables = new();

    private float _lastNodeShowTime;
    private const float INPUT_COOLDOWN = 0.1f;

    public bool IsAutoPlayEnabled => autoPlayEnabled;
    
    private void Start() => SetupInputSystem();
    
    public void SetDialogueUI(DialogueUI ui) { dialogueUI = ui; }
    public void SetDialogue(DialogueContainerSO container) { dialogueContainer = container; }
    public void SetPlayerControls(InputActionAsset controls) { playerControls = controls; }

    private void OnAutoPlayPerformed(InputAction.CallbackContext context) => ToggleAutoPlay();
    private void OnSkipPerformed(InputAction.CallbackContext context) => SkipToNextImportantNode();
    private UIInputManager inputManager;

    void Awake()
    {
        // 在 Awake() 中找到 UIInputManager 實例
        inputManager = FindObjectOfType<UIInputManager>();
        if (inputManager == null)
        {
            Debug.LogError("找不到 UIInputManager 實例！請確認場景中存在一個。", this);
        }
    }

    public void ToggleAutoPlay()
    {
        autoPlayEnabled = !autoPlayEnabled;
        dialogueUI?.UpdateModeButtons(autoPlayEnabled, false);

        if (autoPlayEnabled && _isWaitingForContinue)
        {
            StartCoroutine(AutoAdvanceAfterDelay(_pendingContinueLink));
        }
    }

    private void SetupInputSystem()
    {
        if (dialogueUI == null) { Debug.LogError("DialogueUI 沒有被 DialogueManager 正確指派！", this); return; }
        if (playerControls == null) { Debug.LogError("Player Controls 未設定", this); return; }
        var uiActionMap = playerControls.FindActionMap(UI_ACTION_MAP_NAME);
        if (uiActionMap == null) { Debug.LogError($"在 PlayerControls 中找不到名為 '{UI_ACTION_MAP_NAME}' 的 Action Map！"); return; }
        
        dialogueAdvanceAction = uiActionMap.FindAction(ADVANCE_DIALOGUE_ACTION_NAME);
        autoPlayAction = uiActionMap.FindAction("ToggleAutoPlay");
        skipAction = uiActionMap.FindAction("ToggleSkipMode");
        
        dialogueAdvanceAction.performed += OnNextDialogue;
        autoPlayAction.performed += OnAutoPlayPerformed;
        skipAction.performed += OnSkipPerformed;
    }

    private void OnEnable()
    {
        dialogueAdvanceAction?.Enable();
        autoPlayAction?.Enable();
        skipAction?.Enable();
    }

    private void OnDisable()
    {
        dialogueAdvanceAction?.Disable();
        autoPlayAction?.Disable();
        skipAction?.Disable();
    }

    private void OnDestroy()
    {
        if (dialogueAdvanceAction != null) dialogueAdvanceAction.performed -= OnNextDialogue;
        if (autoPlayAction != null) autoPlayAction.performed -= OnAutoPlayPerformed;
        if (skipAction != null) skipAction.performed -= OnSkipPerformed;
    }
    
    private void OnNextDialogue(InputAction.CallbackContext context)
    {
        if (Time.realtimeSinceStartup < _lastNodeShowTime + INPUT_COOLDOWN) return;
        if (_isTyping) { CompleteTypewriter(); }
        else if (_isWaitingForContinue) { AdvanceDialogue(); }
    }

    private void AdvanceDialogue()
    {
        if (_isWaitingForContinue)
        {
            _isWaitingForContinue = false;
            GoToNextNode(_pendingContinueLink);
        }
    }

    private void ShowNode(DialogueNodeData node)
    {
        if (node == null) return;
        _currentNode = node;
        _isWaitingForContinue = false;
        _lastNodeShowTime = Time.realtimeSinceStartup;

        if (!CheckNodeConditions(node))
        {
            var allLinks = dialogueContainer.NodeLinks.Where(l => l.BaseNodeGuid == node.Guid).ToList();
            var continueLink = allLinks.FirstOrDefault(l => l.PortName == "繼續");
            if (continueLink != null) { GoToNextNode(continueLink); return; }
            
            dialogueUI.Hide();
            OnDialogueEnd?.Invoke();
            return;
        }

        ExecuteNodeActions(node);
        PlayNodeAudio(node);
        StartTypewriter(node);
        HandleNodeBranching(node);
    }
    
    private bool CheckNodeConditions(DialogueNodeData node) => node.Conditions.All(EvaluateCondition);
    private bool EvaluateCondition(DialogueCondition condition)
    {
        if (!_dialogueVariables.TryGetValue(condition.variableName, out var value)) return false;
        string compareValue = condition.compareValue;
        if (float.TryParse(value.ToString(), out float varF) && float.TryParse(compareValue, out float compF))
        {
            return condition.comparisonOperator switch
            {
                ComparisonOperator.Equal => Mathf.Approximately(varF, compF),
                ComparisonOperator.NotEqual => !Mathf.Approximately(varF, compF),
                ComparisonOperator.GreaterThan => varF > compF,
                ComparisonOperator.LessThan => varF < compF,
                ComparisonOperator.GreaterOrEqual => varF >= compF,
                ComparisonOperator.LessOrEqual => varF <= compF,
                _ => false,
            };
        }
        else
        {
            return condition.comparisonOperator switch
            {
                ComparisonOperator.Equal => value.ToString() == compareValue,
                ComparisonOperator.NotEqual => value.ToString() != compareValue,
                _ => false,
            };
        }
    }
    private void ExecuteNodeActions(DialogueNodeData node)
    {
        foreach (var action in node.Actions)
        {
            if (action.actionType == "SetVariable")
                SetVariable(action.parameter1, action.parameter2);
            else if (action.actionType == "TriggerEvent")
                OnVariableChanged?.Invoke($"Event:{action.parameter1}");
        }
    }
    private void PlayNodeAudio(DialogueNodeData node) { }
    private void StartTypewriter(DialogueNodeData node)
    {
        if (_typewriterCoroutine != null) StopCoroutine(_typewriterCoroutine);
        if (node.TextSpeed > 0 && dialogueUI != null)
        {
            _typewriterCoroutine = StartCoroutine(TypewriterEffect(node));
        }
        else
        {
            dialogueUI?.ShowNodeText(node);
            _isTyping = false;
        }
    }
    private IEnumerator TypewriterEffect(DialogueNodeData node)
    {
        _isTyping = true;
        dialogueUI.ShowNodeText(node, "");
        string text = node.DialogueText;
        float delay = node.TextSpeed > 0 ? 1f / node.TextSpeed : 0;
        if (string.IsNullOrEmpty(text))
        {
            _isTyping = false;
            yield break;
        }
        for (int i = 0; i <= text.Length; i++)
        {
            dialogueUI.UpdateDialogueText(text.Substring(0, i));
            if (delay > 0)
            {
                float startTime = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup < startTime + delay)
                {
                    yield return null;
                }
            }
        }
        _isTyping = false;
    }
    private void CompleteTypewriter()
    {
        if (_typewriterCoroutine != null) StopCoroutine(_typewriterCoroutine);
        if (_currentNode != null) dialogueUI.UpdateDialogueText(_currentNode.DialogueText);
        _isTyping = false;
    }
    private void HandleNodeBranching(DialogueNodeData node)
    {
        dialogueUI.HideChoices();
        var allLinks = dialogueContainer.NodeLinks.Where(l => l.BaseNodeGuid == node.Guid).ToList();
        var choiceLinks = allLinks.Where(l => l.PortName != "繼續").ToList();
        var continueLink = allLinks.FirstOrDefault(l => l.PortName == "繼續");
        if (choiceLinks.Count > 0)
        {
            _isWaitingForContinue = false;
            _pendingContinueLink = null;
            dialogueUI.ShowChoices(choiceLinks, OnChoiceSelected);
            dialogueUI.HideContinueIndicator();
        }
        else if (continueLink != null)
        {
            if (_isPlayingAudio && node.WaitForAudio)
                StartCoroutine(WaitForAudioThenContinue(continueLink));
            else if (autoPlayEnabled)
                StartCoroutine(AutoAdvanceAfterDelay(continueLink));
            else
            {
                _isWaitingForContinue = true;
                _pendingContinueLink = continueLink;
                dialogueUI.ShowContinueIndicator();
            }
        }
        else
        {
            Debug.Log("[DialogueRunner] 到達分支的終點，準備隱藏 UI 並結束對話。", this.gameObject);
            dialogueUI.Hide();
            OnDialogueEnd?.Invoke();
        }
    }
    private IEnumerator WaitForAudioThenContinue(NodeLinkData continueLink)
    {
        while (_isPlayingAudio && audioSource?.isPlaying == true) yield return null;
        _isTyping = false;
        _isWaitingForContinue = true;
        _pendingContinueLink = continueLink;
        dialogueUI.ShowContinueIndicator();
    }
    private IEnumerator AutoAdvanceAfterDelay(NodeLinkData link)
    {
        yield return new WaitForSecondsRealtime(autoPlayDelay);
        GoToNextNode(link);
    }
    private void OnChoiceSelected(NodeLinkData link) => GoToNextNode(link);
    private void GoToNextNode(NodeLinkData link)
    {
        if (link == null)
        {
            dialogueUI.Hide();
            // 【✅ 修正：在對話結束時，強制回到準心模式】
            FindObjectOfType<UIInputManager>()?.EnterGameplayMode();
            OnDialogueEnd?.Invoke();
            return;
        }
        var next = dialogueContainer.DialogueNodes.Find(n => n.Guid == link.TargetNodeGuid);
        // 如果根據連線資料找不到目標節點 (可能該節點已被刪除)
        if (next == null)
        {
            // 就將其視為對話的終點，並正常結束對話
            EndDialogue($"找不到目標節點 (GUID: {link.TargetNodeGuid})，可能是一個懸空的連線。");
            return;
        }
        ShowNode(next);
    }
    public void StartDialogue()
    {
        if (dialogueContainer == null) { Debug.LogError("此 DialogueRunner 沒有被指派 DialogueContainerSO！", this); return; }
        if (dialogueUI != null) { dialogueUI.SetActiveRunner(this); }
        
        // 【✅ 修正：只呼叫一次】
        // 這裡我們使用 FindObjectOfType，因為 DialogueRunner 可能不知道 UIInputManager
        FindObjectOfType<UIInputManager>()?.EnterUIMode();

        OnDialogueStart?.Invoke();
        var entry = dialogueContainer.DialogueNodes.Find(n => n.EntryPoint);
        if (entry != null) ShowNode(entry);
        else
        {
            Debug.LogError("找不到對話進入點 (Entry Point)！", this);
            dialogueUI.Hide();
            // 如果找不到進入點，也應該回到遊戲模式
            OnDialogueEnd?.Invoke();
            FindObjectOfType<UIInputManager>()?.EnterGameplayMode();
        }
    }
    private void EndDialogue(string reason)
    {
        Debug.Log($"對話結束，原因: {reason}");
        dialogueUI?.Hide();
        
        // 保留您對 UIInputManager 的呼叫
        inputManager?.EnterGameplayMode();
        
        OnDialogueEnd?.Invoke();
    }
    public void SetVariable(string key, object value)
    {
        _dialogueVariables[key] = value;
        OnVariableChanged?.Invoke($"{key}:{value}");
    }
    public T GetVariable<T>(string key, T defaultValue = default) =>
        _dialogueVariables.TryGetValue(key, out var v) ? (T)Convert.ChangeType(v, typeof(T)) : defaultValue;
    public bool HasVariable(string key) => _dialogueVariables.ContainsKey(key);
    public void JumpToNode(string label)
    {
        var node = dialogueContainer.DialogueNodes.Find(n => n.NodeLabel == label);
        if (node != null) ShowNode(node);
        else Debug.LogWarning($"找不到標籤為 '{label}' 的節點！");
    }

    // 【*** 全新的、基於圖形遍歷的跳轉邏輯 ***】
    public void SkipToNextImportantNode()
    {
        if (_currentNode == null || dialogueContainer == null) return;

        // 使用佇列 (Queue) 進行廣度優先搜尋 (BFS)，並用 HashSet 追蹤已訪問的節點以避免無限循環
        Queue<string> nodesToVisit = new Queue<string>();
        HashSet<string> visitedNodes = new HashSet<string>();

        // 搜尋的起點是當前節點的所有「出口」
        var initialLinks = dialogueContainer.NodeLinks.Where(link => link.BaseNodeGuid == _currentNode.Guid);
        foreach (var link in initialLinks)
        {
            if (!visitedNodes.Contains(link.TargetNodeGuid))
            {
                nodesToVisit.Enqueue(link.TargetNodeGuid);
                visitedNodes.Add(link.TargetNodeGuid);
            }
        }

        // 開始遍歷搜尋
        while (nodesToVisit.Count > 0)
        {
            string currentGuid = nodesToVisit.Dequeue();
            var targetNodeData = dialogueContainer.DialogueNodes.FirstOrDefault(n => n.Guid == currentGuid);

            if (targetNodeData == null) continue;

            // 檢查這個節點是否為一個「停止點」(重要節點 或 選項節點)
            bool hasChoices = dialogueContainer.NodeLinks.Any(link => link.BaseNodeGuid == targetNodeData.Guid && link.PortName != "繼續");

            if (targetNodeData.IsImportant || hasChoices)
            {
                // 找到了目標！跳轉到該節點並結束搜尋
                GoToNextNode(new NodeLinkData { TargetNodeGuid = targetNodeData.Guid });
                return;
            }

            // 如果不是停止點，則將它的所有「出口」加入待訪問佇列
            var nextLinks = dialogueContainer.NodeLinks.Where(link => link.BaseNodeGuid == currentGuid);
            foreach (var link in nextLinks)
            {
                if (!visitedNodes.Contains(link.TargetNodeGuid))
                {
                    nodesToVisit.Enqueue(link.TargetNodeGuid);
                    visitedNodes.Add(link.TargetNodeGuid);
                }
            }
        }

        Debug.Log("找不到下一個重要節點或選項，對話結束。");
        dialogueUI.Hide();
        // 【✅ 修正：在對話結束時，強制回到準心模式】
        FindObjectOfType<UIInputManager>()?.EnterGameplayMode();
        OnDialogueEnd?.Invoke();
    }
}