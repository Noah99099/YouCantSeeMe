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
    public const string _Inventory = "Inventory"; //遊戲開始後的遊戲設置，Player - Setting
    public const string _ModelPreview = "ModelPreview"; //遊戲開始後的遊戲設置，Player - Setting
    // ... 在此處加入您所有的 Action Map 名稱
}