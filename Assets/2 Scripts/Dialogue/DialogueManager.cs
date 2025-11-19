using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using XNode;
using System.Collections.Generic;
using System.Linq;
using System;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    // ***** 需求修改: 2. 在此定義 OnConversationEnd 事件 *****
    /// <summary>
    /// 當對話結束時 (在 PopMap() 並完成所有清理之後) 觸發的事件
    /// </summary>
    public event Action OnConversationEnd;
    // ***** 需求修改: 結束 *****

    [Header("核心元件")]
    [SerializeField] private DialogueUI dialogueUI;

    [Header("對話設定")]
    [SerializeField] private float typeWriterSpeed = 0.05f;
    [SerializeField] private float autoPlayDelay = 2.0f;
    [SerializeField] private float cameraReturnDuration = 1.0f;
    [Header("遊戲 UI 控制")]
    [Tooltip("將您場景中代表準心的 UI GameObject 拖曳到這裡")]
    [SerializeField] private GameObject crosshairUI;
    [Tooltip("將您場景中其他需要隱藏的 UI GameObject 拖曳到這裡")]
    [SerializeField] private GameObject otherUI;


    // 內部狀態變數
    private bool isDialogueActive = false;
    private bool isTyping = false;
    private bool isAutoPlay = false;
    private bool isWaitingForChoice = false;

    // 系統與工具
    [Header("輸入設定")]
    [SerializeField] private string dialogueActionMapName = "Dialogue"; // 用於 InputStackManager
    private Camera mainCamera;

    // 事件系統
    // 【修改】將字典的值改為一個列表 (List)，以支援 "一對多" 廣播
    private Dictionary<string, List<DialogueEventListener>> eventListeners = new Dictionary<string, List<DialogueEventListener>>();
    // 【新功能】: 用於 "遊戲事件 -> 觸發對話" 的註冊表
    private Dictionary<string, DialogueGraph> graphEventRegistry = new Dictionary<string, DialogueGraph>();

    // 攝影機還原
    private Vector3 originalCameraPos;
    private Quaternion originalCameraRot;
    private bool cameraHasBeenMoved = false;
    private DialogueGraph currentGraph;
    private BaseNode currentNode;
    private List<NodePort> _currentChoicePorts;

    #region Unity生命週期與輸入
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
        }
        mainCamera = Camera.main;
    }

    private void OnEnable()
    {

    }

    private void OnDisable()
    {

    }

    #endregion

    #region 公開方法 (啟動/註冊)
    public void StartConversation(DialogueGraph graph)
    {
        if (isDialogueActive) return;

        if (InputStackManager.Instance != null)
        {
            InputStackManager.Instance.PushMap(dialogueActionMapName); //
            Debug.Log($"[DialogueManager] Pushed '{dialogueActionMapName}' map to Input Stack."); //
        }
        else Debug.LogError("[DialogueManager] InputStackManager Instance not found!");

        // +++ 在這裡加入事件訂閱 +++
        if (InputProvider.InputActions != null)
        {
            InputProvider.InputActions.Dialogue.Submit.performed += SubmitDialogue;
            InputProvider.InputActions.Dialogue.ToggleAutoPlay.performed += OnToggleAutoPlay;
            InputProvider.InputActions.Dialogue.Skip.performed += OnSkip;
        }
        else
        {
            Debug.LogError("DialogueManager: 在 StartConversation 時 InputProvider.InputActions 仍為 null！");
        }

        if (crosshairUI != null)
        {
            crosshairUI.SetActive(false);
        }

        if (otherUI != null)
        {
            otherUI.SetActive(false);
        }

        isDialogueActive = true;
        isAutoPlay = false;
        cameraHasBeenMoved = false;
        currentGraph = graph;

        if (currentGraph != null)
        {
            // 【重要】我們不再於此處重置變數。
            // 重置的工作將交給 GameInitializer.cs 
            // 在遊戲一開始就全部處理完畢。
            // currentGraph.ResetVariables(); // <--- 已刪除
        }

        StartNode startNode = graph.nodes.OfType<StartNode>().FirstOrDefault();
        if (startNode == null)
        {
            Debug.LogError($"錯誤：在對話圖形 '{graph.name}' 中找不到 StartNode！");
            EndConversation();
            return;
        }

        currentNode = startNode.GetNextNode();
        if (currentNode == null)
        {
            Debug.LogError($"錯誤：StartNode 的 exit 出口沒有連接到任何節點！");
            EndConversation();
            return;
        }

        dialogueUI.gameObject.SetActive(true); // <--- 啟用 DialogueUI 主物件
        ProcessCurrentNode();
    }
    public void RegisterListener(DialogueEventListener listener)
    {
        if (listener == null) return;

        // 遍歷該監聽器組件中 "所有" 的事件項目
        foreach (var entry in listener.eventEntries)
        {
            string trimmedID = entry.eventID.Trim();
            if (string.IsNullOrEmpty(trimmedID)) continue;

            // 【修改】註冊邏輯
            // 1. 檢查這個 ID 是否已經有列表了
            if (!eventListeners.ContainsKey(trimmedID))
            {
                // 如果沒有，就建立一個新的列表
                eventListeners[trimmedID] = new List<DialogueEventListener>();
            }

            // 2. 將這個監聽器加入到列表中 (如果它還不在列表中的話)
            if (!eventListeners[trimmedID].Contains(listener))
            {
                eventListeners[trimmedID].Add(listener);
                Debug.Log($"[DialogueManager] 成功註冊 ID: \"{trimmedID}\" -> 監聽物件: {listener.gameObject.name}");
            }
        }
    }

    public void UnregisterListener(DialogueEventListener listener)
    {
        if (listener == null) return;

        // 遍歷該監聽器組件中 "所有" 的事件項目
        foreach (var entry in listener.eventEntries)
        {
            string trimmedID = entry.eventID.Trim();
            if (string.IsNullOrEmpty(trimmedID)) continue;

            // 【修改】取消註冊邏輯
            // 1. 檢查這個 ID 的列表是否存在
            if (eventListeners.ContainsKey(trimmedID))
            {
                // 2. 如果存在，就從列表中移除這個監聽器
                if (eventListeners[trimmedID].Contains(listener))
                {
                    eventListeners[trimmedID].Remove(listener);
                }

                // 3. (可選優化) 如果這個列表變空了，就從字典中移除這個 ID
                if (eventListeners[trimmedID].Count == 0)
                {
                    eventListeners.Remove(trimmedID);
                }
            }
        }
    }
    /// <summary>
    /// 【新功能】
    /// 註冊一個 "遊戲事件ID" 到 "對話圖形" 的綁定。
    /// 由 DialogueEventTrigger.cs 在 OnEnable 時呼叫。
    /// </summary>
    public void RegisterDialogueEvent(string eventID, DialogueGraph graph)
    {
        string trimmedID = eventID.Trim();
        if (string.IsNullOrEmpty(trimmedID)) return;

        if (graphEventRegistry.ContainsKey(trimmedID))
        {
            Debug.LogWarning($"[DialogueManager] 遊戲事件 ID '{trimmedID}' 已經被註冊。將被新的註冊覆蓋。");
        }

        graphEventRegistry[trimmedID] = graph;
        Debug.Log($"[DialogueManager] 成功註冊「遊戲事件觸發器」: ID \"{trimmedID}\" -> Graph \"{graph.name}\"");
    }

    /// <summary>
    /// 【新功能】
    /// 取消註冊一個 "遊戲事件ID"。
    /// 由 DialogueEventTrigger.cs 在 OnDisable 時呼叫。
    /// </summary>
    public void UnregisterDialogueEvent(string eventID)
    {
        string trimmedID = eventID.Trim();
        if (string.IsNullOrEmpty(trimmedID)) return;

        if (graphEventRegistry.ContainsKey(trimmedID))
        {
            graphEventRegistry.Remove(trimmedID);
        }
    }

    /// <summary>
    /// 【新功能 - 核心】
    /// 任何其他腳本都可以呼叫此方法，透過 ID 來啟動一個對話。
    /// </summary>
    /// <param name="eventID">在 DialogueEventTrigger 中設定的 ID</param>
    public void TriggerDialogueByEvent(string eventID)
    {
        string trimmedID = eventID.Trim();
        if (string.IsNullOrEmpty(trimmedID)) return;

        if (isDialogueActive)
        {
            Debug.LogWarning($"[DialogueManager] 嘗試觸發事件 '{trimmedID}'，但對話已在進行中。");
            return; // 避免在對話中插入新對話
        }

        if (graphEventRegistry.TryGetValue(trimmedID, out DialogueGraph graphToStart))
        {
            // 找到了！啟動這個對話
            Debug.Log($"[DialogueManager] 接收到遊戲事件 '{trimmedID}'，正在啟動對話: {graphToStart.name}");
            StartConversation(graphToStart);
        }
        else
        {
            // 沒找到
            Debug.LogWarning($"[DialogueManager] 嘗試觸發事件 '{trimmedID}'，但在註冊表中找不到對應的 DialogueGraph。");
        }
    }
    #endregion

    /// <summary>
    /// 【新輔助方法】
    /// 立即停止當前圖形，並無縫跳轉到一個新圖形的第一個節點。
    /// 這用於 "路由器圖形" (Router Graph)。
    /// </summary>
    private void JumpToGraph(DialogueGraph newGraph)
    {
        Debug.Log($"[DialogueManager] 正在從 {currentGraph.name} 跳轉到 {newGraph.name}...");

        if (newGraph == null)
        {
            Debug.LogError("StartGraphNode 嘗試跳轉，但 newGraph 為 null！");
            EndConversation(); // 安全起見，直接結束
            return;
        }

        // 1. 更新當前圖形
        currentGraph = newGraph;

        // 2. 找到新圖形的 StartNode
        StartNode startNode = currentGraph.nodes.OfType<StartNode>().FirstOrDefault();
        if (startNode == null)
        {
            Debug.LogError($"錯誤：在跳轉到的圖形 '{currentGraph.name}' 中找不到 StartNode！");
            EndConversation();
            return;
        }

        // 3. 獲取新圖形的第一個節點
        currentNode = startNode.GetNextNode();

        // 4. 立即開始處理新圖形
        // (注意：我們不呼叫 EndConversation/StartConversation，
        //  是為了保持 UI 和輸入堆疊的連續性)
        ProcessCurrentNode();
    }

    private void ProcessCurrentNode()
    {
        if (currentNode == null)
        {
            EndConversation();
            return;
        }

        // --- 1. 處理「停止點」節點 ---
        if (currentNode is LineNode lineNode)
        {
            if (DialogueAudioManager.Instance != null)
            {
                DialogueAudioManager.Instance.PlayVoiceOver(lineNode.line.voiceClip);
                DialogueAudioManager.Instance.PlaySoundEffect(lineNode.line.soundEffect);
            }
            isTyping = true;
            lineNode.line.onShowLine?.Invoke();
            dialogueUI.SetDialogue(lineNode.line, typeWriterSpeed);
            StartCoroutine(WaitForTypingToEnd());
        }
        else if (currentNode is ChoiceNode choiceNode)
        {
            isWaitingForChoice = true;
            _currentChoicePorts = new List<NodePort>();
            List<string> finalChoiceStrings = new List<string>();

            // 1. 检查是否有本地化 Keys
            bool useKeys = choiceNode.choiceKeys != null && choiceNode.choiceKeys.Count > 0;
            
            // 2. 决定要遍历哪个列表 (如果有 Keys 就用 Keys，否则用 choices)
            int count = useKeys ? choiceNode.choiceKeys.Count : choiceNode.choices.Count;

            for(int i = 0; i < count; i++)
            {
                string textToShow;

                if (useKeys)
                {
                    // 如果有 Key，就去本地化系统查找
                    textToShow = LocalizationManager.Instance.GetLocalizedText(choiceNode.choiceKeys[i]);
                }
                else
                {
                    // 如果没有 Key，直接显示编辑器中输入的原始文字
                    textToShow = choiceNode.choices[i];
                }

                finalChoiceStrings.Add(textToShow);
                _currentChoicePorts.Add(choiceNode.GetOutputPort("choices " + i));
            }

            dialogueUI.ShowChoices(finalChoiceStrings, OnChoiceMade);
        }
        else if (currentNode is TimedChoiceNode timedChoiceNode)
        {
            isWaitingForChoice = true;

            // 【修改】: 也為 TimedChoiceNode 填充 _currentChoicePorts 列表
            _currentChoicePorts = new List<NodePort>();
            // (假設 TimedChoiceNode 也有一個 'choices' 列表)
            for(int i = 0; i < timedChoiceNode.choices.Count; i++) 
            {
                _currentChoicePorts.Add(timedChoiceNode.GetOutputPort("choices " + i));
            }

            dialogueUI.ShowChoices(timedChoiceNode.choiceKeys, OnChoiceMade);
            dialogueUI.StartTimer(timedChoiceNode.timeLimit, OnTimerTimeout);
        }
        else if (currentNode is ConditionalChoiceNode condChoiceNode)
        {
            // 這是一個 "停止點" 節點，呼叫它的專屬處理邏輯
            ProcessConditionalChoiceNode(condChoiceNode);
            // 我們 "不" 呼叫 ProcessCurrentNode()，因為我們要等待玩家輸入
        }
        else if (currentNode is WaitNode waitNode)
        {
            StartCoroutine(HandleWaitNode(waitNode));
        }
        else if (currentNode is CameraControlNode camNode)
        {
            if (camNode.waitForTransition)
            {
                StartCoroutine(HandleCameraControl(camNode));
            }
            else
            {
                StartCoroutine(HandleCameraControl(camNode));
                currentNode = camNode.GetNextNode();
                ProcessCurrentNode();
            }
        }
        // --- 2. 處理「邏輯」節點 (呼叫新的輔助方法) ---
        else
        {
            // 【修正】: 這裡的 "node" 變數名稱必須在每個 else if 中都獨一無二
            if(currentNode is StartGraphNode startGraphNode)
            {
                // 這是一個跳轉節點，立即執行跳轉並 "返回"
                JumpToGraph(startGraphNode.graphToStart);
                return; // 必須返回，停止處理舊圖形
            }
            else if(currentNode is SetVariableNode setVarNode)           currentNode = ProcessSetVariableNode(setVarNode);
            else if (currentNode is ConditionalNode conditionalNode)     currentNode = ProcessConditionalNode(conditionalNode);
            else if (currentNode is PlayAnimationNode animNode)       currentNode = ProcessPlayAnimationNode(animNode);
            else if (currentNode is InvokeEventNode eventNode)         currentNode = ProcessInvokeEventNode(eventNode);
            else if (currentNode is SetGlobalVariableNode setGlobalNode) currentNode = ProcessSetGlobalVariableNode(setGlobalNode);
            else if (currentNode is GetGlobalVariableNode getGlobalNode) currentNode = ProcessGetGlobalVariableNode(getGlobalNode);
            else if (currentNode is UpdateQuestNode questNode)         currentNode = ProcessUpdateQuestNode(questNode);
            else if (currentNode is CheckQuestNode checkQuestNode)      currentNode = ProcessCheckQuestNode(checkQuestNode);
            else if (currentNode is CheckSpecificItemsNode itemsNode)   currentNode = ProcessCheckSpecificItemsNode(itemsNode);
            else if (currentNode is CheckItemNode itemNode)             currentNode = ProcessCheckItemNode(itemNode);
            else if (currentNode is OpenInventoryNode openInvNode)
            {
                ProcessOpenInventoryNode(openInvNode);
                // 不呼叫 ProcessCurrentNode()，因為要等待 UI 回調
                return; 
            }
            else if (currentNode is PriorityRouterNode routerNode)    currentNode = ProcessPriorityRouterNode(routerNode);
            else
            {
                // 未知節點，安全起見直接前進
                Debug.LogWarning($"未知的節點類型: {currentNode.GetType()}，將直接跳過。");
                currentNode = currentNode.GetNextNode();
            }
            // 立即處理下一個節點
            ProcessCurrentNode();
        }
    }

    private BaseNode ProcessSetVariableNode(SetVariableNode node)
    {
        (currentGraph as DialogueGraph).SetVariable(node.variableName, node.value);
        return node.GetNextNode();
    }

    private BaseNode ProcessConditionalNode(ConditionalNode node)
    {
        float actualValue = (currentGraph as DialogueGraph).GetVariable(node.variableName);
        float compareValue = node.valueToCompare;
        bool result = false;
        switch (node.comparison)
        {
            case ComparisonType.EqualTo: result = actualValue == compareValue; break;
            case ComparisonType.NotEqualTo: result = actualValue != compareValue; break;
            case ComparisonType.GreaterThan: result = actualValue > compareValue; break;
            case ComparisonType.LessThan: result = actualValue < compareValue; break;
            case ComparisonType.GreaterThanOrEqualTo: result = actualValue >= compareValue; break;
            case ComparisonType.LessThanOrEqualTo: result = actualValue <= compareValue; break;
        }
        return node.GetNextNode(result);
    }

    private BaseNode ProcessPlayAnimationNode(PlayAnimationNode node)
    {
        if (!string.IsNullOrEmpty(node.targetObjectName))
        {
            GameObject target = GameObject.Find(node.targetObjectName);
            if (target != null)
            {
                Animator animator = target.GetComponent<Animator>();
                if (animator != null && !string.IsNullOrEmpty(node.triggerName))
                    animator.SetTrigger(node.triggerName);
                else
                    Debug.LogWarning($"在物件 {target.name} 上找不到 Animator 元件，或 Trigger Name 為空！");
            }
            else
                Debug.LogWarning($"在場景中找不到名為 {node.targetObjectName} 的物件！");
        }
        return node.GetNextNode();
    }

    private BaseNode ProcessInvokeEventNode(InvokeEventNode node)
    {
        if (!string.IsNullOrEmpty(node.eventID))
        {
            string trimmedID = node.eventID.Trim();
            if (eventListeners.TryGetValue(trimmedID, out List<DialogueEventListener> listenersToTrigger))
            {
                foreach (DialogueEventListener listener in listenersToTrigger.ToList())
                {
                    if (listener != null) listener.TriggerEvent(trimmedID);
                }
            }
            else
                Debug.LogWarning($"[DialogueManager] 找不到事件 ID '{trimmedID}' 的監聽器！");
        }
        return node.GetNextNode();
    }

    private BaseNode ProcessSetGlobalVariableNode(SetGlobalVariableNode node)
    {
        if (node.database != null)
        {
            node.database.SetVariable(node.globalVariableName, node.valueToSet);
        }
        return node.GetNextNode();
    }

    private BaseNode ProcessGetGlobalVariableNode(GetGlobalVariableNode node)
    {
        if (node.database != null && currentGraph != null)
        {
            float globalValue = node.database.GetVariable(node.globalVariableName);
            (currentGraph as DialogueGraph).SetVariable(node.localVariableName, globalValue);
        }
        return node.GetNextNode();
    }
    
    private BaseNode ProcessUpdateQuestNode(UpdateQuestNode node)
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.UpdateQuestStatus(node.questID, node.newStatus);
        else
            Debug.LogWarning("場景中找不到 QuestManager！無法更新任務狀態。");
        return node.GetNextNode();
    }

    private BaseNode ProcessCheckQuestNode(CheckQuestNode node)
    {
        bool conditionResult = false;
        if (QuestManager.Instance != null)
        {
            QuestStatus currentStatus = QuestManager.Instance.GetQuestStatus(node.questID);
            conditionResult = (currentStatus == node.statusToCheck);
        }
        else
            Debug.LogWarning("場景中找不到 QuestManager！無法檢查任務狀態。");
        return node.GetNextNode(conditionResult);
    }

    private BaseNode ProcessCheckSpecificItemsNode(CheckSpecificItemsNode node)
    {
        bool allItemsFound = true;
        if (InventoryManager.Instance != null && node.requiredItems != null)
        {
            foreach (ItemData requiredItem in node.requiredItems)
            {
                if (requiredItem != null && !InventoryManager.Instance.HasItem(requiredItem.itemID))
                {
                    allItemsFound = false;
                    break;
                }
            }
        }
        else { allItemsFound = false; }
        return node.GetNextNode(allItemsFound);
    }

    private BaseNode ProcessCheckItemNode(CheckItemNode node)
    {
        bool conditionResult = false;
        if (InventoryManager.Instance != null && node.itemToCheck != null)
        {
            // 現在 GetItemCount() 存在了，這段程式碼可以正確運作
            int actualQuantity = InventoryManager.Instance.GetItemCount(node.itemToCheck.itemID);
            int requiredQuantity = node.requiredQuantity;
            switch (node.comparison)
            {
                case ComparisonType.EqualTo: conditionResult = actualQuantity == requiredQuantity; break;
                case ComparisonType.NotEqualTo: conditionResult = actualQuantity != requiredQuantity; break;
                case ComparisonType.GreaterThan: conditionResult = actualQuantity > requiredQuantity; break;
                case ComparisonType.LessThan: conditionResult = actualQuantity < requiredQuantity; break;
                case ComparisonType.GreaterThanOrEqualTo: conditionResult = actualQuantity >= requiredQuantity; break;
                case ComparisonType.LessThanOrEqualTo: conditionResult = actualQuantity <= requiredQuantity; break;
            }
        }
        else
            Debug.LogWarning($"CheckItemNode 錯誤：InventoryManager 不存在或 ItemData 未指定！");
        
        return node.GetNextNode(conditionResult);
    }

    private void ProcessOpenInventoryNode(OpenInventoryNode node)
    {
        if (DialogueItemPickerUI.Instance != null)
        {
            // 暫停對話輸入，顯示 UI
            isWaitingForChoice = true; 
            DialogueItemPickerUI.Instance.Show();
        }
        else
        {
            Debug.LogError("場景中找不到 DialogueItemPickerUI！請確保已建立並掛載腳本。");
            // 如果沒 UI，直接跳過此節點
            currentNode = node.GetNextNode();
            ProcessCurrentNode();
        }
    }

    /// <summary>
    /// 【新節點邏輯】
    /// 處理 PriorityRouterNode (條列式) 節點
    /// </summary>
    private BaseNode ProcessPriorityRouterNode(PriorityRouterNode node)
    {
        // 1. 依序 (i = 0, 1, 2...) 檢查節點上的所有條件
        for (int i = 0; i < node.conditions.Count; i++)
        {
            PriorityCondition condition = node.conditions[i];
            bool conditionResult = false;

            // 2. 根據條件類型，執行不同的檢查
            if (condition.checkType == ConditionType.CheckGraphVariable)
            {
                // --- 執行 "檢查圖形變數" 邏輯 (同 ConditionalNode) ---
                float actualValue = (currentGraph as DialogueGraph).GetVariable(condition.variableName);
                float compareValue = condition.valueToCompare;
                switch (condition.comparison)
                {
                    case ComparisonType.EqualTo: conditionResult = actualValue == compareValue; break;
                    case ComparisonType.NotEqualTo: conditionResult = actualValue != compareValue; break;
                    case ComparisonType.GreaterThan: conditionResult = actualValue > compareValue; break;
                    case ComparisonType.LessThan: conditionResult = actualValue < compareValue; break;
                    case ComparisonType.GreaterThanOrEqualTo: conditionResult = actualValue >= compareValue; break;
                    case ComparisonType.LessThanOrEqualTo: conditionResult = actualValue <= compareValue; break;
                }
            }
            else if (condition.checkType == ConditionType.CheckInventoryItem)
            {
                // --- 執行 "檢查背包物品" 邏輯 (同 CheckItemNode) ---
                if (InventoryManager.Instance != null && condition.itemToCheck != null)
                {
                    int actualQuantity = InventoryManager.Instance.GetItemCount(condition.itemToCheck.itemID);
                    int requiredQuantity = condition.requiredQuantity;
                    switch (condition.comparison)
                    {
                        case ComparisonType.EqualTo: conditionResult = actualQuantity == requiredQuantity; break;
                        case ComparisonType.NotEqualTo: conditionResult = actualQuantity != requiredQuantity; break;
                        case ComparisonType.GreaterThan: conditionResult = actualQuantity > requiredQuantity; break;
                        case ComparisonType.LessThan: conditionResult = actualQuantity < requiredQuantity; break;
                        case ComparisonType.GreaterThanOrEqualTo: conditionResult = actualQuantity >= requiredQuantity; break;
                        case ComparisonType.LessThanOrEqualTo: conditionResult = actualQuantity <= requiredQuantity; break;
                    }
                }
                // (如果 InventoryManager 不存在或物品未設定，conditionResult 保持 false)
            }
            // 【新增】 檢查剛剛出示的物品
            else if (condition.checkType == ConditionType.CheckLastPickedItem)
            {
                string pickedID = (currentGraph as DialogueGraph).lastPickedItemID;
                
                if (condition.itemToCheck != null)
                {
                    // 檢查 "剛剛選的 ID" 是否等於 "條件中設定的物品 ID"
                    // (這裡比較簡單，通常只要相等就好，不需要大於小於)
                    conditionResult = (pickedID == condition.itemToCheck.itemID);
                }
                else
                {
                    // 如果沒設定物品，可能是在檢查 "是否什麼都沒選" (空字串)
                    conditionResult = string.IsNullOrEmpty(pickedID);
                }
            }

            // 3. 只要有一個條件滿足 (true)...
            if (conditionResult)
            {
                // ...立即返回 "該條件" 對應的出口節點，並停止檢查
                return node.GetNextNodeForCondition(i);
            }
        }

        // 4. 如果迴圈跑完，所有條件都不滿足，則返回 "Else" 出口
        return node.GetNextNodeElse();
    }

    /// <summary>
    /// 【新節點邏輯】
    /// 處理 ConditionalChoiceNode (動態選項) 節點
    /// </summary>
    private void ProcessConditionalChoiceNode(ConditionalChoiceNode node)
    {
        List<string> finalChoiceStrings = new List<string>(); 
        _currentChoicePorts = new List<NodePort>(); 

        // 1. 依序 (i = 0, 1, 2...) 檢查 "動態選項"
        for (int i = 0; i < node.conditionalChoices.Count; i++)
        {
            ConditionalChoice choice = node.conditionalChoices[i];
            bool conditionResult = false;

            // --- (重用 PriorityRouterNode 的檢查邏輯) ---
            if (choice.checkType == ConditionType.CheckGraphVariable)
            {
                // (省略檢查變數的邏輯...)
                float actualValue = (currentGraph as DialogueGraph).GetVariable(choice.variableName);
                float compareValue = choice.valueToCompare;
                switch (choice.comparison)
                {
                    case ComparisonType.EqualTo: conditionResult = actualValue == compareValue; break;
                    case ComparisonType.NotEqualTo: conditionResult = actualValue != compareValue; break;
                    case ComparisonType.GreaterThan: conditionResult = actualValue > compareValue; break;
                    case ComparisonType.LessThan: conditionResult = actualValue < compareValue; break;
                    case ComparisonType.GreaterThanOrEqualTo: conditionResult = actualValue >= compareValue; break;
                    case ComparisonType.LessThanOrEqualTo: conditionResult = actualValue <= compareValue; break;
                }
            }
            else if (choice.checkType == ConditionType.CheckInventoryItem)
            {
                if (InventoryManager.Instance != null && choice.itemToCheck != null)
                {
                    // (省略檢查物品的邏輯...)
                    int actualQuantity = InventoryManager.Instance.GetItemCount(choice.itemToCheck.itemID);
                    int requiredQuantity = choice.requiredQuantity;
                    switch (choice.comparison)
                    {
                        case ComparisonType.EqualTo: conditionResult = actualQuantity == requiredQuantity; break;
                        case ComparisonType.NotEqualTo: conditionResult = actualQuantity != requiredQuantity; break;
                        case ComparisonType.GreaterThan: conditionResult = actualQuantity > requiredQuantity; break;
                        case ComparisonType.LessThan: conditionResult = actualQuantity < requiredQuantity; break;
                        case ComparisonType.GreaterThanOrEqualTo: conditionResult = actualQuantity >= requiredQuantity; break;
                        case ComparisonType.LessThanOrEqualTo: conditionResult = actualQuantity <= requiredQuantity; break;
                    }
                }
            }
            // --- (檢查邏輯結束) ---

            if (conditionResult)
            {
                // --- 【核心修正】 ---
                // 在這裡就決定好最終的文字
                string stringToShow;
                if (choice.useLocalization)
                {
                    // 如果使用本地化，"現在" 就去查找
                    stringToShow = LocalizationManager.Instance.GetLocalizedText(choice.choiceKey);
                }
                else
                {
                    // 否則，直接使用內容
                    stringToShow = choice.choiceContent;
                }
                
                finalChoiceStrings.Add(stringToShow);
                _currentChoicePorts.Add(node.GetOutputPort("conditionalChoices " + i));
            }
        }

        // 2. 加入所有 "預設選項"
        for (int j = 0; j < node.defaultChoices.Count; j++)
        {
            ConditionalChoice defaultChoice = node.defaultChoices[j];

            // --- 【核心修正】 ---
            // 預設選項也使用這個邏輯
            string stringToShow;
            if (defaultChoice.useLocalization)
            {
                stringToShow = LocalizationManager.Instance.GetLocalizedText(defaultChoice.choiceKey);
            }
            else
            {
                stringToShow = defaultChoice.choiceContent;
            }
                
            finalChoiceStrings.Add(stringToShow);
            _currentChoicePorts.Add(node.GetOutputPort("defaultChoices " + j));
        }

        // 3. 將這個 "最終組合" 的列表傳送給 UI 顯示
        isWaitingForChoice = true;
        dialogueUI.ShowChoices(finalChoiceStrings, OnChoiceMade); // 這裡傳送的是 "已經處理好" 的文字
    }

    public void Debug_PrintListeners()
    {
        Debug.Log("---------- [DialogueManager 通訊錄] ----------");
        if (eventListeners.Count == 0)
        {
            Debug.Log("目前沒有任何已註冊的監聽器。");
        }
        else
        {
            // 遍歷字典中所有的 "Key" (ID)
            foreach (var listenerPair in eventListeners)
            {
                // listenerPair.Key 是 string ID
                // listenerPair.Value 是 List<DialogueEventListener>

                // 【修正】: 必須遍歷 "列表" 中的每一個監聽器
                foreach (DialogueEventListener listener in listenerPair.Value)
                {
                    // 確保監聽器沒有在執行過程中被銷毀
                    if (listener != null)
                    {
                        Debug.Log($"Key: \"{listenerPair.Key}\" -> 監聽物件: {listener.gameObject.name}");
                    }
                    else
                    {
                        Debug.Log($"Key: \"{listenerPair.Key}\" -> 監聽物件: (null/已被銷毀)");
                    }
                }
            }
        }
        Debug.Log("-------------------------------------------");
    }

    /// <summary>
    /// 當玩家在 DialogueItemPickerUI 選擇物品後呼叫此方法
    /// </summary>
    public void OnItemPicked(string itemID)
    {
        // 1. 儲存結果到 Graph 中
        if (currentGraph is DialogueGraph graph)
        {
            graph.lastPickedItemID = itemID;
            Debug.Log($"[DialogueManager] 玩家選擇了物品 ID: {itemID}");
        }

        // 2. 恢復對話狀態
        isWaitingForChoice = false;

        // 3. 前進到下一個節點 (OpenInventoryNode 的 exit)
        if (currentNode is OpenInventoryNode)
        {
            currentNode = currentNode.GetNextNode();
            ProcessCurrentNode();
        }
    }

    private IEnumerator HandleWaitNode(WaitNode waitNode)
    {
        // 等待指定的秒數
        yield return new WaitForSeconds(waitNode.waitDuration);

        // 等待結束後，自動前進到下一個節點
        currentNode = waitNode.GetNextNode();
        ProcessCurrentNode();
    }

    private IEnumerator WaitForTypingToEnd()
    {
        // 等待打字機協程結束
        while (dialogueUI.GetTypeWriterCoroutine() != null)
        {
            yield return null;
        }
        isTyping = false;

        // -- 自動播放邏輯 --
        if (isAutoPlay && !isWaitingForChoice)
        {
            // 等待指定的延遲時間
            yield return new WaitForSeconds(autoPlayDelay);

            // 如果等待期間被玩家關閉了自動播放，就不再前進
            if (!isAutoPlay) yield break;

            // 如果是 LineNode，自動前進到下一個節點
            if (currentNode is LineNode)
            {
                currentNode = currentNode.GetNextNode();
                ProcessCurrentNode();
            }
            // 注意：我們不在這裡處理 ChoiceNode，因為自動播放應該在選項前停下
        }
    }

    private IEnumerator HandleCameraControl(CameraControlNode camNode)
    {
        if (mainCamera == null)
        {
            // 如果需要等待，則必須確保流程能繼續
            if (camNode.waitForTransition)
            {
                currentNode = camNode.GetNextNode();
                ProcessCurrentNode();
            }
            yield break;
        }

        if (!cameraHasBeenMoved)
        {
            originalCameraPos = mainCamera.transform.position;
            originalCameraRot = mainCamera.transform.rotation;
            cameraHasBeenMoved = true;
        }

        Transform cameraTransform = mainCamera.transform;

        // --- 根據模式執行不同邏輯 ---
        if (camNode.moveMode == CameraMoveMode.Simple)
        {
            // --- 執行簡易模式 (舊邏輯) ---
            GameObject target = GameObject.Find(camNode.targetTransformName);
            if (target != null)
            {
                yield return StartCoroutine(MoveCameraToTarget(target.transform, camNode.transitionDuration));
            }
        }
        else if (camNode.moveMode == CameraMoveMode.Sequence)
        {
            // --- 執行序列模式 ---
            foreach (var instruction in camNode.sequence)
            {
                GameObject target = GameObject.Find(instruction.targetTransformName);
                if (target != null)
                {
                    // 1. 移動到目標點
                    yield return StartCoroutine(MoveCameraToTarget(target.transform, instruction.transitionDuration));
                    // 2. 在目標點停留
                    yield return new WaitForSeconds(instruction.holdDuration);
                }
            }
        }

        if (camNode.waitForTransition)
        {
            currentNode = camNode.GetNextNode();
            ProcessCurrentNode();
        }
    }

    private IEnumerator MoveCameraToTarget(Transform targetTransform, float duration)
    {
        Transform cameraTransform = mainCamera.transform;
        float timer = 0f;
        Vector3 startPos = cameraTransform.position;
        Quaternion startRot = cameraTransform.rotation;

        if (duration <= 0)
        {
            cameraTransform.position = targetTransform.position;
            cameraTransform.rotation = targetTransform.rotation;
        }
        else
        {
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / duration);
                cameraTransform.position = Vector3.Lerp(startPos, targetTransform.position, t);
                cameraTransform.rotation = Quaternion.Slerp(startRot, targetTransform.rotation, t);
                yield return null;
            }
            cameraTransform.position = targetTransform.position;
            cameraTransform.rotation = targetTransform.rotation;
        }
    }

    private IEnumerator ReturnCameraToOriginalPosition()
    {
        if (mainCamera == null) yield break;

        Transform cameraTransform = mainCamera.transform;
        float timer = 0f;
        Vector3 startPos = cameraTransform.position;
        Quaternion startRot = cameraTransform.rotation;

        Debug.Log("正在將攝影機還原至原始位置...");

        while (timer < cameraReturnDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / cameraReturnDuration);
            cameraTransform.position = Vector3.Lerp(startPos, originalCameraPos, t);
            cameraTransform.rotation = Quaternion.Slerp(startRot, originalCameraRot, t);
            yield return null;
        }

        // 確保最終位置和旋轉完全準確
        cameraTransform.position = originalCameraPos;
        cameraTransform.rotation = originalCameraRot;

        // 在這裡，您可以重新啟用跟隨玩家的攝影機腳本 (如果有的話)
        // 例如：mainCamera.GetComponent<PlayerFollowCamera>().enabled = true;
    }

    private void OnSubmit(InputAction.CallbackContext context)
    {
        if (!isDialogueActive || isAutoPlay || isWaitingForChoice) return;

        if (DialogueAudioManager.Instance != null) DialogueAudioManager.Instance.StopVoiceOver();

        if (isTyping)
        {
            isTyping = false;
            StopAllCoroutines();
        }
        else
        {
            currentNode = currentNode.GetNextNode();
            ProcessCurrentNode();
        }

    }

    private void OnChoiceMade(int choiceIndex)
    {
        dialogueUI.StopTimer();
        isWaitingForChoice = false;
        dialogueUI.ClearChoices();

        BaseNode nextNode = null;

        // --- 【核心修改】---
        // 檢查我們是否使用了新的 "出口列表" 系統
        if (_currentChoicePorts != null && choiceIndex >= 0 && choiceIndex < _currentChoicePorts.Count)
        {
            // 從我們儲存的列表中，根據索引獲取正確的出口
            NodePort selectedPort = _currentChoicePorts[choiceIndex];

            if (selectedPort != null && selectedPort.IsConnected)
            {
                nextNode = selectedPort.Connection.node as BaseNode;
            }

            // 清除列表，為下一次對話做準備
            _currentChoicePorts = null; 
        }
        else
        {
            // (備用邏輯，以防萬一)
            Debug.LogError($"[DialogueManager] OnChoiceMade 錯誤！ choiceIndex ({choiceIndex}) 超出了 _currentChoicePorts 的範圍，或列表為 null。");
        }

        // (舊的 if (currentNode is...) 邏輯 已不再需要，
        //  因為 _currentChoicePorts 已經儲存了正確的出口)

        currentNode = nextNode;
        ProcessCurrentNode();
    }

    private void OnTimerTimeout()
    {
        Debug.Log("選擇超時！");
        isWaitingForChoice = false;
        dialogueUI.ClearChoices(); // 清除畫面上的選項

        if (currentNode is TimedChoiceNode timedChoiceNode)
        {
            // 獲取超時出口連接的節點
            currentNode = timedChoiceNode.GetNextNodeOnTimeout();
            ProcessCurrentNode();
        }
    }

    private void EndConversation()
    {
        if (DialogueAudioManager.Instance != null) DialogueAudioManager.Instance.StopVoiceOver();
        isDialogueActive = false;
        dialogueUI.gameObject.SetActive(false); // <--- 停用 DialogueUI 主物件
        Debug.Log("對話結束");
        if (crosshairUI != null)
        {
            crosshairUI.SetActive(true);
        }
        if (otherUI != null)
        {
            otherUI.SetActive(true);
        }
        if (cameraHasBeenMoved)
        {
            StartCoroutine(ReturnCameraToOriginalPosition());
        }
        if (InputStackManager.Instance != null)
        {
            InputStackManager.Instance.PopMap(); //
            Debug.Log($"[DialogueManager] Popped '{dialogueActionMapName}' map from Input Stack."); //
        }

        if (InputProvider.InputActions != null)
        {
            InputProvider.InputActions.Dialogue.Submit.performed -= SubmitDialogue;
            InputProvider.InputActions.Dialogue.ToggleAutoPlay.performed -= OnToggleAutoPlay;
            InputProvider.InputActions.Dialogue.Skip.performed -= OnSkip;
        }

        // ***** 需求修改: 3. 在 PopMap() 和所有清理工作完成後，觸發事件 *****
        OnConversationEnd?.Invoke();
    }
    
    /// <summary>
    /// 【新函式】
    /// 執行「跳過」的核心邏輯。
    /// 這個 public 方法可以被 UI 按鈕的 OnClick() 事件呼叫。
    /// </summary>
    public void TriggerSkip()
    {
        // 如果對話未激活、或正在等待選項，則不執行任何操作
        if (!isDialogueActive || isWaitingForChoice) return;

        // 1. 停止所有正在運行的協程 (例如打字、自動播放、等待)
        StopAllCoroutines();
        isTyping = false; 

        // 2. 停止當前語音
        if (DialogueAudioManager.Instance != null) DialogueAudioManager.Instance.StopVoiceOver();

        // 3. 如果當前節點是 LineNode (我們剛剛打斷了它的打字)
        if (currentNode is LineNode lineNode)
        {
            // 確保文字是完整的 (以防萬一)
            dialogueUI.CompleteText(lineNode.line.content);
        }

        // 4. 我們現在處於 "已完成" 當前節點的狀態。
        //    移動到下一個節點，準備開始快速執行。
        currentNode = currentNode.GetNextNode();

        // 5. 呼叫新的輔助方法來 "快速執行" 圖形
        ExecuteGraphUntilStop();
    }

    public void OnSkip(InputAction.CallbackContext context)
    {
        // 這個方法現在只是一個 "包裝器" (wrapper)
        // 它從 Input System 接收 "S" 鍵的輸入
        // 然後呼叫我們新的核心邏輯方法
        TriggerSkip();
    }

    public void SubmitDialogue(InputAction.CallbackContext context)
    {
        // 如果對話未激活、正在自動播放、或等待選項，則不執行任何操作
        if (!isDialogueActive || isAutoPlay || isWaitingForChoice) return; //

        // 停止當前語音
        if (DialogueAudioManager.Instance != null) DialogueAudioManager.Instance.StopVoiceOver(); //

        if (isTyping) //
        {
            // --- 情況 1：玩家想要「跳過打字」 ---

            // 1. 停止 "WaitForTypingToEnd" 協程 (因為我們手動完成了)
            //    StopAllCoroutines() 會停止此腳本上的所有協程
            StopAllCoroutines(); //

            // 2. 更新狀態
            isTyping = false; //

            // 3. 告訴 UI 立即顯示完整文字
            if (currentNode is LineNode lineNode) //
            {
                dialogueUI.CompleteText(lineNode.line.content); //
            }
        }
        else
        {
            // --- 情況 2：玩家想要「進入下一句」 ---

            // 1. 獲取下一個節點
            currentNode = currentNode.GetNextNode(); //

            // 2. 處理下一個節點 (顯示文字、選項...)
            ProcessCurrentNode(); //
        }
    }
    
    /// <summary>
    /// 【新函式】
    /// 切換「自動播放」狀態的核心邏輯。
    /// 這個 public 方法可以被 UI 按鈕的 OnClick() 事件呼叫。
    /// </summary>
    public void ToggleAutoPlay()
    {
        if (!isDialogueActive) return;

        isAutoPlay = !isAutoPlay;
        Debug.Log("自動播放: " + (isAutoPlay ? "開啟" : "關閉"));

        // 如果剛開啟自動播放，且當前對話處於靜止狀態（不在打字，也不在等選項）
        // 我們就主動觸發一次前進，讓對話繼續下去
        if (isAutoPlay && !isTyping && !isWaitingForChoice)
        {
            // 移動到下一個節點
            currentNode = currentNode.GetNextNode();
            ProcessCurrentNode();
        }
    }

    public void OnToggleAutoPlay(InputAction.CallbackContext context)
    {
        // 這個方法現在只是一個 "包裝器" (wrapper)
        // 它從 Input System 接收按鍵輸入
        // 然後呼叫我們新的核心邏輯方法
        ToggleAutoPlay();
    }

    /// <summary>
    /// 【優化】
    /// 快速執行圖形邏輯，直到遇到「停止點」或圖形結束。
    /// </summary>
    private void ExecuteGraphUntilStop()
    {
        int loopLimit = 1000; 

        while (loopLimit-- > 0)
        {
            if (currentNode == null)
            {
                EndConversation();
                return;
            }

            // --- 1. 檢查「停止點」 ---
            if (currentNode.isImportant || 
                currentNode is LineNode || 
                currentNode is ChoiceNode || 
                currentNode is TimedChoiceNode ||
                currentNode is WaitNode || // (在Skip中，WaitNode 也被視為 "邏輯節點" 來跳過)
                currentNode is CameraControlNode) // (在Skip中，Camera 也被跳過)
            {
                // 【修改】只有 "真正" 需要玩家輸入的才停下
                if (currentNode.isImportant || currentNode is LineNode || currentNode is ChoiceNode || currentNode is TimedChoiceNode)
                {
                    Debug.Log($"[Skip] 停止於重要節點或互動節點: {currentNode.name}");
                    ProcessCurrentNode(); 
                    return; 
                }
            }

            // --- 2. 執行「邏輯節點」(並跳過「等待節點」) ---
            Debug.Log($"[Skip] 快速執行邏輯節點: {currentNode.name}");
            
            // 【修正】: 這裡的 "node" 變數名稱也必須獨一無二
            if (currentNode is StartGraphNode startGraphNode)
            {
                // 跳過模式也必須執行跳轉
                JumpToGraph(startGraphNode.graphToStart);
                return; // 停止 while 迴圈
            }
            else if(currentNode is SetVariableNode setVarNode)           currentNode = ProcessSetVariableNode(setVarNode);
            else if (currentNode is ConditionalNode conditionalNode)     currentNode = ProcessConditionalNode(conditionalNode);
            else if (currentNode is PlayAnimationNode animNode)       currentNode = ProcessPlayAnimationNode(animNode);
            else if (currentNode is InvokeEventNode eventNode)         currentNode = ProcessInvokeEventNode(eventNode);
            else if (currentNode is SetGlobalVariableNode setGlobalNode) currentNode = ProcessSetGlobalVariableNode(setGlobalNode);
            else if (currentNode is GetGlobalVariableNode getGlobalNode) currentNode = ProcessGetGlobalVariableNode(getGlobalNode);
            else if (currentNode is UpdateQuestNode questNode)         currentNode = ProcessUpdateQuestNode(questNode);
            else if (currentNode is CheckQuestNode checkQuestNode)      currentNode = ProcessCheckQuestNode(checkQuestNode);
            else if (currentNode is CheckSpecificItemsNode itemsNode)   currentNode = ProcessCheckSpecificItemsNode(itemsNode);
            else if (currentNode is CheckItemNode itemNode)             currentNode = ProcessCheckItemNode(itemNode);
            else if (currentNode is PriorityRouterNode routerNode)    currentNode = ProcessPriorityRouterNode(routerNode);
            // 【修改】在 Skip 模式中，跳過 Wait 和 Camera
            else if (currentNode is WaitNode waitNode)                currentNode = waitNode.GetNextNode();
            else if (currentNode is CameraControlNode camNode)       currentNode = camNode.GetNextNode();
            else
            {
                // 其他未知節點，安全起見直接前進
                currentNode = currentNode.GetNextNode();
            }
        } // 結束 while 迴圈

        if (loopLimit <= 0)
        {
            Debug.LogError("[DialogueManager] Skip 功能達到了迴圈上限 (1000次)。請檢查您的對話圖形是否有無限迴圈。");
            EndConversation();
        }
    }
    public bool IsDialogueActive()
    {
        return isDialogueActive;
    }

    // 讓外部可以獲取當前對話圖形的變數列表
    public List<Variable> GetCurrentGraphVariables()
    {
        if (isDialogueActive && currentGraph != null)
        {
            // --- 核心修正：將 variables 改為 runtimeVariables ---
            return (currentGraph as DialogueGraph).runtimeVariables;
        }
        return null;
    }
}