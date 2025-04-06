using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunController : MonoBehaviour
{
    private Animator gunAnimator;  // 该枪的 Animator

    private int remainingChances = 6;  // 剩余的机会次数
    private bool isBulletReal = false; // 当前子弹是否为真子弹
    public bool gameEnded = false;   // 游戏是否结束


    void Start()
    {
        // 获取枪的 Animator 组件
        gunAnimator = GetComponent<Animator>();

        // 初始化子弹机会（每次游戏开始时重新随机分配）
        InitializeBulletChances();
    }

    void InitializeBulletChances()
    {
        // 在游戏开始时，随机生成一个机会为真子弹
        isBulletReal = Random.Range(0, remainingChances) == 0;  // 只有一次机会是 "真子弹"
        Debug.Log($"Gun initialized with a real bullet: {isBulletReal}");
    }

    // 调用此方法来触发枪的开火动画
    public void FireGun()
    {
        if (gameEnded)
        {
            Debug.Log("The game has already ended.");
            return;
        }

        if (remainingChances <= 0)
        {
            Debug.Log("No remaining chances.");
            return;
        }

        // 消耗一次机会
        remainingChances--;
        Debug.Log($"Remaining chances: {remainingChances}");

        // 触发枪的动画
        if (gunAnimator != null)
        {
            gunAnimator.SetTrigger("GrabGun");  // 设置触发器，开始GrabGun动画
        }

        // 检查是否触发了真子弹
        if (isBulletReal)
        {
            Debug.Log("Bang! A real bullet! The enemy is dead.");
            gameEnded = true;
        }
        else
        {
            // 如果没有命中真子弹，重新初始化机会
            if (remainingChances == 0)
            {
                InitializeBulletChances();  // 重新初始化子弹机会
            }
        }
    }

    // 重置枪的状态（例如回合结束时使用）
    public void ResetGun()
    {
        remainingChances = 6;
        gameEnded = false;
        InitializeBulletChances();
    }
    
}
