// 檔案名稱: InputActionMaps.cs
// 功用: 集中管理所有的 Action Map 名稱，避免在程式碼中直接使用字串，防止打字錯誤。

/// <summary>
/// 靜態類別，用來存放所有 InputActionMap 的名稱常數。
/// </summary>
public static class InputActionMaps
{
    public const string _Player = "Player";
    public const string _UI = "UI"; //給主介面
    public const string _Menu = "Menu";
    public const string _Dialogue = "Dialogue";
    public const string _Common = "Common"; // 例如：一個包含 ESC (暫停) 的共用 Map
    // ... 在此處加入您所有的 Action Map 名稱
}