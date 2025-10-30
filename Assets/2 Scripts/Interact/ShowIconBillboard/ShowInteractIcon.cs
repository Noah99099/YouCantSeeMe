// ShowInteractIcon.cs
using UnityEngine;

/// <summary>
/// 與 Billboard.cs 搭配。
/// 放在父-子結構中的父。
/// </summary>
public class ShowInteractIcon : MonoBehaviour
{
    [Header("公告板交互圖標設置")]
    public GameObject icon;  // 指向 IconPivot
    public float showDistance = 3f; // 與玩家的位移距離

    private Transform player;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        icon.SetActive(false);
    }

    void Update()
    {
        float distance = Vector3.Distance(player.position, transform.position);

        if(icon != null) // 刪除icon後不執行該方法
            icon.SetActive(distance <= showDistance);
    }
}
