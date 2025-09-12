using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

public class PlayerView : MonoBehaviour
{
    [Header("基本設定")]
    [Tooltip("玩家建模位置")]
    [SerializeField] private Transform playerBody;
    [Tooltip("玩家的剛體")]
    [SerializeField] private Rigidbody playerRigidbody;
    [Tooltip("相機樞紐")]
    [SerializeField] private Transform cameraPivot; //不是main camera

    [Header("視角限制設定")]
    [Tooltip("仰角：正，向下看")]
    public float highAngle;
    [Tooltip("俯角：負，向上看")]
    public float lowAngle;
    [Tooltip("相機y軸偏移量（眼睛高度）")]
    public float upper;

    [Header("輸入系統")]
    [Tooltip("Look Action 的 InputActionReference")]
    [SerializeField] private InputActionReference lookAction;

    private Vector2 lookInput;
    private float xRotation = 0f;
    private bool isUsingGamepad;
    private UIInputManager uiInputManager;

    private void Awake()
    {
        // 設定 cameraPivot 的初始位置（眼睛高度）
        if (cameraPivot != null)
        {
            cameraPivot.localPosition = new Vector3(0f, upper, 0f);
        }

        // 獲取 UIInputManager 引用
        uiInputManager = UIInputManager.Instance;
    }
    
    // --- 新增點: 在 Start() 中設定初始狀態 ---
    private void Start()
    {
        // 確保遊戲一開始就進入遊戲模式
        //CursorManager.EnterGameplayMode();

        Debug.Log("滑鼠靈敏度：" + SensitivityManager.Instance.mouseSensitivity);
        Debug.Log("手柄靈敏度：" + SensitivityManager.Instance.gamepadSensitivity);
    }


    private void OnEnable()
    {
        lookAction.action.performed += OnLookPerformed;
    }

    private void OnDisable()
    {
        lookAction.action.performed -= OnLookPerformed;
    }

    private void OnLookPerformed(InputAction.CallbackContext context)
    {
        var device = context.control.device;
        isUsingGamepad = device is Gamepad;
    }

    private void Update()
    {
        // 檢查遊戲是否已開始
        if (!UIInputManager.Instance.IsGameStarted) return;

        // 檢查是否處於玩家模式（允許視角移動的模式）
        if (!UIInputManager.Instance.IsInPlayerMode) return;

        // --- 以下是原本的程式碼，現在只有在滑鼠鎖定時才會執行 ---
        lookInput = lookAction.action.ReadValue<Vector2>();

        //滑鼠靈敏度相關設定
        float currentSensitivity = isUsingGamepad ? SensitivityManager.Instance.gamepadSensitivity : SensitivityManager.Instance.mouseSensitivity;

        if (isUsingGamepad) lookInput = ApplyJoystickDeadZone(lookInput);

        float deltaX = lookInput.x * currentSensitivity * Time.deltaTime;
        float deltaY = lookInput.y * currentSensitivity * Time.deltaTime;

        // 上下看（控制 cameraPivot 的 X 角）
        xRotation -= deltaY;
        xRotation = Mathf.Clamp(xRotation, lowAngle, highAngle);
        cameraPivot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        // 左右轉（控制 playerBody 的 Y 角）
        Quaternion newRotation = playerRigidbody.rotation * Quaternion.Euler(0f, deltaX, 0f);
        playerRigidbody.MoveRotation(newRotation);
    }

    /// <summary>
    /// 處理手柄控制
    /// </summary>
    /// <param name="input">搖桿輸入</param>
    /// <returns></returns>
    private Vector2 ApplyJoystickDeadZone(Vector2 input)
    {
        float deadZone = 0.1f;
        float magnitude = input.magnitude;

        if (magnitude < deadZone) return Vector2.zero;

        float normalizedMagnitude = (magnitude - deadZone) / (1 - deadZone);
        return input.normalized * normalizedMagnitude;
    }
}