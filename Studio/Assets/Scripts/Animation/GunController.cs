using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GunController : NetworkBehaviour
{
    private Animator gunAnimator;  // 该枪的 Animator

    public NetworkVariable<int> remainingChances = new NetworkVariable<int>(6);  // 剩余的机会次数
    public NetworkVariable<int> realBulletPosition = new NetworkVariable<int>(0); // 真子弹的位置，初始为 0（无效值）
    public NetworkVariable<bool> gameEnded = new NetworkVariable<bool>(false);   // 游戏是否结束

    void Start()
    {
        // 获取枪的 Animator 组件
        gunAnimator = GetComponent<Animator>();

        if (IsServer) // 只在服务器端初始化
        {
            InitializeBulletChancesServerRpc();
        }
    }

    // ServerRpc 用于初始化 "真子弹" 的位置
    [ServerRpc(RequireOwnership = false)]
    void InitializeBulletChancesServerRpc()
    {
        // 在游戏开始时，随机生成一个“真子弹”的位置 (1 到 6)
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

        remainingChances.Value--;  // 使用 .Value 访问 NetworkVariable 的值
        Debug.Log($"Remaining chances: {remainingChances.Value}, Real Bullet is at position {realBulletPosition.Value}");

        if (gunAnimator != null)
        {
            gunAnimator.SetTrigger("Grab");
        }

        if (remainingChances.Value == realBulletPosition.Value - 1) // 比较剩余机会数和真子弹位置
        {
            Debug.Log("Bang! A real bullet! The enemy is dead.");
            gameEnded.Value = true;  // 设置 gameEnded 的值
        }
        else
        {
            if (remainingChances.Value == 0)
            {
                InitializeBulletChancesServerRpc(); // 使用 ServerRpc 来初始化真子弹的位置
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
        if (IsServer)
        {
            remainingChances.Value = 6;
            gameEnded.Value = false;
            InitializeBulletChancesServerRpc(); // 在重置时重新初始化
        }
    }
}