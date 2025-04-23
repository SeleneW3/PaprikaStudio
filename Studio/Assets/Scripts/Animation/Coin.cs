using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coin : MonoBehaviour
{
    [Header("Physics Settings")]
    public float initialForce = 5f; // 初始弹跳力
    public Vector3 randomForceRange = new Vector3(1f, 2f, 1f); // 随机力的范围
    public float randomPositionRange = 0.1f; // 随机位置偏移范围
    
    [Header("Appearance")]
    public Color[] possibleColors = new Color[] { 
        new Color(1f, 0.8f, 0f), // 金色
        new Color(0.8f, 0.8f, 0.8f) // 银色
    };
    
    /// <summary>
    /// 在指定位置生成指定数量的硬币
    /// </summary>
    /// <param name="prefab">硬币预制体</param>
    /// <param name="anchor">用于定位的锚点</param>
    /// <param name="amount">生成数量</param>
    public static void SpawnCoins(GameObject prefab, Transform anchor, int amount)
    {
        SpawnCoins(prefab, anchor, anchor.position, amount);
    }
    
    /// <summary>
    /// 在指定位置生成指定数量的硬币
    /// </summary>
    /// <param name="prefab">硬币预制体</param>
    /// <param name="anchor">用于定位的锚点</param>
    /// <param name="spawnPosition">具体生成位置</param>
    /// <param name="amount">生成数量</param>
    public static void SpawnCoins(GameObject prefab, Transform anchor, Vector3 spawnPosition, int amount)
    {
        Debug.Log($"SpawnCoins called: prefab={prefab}, anchor={anchor}, position={spawnPosition}, amount={amount}");
        
        if (prefab == null)
        {
            Debug.LogError("Coin prefab is null!");
            return;
        }
        
        // 检查预制体的组件
        MeshRenderer prefabRenderer = prefab.GetComponent<MeshRenderer>();
        if (prefabRenderer == null)
        {
            Debug.LogError("Coin prefab missing MeshRenderer!");
            return;
        }
        
        if (anchor == null)
        {
            Debug.LogError("Anchor point is null!");
            return;
        }
        
        if (amount <= 0)
        {
            Debug.LogWarning("Coin amount is 0 or negative, nothing to spawn");
            return;
        }
        
        // 创建一个父对象来容纳所有金币，便于管理
        GameObject coinContainer = new GameObject("CoinContainer");
        coinContainer.transform.position = Vector3.zero;
        
        Debug.Log($"Creating {amount} coins at {spawnPosition}");
        
        for (int i = 0; i < amount; i++)
        {
            try
            {
                // 直接在指定位置生成金币，不添加随机偏移
                GameObject coinObject = Instantiate(prefab, spawnPosition, Random.rotation, coinContainer.transform);
                
                if (coinObject != null)
                {
                    Debug.Log($"Coin {i} created at {coinObject.transform.position}");
                    
                    // 检查实例化后的渲染器状态
                    MeshRenderer renderer = coinObject.GetComponent<MeshRenderer>();
                    if (renderer != null)
                    {
                        renderer.enabled = true;
                        
                        // 如果没有材质，创建一个新的
                        if (renderer.material == null)
                        {
                            renderer.material = new Material(Shader.Find("Standard"));
                            renderer.material.color = Color.yellow;
                        }
                    }
                    else
                    {
                        Debug.LogError($"Coin {i} is missing MeshRenderer!");
                    }
                    
                    // 确保脚本应用到实例
                    Coin coinComponent = coinObject.GetComponent<Coin>();
                    if (coinComponent != null)
                    {
                        coinComponent.ApplyPhysics();
                    }
                    else
                    {
                        Debug.LogWarning("Coin component missing on instantiated object!");
                        // 手动添加物理效果
                        Rigidbody rb = coinObject.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            // 注释掉这部分代码，防止金币弹开
                            /*
                            rb.AddForce(new Vector3(
                                Random.Range(-1f, 1f),
                                Random.Range(1f, 2f),
                                Random.Range(-1f, 1f)
                            ), ForceMode.Impulse);
                            
                            rb.AddTorque(new Vector3(
                                Random.Range(-1f, 1f),
                                Random.Range(-1f, 1f),
                                Random.Range(-1f, 1f)
                            ), ForceMode.Impulse);
                            */
                            
                            // 增加阻力
                            rb.drag = 2.0f;
                            rb.angularDrag = 2.0f;
                        }
                    }
                }
                else
                {
                    Debug.LogError($"Failed to instantiate coin {i}!");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error spawning coin {i+1}: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// 初始化硬币的物理属性和外观
    /// </summary>
    public void ApplyPhysics()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // 确保刚体不是运动学的并且使用重力
            rb.isKinematic = false;
            rb.useGravity = true;
            
            // 注释掉随机力，避免金币弹开
            /*
            rb.AddForce(new Vector3(
                Random.Range(-0.05f, 0.05f),
                Random.Range(0.1f, 0.2f),
                Random.Range(-0.05f, 0.05f)
            ), ForceMode.Impulse);
            
            // 注释掉随机旋转
            rb.AddTorque(new Vector3(
                Random.Range(-0.1f, 0.1f),
                Random.Range(-0.1f, 0.1f),
                Random.Range(-0.1f, 0.1f)
            ), ForceMode.Impulse);
            */
            
            // 增加阻力，使金币更快停下来
            rb.drag = 2.0f;
            rb.angularDrag = 2.0f;
            
            Debug.Log($"Physics applied to coin: {gameObject.name} (static, no force)");
        }
        else
        {
            Debug.LogWarning("No Rigidbody found on coin!");
        }
    }
    
    void Start()
    {
        // 在Start中再次检查渲染器状态
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Debug.Log($"Start: Coin renderer enabled: {renderer.enabled}");
            Debug.Log($"Start: Coin material exists: {(renderer.material != null)}");
            Debug.Log($"Start: Coin is visible: {renderer.isVisible}");
        }
    }
}