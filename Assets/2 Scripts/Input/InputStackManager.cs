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

    private PlayerInput playerInput;
    private readonly Stack<string> mapStack = new Stack<string>();
    //private Stack<string> mapStack = new Stack<string>();

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
        // -------------------------

        //playerInput = GetComponent<PlayerInput>();
        //if (playerInput == null)
        //{
        //    Debug.LogError("InputStackManager 必須掛載在擁有 PlayerInput 元件的 GameObject 上！");
        //    enabled = false; // 禁用此腳本以避免後續錯誤
        //}
    }

    // +++ 新增 +++
    /// <summary>
    /// (註冊) 向管理器註冊一個 PlayerInput 實例。
    /// PlayerInput 物件應該在自己的 Awake() 中呼叫此方法。
    /// </summary>
    public void RegisterPlayerInput(PlayerInput pi)
    {
        // 如果已經有一個 PlayerInput，先將其註銷，以新的為主
        if (this.playerInput != null)
        {
            Debug.LogWarning($"InputStackManager: 一個新的 PlayerInput ({pi.gameObject.name}) 正在註冊，舊的 PlayerInput ({this.playerInput.gameObject.name}) 將被覆蓋。");
        }

        this.playerInput = pi;
        Debug.Log($"InputStackManager: 成功註冊了來自 '{pi.gameObject.name}' 的 PlayerInput。");

        // 可選：如果註冊時有特殊需求，可以在此處處理，例如強制初始化
        // Init(InputActionMaps.UI); 
    }

    // +++ 新增 +++
    /// <summary>
    /// (註銷) 從管理器中移除 PlayerInput 的引用。
    /// PlayerInput 物件應該在自己的 OnDestroy() 中呼叫此方法。
    /// </summary>
    public void UnregisterPlayerInput(PlayerInput pi)
    {
        // 確保要註銷的是當前註冊的那個實例，避免錯誤註銷
        if (this.playerInput == pi)
        {
            Debug.Log($"InputStackManager: 來自 '{pi.gameObject.name}' 的 PlayerInput 已註銷。");
            this.playerInput = null;
            mapStack.Clear(); // 清空輸入棧，因為輸入源已消失
        }
    }

    /// <summary>
    /// 初始化輸入棧，清空現有狀態並設置一個初始的 Action Map。
    /// 通常在遊戲或場景開始時呼叫。
    /// </summary>
    /// <param name="initialMap">要設定為棧底的 Action Map 名稱。</param>
    public void Init(string initialMap)
    {
        if (playerInput == null) return; // 如果沒有 PlayerInput，不做任何事
        mapStack.Clear();
        SwitchToExclusive(null); // 停用所有 Map

        mapStack.Push(initialMap);
        SetMapEnabled(initialMap, true);
        Debug.Log($"輸入棧已初始化，當前 Action Map: {initialMap}");
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
        if (playerInput == null) return; // 如果沒有 PlayerInput，不做任何事

        if (playerInput.actions.FindActionMap(mapName) == null)
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
    }

    /// <summary>
    /// 從棧頂彈出當前的 Action Map，並重新啟用棧中新的頂層 Map。
    /// </summary>
    public void PopMap()
    {
        if (playerInput == null) return; // 如果沒有 PlayerInput，不做任何事

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
        return playerInput.actions.FindActionMap(mapStack.Peek());
    }

    /// <summary>
    /// 強制切換到某個 Map，並停用所有其他的 Map。
    /// 這是一個較為底層的獨佔操作。
    /// </summary>
    private void SwitchToExclusive(string mapName)
    {
        foreach (var map in playerInput.actions.actionMaps)
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
        var map = playerInput.actions.FindActionMap(mapName);
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