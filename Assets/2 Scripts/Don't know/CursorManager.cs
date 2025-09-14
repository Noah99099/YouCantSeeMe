using UnityEngine;

public class CursorManager : MonoBehaviour
{
    /// <summary>
    /// 進入 UI 互動模式
    /// (顯示滑鼠、解除鎖定)
    /// </summary>
    public static void EnterUIMode()
    {
        // 讓滑鼠指標可以自由移動
        Cursor.lockState = CursorLockMode.None;
        // 讓滑鼠指標的圖案顯示出來
        Cursor.visible = true;
        Debug.Log("Entered UI Mode: Cursor Unlocked and Visible.");
    }

    /// <summary>
    /// 進入遊戲準心模式
    /// (隱藏滑鼠、鎖定在中央)
    /// </summary>
    public static void EnterGameplayMode()
    {
        // 將滑鼠指標鎖定在畫面中央
        Cursor.lockState = CursorLockMode.Locked;
        // 將滑鼠指標的圖案隱藏起來
        Cursor.visible = false;
        Debug.Log("Entered Gameplay Mode: Cursor Locked and Hidden.");
    }
}