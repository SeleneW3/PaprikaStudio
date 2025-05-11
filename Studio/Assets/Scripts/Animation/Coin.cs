using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class Coin : NetworkBehaviour
{
    private static Coin instance;
    public static GameObject coinPrefab;
    
    [Header("Physics Settings")]
    public float initialForce = 0.2f;
    public Vector3 randomForceRange = new Vector3(0.2f, 0.5f, 0.2f);
    public float randomPositionRange = 0.05f;
    
    [Header("Appearance")]
    public Color[] possibleColors = new Color[] { 
        new Color(1f, 0.8f, 0f), // 金色
        new Color(0.8f, 0.8f, 0.8f) // 银色
    };

    private bool hasLanded = false;
    private Transform originalParent;
    private Rigidbody rb;
    private NetworkTransform netTransform;
    
    void Awake()
    {
        instance = this;
        rb = GetComponent<Rigidbody>();
        netTransform = GetComponent<NetworkTransform>();
    }
    
    public static void SpawnCoins(GameObject prefab, Transform anchor, Vector3 spawnPosition, int amount)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            coinPrefab = prefab;
            // 创建一个临时的Coin组件来调用ServerRpc
            GameObject tempCoin = new GameObject("TempCoin");
            Coin coinComponent = tempCoin.AddComponent<Coin>();
            NetworkObject netObj = tempCoin.AddComponent<NetworkObject>();
            netObj.Spawn();
            
            // 调用ServerRpc
            coinComponent.SpawnCoinsServerRpc(spawnPosition, amount, anchor.gameObject.name);
            
            // 清理临时对象
            netObj.Despawn();
            GameObject.Destroy(tempCoin);
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void SpawnCoinsServerRpc(Vector3 spawnPosition, int amount, string anchorName)
    {
        if (coinPrefab == null) return;
        
        // 找到对应的锚点
        GameObject anchorObj = GameObject.Find(anchorName);
        if (anchorObj == null) return;
        
        // 创建容器并设置父物体
        GameObject coinContainer = new GameObject($"CoinContainer_{Random.Range(0, 10000)}");
        coinContainer.transform.SetParent(anchorObj.transform, false);
        coinContainer.transform.localPosition = Vector3.zero;
        
        NetworkObject containerNetObj = coinContainer.AddComponent<NetworkObject>();
        containerNetObj.Spawn();
        
        // 在服务器上生成所有硬币
        for (int i = 0; i < amount; i++)
        {
            Vector3 randomOffset = Random.insideUnitSphere * randomPositionRange;
            Vector3 finalPosition = spawnPosition + randomOffset + Vector3.up * 0.1f; // 稍微抬高一点
            
            GameObject coinObj = Instantiate(coinPrefab, finalPosition, Random.rotation);
            NetworkObject coinNetObj = coinObj.GetComponent<NetworkObject>();
            
            if (coinNetObj != null)
            {
                // 设置父物体关系
                coinObj.transform.SetParent(coinContainer.transform, true);
                
                // 确保有刚体组件
                Rigidbody coinRb = coinObj.GetComponent<Rigidbody>();
                if (coinRb == null)
                {
                    coinRb = coinObj.AddComponent<Rigidbody>();
                }
                
                // 配置刚体属性
                coinRb.isKinematic = false;
                coinRb.useGravity = true;
                coinRb.drag = 0.1f;
                coinRb.angularDrag = 0.1f;
                coinRb.mass = 0.1f;
                coinRb.interpolation = RigidbodyInterpolation.Interpolate;
                coinRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                
                // 生成到网络中
                coinNetObj.Spawn();

                // 在服务器端添加随机力，但力度更温和
                Vector3 randomForce = new Vector3(
                    Random.Range(-randomForceRange.x, randomForceRange.x),
                    Mathf.Abs(Random.Range(0, randomForceRange.y)),
                    Random.Range(-randomForceRange.z, randomForceRange.z)
                ) * initialForce;
                
                coinRb.AddForce(randomForce, ForceMode.Impulse);
            }
        }
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // 确保有必要的组件
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (netTransform == null) netTransform = GetComponent<NetworkTransform>();
        
        // 确保渲染器可见
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.enabled = true;
            if (renderer.material == null)
            {
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = possibleColors[0];
            }
        }

        // 设置网络同步
        if (netTransform != null)
        {
            // NetworkTransform 会自动同步位置和旋转
            netTransform.Interpolate = true;  // 启用插值
        }
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;
        
        // 服务器端处理物理更新
        if (rb != null && !rb.isKinematic)
        {
            // 物理更新会通过 NetworkTransform 自动同步到客户端
            rb.AddForce(Physics.gravity, ForceMode.Acceleration);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;
        
        if (!hasLanded && collision.gameObject.CompareTag("BalanceScale"))
        {
            hasLanded = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}