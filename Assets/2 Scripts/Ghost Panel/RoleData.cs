using UnityEngine;

[CreateAssetMenu(fileName = "NewRoleData", menuName = "Carousel/RoleData")]
public class RoleData : ScriptableObject
{
    public string roleName;       // 角色名稱
    public CarouselData[] carousels;  // 一個方案內可以有多個CarouselData
}
