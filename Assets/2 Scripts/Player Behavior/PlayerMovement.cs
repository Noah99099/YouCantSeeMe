using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// UIInputManager 相關的內容不用
/// 要重寫，代替UIInputManager
/// 改好了
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    [Tooltip("玩家設置")]
    [SerializeField] private Transform playerBody;
    [SerializeField] private Rigidbody rb;
    [Tooltip("玩家基礎移動速度")]
    [SerializeField] private float moveSpeed = 50f;

    private Level1UIController inputHandler; // 新增：從這裡拿輸入
    private Vector2 moveInput; //儲存WASD、手柄移動的數值

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (playerBody == null) playerBody = transform;

        inputHandler = GetComponent<Level1UIController>(); // 同物件上自動取得

        // 0924訂閱場景加載事件
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    void OnDestroy()
    {
        // 取消訂閱避免記憶體洩漏
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 場景加載後強制重新啟用腳本
        this.enabled = true;
        Debug.Log($"場景加載完成，強制啟用 PlayerMovement: {this.enabled}");
    }

    private void FixedUpdate()
    {
        if (rb == null || inputHandler == null) return;

        moveInput = inputHandler.MoveInput; // 從 Level1UIController 取得輸入
        if (moveInput.sqrMagnitude < 0.01f) return;

        Vector3 moveDirection = (playerBody.forward * moveInput.y + playerBody.right * moveInput.x).normalized;
        Vector3 movement = moveDirection * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);

        print("移動成功");
    }
}