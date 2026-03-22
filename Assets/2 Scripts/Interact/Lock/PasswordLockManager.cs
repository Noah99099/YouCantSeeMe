using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(AudioSource))]
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

    [Header("完成後的對話")]
    public GameObject finishLock_DiaPos;

    [Header("Debug")]
    [SerializeField] private bool logButtonPresses = true;

    private string currentInput = "";
    private const int MaxDigits = 4;

    // 【新增】一個 bool 變數來防止在重置延遲期間重複輸入
    private bool isChecking = false;

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

        // 【新增這行】把同一物件上的 AudioSource 抓進變數裡
        audioSource = GetComponent<AudioSource>();

        ResetLock();
    }

    public void HandleButtonPress(PasswordButton button)
    {
        // 【修改】增加 isChecking 判斷
        if (currentInput.Length >= MaxDigits || isChecking) return;
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
            // 【修改】設定 isChecking 為 true
            isChecking = true;
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

            finishLock_DiaPos.SetActive(true);

            // 注意：密碼正確後，我們沒有重置 isChecking，
            // 這會使密碼鎖 "鎖定" 在正確狀態，是合理的。
            // 如果你希望解鎖後還能重置，可以在此處呼叫 ResetLock() 或 ResetLock(false)。
        }
        else
        {
            Debug.Log("密碼錯誤，重置鎖...");

            // 播放錯誤音效
            if (failSE != null && audioSource != null)
            {
                audioSource.PlayOneShot(failSE, pressSEVolume);
            }

            // 【修改】不再直接呼叫 ResetLock()，
            // 而是啟動帶有延遲的協程
            StartCoroutine(ResetAfterDelay(0.5f)); //可調延遲時間
        }
    }

    // 【新增】用於延遲重置的協程
    private IEnumerator ResetAfterDelay(float delay)
    {
        // 等待指定的秒數
        yield return new WaitForSeconds(delay);

        // 等待結束後，執行重置
        ResetLock();
    }

    private void ResetLock()
    {
        Debug.Log("ResetLock() 執行");
        currentInput = "";
        UpdateDisplay();

        // 【修改】重置 isChecking 狀態，允許再次輸入
        isChecking = false;
    }
}
