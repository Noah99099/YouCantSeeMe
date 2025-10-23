using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using XNode;
using System.Collections.Generic;
using System.Linq;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private float typeWriterSpeed = 0.05f;
    [SerializeField] private float autoPlayDelay = 2.0f;
    [Header("遊戲 UI 控制")]
    [Tooltip("將您場景中代表準心的 UI GameObject 拖曳到這裡")]
    [SerializeField] private GameObject crosshairUI;
    [Tooltip("將您場景中其他需要隱藏的 UI GameObject 拖曳到這裡")]
    [SerializeField] private GameObject otherUI;

    private bool isDialogueActive = false;
    private bool isTyping = false;
    private bool isAutoPlay = false;
    private bool isWaitingForChoice = false;

    private PlayerControls playerControls;

    private DialogueGraph currentGraph;
    private BaseNode currentNode;
    private Camera mainCamera;
    private Vector3 originalCameraPos;
    private Quaternion originalCameraRot;
    private bool cameraHasBeenMoved = false; // 旗標，用來追蹤攝影機是否被移動過
    [SerializeField] private float cameraReturnDuration = 1.0f; // 攝影機歸位的時間
    private Dictionary<string, DialogueEventListener> eventListeners = new Dictionary<string, DialogueEventListener>();

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
        playerControls = new PlayerControls();
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("場景中找不到主攝影機 (Main Camera)！");
        }
    }

    private void OnEnable()
    {
        playerControls.Dialogue.Enable();
        playerControls.Dialogue.Submit.performed += OnSubmit;
        // OnSkip 和 OnToggleAutoPlay 的邏輯需要基於圖形重寫，暫時禁用
        //playerControls.Dialogue.Skip.performed += OnSkip;
        //playerControls.Dialogue.ToggleAutoPlay.performed += OnToggleAutoPlay;
    }

    private void OnDisable()
    {
        playerControls.Dialogue.Disable();
        playerControls.Dialogue.Submit.performed -= OnSubmit;
       // playerControls.Dialogue.Skip.performed -= OnSkip;
        //playerControls.Dialogue.ToggleAutoPlay.performed -= OnToggleAutoPlay;
    }

    public void RegisterListener(DialogueEventListener listener)
    {
        string trimmedID = listener.eventID.Trim();
        if (eventListeners.ContainsKey(trimmedID))
        {
            Debug.LogWarning($"[DialogueManager] 事件 ID '{trimmedID}' 已被 '{eventListeners[trimmedID].gameObject.name}' 註冊，新的監聽器 '{listener.gameObject.name}' 將會覆蓋它。請確保 ID 的唯一性。");
        }
        eventListeners[trimmedID] = listener;
        // --- 新增日誌 ---
        Debug.Log($"[DialogueManager] 成功註冊監聽器. ID: \"{trimmedID}\", 物件: {listener.gameObject.name}");
    }

    public void UnregisterListener(DialogueEventListener listener)
    {
        string trimmedID = listener.eventID.Trim();
        if (eventListeners.ContainsKey(trimmedID))
        {
            eventListeners.Remove(trimmedID);
        }
    }

    public void StartConversation(DialogueGraph graph)
    {
        if (isDialogueActive) return;

        if (crosshairUI != null)
        {
            crosshairUI.SetActive(false);
        }

        if (otherUI != null)
        {
            otherUI.SetActive(false);
        }

        currentGraph = graph;
        isDialogueActive = true;
        isAutoPlay = false;
        cameraHasBeenMoved = false;

        StartNode startNode = null;
        foreach (Node node in currentGraph.nodes)
        {
            if (node is StartNode)
            {
                startNode = node as StartNode;
                break;
            }
        }

        if (startNode == null)
        {
            Debug.LogError("對話圖形中找不到 StartNode！");
            EndConversation();
            return;
        }

        currentNode = startNode.GetNextNode();
        if (currentNode == null)
        {
            EndConversation();
            return;
        }

        dialogueUI.ShowDialogueBox();
        ProcessCurrentNode();
    }

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
        if (mainCamera == null) yield break;

        if (!cameraHasBeenMoved)
        {
            originalCameraPos = mainCamera.transform.position;
            originalCameraRot = mainCamera.transform.rotation;
            cameraHasBeenMoved = true;
        }

        GameObject target = GameObject.Find(camNode.targetTransformName);
        if (target == null)
        {
            Debug.LogWarning($"CameraControlNode: 在場景中找不到名為 {camNode.targetTransformName} 的物件！");
            yield break;
        }

        Transform targetTransform = target.transform;
        Transform cameraTransform = mainCamera.transform;

        float timer = 0f;
        Vector3 startPos = cameraTransform.position;
        Quaternion startRot = cameraTransform.rotation;

        // 如果移動時間為 0 或更少，則瞬間移動
        if (camNode.transitionDuration <= 0)
        {
            cameraTransform.position = targetTransform.position;
            cameraTransform.rotation = targetTransform.rotation;
        }
        else
        {
            // 否則，進行平滑移動 (Lerp/Slerp)
            while (timer < camNode.transitionDuration)
            {
                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / camNode.transitionDuration);
                cameraTransform.position = Vector3.Lerp(startPos, targetTransform.position, t);
                cameraTransform.rotation = Quaternion.Slerp(startRot, targetTransform.rotation, t);
                yield return null;
            }
            // 確保最終位置和旋轉完全準確
            cameraTransform.position = targetTransform.position;
            cameraTransform.rotation = targetTransform.rotation;
        }

        // 如果需要等待，則在運鏡結束後才繼續對話
        if (camNode.waitForTransition)
        {
            currentNode = camNode.GetNextNode();
            ProcessCurrentNode();
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
            if (currentNode is LineNode lineNode)
            {
                string fullContent = LocalizationManager.Instance.GetLocalizedText(lineNode.line.contentKey);
                dialogueUI.CompleteText(fullContent);
            }
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
        isWaitingForChoice = false;
        dialogueUI.ClearChoices();

        if (currentNode is ChoiceNode choiceNode)
        {
            currentNode = choiceNode.GetNextNodeForChoice(choiceIndex);
            ProcessCurrentNode();
        }
    }

    private void EndConversation()
    {
        if (DialogueAudioManager.Instance != null) DialogueAudioManager.Instance.StopVoiceOver();
        isDialogueActive = false;
        dialogueUI.HideDialogueBox();
        Debug.Log("對話結束");

        if (crosshairUI != null)
        {
            crosshairUI.SetActive(true);
        }
        if (otherUI != null)
        {
            otherUI.SetActive(true);
        }

        // --- 新增：如果攝影機被移動過，就觸發歸位 ---
        if (cameraHasBeenMoved)
        {
            StartCoroutine(ReturnCameraToOriginalPosition());
        }
    }

    // Skip 和 AutoPlay 功能需要用新的圖形邏輯重寫，暫時保留或移除
    public void Skip()
    {
        Debug.Log("<color=cyan>[Debug] Skip() method called!</color>");
        // 增加 isDialogueActive 判斷
        if (!isDialogueActive || isTyping || isWaitingForChoice) return;

        StopAllCoroutines();
        isTyping = false;
        dialogueUI.ClearChoices();
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
    public void ToggleAutoPlay()
    {
        Debug.Log("<color=yellow>[Debug] ToggleAutoPlay() method called!</color>");
        if (!isDialogueActive) return;

        isAutoPlay = !isAutoPlay;
        Debug.Log("自動播放: " + (isAutoPlay ? "開啟" : "關閉"));

        // 如果剛開啟自動播放，且當前對話處於靜止狀態
        if (isAutoPlay && !isTyping && !isWaitingForChoice)
        {
            // 自動前進到下一個節點
            currentNode = currentNode.GetNextNode();
            ProcessCurrentNode();
        }
    }

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
    
}