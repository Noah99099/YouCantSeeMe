// BidirectionalDoor.cs
using UnityEngine;
using System.Collections; // 需要用到 IEnumerator

public class BidirectionalDoor : MonoBehaviour
{
    public float openAngle = 90f;

    [Header("Speed")]
    public float openSpeed = 180f;   // 開門快
    public float closeSpeed = 60f;   // 關門慢

    [Header("Close Delay")]
    public float closeDelay = 1.5f;  // 玩家離開後多久才關門

    [Header("Triggers")]
    // 儲存門兩側的 DoorTrigger 腳本
    public DoorTrigger[] doorTriggers;

    private Quaternion closedRotation;
    private Quaternion targetRotation;
    private float currentSpeed;
    private bool isMoving = false;
    private Coroutine closeCoroutine; // 用來儲存關門協程

    // 新增：追蹤門的狀態
    public enum DoorState { Closed, Open, Opening, Closing }
    public DoorState currentState = DoorState.Closed;

    void Start()
    {
        closedRotation = transform.rotation;
        currentState = DoorState.Closed;
    }

    void FixedUpdate()
    {
        if (!isMoving) return;

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            currentSpeed * Time.fixedDeltaTime
        );

        if (Quaternion.Angle(transform.rotation, targetRotation) < 0.1f)
        {
            isMoving = false;
            // 判斷門是否到達目標位置
            if (targetRotation == closedRotation)
            {
                currentState = DoorState.Closed;
                // 新增：門完全關閉後，重新啟用所有觸發器
                SetTriggersEnabled(true);
            }
            else
            {
                currentState = DoorState.Open;
            }
        }
    }

    public void OpenToSide(int direction, DoorTrigger callerTrigger)
    {
        // 停止之前的關門協程
        if (closeCoroutine != null)
        {
            StopCoroutine(closeCoroutine);
        }
        CancelInvoke(nameof(CloseDelayed)); // 避免使用 Invoke

        // 如果門正在關閉，則禁用非呼叫者的觸發器，並立即開門
        if (currentState == DoorState.Closing || currentState == DoorState.Closed)
        {
            // 立即禁用非呼叫者的觸發器
            DisableOppositeTrigger(callerTrigger);

            targetRotation = closedRotation * Quaternion.Euler(0, openAngle * direction, 0);
            currentSpeed = openSpeed;
            isMoving = true;
            currentState = DoorState.Opening;
        }
    }

    public void Close(DoorTrigger callerTrigger)
    {
        // 停止之前的關門協程
        if (closeCoroutine != null)
        {
            StopCoroutine(closeCoroutine);
        }

        // 使用 Invoke 或協程延遲關門
        // 這裡改用 Invoke 保持與原程式碼的相似性，但建議使用協程 (如 CloseDelayedCorouine)
        CancelInvoke(nameof(CloseDelayed));
        Invoke(nameof(CloseDelayed), closeDelay);

        // 在玩家離開時，重新啟用觸發器（如果有被禁用），因為這次離開才是真正的“離開”
        // 禁用是在 OpenToSide 裡處理的。如果玩家只是離開觸發器但門還開著，就不用管觸發器狀態。
    }

    // 延遲關門的呼叫函數
    private void CloseDelayed()
    {
        closeCoroutine = StartCoroutine(CloseInternalCorouine());
    }

    // 將關門過程改為協程，以便在關門開始時禁用所有觸發器
    private IEnumerator CloseInternalCorouine()
    {
        currentState = DoorState.Closing;
        // 新增：在開始關門時，禁用所有觸發器以防止門正在關閉時被重新打開
        SetTriggersEnabled(false);

        targetRotation = closedRotation;
        currentSpeed = closeSpeed;
        isMoving = true;

        // 等待門完全關閉 (FixedUpdate 會處理 isMoving = false)
        while (isMoving)
        {
            yield return new WaitForFixedUpdate();
        }

        // 門完全關閉後，SetTriggersEnabled(true) 會在 FixedUpdate 中被呼叫
        // 確保門鎖定在關閉狀態，並重新啟用觸發器。
    }

    /// <summary>
    /// 啟用或禁用門兩側的所有 DoorTrigger 腳本。
    /// </summary>
    private void SetTriggersEnabled(bool enabled)
    {
        foreach (DoorTrigger trigger in doorTriggers)
        {
            if (trigger != null)
            {
                trigger.enabled = enabled;
            }
        }
    }

    /// <summary>
    /// 禁用非呼叫者 (Caller) 的 DoorTrigger 腳本。
    /// </summary>
    private void DisableOppositeTrigger(DoorTrigger callerTrigger)
    {
        foreach (DoorTrigger trigger in doorTriggers)
        {
            if (trigger != null && trigger != callerTrigger)
            {
                trigger.enabled = false;
                // 為了安全，也可以在 FixedUpdate 門完全關閉時再重新啟用所有觸發器。
                // 在 OpenToSide 裡禁用，在 FixedUpdate 裡完全關閉時啟用。
            }
        }
    }
}
