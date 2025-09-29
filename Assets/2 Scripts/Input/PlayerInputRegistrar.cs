// 檔案名稱: PlayerInputRegistrar.cs
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 此腳本的唯一職責：
/// 在 Awake 時，將同物件上的 PlayerInput 元件註冊到 InputStackManager。
/// 在 OnDestroy 時，將其從 InputStackManager 中註銷。
/// </summary>
[RequireComponent(typeof(PlayerInput))] // 確保此腳本一定和 PlayerInput 掛在一起
public class PlayerInputRegistrar : MonoBehaviour
{
    private PlayerInput playerInput;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();

        // 確保 InputStackManager 實例已存在
        if (InputStackManager.Instance != null)
        {
            InputStackManager.Instance.RegisterPlayerInput(playerInput);
        }
        else
        {
            Debug.LogError("PlayerInputRegistrar: 找不到 InputStackManager 的實例！請確保 InputStackManager 物件比 Player 物件更早被建立。");
        }
    }

    private void OnDestroy()
    {
        // 檢查 Instance 是否還存在，因為在遊戲關閉時，管理器可能已被銷毀
        if (InputStackManager.Instance != null)
        {
            InputStackManager.Instance.UnregisterPlayerInput(playerInput);
        }
    }
}