// 檔案名稱: InputActionMaps.cs
// 功用: 集中管理所有的 Action Map 名稱，避免在程式碼中直接使用字串，防止打字錯誤。

/// <summary>
/// 靜態類別，用來存放所有 InputActionMap 的名稱常數。
/// </summary>
public static class InputActionMaps
{
    public const string _Player = "Player"; //Level1開始的默認模式
    public const string _UI = "UI"; //主介面開始的默認模式
    public const string _Dialogue = "Dialogue";
    public const string _Setting = "Setting"; //遊戲開始後的遊戲設置，Player - Setting
    public const string _Inventory = "Inventory"; //Player - Inventory，案件紀錄簿-物品
    public const string _ModelPreview = "ModelPreview"; //遊戲開始後的遊戲設置，Player - Setting
    public const string _Loading = "Loading"; //轉場使用，沒有任何 action
    public const string _GhostPanel = "GhostPanel"; //Player - Inventory - GhostPanel，案件紀錄簿-鬼
    public const string _VoicePanel = "VoicePanel"; //Player - Inventory - GhostPanel，案件紀錄簿-聲音
    public const string _CluePanel = "CluePanel"; //Player - Inventory - GhostPanel，案件紀錄簿-組合線索
    public const string _Map = "Map"; //Player - Map，平面圖
    public const string _Tutorial = "Tutorial"; //Player - Tutorial，教學分頁圖
    public const string _Keypad = "Keypad"; //Player - Keypad，近距離按鍵密碼鎖交互
    public const string _ViewImagePanel = "ViewImagePanel"; //Player - ViewImagePanel，對應物件的陰陽UI圖片顯示
    // ... 在此處加入您所有的 Action Map 名稱
}