using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GhostWall : MonoBehaviour
{
    [Header("鬼打牆的鏡像設置，反轉二選一，不能都勾")]
    public Transform playerTransform;
    [Tooltip("接觸的面同x值")]public bool mirrorX = true;
    [Tooltip("接觸的面同z值")] public bool mirrorZ = false; 

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) 
        {
            Vector3 wallCenter = transform.position; //空氣牆的中心點

            if (mirrorX) 
            {
                Vector3 mirrorPosition = 2 * wallCenter - playerTransform.position;
                mirrorPosition.x = playerTransform.position.x;
                mirrorPosition.y = playerTransform.position.y;

                playerTransform.position = mirrorPosition; //賦值，重新定位玩家位置
            }
            if (mirrorZ) 
            {
                Vector3 mirrorPosition = 2 * wallCenter - playerTransform.position;
                mirrorPosition.z = playerTransform.position.z;
                mirrorPosition.y = playerTransform.position.y;

                playerTransform.position = mirrorPosition; //賦值，重新定位玩家位置
            }

            //旋轉方向反射（旋轉 180 度）
            Vector3 euler = playerTransform.eulerAngles;
            euler.y = (euler.y + 180f) % 360f;
            playerTransform.eulerAngles = euler;
        }
    }
}
