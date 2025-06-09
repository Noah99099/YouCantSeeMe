using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("交互設置")]
    [SerializeField]
    [Tooltip("範圍")]private float interactionRange = 3f;
    [SerializeField]
    [Tooltip("交互層級")] private LayerMask interactionLayer;

    [Header("Debug")]
    [SerializeField] private bool showDebugRay = true;

    private PlayerControls controls;
    private Camera playerCamera;

    private void Awake()
    {
        controls = new PlayerControls();
        playerCamera = Camera.main;
    }
    private void OnEnable()
    {
        controls.Player.Enable();
        controls.Player.Interaction.performed += HandleInteraction;
    }

    private void OnDisable()
    {
        controls.Player.Disable();
        controls.Player.Interaction.performed -= HandleInteraction;
    }
    private void HandleInteraction(InputAction.CallbackContext context)
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        // 繪製調試射線
        if (showDebugRay)
        {
            Debug.DrawRay(ray.origin, ray.direction * interactionRange, Color.red, 1f);
        }

        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactionLayer))
        {
            Debug.Log($"Hit: {hit.collider.gameObject.name}");

            if (hit.collider.TryGetComponent<PasswordButton>(out var button))
            {
                Debug.Log($"Pressed button: {button.Value}");
                PasswordLockManager.Instance?.HandleButtonPress(button);
            }
        }
        else
        {
            Debug.Log("No interactable object detected");
        }
    }
}
