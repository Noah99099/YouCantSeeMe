using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))] // 確保物件上有 AudioSource
public class SequenceLockManager : MonoBehaviour
{
    public static SequenceLockManager Instance { get; private set; }

    [Header("密碼設定")]
    [SerializeField, Tooltip("正確的6位密碼")]
    private string correctPassword = "311323";
    private const int MaxDigits = 6;

    [Header("解鎖後啟動物件")]
    [SerializeField, Tooltip("密碼正確後要顯示的物品")]
    private GameObject objectToActivate;

    [Header("音效設定")]
    [SerializeField, Tooltip("正確提示音 B")] private AudioClip correctSE;
    [SerializeField, Tooltip("錯誤提示音 A")] private AudioClip failSE;
    [SerializeField, Range(0f, 1f)] private float seVolume = 1f;

    private AudioSource audioSource;
    private string currentInput = "";
    private bool isChecking = false;

    [Header("Debug")]
    [SerializeField] private bool logButtonPresses = true;

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

        audioSource = GetComponent<AudioSource>(); // 抓取 AudioSource
        ResetLock();
    }

    public void HandleButtonPress(SequenceButton button)
    {
        // 防呆：如果輸入已經達到上限，或是正在判定中，就忽略輸入
        if (currentInput.Length >= MaxDigits || isChecking) return;

        currentInput += button.Value.ToString();

        if (logButtonPresses)
        {
            Debug.Log($"按下了: {button.Value}, 目前累積輸入: {currentInput}");
        }

        // 當輸入長度達到 6 位數時，進行檢查
        if (currentInput.Length == MaxDigits)
        {
            isChecking = true;
            CheckPassword();
        }
    }

    private void CheckPassword()
    {
        if (currentInput == correctPassword)
        {
            Debug.Log("密碼完全正確！");

            // 播放正確音效 B
            if (correctSE != null && audioSource != null)
            {
                audioSource.PlayOneShot(correctSE, seVolume);
            }

            // 啟動場景上的物品
            if (objectToActivate != null)
            {
                objectToActivate.SetActive(true);
            }

            // 成功後不重置 isChecking，讓按鈕維持鎖定狀態
        }
        else
        {
            Debug.Log("密碼錯誤，稍後重置鎖...");

            // 播放錯誤音效 A
            if (failSE != null && audioSource != null)
            {
                audioSource.PlayOneShot(failSE, seVolume);
            }

            // 啟動延遲重置
            StartCoroutine(ResetAfterDelay(0.5f));
        }
    }

    private IEnumerator ResetAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ResetLock();
    }

    private void ResetLock()
    {
        currentInput = "";
        isChecking = false;
        if (logButtonPresses) Debug.Log("密碼鎖已重置，可以重新輸入。");
    }
}