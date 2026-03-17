using UnityEngine;
using UnityEditor;

public class Create3DNoise : EditorWindow
{
    [MenuItem("Tools/Generate 3D Noise")]
    public static void Generate()
    {
        int size = 32; // 3D 貼圖解析度，32 或 64 即可，太高會佔內存
        Texture3D texture = new Texture3D(size, size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Trilinear;
        texture.wrapMode = TextureWrapMode.Repeat;

        Color[] colors = new Color[size * size * size];
        float frequency = 5.0f; // 頻率越高，雜訊越碎

        for (int z = 0; z < size; z++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float xCoord = (float)x / size * frequency;
                    float yCoord = (float)y / size * frequency;
                    float zCoord = (float)z / size * frequency;

                    // 生成簡單的 Perlin Noise 值
                    float noise = Mathf.PerlinNoise(xCoord, yCoord); 
                    // 為了 3D 效果，我們混合 Z 軸
                    float noiseZ = Mathf.PerlinNoise(yCoord, zCoord);
                    float finalNoise = (noise + noiseZ) * 0.5f;

                    colors[x + y * size + z * size * size] = new Color(finalNoise, finalNoise, finalNoise, 1);
                }
            }
        }

        texture.SetPixels(colors);
        texture.Apply();

        AssetDatabase.CreateAsset(texture, "Assets/FogNoise3D.asset");
        AssetDatabase.SaveAssets();
        Debug.Log("3D Noise 貼圖已生成至 Assets/FogNoise3D.asset");
    }
}