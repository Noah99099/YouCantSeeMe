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
    private Dictionary<string, DialogueEventListener> eventListeners = new Dictionary<string, DialogueEventListener>();

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
        string trimmedID = listener.eventID.Trim();
        if (eventListeners.ContainsKey(trimmedID))
        {
            Debug.LogWarning($"事件ID '{trimmedID}' 已被註冊，將被覆蓋。");
        }
        eventListeners[trimmedID] = listener;
    }

    public void UnregisterListener(DialogueEventListener listener)
    {
        string trimmedID = listener.eventID.Trim();
        if (eventListeners.ContainsKey(trimmedID))
        {
            eventListeners.Remove(trimmedID);
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
                // --- 新增日誌：在查找前，打印出字典所有內容 ---
                Debug_PrintListeners();

                string trimmedID = eventNode.eventID.Trim();
                Debug.Log($"[DialogueManager] 正在廣播事件，嘗試查找 ID: \"{trimmedID}\"");

                if (eventListeners.ContainsKey(trimmedID))
                {
                    eventListeners[trimmedID].TriggerEvent();
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
            foreach (var listenerPair in eventListeners)
            {
                Debug.Log($"Key: \"{listenerPair.Key}\" -> 監聽物件: {listenerPair.Value.gameObject.name}");
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

    // Skip 和 AutoPlay 功能需要用新的圖形邏輯重寫，暫時保留或移除
    public void OnSkip(InputAction.CallbackContext context)
    {
        if (!isDialogueActive || isTyping) return;

        // 停止所有正在進行的協程，例如打字效果
        StopAllCoroutines();
        isTyping = false;
        dialogueUI.ClearChoices(); // 如果剛好在選項處，也清除選項
                                   // --- 在跳過之前，停止當前語音 ---
        if (DialogueAudioManager.Instance != null) DialogueAudioManager.Instance.StopVoiceOver();

        Debug.Log("開始尋找下一個重要節點...");
        BaseNode nextImportantNode = FindNextImportantNode();

        if (nextImportantNode != null)
        {
            Debug.Log("找到重要節點: " + nextImportantNode.name);
            currentNode = nextImportantNode;
            ProcessCurrentNode();
        }
        else
        {
            Debug.Log("找不到更多重要節點，對話結束。");
            EndConversation();
        }
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

    public void OnToggleAutoPlay(InputAction.CallbackContext context)
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
    /*
    private void OnToggleAutoPlay(InputAction.CallbackContext context)
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
    */

    private BaseNode FindNextImportantNode()
    {
        // 使用佇列 (Queue) 進行廣度優先搜索
        Queue<BaseNode> nodesToVisit = new Queue<BaseNode>();

        // 使用 HashSet 記錄已訪問過的節點，避免在迴圈中卡死
        HashSet<BaseNode> visitedNodes = new HashSet<BaseNode>();

        // 將當前節點的所有直接後續節點加入佇列
        if (currentNode is ChoiceNode choiceNode)
        {
            for (int i = 0; i < choiceNode.choices.Count; i++)
            {
                BaseNode nextNode = choiceNode.GetNextNodeForChoice(i);
                if (nextNode != null) nodesToVisit.Enqueue(nextNode);
            }
        }
        else
        {
            BaseNode nextNode = currentNode.GetNextNode();
            if (nextNode != null) nodesToVisit.Enqueue(nextNode);
        }

        // 將當前節點標記為已訪問
        visitedNodes.Add(currentNode);

        // 開始搜索
        while (nodesToVisit.Count > 0)
        {
            BaseNode node = nodesToVisit.Dequeue();

            if (visitedNodes.Contains(node)) continue;
            visitedNodes.Add(node);

            // 檢查是否找到了重要節點
            if (node.isImportant)
            {
                return node; // 找到了！返回這個節點
            }

            // 如果沒找到，將這個節點的後續節點加入佇列，繼續搜索
            if (node is ChoiceNode cNode)
            {
                for (int i = 0; i < cNode.choices.Count; i++)
                {
                    BaseNode next = cNode.GetNextNodeForChoice(i);
                    if (next != null) nodesToVisit.Enqueue(next);
                }
            }
            else
            {
                BaseNode next = node.GetNextNode();
                if (next != null) nodesToVisit.Enqueue(next);
            }
        }

        // 如果遍歷完所有可達節點都沒找到，返回 null
        return null;
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