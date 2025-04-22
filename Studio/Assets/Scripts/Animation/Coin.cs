using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coin : MonoBehaviour
{
    [Header("Physics Settings")]
    public float initialForce = 5f; // 初始弹跳力
    public Vector3 randomForceRange = new Vector3(2f, 5f, 2f); // 随机力的范围
    public float randomPositionRange = 0.2f; // 随机位置偏移范围
    
    [Header("Appearance")]
    public Color[] possibleColors = new Color[] { 
        new Color(1f, 0.8f, 0f), // 金色
        new Color(0.8f, 0.8f, 0.8f) // 银色
    };
    
    /// <summary>
    /// 在指定位置生成指定数量的硬币
    /// </summary>
    /// <param name="prefab">硬币预制体</param>
    /// <param name="spawnPoint">生成位置</param>
    /// <param name="yOffset">Y轴偏移</param>
    /// <param name="amount">数量</param>
    public static void SpawnCoins(GameObject prefab, Transform spawnPoint, float yOffset, int amount)
    {
        if (prefab == null || spawnPoint == null) return;
        
        for (int i = 0; i < amount; i++)
        {
            // 计算偏移位置
            Vector3 spawnPosition = spawnPoint.position + new Vector3(0, yOffset, 0);
            
            // 实例化硬币
            GameObject coinObject = Instantiate(prefab, spawnPosition, Random.rotation);
            Coin coin = coinObject.GetComponent<Coin>();
            
            if (coin != null)
            {
                coin.Initialize();
            }
        }
    }
    
    /// <summary>
    /// 初始化硬币的物理属性和外观
    /// </summary>
    public void Initialize()
    {
        // 添加随机位置偏移
        transform.position += new Vector3(
            Random.Range(-randomPositionRange, randomPositionRange),
            Random.Range(-randomPositionRange/2, randomPositionRange/2),
            Random.Range(-randomPositionRange, randomPositionRange)
        );
        
        // 随机设置外观
        SetRandomAppearance();
        
        // 添加随机初始力和旋转
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // 重置物理属性
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            
            // 随机向上弹跳力
            Vector3 force = new Vector3(
                Random.Range(-randomForceRange.x, randomForceRange.x),
                Random.Range(initialForce, initialForce + randomForceRange.y),
                Random.Range(-randomForceRange.z, randomForceRange.z)
            );
            
            rb.AddForce(force, ForceMode.Impulse);
            
            // 添加随机旋转
            rb.AddTorque(new Vector3(
                Random.Range(-10f, 10f),
                Random.Range(-10f, 10f),
                Random.Range(-10f, 10f)
            ), ForceMode.Impulse);
        }
    }
    
    /// <summary>
    /// 随机设置硬币的外观
    /// </summary>
    private void SetRandomAppearance()
    {
        if (possibleColors.Length == 0) return;
        
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
        {
            // 随机选择一种颜色
            Color selectedColor = possibleColors[Random.Range(0, possibleColors.Length)];
            renderer.material.color = selectedColor;
            
            // 可选：也可以随机调整硬币的大小
            float randomScale = Random.Range(0.9f, 1.1f);
            transform.localScale = Vector3.one * randomScale;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
