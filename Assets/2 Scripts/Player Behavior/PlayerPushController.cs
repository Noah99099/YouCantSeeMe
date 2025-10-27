using UnityEngine;

public class PlayerPushController : MonoBehaviour
{
    private Rigidbody rb;

    private void Awake()
    {
        // 找場景中唯一 Tag = "Player" 的物件
        GameObject playerObj = GameObject.FindWithTag("Player");
        rb = playerObj.GetComponent<Rigidbody>();

        if (playerObj != null)
        {
            Debug.LogWarning("找到 Player！");
        }
        else
        {
            Debug.LogWarning("場景中沒有 Tag 為 Player 的物件！");
        }
    }

    /// <summary>
    /// 讓玩家往自身面向的反方向退指定距離。
    /// </summary>
    /// <param name="distance">退後距離</param>
    /// <param name="speed">退後速度 (0 代表瞬間移動)</param>
    public void PushBack(float distance, float speed = 0f)
    {
        if (rb == null) return;

        // 玩家面向的反方向
        Vector3 pushDirection = -transform.forward;
        Vector3 targetPosition = transform.position + pushDirection * distance;

        if (speed <= 0f)
        {
            // 瞬間退後
            rb.MovePosition(targetPosition);
        }
        else
        {
            StopAllCoroutines();
            StartCoroutine(PushBackRoutine(targetPosition, speed));
        }
    }

    private System.Collections.IEnumerator PushBackRoutine(Vector3 targetPos, float speed)
    {
        Vector3 startPos = rb.position;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * speed;
            rb.MovePosition(Vector3.Lerp(startPos, targetPos, t));
            yield return null;
        }
    }

    // 給 UnityEvent 用的包裝方法（可在 Inspector 中直接綁定）
    public void PushBackDefault()
    {
        PushBack(2f, 10f);
    }
}
