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
            // 在對話開始時，強制將 runtimeVariables 重置為 initialVariables 的內容
            currentGraph.ResetVariables(); //
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

    private void ProcessCurrentNode()
    {
        if (currentNode == null)
        {
            EndConversation();
            return;
        }

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
            // 注意：ShowChoices 方法的參數可能需要從 List<Choice> 改為 List<string>
            // 這取決於你的 DialogueUI.cs 如何實現
            dialogueUI.ShowChoices(choiceNode.choiceKeys, OnChoiceMade);
        }
        else if (currentNode is SetVariableNode setVarNode)
        {
            // 執行設置變數的邏輯
            (currentGraph as DialogueGraph).SetVariable(setVarNode.variableName, setVarNode.value);

            // 邏輯節點不應停留，立即前進到下一個節點
            currentNode = setVarNode.GetNextNode();
            ProcessCurrentNode(); // 使用遞迴立即處理下一個節點
        }
        else if (currentNode is ConditionalNode conditionalNode)
        {
            // 執行條件判斷
            float actualValue = (currentGraph as DialogueGraph).GetVariable(conditionalNode.variableName);
            float compareValue = conditionalNode.valueToCompare;
            bool result = false;

            switch (conditionalNode.comparison)
            {
                case ComparisonType.EqualTo: result = actualValue == compareValue; break;
                case ComparisonType.NotEqualTo: result = actualValue != compareValue; break;
                case ComparisonType.GreaterThan: result = actualValue > compareValue; break;
                case ComparisonType.LessThan: result = actualValue < compareValue; break;
                case ComparisonType.GreaterThanOrEqualTo: result = actualValue >= compareValue; break;
                case ComparisonType.LessThanOrEqualTo: result = actualValue <= compareValue; break;
            }

            // 根據判斷結果，前進到 True 或 False 的出口
            currentNode = conditionalNode.GetNextNode(result);
            ProcessCurrentNode(); // 使用遞迴立即處理下一個節點
        }
        else if (currentNode is WaitNode waitNode)
        {
            // 如果是等待節點，就啟動等待協程
            StartCoroutine(HandleWaitNode(waitNode));
        }
        else if (currentNode is PlayAnimationNode animNode)
        {
            if (!string.IsNullOrEmpty(animNode.targetObjectName))
            {
                // 1. 根據名字在場景中尋找物件
                GameObject target = GameObject.Find(animNode.targetObjectName);

                if (target != null)
                {
                    // 2. 獲取該物件上的 Animator 元件
                    Animator animator = target.GetComponent<Animator>();
                    if (animator != null && !string.IsNullOrEmpty(animNode.triggerName))
                    {
                        // 3. 觸發動畫
                        animator.SetTrigger(animNode.triggerName);
                        Debug.Log($"正在為 {target.name} 播放動畫觸發器: {animNode.triggerName}");
                    }
                    else
                    {
                        Debug.LogWarning($"在物件 {target.name} 上找不到 Animator 元件，或 Trigger Name 為空！");
                    }
                }
                else
                {
                    Debug.LogWarning($"在場景中找不到名為 {animNode.targetObjectName} 的物件！");
                }
            }
            else
            {
                Debug.LogWarning("PlayAnimationNode 沒有設定 Target Object Name！");
            }

            // 立即前進到下一個節點
            currentNode = animNode.GetNextNode();
            ProcessCurrentNode();
        }
        else if (currentNode is CameraControlNode camNode)
        {
            // 如果需要等待運鏡結束
            if (camNode.waitForTransition)
            {
                StartCoroutine(HandleCameraControl(camNode));
            }
            // 如果不需要等待，直接開始運鏡並立刻前進到下一個節點
            else
            {
                StartCoroutine(HandleCameraControl(camNode));
                currentNode = camNode.GetNextNode();
                ProcessCurrentNode();
            }
        }
        else if (currentNode is InvokeEventNode eventNode)
        {
            if (!string.IsNullOrEmpty(eventNode.eventID))
            {
                string trimmedID = eventNode.eventID.Trim();
                Debug.Log($"[DialogueManager] 正在廣播事件，嘗試查找 ID: \"{trimmedID}\"");

                // 【修改】廣播邏輯
                // 1. 檢查是否有監聽器列表
                if (eventListeners.TryGetValue(trimmedID, out List<DialogueEventListener> listenersToTrigger))
                {
                    // 2. 遍歷這個列表中的 "所有" 監聽器
                    //    (使用 .ToList() 建立一個副本，以防有監聽器在執行中途取消註冊自己)
                    foreach (DialogueEventListener listener in listenersToTrigger.ToList()) 
                    {
                        if (listener != null)
                        {
                            // 3. 向 "每一個" 監聽器觸發事件
                            listener.TriggerEvent(trimmedID);
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"[DialogueManager] 在場景中找不到監聽事件 ID '{trimmedID}' 的監聽器！");
                }
            }

            currentNode = eventNode.GetNextNode();
            ProcessCurrentNode();
        }
        else if (currentNode is TimedChoiceNode timedChoiceNode)
        {
            isWaitingForChoice = true;
            dialogueUI.ShowChoices(timedChoiceNode.choiceKeys, OnChoiceMade);
            // 同時啟動計時器，並將 OnTimerTimeout 方法作為回呼
            dialogueUI.StartTimer(timedChoiceNode.timeLimit, OnTimerTimeout);
        }
        else if (currentNode is SetGlobalVariableNode setGlobalNode)
        {
            if (setGlobalNode.database != null)
            {
                setGlobalNode.database.SetVariable(setGlobalNode.globalVariableName, setGlobalNode.valueToSet);
            }
            // 邏輯節點，立即前進
            currentNode = setGlobalNode.GetNextNode();
            ProcessCurrentNode();
        }
        else if (currentNode is GetGlobalVariableNode getGlobalNode)
        {
            if (getGlobalNode.database != null && currentGraph != null)
            {
                float globalValue = getGlobalNode.database.GetVariable(getGlobalNode.globalVariableName);
                (currentGraph as DialogueGraph).SetVariable(getGlobalNode.localVariableName, globalValue);
            }
            // 邏輯節點，立即前進
            currentNode = getGlobalNode.GetNextNode();
            ProcessCurrentNode();
        }
        else if (currentNode is UpdateQuestNode questNode)
        {
            // 確保 QuestManager 存在
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.UpdateQuestStatus(questNode.questID, questNode.newStatus);
            }
            else
            {
                Debug.LogWarning("場景中找不到 QuestManager！無法更新任務狀態。");
            }

            // 立即前進到下一個節點
            currentNode = questNode.GetNextNode();
            ProcessCurrentNode();
        }
        else if (currentNode is CheckQuestNode checkQuestNode)
        {
            bool conditionResult = false;
            // 確保 QuestManager 存在
            if (QuestManager.Instance != null)
            {
                // 獲取當前任務狀態，並與節點中設定的狀態進行比較
                QuestStatus currentStatus = QuestManager.Instance.GetQuestStatus(checkQuestNode.questID);
                conditionResult = (currentStatus == checkQuestNode.statusToCheck);
            }
            else
            {
                Debug.LogWarning("場景中找不到 QuestManager！無法檢查任務狀態。");
            }

            // 根據比較結果，前進到 Pass 或 Fail 的出口
            currentNode = checkQuestNode.GetNextNode(conditionResult);
            ProcessCurrentNode();
        }
        else if (currentNode is CheckSpecificItemsNode checkSpecificItemsNode)
        {
            bool allItemsFound = true; // 先假設所有物品都找到了

            // 確保 InventoryManager 存在
            if (InventoryManager.Instance != null && checkSpecificItemsNode.requiredItems != null)
            {
                // 遍歷節點中設定的每一個必要物品
                foreach (ItemData requiredItem in checkSpecificItemsNode.requiredItems)
                {
                    // --- 核心修正：傳遞 ItemData 中的 itemID (或 itemName) 字串 ---
                    // 假設您的 ItemData.cs 中有 public string itemID;
                    if (requiredItem != null && !InventoryManager.Instance.HasItem(requiredItem.itemID))
                    {
                        allItemsFound = false;
                        Debug.Log($"[Dialogue CheckSpecificItems] 檢查失敗：缺少物品 ID '{requiredItem.itemID}'"); // 建議 Log ID
                        break;
                    }
                }
            }
            else
            {
                Debug.LogWarning("場景中找不到 InventoryManager 或 CheckSpecificItemsNode 未指定 Required Items！");
                allItemsFound = false; // 如果有問題，也視為失敗
            }

            if (allItemsFound)
            {
                Debug.Log($"[Dialogue CheckSpecificItems] 檢查通過：所有必需物品都已找到。");
            }

            // 根據最終檢查結果，前進到 Pass 或 Fail 的出口
            currentNode = checkSpecificItemsNode.GetNextNode(allItemsFound);
            ProcessCurrentNode();
        }
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
        // --- 核心修改：在做出選擇時，停止計時器 ---
        dialogueUI.StopTimer();
        isWaitingForChoice = false;
        dialogueUI.ClearChoices();

        BaseNode nextNode = null;
        if (currentNode is ChoiceNode choiceNode)
        {
            nextNode = choiceNode.GetNextNodeForChoice(choiceIndex);
        }
        // 也處理 TimedChoiceNode 的情況
        else if (currentNode is TimedChoiceNode timedChoiceNode)
        {
            nextNode = timedChoiceNode.GetNextNodeForChoice(choiceIndex);
        }

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
    /// 【新函式】
    /// 快速執行圖形邏輯，直到遇到「停止點」或圖形結束。
    /// </summary>
    private void ExecuteGraphUntilStop()
    {
        // 設置一個安全迴圈上限，防止圖形邏輯錯誤導致死循環
        int loopLimit = 1000; 

        while (loopLimit-- > 0)
        {
            if (currentNode == null)
            {
                // 到達圖形終點
                EndConversation();
                return;
            }

            // --- 1. 檢查「停止點」 ---
            // 如果節點是「重要節點」、或是需要玩家互動的「顯示文字」或「選項」
            // 我們就必須停止快速執行，並正常處理這個節點。
            if (currentNode.isImportant || currentNode is LineNode || currentNode is ChoiceNode || currentNode is TimedChoiceNode)
            {
                Debug.Log($"[Skip] 停止於重要節點或互動節點: {currentNode.name}");
                ProcessCurrentNode(); // 正常處理這個節點 (例如：顯示文字、顯示選項)
                return; // 結束快速執行
            }

            // --- 2. 執行「邏輯節點」(並跳過「等待節點」) ---
            // 如果不是停止點，我們就 "立即執行" 該節點的邏輯，然後前進。

            Debug.Log($"[Skip] 快速執行邏輯節點: {currentNode.name}");

            if (currentNode is SetVariableNode setVarNode)
            {
                (currentGraph as DialogueGraph).SetVariable(setVarNode.variableName, setVarNode.value);
                currentNode = setVarNode.GetNextNode();
            }
            else if (currentNode is ConditionalNode conditionalNode)
            {
                float actualValue = (currentGraph as DialogueGraph).GetVariable(conditionalNode.variableName);
                float compareValue = conditionalNode.valueToCompare;
                bool result = false;
                switch (conditionalNode.comparison)
                {
                    case ComparisonType.EqualTo: result = actualValue == compareValue; break;
                    case ComparisonType.NotEqualTo: result = actualValue != compareValue; break;
                    case ComparisonType.GreaterThan: result = actualValue > compareValue; break;
                    case ComparisonType.LessThan: result = actualValue < compareValue; break;
                    case ComparisonType.GreaterThanOrEqualTo: result = actualValue >= compareValue; break;
                    case ComparisonType.LessThanOrEqualTo: result = actualValue <= compareValue; break;
                }
                currentNode = conditionalNode.GetNextNode(result);
            }
            else if (currentNode is WaitNode waitNode)
            {
                // 跳過等待
                currentNode = waitNode.GetNextNode();
            }
            else if (currentNode is PlayAnimationNode animNode)
            {
                // 觸發動畫 (這是非阻塞的)
                if (!string.IsNullOrEmpty(animNode.targetObjectName))
                {
                    GameObject target = GameObject.Find(animNode.targetObjectName);
                    if (target != null)
                    {
                        Animator animator = target.GetComponent<Animator>();
                        if (animator != null && !string.IsNullOrEmpty(animNode.triggerName))
                            animator.SetTrigger(animNode.triggerName);
                    }
                }
                currentNode = animNode.GetNextNode();
            }
            else if (currentNode is CameraControlNode camNode)
            {
                // 跳過運鏡 (不等待)
                currentNode = camNode.GetNextNode();
            }
            else if (currentNode is InvokeEventNode eventNode)
            {
                // 觸發事件
                if (!string.IsNullOrEmpty(eventNode.eventID))
                {
                    string trimmedID = eventNode.eventID.Trim();

                    // 【修正】: 必須遍歷列表，對 "每一個" 監聽器呼叫 TriggerEvent
                    // (這個邏輯現在和 ProcessCurrentNode 中的邏輯一致了)
                    if (eventListeners.TryGetValue(trimmedID, out List<DialogueEventListener> listenersToTrigger))
                    {
                        // 遍歷這個列表中的 "所有" 監聽器
                        foreach (DialogueEventListener listener in listenersToTrigger.ToList())
                        {
                            if (listener != null)
                            {
                                // 向 "每一個" 監聽器觸發事件
                                listener.TriggerEvent(trimmedID);
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[Skip] 找不到事件 ID '{trimmedID}' 的監聽器！");
                    }
                }
                currentNode = eventNode.GetNextNode();
            }
            else if (currentNode is SetGlobalVariableNode setGlobalNode)
            {
                // 執行設置全域變數
                if (setGlobalNode.database != null)
                    setGlobalNode.database.SetVariable(setGlobalNode.globalVariableName, setGlobalNode.valueToSet);
                currentNode = setGlobalNode.GetNextNode();
            }
            else if (currentNode is GetGlobalVariableNode getGlobalNode)
            {
                // 執行獲取全域變數
                if (getGlobalNode.database != null && currentGraph != null)
                {
                    float globalValue = getGlobalNode.database.GetVariable(getGlobalNode.globalVariableName);
                    (currentGraph as DialogueGraph).SetVariable(getGlobalNode.localVariableName, globalValue);
                }
                currentNode = getGlobalNode.GetNextNode();
            }
            else if (currentNode is UpdateQuestNode questNode)
            {
                // 執行更新任務
                if (QuestManager.Instance != null)
                    QuestManager.Instance.UpdateQuestStatus(questNode.questID, questNode.newStatus);
                currentNode = questNode.GetNextNode();
            }
            else if (currentNode is CheckQuestNode checkQuestNode)
            {
                // 執行檢查任務
                bool conditionResult = false;
                if (QuestManager.Instance != null)
                {
                    QuestStatus currentStatus = QuestManager.Instance.GetQuestStatus(checkQuestNode.questID);
                    conditionResult = (currentStatus == checkQuestNode.statusToCheck);
                }
                currentNode = checkQuestNode.GetNextNode(conditionResult);
            }
            else if (currentNode is CheckSpecificItemsNode checkSpecificItemsNode)
            {
                // 執行檢查物品
                bool allItemsFound = true;
                if (InventoryManager.Instance != null && checkSpecificItemsNode.requiredItems != null)
                {
                    foreach (ItemData requiredItem in checkSpecificItemsNode.requiredItems)
                    {
                        if (requiredItem != null && !InventoryManager.Instance.HasItem(requiredItem.itemID))
                        {
                            allItemsFound = false;
                            break;
                        }
                    }
                }
                else { allItemsFound = false; }
                currentNode = checkSpecificItemsNode.GetNextNode(allItemsFound);
            }
            else
            {
                // 其他未知的節點類型，安全起見直接前進
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