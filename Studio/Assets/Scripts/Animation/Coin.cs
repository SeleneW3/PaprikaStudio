using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class Coin : NetworkBehaviour
{ 
    public static Coin Instance { get; private set; }

    [Header("Prefab & 延迟设置")]
    [Tooltip("带有 NetworkObject 组件 的硬币预制体")]
    public GameObject coinPrefab;
    [Tooltip("每次生成硬币之间的间隔（秒）")]
    public float spawnDelay = 0.2f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 在客户端或服务器端请求生成硬币
    /// </summary>
    public void RequestSpawnCoins(Vector3 position, int amount)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            // 如果已经是服务器，直接启动协程
            StartCoroutine(SpawnCoinsCoroutine(position, amount));
        }
        else
        {
            // 否则发起 ServerRpc 让服务器来做
            SpawnCoinsServerRpc(position, amount);
            Debug.Log("send request");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnCoinsServerRpc(Vector3 position, int amount)
    {
        StartCoroutine(SpawnCoinsCoroutine(position, amount));
    }

    /// <summary>
    /// 在服务器上按顺序生成指定数量的硬币，并在每个生成之间等待 spawnDelay
    /// </summary>
    private IEnumerator SpawnCoinsCoroutine(Vector3 position, int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            // 可以加一点随机偏移，让硬币不完全重叠
            Vector3 randomOffset = Random.insideUnitSphere * 0.1f;
            Vector3 spawnPos = position + randomOffset + Vector3.up * 0.1f;

            GameObject coin = Instantiate(coinPrefab, spawnPos, Random.rotation);
            NetworkObject netObj = coin.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();
            }

            yield return new WaitForSeconds(spawnDelay);
        }
    }
}
