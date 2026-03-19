// LookAtPlayer.cs
using UnityEngine;

/// <summary>
/// 為了避免穿幫 icon 而存在
/// 目前是只想掛在角色身上
/// </summary>
public class LookAtPlayer : MonoBehaviour
{
    private Transform target;  // 通常是 Player 或 Camera

    private Vector3 originalWorldPosition;

    void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player").transform;

        // 記錄初始世界位置
        originalWorldPosition = transform.position;
    }

    void LateUpdate()
    {
        // 固定世界位置（無論是否是子物件）
        transform.position = originalWorldPosition;

        // 計算要面向的方向(原本)
        //Vector3 direction = (target.position - transform.position).normalized;

        // 計算要面向的方向 (不加 .normalized，因為我們後面要修改它)
        Vector3 direction = target.position - transform.position;

        // [!! 核心修改 !!] 強制把高度差歸零，這樣就不會前傾後仰了
        direction.y = 0;

        // 防止除以零的錯誤（當玩家跟 Icon 剛好在完全同一個 XZ 座標時）
        if (direction != Vector3.zero)
        {
            // 計算新的旋轉（世界空間）
            Quaternion lookRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

            // 套用到世界旋轉，不受父級干擾
            transform.rotation = lookRotation;
        }
    }
}
