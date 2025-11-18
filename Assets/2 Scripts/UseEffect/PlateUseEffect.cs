using UnityEngine;

public class PlateUseEffect : MonoBehaviour
{
    // 掛載在PlateEventManager物件上
    [System.Serializable]
    public class PlateData
    {
        public GameObject plateObject;
        public MonoBehaviour plateScript;
    }

    [Header("盤子配置")]
    public PlateData[] plates = new PlateData[7];
    public GameObject magicCircle;

    private int usedPlateCount = 0;
    private int totalPlates => plates.Length; // 4

    //由每個盤子交互點上的InteractableObject執行方法
    public void UsePlate(int plateIndex)
    {
        if (plateIndex < 0 || plateIndex >= plates.Length)
        {
            Debug.LogError($"無效的盤子索引: {plateIndex}");
            return;
        }

        var plate = plates[plateIndex];
        if (plate == null) return;

        // 防止重複激活
        if (plate.plateObject != null && !plate.plateObject.activeInHierarchy)
        {
            plate.plateObject.SetActive(true); //顯示盤子
            if (plate.plateScript != null)
                plate.plateScript.enabled = true; //啟用ViewInteractableObject腳本

            usedPlateCount++; //總共使用盤子數+=1
            CheckCompletion(); //檢查有沒有徹底完成盤子3、5、7、8
        }
    }

    private void CheckCompletion()
    {
        if (usedPlateCount >= totalPlates)
        {
            Debug.Log("所有盤子收集完成！打開魔法陣");
            magicCircle.SetActive(true);
            Destroy(gameObject);
        }
    }

    // 保持原有方法名稱的兼容性
    //public void UsePlate_2() => UsePlate(0);
    public void UsePlate_3() => UsePlate(0);
    //public void UsePlate_4() => UsePlate(1);
    public void UsePlate_5() => UsePlate(1);
    //public void UsePlate_6() => UsePlate(2);
    public void UsePlate_7() => UsePlate(2);
    public void UsePlate_8() => UsePlate(3);
}
