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

    [Header("輸入設定")]
    [Tooltip("滑鼠靈敏度")]
    [SerializeField] private float mouseSensitivity = 100f;
    [Tooltip("手柄靈敏度")]
    [SerializeField] private float gamepadSensitivity = 30f;

    [Header("視角限制")]
    [Tooltip("仰角：正，向下看")]
    public float highAngle;
    [Tooltip("俯角：負，向上看")]
    public float lowAngle;
    [Tooltip("相機y軸偏移量")]
    public float upper;

    private Vector2 lookInput; //滑鼠或手柄的輸入
    private float xRotation = 0f; //相機x軸旋轉程度（上下看）

    private PlayerControls playerControls; //以InputSystem為基礎創建的 PlayerControls類
    private InputDevice lastUsedDevice; //紀錄最後使用的輸入設備，手柄或滑鼠
    private bool isUsingGamepad; //是否使用手柄

    private void Awake()
    {
        playerControls = new PlayerControls(); //將 PlayerControls類 實例化
        Cursor.lockState = CursorLockMode.Locked; //設定鼠標的鎖定狀態 = 鎖定
        print("將 PlayerControls類 實例化");
        print("設定鼠標的鎖定狀態 = 鎖定");

        //初始化相機的位置
        float posX = playerBody.position.x;
        float posY = playerBody.position.y + upper;
        cameraTransform.position = new Vector3(posX, posY, 0f);
    }

    private void OnEnable()
    {
        playerControls.Player.Enable(); //開啟PlayerControls(Input System)，也開啟裡面的Player(Action Map)
        print("開啟PlayerControls(Input System)，也開啟裡面的Player(Action Map)");
        playerControls.Player.Look.performed += OnLookPerformed; //添加 OnLookPerformed方法 到 Look.performed事件
        //performed: 與動作(Look-Action)的互動已完成
    }
    private void OnDisable()
    {
        playerControls.Player.Disable(); //關閉PlayerControls(Input System)，也開啟裡面的Player(Action Map)
        print("關閉PlayerControls(Input System)，也開啟裡面的Player(Action Map)");
        playerControls.Player.Look.performed -= OnLookPerformed; //從 Look.performed事件中移除 OnLookPerformed方法
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    private void OnLookPerformed(InputAction.CallbackContext context)
    {
        lastUsedDevice = context.control.device;
        isUsingGamepad = lastUsedDevice is Gamepad;
    }
    private void Update()
    {
        lookInput = playerControls.Player.Look.ReadValue<Vector2>(); //讀取滑鼠的位置or輸入

        float currentSensitivity = isUsingGamepad ? gamepadSensitivity : mouseSensitivity;

        if (isUsingGamepad) 
        {
            lookInput = ApplyJoystickDeadZone(lookInput);
        }

        //根據 讀取的位置*靈敏度*幀率調整
        float deltaX = lookInput.x * currentSensitivity * Time.deltaTime;
        float deltaY = lookInput.y * currentSensitivity * Time.deltaTime;

        //調整垂直旋轉（上下看）
        xRotation -= deltaY;
        xRotation = Mathf.Clamp(xRotation, lowAngle, highAngle); //將xRotation限制在最小值(lowAngle)和最大值(highAngle)之間


        //應用在相機（上下看）
        //將歐拉角（x,y,z的旋轉角度）轉換為 四元數（Quaternion），避免"萬向節鎖"發生
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f); //因為父級是playerBody不會上下旋轉，所以相機要本地旋轉
        //應用在玩家（左右看）
        //Vector3.up 表示繞世界坐標的 Y 軸旋轉，mouseX 是旋轉量（角度）
        playerBody.Rotate(Vector3.up * deltaX); // Vector3.up = new Vector3(0, 1, 0)，表示 世界坐標系中 Y 軸正方向的單位向量
    }

    private Vector2 ApplyJoystickDeadZone(Vector2 input)
    {
        // 摇杆死區閥值
        float deadZone = 0.1f;

        // 計算摇杆輸入向量的長度
        float magnitude = input.magnitude; //向量可以用來描述方向和大小

        if (magnitude < deadZone)
        {
            return Vector2.zero;
        }

        // 重新映射輸入值 (死區到1的範圍映射到0到1)
        float normalizedMagnitude = (magnitude - deadZone) / (1 - deadZone);
        return input.normalized * normalizedMagnitude;
        //Vector2.normalized 傳回基於當前向量的歸一化向量。歸一化向量的振幅為 1，且方向與目前向量相同。如果目前向量太小而無法歸一化，則傳回零向量。
    }
}
