using UnityEngine;

[CreateAssetMenu(fileName = "NewRoleData", menuName = "Carousel/RoleData")]
public class RoleData : ScriptableObject
{
    public string roleName;       // 角色名稱
    public CarouselData[] carousels;  // 一個方案內可以有多個CarouselData

    // 動態新增 Carousel
    public void AddCarousel(CarouselData newCarousel)
    {
        var list = new System.Collections.Generic.List<CarouselData>(carousels);
        list.Add(newCarousel);
        carousels = list.ToArray();
    }
}
