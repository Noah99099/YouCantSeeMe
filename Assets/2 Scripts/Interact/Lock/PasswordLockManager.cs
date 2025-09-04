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

    [Header("Door")]
    [SerializeField] private DoorController doorToOpen;

    [Header("音效設定")]
    [SerializeField] private AudioClip correctSE;
    [SerializeField] private AudioClip failSE;
    [SerializeField, Range(0f, 1f)] private float pressSEVolume = 1f;
    private AudioSource audioSource;

    [Header("Debug")]
    [SerializeField] private bool logButtonPresses = true;

    private string currentInput = "";
    private const int MaxDigits = 4;

    private void Awake()
    {
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
        Debug.Log($"== Before Append: currentInput = {currentInput}");
        currentInput += button.Value.ToString();
        Debug.Log($"== After Append: currentInput = {currentInput}");
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
            Debug.Log("密碼正確！");

            // 播放正確音效
            if (correctSE != null && audioSource != null)
            {
                audioSource.PlayOneShot(correctSE, pressSEVolume);
            }

            if (doorToOpen != null)
            {
                doorToOpen.OpenDoor();
            }
        }
        else
        {
            Debug.Log("密碼錯誤，重置鎖...");

            // 播放錯誤音效
            if (failSE != null && audioSource != null)
            {
                audioSource.PlayOneShot(failSE, pressSEVolume);
            }

            ResetLock();
        }
    }

    private void ResetLock()
    {
        Debug.Log("ResetLock() 執行");
        currentInput = "";
        UpdateDisplay();
    }
}
