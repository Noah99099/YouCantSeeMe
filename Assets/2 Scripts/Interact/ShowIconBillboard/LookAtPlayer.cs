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

        // 計算要面向的方向
        Vector3 direction = (target.position - transform.position).normalized;

        // 計算新的旋轉（世界空間）
        Quaternion lookRotation = Quaternion.LookRotation(direction, Vector3.up);

        // 套用到世界旋轉，不受父級干擾
        transform.rotation = lookRotation;
    }
}
