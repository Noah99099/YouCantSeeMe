using UnityEngine;

[CreateAssetMenu(fileName = "NewCarouselData", menuName = "Carousel/CarouselData")]
public class CarouselData : ScriptableObject
{
    public Sprite[] images;   // 輪播圖片
    public string[] texts;    // 對應文字*2 (TextMeshPro)
}
