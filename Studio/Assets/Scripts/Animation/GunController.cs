using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GunController : NetworkBehaviour
{
    private Animator gunAnimator;  // 该枪的 Animator
    private NetworkObject networkObject;  // 引用 NetworkObject 组件

    public NetworkVariable<int> remainingChances = new NetworkVariable<int>(6);  // 剩余的机会次数
    public NetworkVariable<int> realBulletPosition = new NetworkVariable<int>(0); // 真子弹的位置，初始为 0（无效值）
    public NetworkVariable<bool> gameEnded = new NetworkVariable<bool>(false);   // 游戏是否结束
    
    void Awake()
    {
        // 在 Awake 中获取 NetworkObject，确保最早获取到组件
        networkObject = GetComponent<NetworkObject>();
        Debug.Log($"Awake - NetworkObject component {(networkObject != null ? "found" : "not found")}");
    }

    void Start()
    {
        // 重新获取一次，以防在运行时添加
        if (networkObject == null)
        {
            networkObject = GetComponent<NetworkObject>();
        }

        if (networkObject == null)
        {
            Debug.LogError("NetworkObject component missing on GunController!");
            return;
        }

        Debug.Log($"Start - NetworkObject state - IsSpawned: {networkObject.IsSpawned}, IsLocalPlayer: {networkObject.IsLocalPlayer}, IsOwner: {networkObject.IsOwner}");
        
        // 如果在服务器上且还没有生成，则生成对象
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && !networkObject.IsSpawned)
        {
            Debug.Log("Start - Spawning NetworkObject for GunController");
            networkObject.Spawn();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log($"OnNetworkSpawn - IsServer: {IsServer}, NetworkManager.Singleton.IsServer: {NetworkManager.Singleton.IsServer}, IsSpawned: {IsSpawned}, IsClient: {IsClient}");
        
        // 获取枪的 Animator 组件
        gunAnimator = GetComponent<Animator>();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer) // 检查 NetworkManager 是否存在
        {
            Debug.Log("Server side - About to call InitializeBulletChancesServerRpc");
            try 
            {
                InitializeBulletChancesServerRpc();
                Debug.Log("Successfully called InitializeBulletChancesServerRpc");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to call InitializeBulletChancesServerRpc: {e.Message}");
            }
        }
        else
        {
            Debug.Log($"Client side or NetworkManager not ready - NetworkManager.Singleton is {(NetworkManager.Singleton == null ? "null" : "not null")}");
        }
    }

    // ServerRpc 用于初始化 "真子弹" 的位置
    [ServerRpc(RequireOwnership = false)]
    void InitializeBulletChancesServerRpc()
    {
        Debug.Log($"InitializeBulletChancesServerRpc called on {(IsServer ? "Server" : "Client")}");
        // 在游戏开始时，随机生成一个"真子弹"的位置 (1 到 6)
        realBulletPosition.Value = Random.Range(1, 7);  // 返回 1 到 6 之间的整数
        Debug.Log($"Server initialized with a real bullet at position: {realBulletPosition.Value}");
    }

    public void FireGun()
    {
        if (gameEnded.Value)
        {
            Debug.Log("The game has already ended.");
            return;
        }

        if (remainingChances.Value <= 0)
        {
            Debug.Log("No remaining chances.");
            return;
        }

        // 当前位置是 7 - remainingChances (从1开始，因为remainingChances初始值是6)
        int currentPosition = 7 - remainingChances.Value;
        remainingChances.Value--;  // 使用 .Value 访问 NetworkVariable 的值
        Debug.Log($"Current position: {currentPosition}, Remaining chances: {remainingChances.Value}, Real Bullet is at position {realBulletPosition.Value}");

        if (gunAnimator != null)
        {
            gunAnimator.SetTrigger("Grab");
        }

        if (currentPosition == realBulletPosition.Value) // 检查当前位置是否是真子弹位置
        {
            Debug.Log("Bang! A real bullet! The enemy is dead.");
            gameEnded.Value = true;  // 设置 gameEnded 的值
        }
        else
        {
            if (remainingChances.Value == 0)
            {
                InitializeBulletChancesServerRpc(); // 使用 ServerRpc 来初始化真子弹的位置
                remainingChances.Value = 6;  // 重置剩余次数
            }
        }
    }

    [ClientRpc]
    void PlayFireAnimationClientRpc()
    {
        if (gunAnimator != null)
        {
            gunAnimator.SetTrigger("Grab");
        }
    }

    public void ResetGun()
    {
        if (NetworkManager.Singleton.IsServer)  // 修改这里也使用 NetworkManager.Singleton.IsServer
        {
            remainingChances.Value = 6;
            gameEnded.Value = false;
            InitializeBulletChancesServerRpc(); // 在重置时重新初始化
        }
    }

    // 添加一个方法来手动生成对象（如果需要的话）
    public void ManualSpawn()
    {
        if (networkObject != null && !networkObject.IsSpawned && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            Debug.Log("Manually spawning NetworkObject");
            networkObject.Spawn();
        }
    }
}