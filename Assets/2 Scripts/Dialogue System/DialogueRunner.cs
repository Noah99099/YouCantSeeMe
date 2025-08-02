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
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private bool autoPlayEnabled = false;
    [SerializeField] private bool skipModeEnabled = false;
    [SerializeField] private float autoPlayDelay = 1.5f;

    [Header("事件")]
    public UnityEvent OnDialogueStart;
    public UnityEvent OnDialogueEnd;
    public DialogueEvent OnVariableChanged;

    [SerializeField] private InputActionAsset playerControls;
    private InputAction dialogueAdvanceAction;
    private InputAction autoPlayAction;
    private InputAction skipModeAction;

    private const string UI_ACTION_MAP_NAME = "UI";
    private const string ADVANCE_DIALOGUE_ACTION_NAME = "AdvanceDialogue";

    private DialogueNodeData _currentNode;
    private bool _isWaitingForContinue = false;
    private NodeLinkData _pendingContinueLink;
    private bool _isPlayingAudio = false;
    private Coroutine _typewriterCoroutine;
    private bool _isTyping = false;

    private Dictionary<string, object> _dialogueVariables = new();

    public bool IsAutoPlayEnabled => autoPlayEnabled;
    public bool IsSkipModeEnabled => skipModeEnabled;
    private void OnAutoPlayPerformed(InputAction.CallbackContext context) => ToggleAutoPlay();
    private void OnSkipModePerformed(InputAction.CallbackContext context) => ToggleSkipMode();

    public void ToggleAutoPlay()
    {
        autoPlayEnabled = !autoPlayEnabled;
        if (autoPlayEnabled) skipModeEnabled = false;
        dialogueUI?.UpdateModeButtons(autoPlayEnabled, skipModeEnabled);
    }

    public void ToggleSkipMode()
    {
        skipModeEnabled = !skipModeEnabled;
        if (skipModeEnabled) autoPlayEnabled = false;
        dialogueUI?.UpdateModeButtons(autoPlayEnabled, skipModeEnabled);
    }

    private void Awake() => SetupInputSystem();

    private void SetupInputSystem()
    {
        if (playerControls == null)
        {
            Debug.LogError("Player Controls 未設定", this);
            return;
        }

        var uiActionMap = playerControls.FindActionMap(UI_ACTION_MAP_NAME);
        if (uiActionMap == null) return;

        dialogueAdvanceAction = uiActionMap.FindAction(ADVANCE_DIALOGUE_ACTION_NAME);
        autoPlayAction = uiActionMap.FindAction("ToggleAutoPlay");
        skipModeAction = uiActionMap.FindAction("ToggleSkipMode");

        dialogueAdvanceAction.canceled += OnNextDialogue;
        autoPlayAction.performed += OnAutoPlayPerformed;
        skipModeAction.performed += OnSkipModePerformed;
    }

    private void OnEnable()
    {
        dialogueAdvanceAction?.Enable();
        autoPlayAction?.Enable();
        skipModeAction?.Enable();
    }

    private void OnDisable()
    {
        dialogueAdvanceAction?.Disable();
        autoPlayAction?.Disable();
        skipModeAction?.Disable();
    }

    private void OnDestroy()
    {
        dialogueAdvanceAction.canceled -= OnNextDialogue;
        autoPlayAction.performed -= ctx => ToggleAutoPlay();
        skipModeAction.performed -= ctx => ToggleSkipMode();
    }

    private void OnNextDialogue(InputAction.CallbackContext context)
    {
        if (_isTyping)
        {
            CompleteTypewriter();
            return;
        }

        if (_isPlayingAudio && _currentNode != null && _currentNode.WaitForAudio)
        {
            if (audioSource?.isPlaying == true)
                audioSource.Stop();
            _isPlayingAudio = false;
        }

        if (_isWaitingForContinue || skipModeEnabled)
            AdvanceDialogue();
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

        if (!CheckNodeConditions(node))
        {
            var allLinks = dialogueContainer.NodeLinks.Where(l => l.BaseNodeGuid == node.Guid).ToList();
            var continueLink = allLinks.FirstOrDefault(l => l.PortName == "繼續");
            if (continueLink != null)
            {
                GoToNextNode(continueLink);
                return;
            }
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
                _ => false
            };
        }
        else
        {
            return condition.comparisonOperator switch
            {
                ComparisonOperator.Equal => value.ToString() == compareValue,
                ComparisonOperator.NotEqual => value.ToString() != compareValue,
                _ => false
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

    private void PlayNodeAudio(DialogueNodeData node)
    {
        // 你可在這裡實作音效載入邏輯
    }

    private void StartTypewriter(DialogueNodeData node)
    {
        if (_typewriterCoroutine != null)
            StopCoroutine(_typewriterCoroutine);

        if (node.TextSpeed > 0)
            _typewriterCoroutine = StartCoroutine(TypewriterEffect(node));
        else
        {
            dialogueUI.ShowNodeText(node);
            _isTyping = false;
        }
    }

    private IEnumerator TypewriterEffect(DialogueNodeData node)
    {
        _isTyping = true;
        dialogueUI.ShowNodeText(node, "");

        string text = node.DialogueText;
        float delay = 1f / node.TextSpeed;

        for (int i = 0; i <= text.Length; i++)
        {
            dialogueUI.UpdateDialogueText(text[..i]);
            yield return new WaitForSeconds(delay);
        }

        _isTyping = false;
    }

    private void CompleteTypewriter()
    {
        if (_typewriterCoroutine != null)
            StopCoroutine(_typewriterCoroutine);

        if (_currentNode != null)
            dialogueUI.UpdateDialogueText(_currentNode.DialogueText);

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
            else if (skipModeEnabled)
                GoToNextNode(continueLink);
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
            dialogueUI.Hide();
            OnDialogueEnd?.Invoke();
        }
    }

    private IEnumerator WaitForAudioThenContinue(NodeLinkData continueLink)
    {
        while (_isPlayingAudio && audioSource?.isPlaying == true)
            yield return null;

        _isPlayingAudio = false;
        _isWaitingForContinue = true;
        _pendingContinueLink = continueLink;
        dialogueUI.ShowContinueIndicator();
    }

    private IEnumerator AutoAdvanceAfterDelay(NodeLinkData link)
    {
        yield return new WaitForSeconds(autoPlayDelay);
        GoToNextNode(link);
    }

    private void OnChoiceSelected(NodeLinkData link) => GoToNextNode(link);

    private void GoToNextNode(NodeLinkData link)
    {
        if (link == null)
        {
            dialogueUI.Hide();
            OnDialogueEnd?.Invoke();
            return;
        }

        var next = dialogueContainer.DialogueNodes.Find(n => n.Guid == link.TargetNodeGuid);
        ShowNode(next);
    }

    public void StartDialogue()
    {
        OnDialogueStart?.Invoke();
        var entry = dialogueContainer.DialogueNodes.Find(n => n.EntryPoint);
        if (entry != null) ShowNode(entry);
        else
        {
            Debug.LogError("找不到對話進入點 (Entry Point)！");
            dialogueUI.Hide();
            OnDialogueEnd?.Invoke();
        }
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
}
