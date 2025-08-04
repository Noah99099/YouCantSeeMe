using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PasswordButton : MonoBehaviour
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
    }

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
