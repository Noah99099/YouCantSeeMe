// 檔案名稱: InputProvider.cs
// 將此腳本掛在您的 Player 或一個專門的管理物件上
using UnityEngine;

//[DefaultExecutionOrder(-200)]  // 數字越小越早執行
public class InputProvider : MonoBehaviour
{
    // 全局靜態屬性，儲存唯一的 PlayerControls 實例
    public static PlayerControls InputActions { get; private set; }

    void Awake()
    {
        // 如果還沒有實例，就創建一個
        if (InputActions == null)
        {
            InputActions = new PlayerControls();
            Debug.Log("唯一的 PlayerControls 實例已創建。");
        }

        // 將這個唯一的實例註冊到管理器中
        if (InputStackManager.Instance != null)
        {
            InputStackManager.Instance.RegisterControls(InputActions);
        }
        else
        {
            Debug.LogError("找不到 InputStackManager 實例！");
        }
    }

    // 當遊戲關閉時，清理實例
    private void OnDestroy()
    {
        InputActions = null;
    }
}