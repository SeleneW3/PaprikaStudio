using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GunController : NetworkBehaviour
{
    private Animator gunAnimator;  // 该枪的 Animator

    public NetworkVariable<int> remainingChances = new NetworkVariable<int>(6);  // 剩余的机会次数
    public NetworkVariable<bool> isBulletReal = new NetworkVariable<bool>(false); // 当前子弹是否为真子弹
    public NetworkVariable<bool> gameEnded = new NetworkVariable<bool>(false);   // 游戏是否结束


    void Start()
    {
        // 获取枪的 Animator 组件
        gunAnimator = GetComponent<Animator>();

        if (IsServer) // 只在服务器端初始化
        {
            InitializeBulletChances();
        }
    }

    void InitializeBulletChances()
    {
        // 在游戏开始时，随机生成一个机会为真子弹
        isBulletReal.Value = Random.Range(0, remainingChances.Value) == 0;
        Debug.Log($"Gun initialized with a real bullet: {isBulletReal}");
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
        Debug.Log($"Remaining chances: {remainingChances.Value}");

        if (gunAnimator != null)
        {
            gunAnimator.SetTrigger("Grab");
        }

        if (isBulletReal.Value)
        {
            Debug.Log("Bang! A real bullet! The enemy is dead.");
            gameEnded.Value = true;  // 设置 gameEnded 的值
        }
        else
        {
            if (remainingChances.Value == 0)
            {
                InitializeBulletChances();
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
            InitializeBulletChances();
        }
    }
}