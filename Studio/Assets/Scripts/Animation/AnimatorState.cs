using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class AnimatorState : NetworkBehaviour
{
    public Animator animator;
    public DialogManager dialogManager; // 对话管理器，用于检测当前对话行
    public int element4Index = 5; // 假设 Element4 是对话的第4行（索引从0开始）
    private bool hasPlayedAddBullet = false;  // 确保动画只播放一次
    private bool isAnimating = false;  // 防止动画重复触发

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        // 检查对话管理器是否存在
        if (dialogManager == null) return;

        // 检查当前播放的对话行是否为 Element4，且尚未播放动画，且当前没有动画在播放
        if (dialogManager.currentLineIndex == element4Index && !hasPlayedAddBullet && !isAnimating)
        {
            // 播放 AddBullet 动画
            PlayAddBulletAnimation();
            hasPlayedAddBullet = true; // 确保动画只播放一次
        }
    }

    // 播放 AddBullet 动画
    private void PlayAddBulletAnimation()
    {
        if (isAnimating) return;  // 如果已经在播放动画，则退出
        
        isAnimating = true;  // 标记动画正在播放
        
        // 激活 AddBullet 动画
        animator.SetTrigger("Add");
        
        // 延迟播放装弹音效
        Invoke("PlayAddBulletSound", 1.6f);
        
        // 播放None动画(立即触发)，但延迟播放重置音效
        Invoke("PlayResetSound", 3.5f);
        
        // 延迟一小段时间后触发None动画，确保Add动画已经播放完成
        Invoke("TriggerNoneAnimation", 3f);
    }
    
    // 播放装弹音效
    private void PlayAddBulletSound()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("GunAddBullet");
        }
    }
    
    // 只播放重置音效
    private void PlayResetSound()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("GunReset");
        }
    }
    
    // 只触发None动画
    private void TriggerNoneAnimation()
    {
        // 播放完毕后切换回 None
        animator.SetTrigger("None");
        
        // 动画播放完毕
        isAnimating = false;
    }
}

