using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // 引入 TextMeshPro 命名空間

[RequireComponent(typeof(AudioSource))] // 確保物件上有 AudioSource
public class SequenceLockManager : MonoBehaviour
{
    public static SequenceLockManager Instance { get; private set; }

    [Header("密碼設定")]
    [SerializeField, Tooltip("正確的6位密碼")]
    private string correctPassword = "311323";
    private const int MaxDigits = 6;

    [Header("可視化密碼顯示")]
    [SerializeField, Tooltip("顯示剩餘輸入次數的 3D Text (TMP)")]
    private TMP_Text remainingDigitsText;

    [Header("解鎖後啟動物件")]
    [SerializeField, Tooltip("密碼正確後要顯示的物品")]
    private GameObject objectToActivate;

    [Header("音效設定")]
    [SerializeField, Tooltip("正確提示音 B")] private AudioClip correctSE;
    [SerializeField, Tooltip("錯誤提示音 A")] private AudioClip failSE;
    [SerializeField, Range(0f, 1f)] private float seVolume = 1f;

    [Header("時間設定")]
    [SerializeField, Tooltip("輸入第6碼後，等待幾秒才播放判定音效")]
    private float validationDelay = 0.4f; // 預設0.4秒，你可以依照按鍵音效長度在 Inspector 調整

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

        // 每次按下按鈕後更新剩餘次數文字
        UpdateRemainingText();

        if (logButtonPresses)
        {
            Debug.Log($"按下了: {button.Value}, 目前累積輸入: {currentInput}");
        }

        // 當輸入長度達到 6 位數時，進行檢查
        if (currentInput.Length == MaxDigits)
        {
            isChecking = true; // 鎖定輸入，防止玩家在延遲期間狂按
            StartCoroutine(WaitAndCheckPassword());
        }
    }

    // 新增：延遲判定的協程
    private IEnumerator WaitAndCheckPassword()
    {
        if (logButtonPresses) Debug.Log($"等待 {validationDelay} 秒後進行判定...");

        // 等待設定的秒數 (讓第6個按鍵音效先播完)
        yield return new WaitForSeconds(validationDelay);

        // 執行判定
        CheckPassword();
    }

    private void CheckPassword()
    {
        if (currentInput == correctPassword)
        {
            Debug.Log("密碼完全正確！");

            // 密碼正確，將文字設置為 "-"
            if (remainingDigitsText != null)
            {
                remainingDigitsText.text = "-";
            }

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

        // 鎖重置時，也將文字重置回最大位數 (例如: 6)
        UpdateRemainingText();

        if (logButtonPresses) Debug.Log("密碼鎖已重置，可以重新輸入。");
    }

    /// <summary>
    /// 計算並更新剩餘位數的文字顯示
    /// </summary>
    private void UpdateRemainingText()
    {
        if (remainingDigitsText != null)
        {
            int remaining = MaxDigits - currentInput.Length;
            remainingDigitsText.text = remaining.ToString();
        }
    }
}