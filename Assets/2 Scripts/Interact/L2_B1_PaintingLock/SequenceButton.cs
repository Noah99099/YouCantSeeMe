using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))] // 確保物件上有 AudioSource
public class SequenceButton : MonoBehaviour, IInteractable
{
    [SerializeField]
    [Tooltip("對應的數字 (請在場景中分別設定為1, 2, 3)")]
    private int buttonValue;
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
        audioSource = GetComponent<AudioSource>(); // 抓取 AudioSource
    }

    #region ** IInteractable要求內容 **
    public string GetInteractPrompt(bool isGamepad)
    {
        return isGamepad ? "按 [叉] 與 按鈕 交互" : "按 [滑鼠左鍵] 與 按鈕 交互";
    }

    public void Interact(PlayerInteraction player)
    {
        OnPress();
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
            // 呼叫新的 Manager
            SequenceLockManager.Instance.HandleButtonPress(this);

            // 播放按鍵音效
            if (pressSE != null && audioSource != null)
            {
                audioSource.PlayOneShot(pressSE, pressSEVolume);
            }
        }
    }

    private void ResetPress()
    {
        hasBeenPressedRecently = false;
    }

    private IEnumerator PressAnimation()
    {
        isAnimating = true;

        // 按下
        transform.localPosition = originalPosition + new Vector3(-pressDepth, 0, 0);
        yield return new WaitForSeconds(pressDuration);

        // 彈起
        transform.localPosition = originalPosition;
        isAnimating = false;
    }
}