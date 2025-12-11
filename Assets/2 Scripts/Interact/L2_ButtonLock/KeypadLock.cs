// KeypadLock.cs
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static InputStackManager;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class KeypadLock : MonoBehaviour
{
    // ***** 【新增：單例實例】 *****
    public static KeypadLock Instance { get; private set; }

    [Header("物件名稱")]
    public string objectName = "";

    [Header("場景物件引用")]
    [Tooltip("場景中未解鎖的密碼鎖物件 (自身)")]
    [SerializeField] private GameObject _sceneLockedKeypad; // L2_Keypad_Root_Locked (即 this.gameObject)
    [Tooltip("場景中解鎖後的密碼鎖物件")]
    [SerializeField] private GameObject _sceneUnlockedKeypad; // L2_Keypad_Root_Unlocked (需預設隱藏)

    [Header("畫面 UI 引用")]
    [Tooltip("包含 UI Button 的整個 UI 面板")]
    [SerializeField] private GameObject _uiKeypadRoot; // UI_Keypad_Root (Canvas/Panel)

    // ***** 【新增：UI 密碼鎖 Prefab 與錨點】 *****
    [Tooltip("包含 KeypadButton.cs 的 3D 密碼鎖模型 Prefab")]
    [SerializeField] private GameObject _screen3DKeypadModelPrefab; // L2_UI_Lock Prefab

    [Tooltip("場景 1 中 L2_UI_Lock 的父物件 (相機子物件，作為 Prefab 的錨點)")]
    private Transform _screen3DKeypadModelAnchor; // 改為 Transform 類型，用於設置父級
    private GameObject _currentScreen3DKeypadModelInstance; // 用於儲存當前生成的實例

    [Header("密碼設定")]
    [Tooltip("正確密碼組合 (數字 0-9)，順序不重要")]
    [SerializeField] private List<int> _correctPassword = new List<int> { 1, 3, 5, 7 };

    [Header("Layer 設定")]
    [SerializeField] private string _interactableLayerName = "Interactable";
    // 解鎖後密碼鎖的 Layer 保持 _interactableLayerName 也可以，但最好是 Default 或 Ignore Raycast，避免再次觸發。
    [SerializeField] private string _disabledLayerName = "Ignore Raycast";

    // ----- 私有狀態 -----
    public bool IsLocked { get; private set; } = true;
    private ChangeObjectLayer _layerChanger;
    private bool[] _currentInputState = new bool[10];

    // ----- 事件 (供 KeypadButton 訂閱視覺更新) -----
    public event System.Action<int, bool> OnDigitStateChanged;

    private void Awake()
    {
        // ***** 【修正 1：單例初始化邏輯】 *****
        if (Instance != null && Instance != this)
        {
            // 如果場景中已經有一個實例，銷毀這個新的實例
            Destroy(this.gameObject);
            return;
        }
        Instance = this; // 將自身設置為唯一的實例

        _layerChanger = GetComponent<ChangeObjectLayer>();
        if (_layerChanger == null)
        {
            Debug.LogError("KeypadLock 需要 ChangeObjectLayer 腳本!");
            enabled = false;
        }
        // ***** 【修正 2：場景鎖物件引用檢查】 *****
        if (_sceneLockedKeypad == null)
        {
            Debug.LogError("請在 Inspector 中引用 _sceneLockedKeypad 物件!");
            enabled = false;
            return;
        }

        // ** 【關鍵修正：查找錨點並儲存為 Transform】 **
        GameObject playerCameraObject = GameObject.FindWithTag("PlayerCamera");
        if (playerCameraObject != null)
        {
            Transform cameraTransform = playerCameraObject.transform;
            // 假設 L2_UI_Lock 是一個空物件錨點
            Transform screenModelAnchor = cameraTransform.Find("L2_UI_Lock_Anchor");
            // 建議將場景 1 的物件改名為 L2_UI_Lock_Anchor，以區別 Prefab

            if (screenModelAnchor != null)
            {
                // 儲存錨點 Transform
                _screen3DKeypadModelAnchor = screenModelAnchor;
            }
            else
            {
                Debug.LogWarning("在 PlayerCamera 子物件中找不到名為 'L2_UI_Lock_Anchor' 的物件！請確認名稱是否正確。");
            }
        }
    }

    private void Start()
    {
        // 確保初始狀態正確
        _sceneLockedKeypad.SetActive(true);
        _sceneUnlockedKeypad.SetActive(false);
        _uiKeypadRoot.SetActive(false);
        SetLayer(_sceneLockedKeypad, _interactableLayerName);
    }

    // ----- 核心狀態切換 (供 PlayerInteraction.cs 呼叫) -----

    /// <summary>
    /// 進入密碼輸入模式 (由 PlayerInteraction.cs 呼叫)
    /// </summary>
    public void EnterInteractionState()
    {
        if (!IsLocked || _uiKeypadRoot.activeSelf) return;

        // 1. 隱藏場景中的密碼鎖
        _sceneLockedKeypad.SetActive(false);

        // 2. 禁用 Player Map, 啟用 Keypad Map
        InputStackManager.Instance.PushMap(InputActionMaps._Keypad);

        // ***** 【關鍵修正：生成 3D 密碼鎖模型】 *****
        if (_screen3DKeypadModelPrefab != null && _screen3DKeypadModelAnchor != null)
        {
            // 在錨點位置生成 Prefab
            _currentScreen3DKeypadModelInstance = Instantiate(_screen3DKeypadModelPrefab, _screen3DKeypadModelAnchor);
        }
        else
        {
            Debug.LogError("KeypadLock 缺少 3D 密碼鎖 Prefab 或錨點！");
        }

        // 3. 顯示 UI 密碼鎖畫面
        _uiKeypadRoot.SetActive(true);

        // 4. 重設輸入狀態
        _currentInputState = new bool[10];
        Debug.Log("進入密碼輸入畫面。");

        // 5.關掉UI
        PlayerInteraction.Instance.crossHair.SetActive(false);
        PlayerInteraction.Instance.titleUI.SetActive(false);
    }

    /// <summary>
    /// [UI 點擊呼叫] 處理數字按鈕 (0-9) 的點擊
    /// </summary>
    public void HandleDigitPress(int digit)
    {
        if (!IsLocked || !_uiKeypadRoot.activeSelf) return;

        _currentInputState[digit] = !_currentInputState[digit];
        OnDigitStateChanged?.Invoke(digit, _currentInputState[digit]);

        // ***** 【新增：清除 UI 焦點】 *****
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    /// <summary>
    /// [UI 點擊呼叫] 處理確認按鈕 (L2_Lock_Bottom) 的點擊
    /// </summary>
    public void CheckPassword()
    {
        if (!IsLocked || !_uiKeypadRoot.activeSelf) return;

        bool allCorrect = true;
        // 遍歷所有 10 個數字 (從 0 到 9)
        for (int i = 0; i <= 9; i++)
        {
            // 判斷第 i 個數字的正確狀態：
            // true = 該數字在 _correctPassword 列表中 (應被按下)
            // false = 該數字不在 _correctPassword 列表中 (應被彈起)
            bool isDigitCorrectlyPressed = _correctPassword.Contains(i);

            // 檢查玩家當前的輸入狀態 (_currentInputState[i]) 是否等於正確狀態 (isDigitCorrectlyPressed)
            if (_currentInputState[i] != isDigitCorrectlyPressed)
            {
                // 如果任何一個按鈕的狀態不符合要求 (無論是該按下的沒按，還是不該按的按了)
                allCorrect = false;
                break; // 只要發現一個錯誤，就可以停止檢查
            }
        }

        if (allCorrect)
        {
            Debug.Log("密碼正確！解鎖！");
            UnlockKeypadAndExit();
        }
        else
        {
            Debug.Log("密碼錯誤，請重新輸入。");
            // 密碼錯誤處理
        }
    }

    /// <summary>
    /// 密碼成功時的退出流程
    /// </summary>
    private void UnlockKeypadAndExit()
    {
        IsLocked = false;

        // 1. 隱藏 UI 密碼鎖畫面
        _uiKeypadRoot.SetActive(false);

        // ***** 【關鍵修正：銷毀生成的 3D 密碼鎖模型】 *****
        if (_currentScreen3DKeypadModelInstance != null)
        {
            Destroy(_currentScreen3DKeypadModelInstance);
            _currentScreen3DKeypadModelInstance = null;
        }

        // 2. 退出 Keypad Map，恢復 Player Map
        InputStackManager.Instance.PopMap();

        // 3. 播放解鎖鎖鉤動畫 (可選，如果 _sceneUnlockedKeypad 包含動畫)
        // 4. 顯示已解鎖的場景密碼鎖
        _sceneUnlockedKeypad.SetActive(true);
        // 將解鎖後的物件 Layer 設定為無法交互 (例如 Default 或 Ignore Raycast)
        SetLayer(_sceneUnlockedKeypad, _disabledLayerName);

        Debug.Log("密碼鎖：已解鎖並替換模型。");
        // 禁用自身腳本 (L2_Keypad_Root_Locked) 以防萬一
        enabled = false;

        // 5.打開UI
        PlayerInteraction.Instance.crossHair.SetActive(true);
        PlayerInteraction.Instance.titleUI.SetActive(true);
    }

    /// <summary>
    /// 退出輸入模式 (由 KeypadPanelUIController.cs 呼叫，通常是按 ESC 鍵)
    /// </summary>
    public void ExitInteractionState()
    {
        if (!IsLocked || !_uiKeypadRoot.activeSelf) return;

        // 1. 隱藏 UI
        _uiKeypadRoot.SetActive(false);

        // ***** 【關鍵修正：銷毀生成的 3D 密碼鎖模型】 *****
        if (_currentScreen3DKeypadModelInstance != null)
        {
            Destroy(_currentScreen3DKeypadModelInstance);
            _currentScreen3DKeypadModelInstance = null;
        }

        // 2. 顯示場景中的密碼鎖 (恢復到可交互狀態)
        _sceneLockedKeypad.SetActive(true);
        
        // 3. 退出 Keypad Map，恢復 Player Map
        InputStackManager.Instance.PopMap();

        Debug.Log("密碼鎖：強制退出輸入，Player Map 已恢復。");

        // 4.打開UI
        PlayerInteraction.Instance.crossHair.SetActive(true);
        PlayerInteraction.Instance.titleUI.SetActive(true);
    }

    // ----- 輔助方法 -----
    private void SetLayer(GameObject targetObject, string targetLayerName)
    {
        if (targetObject.TryGetComponent<ChangeObjectLayer>(out var layerChanger))
        {
            layerChanger.targetLayerName = targetLayerName;
            layerChanger.ChangeLayer();
        }
        else
        {
            Debug.LogError($"物件 {targetObject.name} 缺少 ChangeObjectLayer 腳本!");
        }
    }

    private void OnDestroy()
    {
        // 【新增：在銷毀時清除靜態引用】
        if (Instance == this)
        {
            Instance = null;
        }
    }
}