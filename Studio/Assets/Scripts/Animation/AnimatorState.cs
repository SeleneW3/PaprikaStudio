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
    public int element4Index = 3; // 假设 Element4 是对话的第4行（索引从0开始）
    private bool hasPlayedAddBullet = false;  // 确保动画只播放一次


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

        // 检查当前播放的对话行是否为 Element4
        if (dialogManager.currentLineIndex == element4Index && !hasPlayedAddBullet)
        {
            // 播放 AddBullet 动画
            PlayAddBulletAnimation();

            hasPlayedAddBullet = true; // 确保动画只播放一次
        }
    }

    // 播放 AddBullet 动画并在播放完后切回 None
    private void PlayAddBulletAnimation()
    {
        // 激活 AddBullet 动画
        animator.SetTrigger("Add");

        // 假设动画的长度为2秒
        StartCoroutine(WaitForAnimationToEnd(2f));
    }

    // 等待动画播放完毕后切换回 None
    private IEnumerator WaitForAnimationToEnd(float animationDuration)
    {
        yield return new WaitForSeconds(animationDuration);

        // 播放完毕后切换回 None
        animator.SetTrigger("None");
    }
}

