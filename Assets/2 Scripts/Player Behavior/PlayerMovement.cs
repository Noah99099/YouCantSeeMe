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
    [SerializeField] private float moveSpeed = 5f;

    private Vector2 moveInput; //儲存WASD、手柄移動的數值
    private PlayerControls playerControls;

    private void Awake() 
    {
        playerControls = new PlayerControls(); //將 PlayerControls類 實例化
        if (rb == null) print(gameObject.name + "的" + this.name + "缺東西");
    }
 
    private void OnEnable()
    {
        playerControls.Player.Enable();
    }

    private void OnDisable()
    {
        playerControls.Player.Disable();
    }
    private void FixedUpdate()
    {
        //讀取儲存WASD、手柄移動的輸入值
        moveInput = playerControls.Player.Move.ReadValue<Vector2>();
        //計算移動方向
        Vector3 moveDirection = (playerBody.forward * moveInput.y + playerBody.right * moveInput.x).normalized;
        Vector3 targetPos = rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(targetPos); //用rigidbody的方式控制移動，而非position
    }
}
