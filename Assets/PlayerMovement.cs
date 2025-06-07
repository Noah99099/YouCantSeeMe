using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Tooltip("玩家建模位置")]
    [SerializeField] private Transform playerBody;
    [Tooltip("玩家基礎移動速度")]
    [SerializeField] private float moveSpeed = 5f;
    //[Tooltip("最終加速倍率")]
    //[SerializeField] private float speedMultiplier;
    //[Tooltip("加速過渡時間")]
    //[SerializeField] private float accelerationTime = 0.5f;
    //[Tooltip("是否加速移動")]
    //[SerializeField] private bool isFaster = false; //默認不加速

    private Vector2 moveInput; //儲存WASD、手柄移動的數值
    private PlayerControls playerControls;
    //private float accelerationProgress; // 0~1的加速進度值

    private void Awake() => playerControls = new PlayerControls(); //將 PlayerControls類 實例化
 
    private void OnEnable()
    {
        playerControls.Player.Enable();

        //註冊跑步事件監聽
        //playerControls.Player.Run.started += OnRunStarted;
        //playerControls.Player.Run.canceled += OnRunCanceled;
    }

    private void OnDisable()
    {
        playerControls.Player.Disable();

        //取消註冊跑步事件監聽
        //playerControls.Player.Run.started -= OnRunStarted;
        //playerControls.Player.Run.canceled -= OnRunCanceled;
    }
    private void Update()
    {
        //讀取儲存WASD、手柄移動的輸入值
        moveInput = playerControls.Player.Move.ReadValue<Vector2>();

        //計算移動方向
        Vector3 moveDirection = (playerBody.forward * moveInput.y + playerBody.right * moveInput.x).normalized;

        //if (isFaster)
        //{
        //    //按住加速鍵時
        //    accelerationProgress += Time.deltaTime / accelerationTime;
        //    accelerationProgress = Mathf.Clamp01(accelerationProgress);
        //}
        //else
        //{
        //    //放開加速鍵時
        //    accelerationProgress -= Time.deltaTime / accelerationTime;
        //    accelerationProgress = Mathf.Clamp01(accelerationProgress);
        //}

        //根據是否加速計算最終速度
        //float currentSpeed = moveSpeed * (isFaster ? Mathf.Lerp(1f, speedMultiplier, accelerationProgress) : 1f);

        //應用玩家移動1：有緩緩加速
        //playerBody.Translate(moveDirection * currentSpeed * Time.deltaTime, Space.World);
        //應用玩家移動2：直接加速
        //playerBody.Translate(moveDirection * currentSpeed * Time.deltaTime, Space.World);
        //沒有加速功能
        playerBody.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);
    }

    //private void OnRunStarted(InputAction.CallbackContext context)
    //{
    //    isFaster = true;
    //}
    //private void OnRunCanceled(InputAction.CallbackContext context)
    //{
    //    isFaster = false;
    //}
}
