using UnityEngine;

// 檔案名稱: IInteractable.cs
/// <summary>
/// 它只是一個「合約」，規定所有能互動的物件都必須提供以下兩個功能
/// </summary>
public interface IInteractable
{
    // 1. 提供給 UI 顯示的提示文字 (傳入 isGamepad 判斷要顯示按鍵還是滑鼠)
    string GetInteractPrompt(bool isGamepad);

    // 2. 玩家按下按鍵時實際執行的邏輯 (傳入 player 讓物件可以呼叫玩家的功能，如開背包)
    void Interact(PlayerInteraction player);
}
