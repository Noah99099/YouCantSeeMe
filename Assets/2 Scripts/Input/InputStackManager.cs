// 檔案名稱: InputStackManager.cs
using System.Collections.Generic;
using System.Linq; // 用於 PeekOrDefault 等擴充
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 管理 Unity Input System 的 InputActionMap 啟用狀態的堆疊式管理器。
/// 確保在任何時候只有最上層的或指定的 Map 處於活躍狀態。
/// 使用單例模式，方便全局存取。
/// </summary>
public class InputStackManager : MonoBehaviour
{
    /// <summary>
    /// 全局靜態實例
    /// </summary>
    public static InputStackManager Instance { get; private set; }

    // +++ 新增 +++
    [Header("滑鼠狀態管理")]
    [Tooltip("請在此處填入所有「鍵鼠和手柄交替」的 Action Map 名稱")]
    [SerializeField] private List<string> uiMapNames = new List<string>();

    private readonly Stack<string> mapStack = new Stack<string>();
    //private Stack<string> mapStack = new Stack<string>();
    private InputActionAsset inputActionAsset; // 使用通用的 InputActionAsset

    private void Awake()
    {
        // --- Singleton Pattern ---
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("發現重複的 InputStackManager 實例，將銷毀此物件。");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // +++ 新增 +++
    /// <summary>
    /// (註冊) 向管理器註冊一個 PlayerControls 實例。
    /// </summary>
    public void RegisterControls(PlayerControls controls)
    {
        // 從 PlayerControls 實例中獲取底層的 asset
        this.inputActionAsset = controls.asset;
        Debug.Log("InputStackManager: 成功註冊了 PlayerControls 資源。");
    }

    // +++ 新增 +++
    public void UnregisterControls()
    {
        this.inputActionAsset = null;
        mapStack.Clear();
    }

    /// <summary>
    /// 初始化輸入棧，清空現有狀態並設置一個初始的 Action Map。
    /// 通常在遊戲或場景開始時呼叫。
    /// </summary>
    /// <param name="initialMap">要設定為棧底的 Action Map 名稱。</param>
    public void Init(string initialMap)
    {
        if (inputActionAsset == null) return; // 如果沒有 inputActionAsset，不做任何事
        mapStack.Clear();
        SwitchToExclusive(null); // 停用所有 Map

        mapStack.Push(initialMap);
        SetMapEnabled(initialMap, true);
        Debug.Log($"輸入棧已初始化，當前 Action Map: {initialMap}");

        // +++ 新增 +++
        UpdateCursorStateBasedOnTopMap();
    }

    /// <summary>
    /// 將一個新的 Action Map 推入棧頂並啟用它。
    /// </summary>
    /// <param name="mapName">要啟用的 Action Map 名稱。</param>
    /// <param name="isOverlay">
    /// 是否為疊加模式？
    /// false (預設): 獨佔模式，會停用棧中前一個 Map。適用於暫停選單、全螢幕 UI。
    /// true: 疊加模式，不會停用前一個 Map。適用於遊戲內的訊息提示、不中斷操作的快捷選單。
    /// </param>
    public void PushMap(string mapName, bool isOverlay = false)
    {
        if (inputActionAsset == null) return; // 如果沒有 inputActionAsset，不做任何事

        if (inputActionAsset.FindActionMap(mapName) == null)
        {
            Debug.LogError($"嘗試 Push 一個不存在的 Action Map: {mapName}");
            return;
        }

        if (mapStack.Count > 0 && !isOverlay)
        {
            // 在獨佔模式下，停用前一個 Map
            string previousMap = mapStack.Peek();
            SetMapEnabled(previousMap, false);
        }

        mapStack.Push(mapName);
        SetMapEnabled(mapName, true);
        Debug.Log($"Push Map: {mapName} (Overlay: {isOverlay})。當前棧: [{string.Join(", ", mapStack.Reverse())}]");

        // +++ 新增 +++
        UpdateCursorStateBasedOnTopMap();
    }

    /// <summary>
    /// 從棧頂彈出當前的 Action Map，並重新啟用棧中新的頂層 Map。
    /// </summary>
    public void PopMap()
    {
        if (inputActionAsset == null) return; // 如果沒有 PlayerInput，不做任何事
        if (mapStack.Count <= 1)
        {
            Debug.LogWarning("嘗試 PopMap 但棧中只剩下最後一個 Map，操作已取消。");
            return;
        }

        string poppedMap = mapStack.Pop();
        SetMapEnabled(poppedMap, false);

        // 重新啟用新的棧頂 Map
        string newTopMap = mapStack.Peek();
        SetMapEnabled(newTopMap, true);

        Debug.Log($"Pop Map: {poppedMap}。當前啟用的 Action Map: {newTopMap}。當前棧: [{string.Join(", ", mapStack.Reverse())}]");

        // +++ 新增 +++
        UpdateCursorStateBasedOnTopMap();
    }

    /// <summary>
    /// 根據棧頂的 Action Map，自動更新滑鼠的鎖定和可見狀態。
    /// </summary>
    private void UpdateCursorStateBasedOnTopMap()
    {
        if (mapStack.Count == 0) return;

        string currentTopMap = mapStack.Peek();

        // 檢查當前的 Map 是否在我們定義的 UI Map 列表中
        if (uiMapNames.Contains(currentTopMap))
        {
            // 如果是 UI Map，則顯示並解鎖滑鼠
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Debug.Log($"[InputStackManager] 進入 UI Map ({currentTopMap})，顯示滑鼠。");
        }
        else
        {
            // 如果不是 UI Map (即遊戲世界 Map)，則隱藏並鎖定滑鼠
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            Debug.Log($"[InputStackManager] 進入 Gameplay Map ({currentTopMap})，隱藏滑鼠。");
        }
    }

    /// <summary>
    /// 替換棧底的 Action Map。常用於場景切換，改變基礎控制模式。
    /// 此函式已修正邏輯，會確保棧頂的 Map 保持啟用狀態。
    /// </summary>
    /// <param name="newBottomMap">新的棧底 Action Map 名稱。</param>
    public void ReplaceBottom(string newBottomMap)
    {
        if (mapStack.Count == 0)
        {
            Init(newBottomMap);
            return;
        }

        // 進行替換
        string[] maps = mapStack.ToArray();
        mapStack.Clear();
        mapStack.Push(newBottomMap);
        for (int i = maps.Length - 2; i >= 0; i--)
        {
            mapStack.Push(maps[i]);
        }

        // 修正邏輯：永遠只啟用棧頂的 Map
        string currentTopMap = mapStack.Peek();
        SwitchToExclusive(currentTopMap);

        Debug.Log($"棧底已替換為 {newBottomMap}，當前啟用的 Map 為: {currentTopMap}。當前棧: [{string.Join(", ", mapStack.Reverse())}]");
    }

    /// <summary>
    /// 清空整個輸入棧，並停用所有 Action Map。
    /// 在場景轉換或需要重置輸入狀態時非常有用。
    /// </summary>
    public void ClearStack()
    {
        SwitchToExclusive(null);
        mapStack.Clear();
        Debug.Log("輸入棧已清空。");
    }

    /// <summary>
    /// 獲取當前在棧頂的 Action Map 名稱。
    /// </summary>
    /// <returns>棧頂 Map 名稱，如果棧為空則返回 null。</returns>
    public string GetCurrentMapName()
    {
        return mapStack.Count > 0 ? mapStack.Peek() : null;
    }

    /// <summary>
    /// 獲取當前在棧頂的 InputActionMap 物件實例。
    /// </summary>
    /// <returns>InputActionMap 物件，如果找不到或棧為空則返回 null。</returns>
    public InputActionMap GetCurrentActionMap()
    {
        if (mapStack.Count == 0) return null;
        return inputActionAsset.FindActionMap(mapStack.Peek());
    }

    /// <summary>
    /// 強制切換到某個 Map，並停用所有其他的 Map。
    /// 這是一個較為底層的獨佔操作。
    /// </summary>
    private void SwitchToExclusive(string mapName)
    {
        if (inputActionAsset == null) return;
        foreach (var map in inputActionAsset.actionMaps)
        {
            if (map.name != mapName)
            {
                map.Disable();
            }
        }

        if (!string.IsNullOrEmpty(mapName))
        {
            SetMapEnabled(mapName, true);
        }
    }

    /// <summary>
    /// 啟用或停用指定的 Action Map。
    /// </summary>
    private void SetMapEnabled(string mapName, bool enabled)
    {
        if (inputActionAsset == null) return;
        var map = inputActionAsset.FindActionMap(mapName);
        if (map != null)
        {
            if (enabled) map.Enable();
            else map.Disable();
        }
        else
        {
            Debug.LogError($"找不到名為 '{mapName}' 的 Action Map。");
        }
    }
}