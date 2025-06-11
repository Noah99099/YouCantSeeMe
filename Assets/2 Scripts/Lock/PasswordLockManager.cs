using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PasswordLockManager : MonoBehaviour
{
    public static PasswordLockManager Instance { get; private set; }

    [Header("UI Settings")]
    [SerializeField] private TMP_Text[] digitDisplays;
    [SerializeField] private string correctPassword = "1234";

    [Header("Debug")]
    [SerializeField] private bool logButtonPresses = true;

    private string currentInput = "";
    private const int MaxDigits = 4;

    private void Awake()
    {
        // ��ҼҦ��]�m
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }

        ResetLock();
    }

    public void HandleButtonPress(PasswordButton button)
    {
        if (currentInput.Length >= MaxDigits) return;

        currentInput += button.Value.ToString();
        if (logButtonPresses)
        {
            Debug.Log($"Button pressed: {button.Value}, Current input: {currentInput}");
        }

        UpdateDisplay();

        if (currentInput.Length == MaxDigits)
        {
            CheckPassword();
        }
    }

    private void UpdateDisplay()
    {
        for (int i = 0; i < MaxDigits; i++)
        {
            if (i < currentInput.Length)
            {
                digitDisplays[i].text = currentInput[i].ToString();
            }
            else
            {
                digitDisplays[i].text = "";
            }
        }
    }

    private void CheckPassword()
    {
        Debug.Log($"Checking password: {currentInput} vs {correctPassword}");
        if (currentInput == correctPassword)
        {
            Debug.Log("�K�X���T�I���\�F�I");
            // �o�̲K�[���ꦨ�\�᪺�޿�
        }
        else
        {
            Debug.Log("�K�X���~�A���m��...");
            ResetLock();
        }
    }

    private void ResetLock()
    {
        Debug.Log("���m�K�X��...");
        currentInput = "";
        UpdateDisplay();
    }
}
