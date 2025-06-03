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
    
    [Header("Sound Effects")]
    [SerializeField] private string balanceSound = "Balance"; // 天平平衡音效名称
    [SerializeField] private string coinSpawnSound = "Coin"; // 金币生成音效名称
    [SerializeField] private float minVolume = 0.6f; // 最小音量
    [SerializeField] private float maxVolume = 1.0f; // 最大音量
    [SerializeField] private float minPitch = 0.9f; // 最低音调
    [SerializeField] private float maxPitch = 1.1f; // 最高音调

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
        Debug.Log("[Coin] 请求生成 " + amount + " 个金币");
        
        // 播放天平音效
        if (amount > 0 && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(balanceSound);
        }
        
        if (NetworkManager.Singleton.IsServer)
        {
            // 如果已经是服务器，直接启动协程
            Debug.Log("[Coin] 服务器端直接启动生成协程");
            StartCoroutine(SpawnCoinsCoroutine(position, amount));
        }
        else
        {
            // 否则发起 ServerRpc 让服务器来做
            Debug.Log("[Coin] 客户端发起ServerRpc请求");
            SpawnCoinsServerRpc(position, amount);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnCoinsServerRpc(Vector3 position, int amount)
    {
        Debug.Log("[Coin] 服务器收到RPC请求，开始生成 " + amount + " 个金币");
        StartCoroutine(SpawnCoinsCoroutine(position, amount));
    }

    /// <summary>
    /// 在服务器上按顺序生成指定数量的硬币，并在每个生成之间等待 spawnDelay
    /// </summary>
    private IEnumerator SpawnCoinsCoroutine(Vector3 position, int amount)
    {
        Debug.Log("[Coin] 开始生成金币协程，数量: " + amount);
        yield return new WaitForSeconds(0.1f);  // 可以调整这个时间

        for (int i = 0; i < amount; i++)
        {
            // 可以加一点随机偏移，让硬币不完全重叠
            Vector3 randomOffset = Random.insideUnitSphere * 0.1f;
            Vector3 spawnPos = position + randomOffset + Vector3.up * 0.1f;

            GameObject coin = Instantiate(coinPrefab, spawnPos, Random.rotation);
            NetworkObject netObj = coin.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn(destroyWithScene: true);
                
                // 直接尝试在服务器上播放音效，使用随机音量和音调
                if (SoundManager.Instance != null)
                {
                    float randomVolume = Random.Range(minVolume, maxVolume);
                    float randomPitch = Random.Range(minPitch, maxPitch);
                    Debug.Log($"[Coin] 服务器直接播放金币音效: {coinSpawnSound}，音量: {randomVolume}，音调: {randomPitch}");
                    
                    // 设置该音效的音量，然后用随机音调播放
                    SoundManager.Instance.SetSFXVolumeForClip(coinSpawnSound, randomVolume);
                    SoundManager.Instance.PlaySFXWithPitch(coinSpawnSound, randomPitch);
                }
            }
            else
            {
                Debug.LogError("[Coin] 金币预制体没有NetworkObject组件!");
            }

            yield return new WaitForSeconds(spawnDelay);
        }
        
        Debug.Log("[Coin] 金币生成协程完成");
    }
}