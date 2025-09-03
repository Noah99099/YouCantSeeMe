using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Tooltip("玩家設置")]
    [SerializeField] private Transform playerBody;
    [SerializeField] private Rigidbody rb;
    [Tooltip("玩家基礎移動速度")]
    [SerializeField] private float moveSpeed = 50f;

    [Header("輸入設定")]
    [Tooltip("Move Action 的 InputActionReference")]
    [SerializeField] private InputActionReference moveAction;

    private Vector2 moveInput; //儲存WASD、手柄移動的數值

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (playerBody == null) playerBody = transform;
    }

    private void OnEnable()
    {
        if (moveAction != null)
            moveAction.action.Enable();
    }

    private void OnDisable()
    {
        if (moveAction != null)
            moveAction.action.Disable();
    }

    private void FixedUpdate()
    {
        if (!UIInputManager.Instance.IsGameStarted) return; //新增

        if (moveAction == null || rb == null) return;

        moveInput = moveAction.action.ReadValue<Vector2>();
        if (moveInput.sqrMagnitude < 0.01f) return;

        Vector3 moveDirection = (playerBody.forward * moveInput.y + playerBody.right * moveInput.x).normalized;
        Vector3 movement = moveDirection * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);
    }
}