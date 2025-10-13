// PlayerController.cs
using UnityEngine;

// 這個腳本取代了 FirstPersonController.cs
// 它需要 CharacterController 元件
[RequireComponent(typeof(CharacterController))]
public class SimpleFirstPersonController : MonoBehaviour
{
    // --- 這部分是從 FirstPersonController 複製過來的公開變數 ---
    [Header("Player")]
    [Tooltip("Move speed of the character in m/s")]
    public float MoveSpeed = 4.0f;
    [Tooltip("Sprint speed of the character in m/s")]
    public float SprintSpeed = 6.0f;
    [Tooltip("Rotation speed of the character")]
    public float RotationSpeed = 1.0f;
    [Tooltip("Acceleration and deceleration")]
    public float SpeedChangeRate = 10.0f;

    // ----- Gravity variables (已移除 JumpHeight 和 JumpTimeout) -----
    [Space(10)]
    [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
    public float Gravity = -15.0f;
    [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
    public float FallTimeout = 0.15f;

    [Header("Player Grounded")]
    [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
    public bool Grounded = true;
    [Tooltip("Useful for rough ground")]
    public float GroundedOffset = -0.14f;
    [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
    public float GroundedRadius = 0.28f;
    [Tooltip("What layers the character uses as ground")]
    public LayerMask GroundLayers;

    [Header("Cinemachine")]
    [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
    public GameObject CinemachineCameraTarget;
    [Tooltip("How far in degrees can you move the camera up")]
    public float TopClamp = 90.0f;
    [Tooltip("How far in degrees can you move the camera down")]
    public float BottomClamp = -90.0f;

    // --- 私有變數 ---
    private float _speed;
    private float _rotationVelocity;
    private float _cinemachineTargetPitch;
    private float _verticalVelocity;
    private float _terminalVelocity = 53.0f;

    private float _fallTimeoutDelta;
    private const float _threshold = 0.01f;

    // --- 腳本引用 ---
    private CharacterController _controller;
    public Level1UIController _inputHandler; // 改為引用我們新的輸入處理腳本，改成公共不然容易出問題

    private void Start()
    {
        _controller = GetComponent<CharacterController>();
        _fallTimeoutDelta = FallTimeout;
    }

    private void Update()
    {
        GroundedCheck();
        ApplyGravity();
        Move();
    }

    private void LateUpdate()
    {
        CameraRotation();
    }

    private void GroundedCheck()
    {
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
    }

    // 將 JumpAndGravity() 改名並簡化為 ApplyGravity()
    private void ApplyGravity()
    {
        if (Grounded)
        {
            _fallTimeoutDelta = FallTimeout;

            if (_verticalVelocity < 0.0f)
            {
                _verticalVelocity = -2f;
            }

            // 已移除所有 Jump 相關的判斷式
        }
        else
        {
            if (_fallTimeoutDelta >= 0.0f)
            {
                _fallTimeoutDelta -= Time.deltaTime;
            }
        }

        if (_verticalVelocity < _terminalVelocity)
        {
            _verticalVelocity += Gravity * Time.deltaTime;
        }
    }

    private void CameraRotation()
    {
        // 讀取 LookInput，而不是 _input.look
        if (_inputHandler.LookInput.sqrMagnitude >= _threshold)
        {
            // 判斷是否為滑鼠輸入，來決定是否乘以 Time.deltaTime
            float deltaTimeMultiplier = _inputHandler.IsMouseDevice ? 1.0f : Time.deltaTime;

            // 使用 _inputHandler.LookInput 來計算
            _cinemachineTargetPitch += _inputHandler.LookInput.y * RotationSpeed * deltaTimeMultiplier;
            _rotationVelocity = _inputHandler.LookInput.x * RotationSpeed * deltaTimeMultiplier;

            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);
            transform.Rotate(Vector3.up * _rotationVelocity);
        }
    }

    private void Move()
    {
        // Sprint 的部分如果需要，可以比照 Move/Look 的方式在 InputHandler 中加入
        float targetSpeed = MoveSpeed; // 簡化為只有 MoveSpeed

        // 如果沒有輸入，目標速度為 0
        // 讀取 MoveInput，而不是 _input.move
        if (_inputHandler.MoveInput == Vector2.zero) targetSpeed = 0.0f;

        float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;
        float speedOffset = 0.1f;

        if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * _inputHandler.MoveInput.magnitude, Time.deltaTime * SpeedChangeRate);
            _speed = Mathf.Round(_speed * 1000f) / 1000f;
        }
        else
        {
            _speed = targetSpeed;
        }

        Vector3 inputDirection = new Vector3(_inputHandler.MoveInput.x, 0.0f, _inputHandler.MoveInput.y).normalized;

        if (_inputHandler.MoveInput != Vector2.zero)
        {
            inputDirection = transform.right * _inputHandler.MoveInput.x + transform.forward * _inputHandler.MoveInput.y;
        }

        // 這是最關鍵的修改：將水平移動和垂直速度（重力）結合起來
        _controller.Move(inputDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }
}