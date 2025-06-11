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
    [Tooltip("相機位置")]
    [SerializeField] private Transform cameraTransform;

    [Tooltip("手柄靈敏度")]
    [SerializeField] private float gamepadSensitivity = 30f;

    [Header("視角限制")]
    [Tooltip("仰角：正，向下看")]
    public float highAngle;
    [Tooltip("俯角：負，向上看")]
    public float lowAngle;
    [Tooltip("相機y軸偏移量")]
    public float upper;

    private Vector2 lookInput;
    private float xRotation = 0f;

    private PlayerControls playerControls;
    private InputDevice lastUsedDevice;
    private bool isUsingGamepad;

    private void Awake()
    {
        playerControls = new PlayerControls();

        // --- 修改點 1: 移除這裡的滑鼠鎖定 ---
        // Cursor.lockState = CursorLockMode.Locked; 
        // 理由：我們把滑鼠狀態的管理權完全交給 CursorManager，避免多頭馬車。
        
        //初始化相機的位置
        float posX = playerBody.position.x;
        float posY = playerBody.position.y + upper;
        cameraTransform.position = new Vector3(posX, posY, 0f);
    }
    
    // --- 新增點: 在 Start() 中設定初始狀態 ---
    private void Start()
    {
        // 確保遊戲一開始就進入遊戲模式
        CursorManager.EnterGameplayMode();
    }


    private void OnEnable()
    {
        playerControls.Player.Enable();
        playerControls.Player.Look.performed += OnLookPerformed;
    }

    private void OnDisable()
    {
        playerControls.Player.Disable();
        playerControls.Player.Look.performed -= OnLookPerformed;
    }

    private void OnLookPerformed(InputAction.CallbackContext context)
    {
        lastUsedDevice = context.control.device;
        isUsingGamepad = lastUsedDevice is Gamepad;
    }

    private void Update()
    {
        // --- 修改點 2: 在 Update 開頭加入守衛條件 ---
        // 如果當前滑鼠不是鎖定狀態（代表在UI模式），就直接跳出 Update，不執行後面的程式碼。
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            return; // 提早結束此函式
        }

        // --- 以下是原本的程式碼，現在只有在滑鼠鎖定時才會執行 ---
        lookInput = playerControls.Player.Look.ReadValue<Vector2>();

        //滑鼠靈敏度相關設定
        float currentSensitivity = isUsingGamepad ? gamepadSensitivity : SensitivityManager.Instance.mouseSensitivity;

        if (isUsingGamepad)
        {
            lookInput = ApplyJoystickDeadZone(lookInput);
        }

        float deltaX = lookInput.x * currentSensitivity * Time.deltaTime;
        float deltaY = lookInput.y * currentSensitivity * Time.deltaTime;

        xRotation -= deltaY;
        xRotation = Mathf.Clamp(xRotation, lowAngle, highAngle);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * deltaX);
    }

    private Vector2 ApplyJoystickDeadZone(Vector2 input)
    {
        float deadZone = 0.1f;
        float magnitude = input.magnitude;

        if (magnitude < deadZone)
        {
            return Vector2.zero;
        }

        float normalizedMagnitude = (magnitude - deadZone) / (1 - deadZone);
        return input.normalized * normalizedMagnitude;
    }
}