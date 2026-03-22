using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 【新增這行】確保掛載腳本時，Unity 會自動幫你加 AudioSource
[RequireComponent(typeof(AudioSource))]
public class PasswordButton : MonoBehaviour, IInteractable
{
    [SerializeField]
    [Tooltip("對應的數字")] private int buttonValue;
    public int Value => buttonValue;

    [Header("按壓動畫設定")]
    [SerializeField] private float pressDepth = 0.02f;
    [SerializeField] private float pressDuration = 0.1f;

    [Header("音效設定")] 
    [SerializeField] private AudioClip pressSE;
    [SerializeField, Range(0f, 1f)] private float pressSEVolume = 1f;
    private AudioSource audioSource;

    private Vector3 originalPosition;
    private bool isAnimating = false;
    private bool hasBeenPressedRecently = false;
    [SerializeField] private float pressCooldown = 0.3f;

    private void Start()
    {
        originalPosition = transform.localPosition;

        // 【新增這行】把同一物件上的 AudioSource 抓進變數裡
        audioSource = GetComponent<AudioSource>();
    }

    #region ** IInteractable要求內容 **
    // 2. 實作提示文字
    public string GetInteractPrompt(bool isGamepad)
    {
        return isGamepad ? "按 [叉] 與 按鈕 交互" : "按 [滑鼠左鍵] 與 按鈕 交互";
    }

    // 3. 實作互動行為
    public void Interact(PlayerInteraction player)
    {
        Debug.Log($"Pressed button: {Value}");
        OnPress(); // 執行它原本的邏輯
    }
    #endregion

    public void OnPress()
    {
        if (hasBeenPressedRecently) return;
        hasBeenPressedRecently = true;
        Invoke(nameof(ResetPress), pressCooldown);

        if (!isAnimating)
        {
            StartCoroutine(PressAnimation());
            PasswordLockManager.Instance.HandleButtonPress(this);

            // 播放音效
            if (pressSE != null && audioSource != null)
            {
                audioSource.PlayOneShot(pressSE, pressSEVolume);
            }
        }
        Debug.Log($"OnPress triggered on: {this.name}, time: {Time.time}");
    }
    private void ResetPress()
    {
        hasBeenPressedRecently = false;
    }

    private IEnumerator PressAnimation()
    {
        isAnimating = true;

        // 按下
        transform.localPosition = originalPosition + new Vector3(0, 0, -pressDepth);
        yield return new WaitForSeconds(pressDuration);

        // 彈起
        transform.localPosition = originalPosition;
        isAnimating = false;
    }
}
