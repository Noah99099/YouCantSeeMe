using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Tooltip("���a�ؼҦ�m")]
    [SerializeField] private Transform playerBody;
    [Tooltip("���a��¦���ʳt��")]
    [SerializeField] private float moveSpeed = 5f;
    //[Tooltip("�̲ץ[�t���v")]
    //[SerializeField] private float speedMultiplier;
    //[Tooltip("�[�t�L��ɶ�")]
    //[SerializeField] private float accelerationTime = 0.5f;
    //[Tooltip("�O�_�[�t����")]
    //[SerializeField] private bool isFaster = false; //�q�{���[�t

    private Vector2 moveInput; //�x�sWASD�B��`���ʪ��ƭ�
    private PlayerControls playerControls;
    //private float accelerationProgress; // 0~1���[�t�i�׭�

    private void Awake() => playerControls = new PlayerControls(); //�N PlayerControls�� ��Ҥ�
 
    private void OnEnable()
    {
        playerControls.Player.Enable();

        //���U�]�B�ƥ��ť
        //playerControls.Player.Run.started += OnRunStarted;
        //playerControls.Player.Run.canceled += OnRunCanceled;
    }

    private void OnDisable()
    {
        playerControls.Player.Disable();

        //�������U�]�B�ƥ��ť
        //playerControls.Player.Run.started -= OnRunStarted;
        //playerControls.Player.Run.canceled -= OnRunCanceled;
    }
    private void Update()
    {
        //Ū���x�sWASD�B��`���ʪ���J��
        moveInput = playerControls.Player.Move.ReadValue<Vector2>();

        //�p�Ⲿ�ʤ�V
        Vector3 moveDirection = (playerBody.forward * moveInput.y + playerBody.right * moveInput.x).normalized;

        //if (isFaster)
        //{
        //    //����[�t���
        //    accelerationProgress += Time.deltaTime / accelerationTime;
        //    accelerationProgress = Mathf.Clamp01(accelerationProgress);
        //}
        //else
        //{
        //    //��}�[�t���
        //    accelerationProgress -= Time.deltaTime / accelerationTime;
        //    accelerationProgress = Mathf.Clamp01(accelerationProgress);
        //}

        //�ھڬO�_�[�t�p��̲׳t��
        //float currentSpeed = moveSpeed * (isFaster ? Mathf.Lerp(1f, speedMultiplier, accelerationProgress) : 1f);

        //���Ϊ��a����1�G���w�w�[�t
        //playerBody.Translate(moveDirection * currentSpeed * Time.deltaTime, Space.World);
        //���Ϊ��a����2�G�����[�t
        //playerBody.Translate(moveDirection * currentSpeed * Time.deltaTime, Space.World);
        //�S���[�t�\��
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
