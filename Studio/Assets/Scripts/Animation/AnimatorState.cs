using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class AnimatorState : NetworkBehaviour
{
    public Animator animator;
    [HideInInspector] // 隐藏Inspector中的引用，因为我们会动态获取
    public DialogManager dialogManager; // 对话管理器，用于检测当前对话行
    public int element4Index = 17; // 假设 Element4 是对话的第4行（索引从0开始）
    private bool hasPlayedAddBullet = false;  // 确保动画只播放一次
    private bool isAnimating = false;  // 防止动画重复触发
    
    // 添加DeckLogic引用，用于检查卡牌展示状态
    private DeckLogic deckLogic;
    
    // 添加标志，记录是否需要在卡牌展示结束后恢复摄像机
    private bool needRestoreCameraAfterCardShow = false;
    
    // 添加字段记录摄像机切换前的状态
    private CameraLogic cameraLogic;
    private bool wasShowingCards = false;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        // 不在Start中获取DialogManager，因为它可能还没有被加载
        
        // 获取DeckLogic和CameraLogic引用
        deckLogic = FindObjectOfType<DeckLogic>();
        cameraLogic = FindObjectOfType<CameraLogic>();
    }

    // Update is called once per frame
    void Update()
    {
        // 每次Update时尝试获取DialogManager实例
        if (dialogManager == null)
        {
            dialogManager = DialogManager.Instance;
            if (dialogManager == null) return; // 如果还是找不到，直接返回
        }
        
        // 如果DeckLogic为空，尝试获取
        if (deckLogic == null)
        {
            deckLogic = FindObjectOfType<DeckLogic>();
        }
        
        // 如果CameraLogic为空，尝试获取
        if (cameraLogic == null)
        {
            cameraLogic = FindObjectOfType<CameraLogic>();
        }

        // 检查当前播放的对话行是否为 Element4，且尚未播放动画，且当前没有动画在播放
        if (dialogManager.currentLineIndex == element4Index && !hasPlayedAddBullet && !isAnimating)
        {
            Debug.Log($"[AnimatorState] 检测到对话索引 {element4Index}，准备播放AddBullet动画");
            // 播放 AddBullet 动画
            PlayAddBulletAnimation();
            hasPlayedAddBullet = true; // 确保动画只播放一次
        }
        
        // 检查是否需要在卡牌展示结束后恢复摄像机
        if (needRestoreCameraAfterCardShow && deckLogic != null)
        {
            // 如果DeckLogic之前在展示卡牌，但现在不再展示，则恢复摄像机
            if (wasShowingCards && !deckLogic.hasShown)
            {
                Debug.Log("[AnimatorState] 检测到卡牌展示结束，恢复枪摄像机");
                wasShowingCards = false;
                needRestoreCameraAfterCardShow = false;
                
                // 重新切换到枪摄像机
                SwitchToGunCamera();
            }
        }
    }

    // 播放 AddBullet 动画
    private void PlayAddBulletAnimation()
    {
        if (isAnimating) return;  // 如果已经在播放动画，则退出
        
        Debug.Log("[AnimatorState] 开始播放AddBullet动画");
        isAnimating = true;  // 标记动画正在播放
        
        // 激活 AddBullet 动画
        animator.SetTrigger("Add");
        
        // 切换到枪的摄像机视角
        SwitchToGunCamera();
        
        // 延迟播放装弹音效
        Invoke("PlayAddBulletSound", 1.6f);
        
        // 播放None动画(立即触发)，但延迟播放重置音效
        Invoke("PlayResetSound", 3.5f);
        
        // 延迟一小段时间后触发None动画，确保Add动画已经播放完成
        Invoke("TriggerNoneAnimation", 3f);
        
        // 延迟切回主摄像机
        Invoke("SwitchToMainCamera", 2.5f);
    }
    
    // 切换到枪的摄像机
    private void SwitchToGunCamera()
    {
        // 检查DeckLogic是否正在展示卡牌
        if (deckLogic != null && deckLogic.hasShown)
        {
            Debug.Log("[AnimatorState] DeckLogic正在展示卡牌，暂时不切换摄像机");
            wasShowingCards = true;
            needRestoreCameraAfterCardShow = true;
            return; // 如果正在展示卡牌，不切换摄像机
        }
        
        // 查找CameraLogic组件
        if (cameraLogic != null)
        {
            Debug.Log("[AnimatorState] 切换到枪摄像机视角");
            
            // 确定当前是哪个玩家的枪
            ulong localClientId = NetworkManager.Singleton.LocalClientId;
            
            // 根据玩家ID切换到对应的枪摄像机状态
            if (localClientId == 0)
            {
                // 玩家1的枪摄像机
                if (cameraLogic.player1GunState != null)
                {
                    // 激活玩家1的枪摄像机，禁用其他摄像机
                    cameraLogic.player1GunState.gameObject.SetActive(true);
                    cameraLogic.player1BalanceState.gameObject.SetActive(false);
                    cameraLogic.player1ShowState.gameObject.SetActive(false);
                    
                    cameraLogic.player2CameraController_Gun.gameObject.SetActive(false);
                    cameraLogic.player2CameraController_Balance.gameObject.SetActive(false);
                    cameraLogic.player2ShowState.gameObject.SetActive(false);
                }
            }
            else
            {
                // 玩家2的枪摄像机
                if (cameraLogic.player2CameraController_Gun != null)
                {
                    // 激活玩家2的枪摄像机，禁用其他摄像机
                    cameraLogic.player2CameraController_Gun.gameObject.SetActive(true);
                    cameraLogic.player2CameraController_Balance.gameObject.SetActive(false);
                    cameraLogic.player2ShowState.gameObject.SetActive(false);
                    
                    cameraLogic.player1GunState.gameObject.SetActive(false);
                    cameraLogic.player1BalanceState.gameObject.SetActive(false);
                    cameraLogic.player1ShowState.gameObject.SetActive(false);
                }
            }
        }
        else
        {
            Debug.LogError("[AnimatorState] 未找到CameraLogic组件，无法切换摄像机");
        }
    }
    
    // 切回主摄像机
    private void SwitchToMainCamera()
    {
        // 如果DeckLogic正在展示卡牌，不切换回主摄像机
        if (deckLogic != null && deckLogic.hasShown)
        {
            Debug.Log("[AnimatorState] DeckLogic正在展示卡牌，不切换回主摄像机");
            // 不设置isAnimating = false，因为我们还需要等待卡牌展示结束后恢复摄像机
            return;
        }
        
        // 查找CameraLogic组件
        if (cameraLogic != null)
        {
            Debug.Log("[AnimatorState] 切回主摄像机视角");
            
            // 确定当前是哪个玩家
            ulong localClientId = NetworkManager.Singleton.LocalClientId;
            
            // 根据玩家ID切换回对应的主摄像机
            if (localClientId == 0)
            {
                // 切换回玩家1的主摄像机
                cameraLogic.SwitchToPlayer1Camera();
            }
            else
            {
                // 切换回玩家2的主摄像机
                cameraLogic.SwitchToPlayer2Camera();
            }
        }
        else
        {
            Debug.LogError("[AnimatorState] 未找到CameraLogic组件，无法切换摄像机");
        }
        
        // 动画播放完毕，重置状态
        isAnimating = false;
    }
    
    // 播放装弹音效
    private void PlayAddBulletSound()
    {
        if (SoundManager.Instance != null)
        {
            Debug.Log("[AnimatorState] 播放装弹音效");
            SoundManager.Instance.PlaySFX("GunAddBullet");
        }
    }
    
    // 只播放重置音效
    public void PlayResetSound()
    {
        if (SoundManager.Instance != null)
        {
            Debug.Log("[AnimatorState] 播放重置音效");
            SoundManager.Instance.PlaySFX("GunReset");
        }
    }
    
    // 只触发None动画
    private void TriggerNoneAnimation()
    {
        // 播放完毕后切换回 None
        animator.SetTrigger("None");
        Debug.Log("[AnimatorState] 触发None动画，动画播放完毕");
        
        // 注意：不在这里重置isAnimating，因为我们需要等待摄像机切回主视角后才完全结束
    }
    
    // 添加一个公共方法，允许手动触发动画
    public void ManualTriggerAnimation()
    {
        if (!hasPlayedAddBullet && !isAnimating)
        {
            Debug.Log("[AnimatorState] 手动触发AddBullet动画");
            PlayAddBulletAnimation();
            hasPlayedAddBullet = true;
        }
    }
    
    // 添加重置方法，允许在需要时重新播放动画
    public void ResetAnimationState()
    {
        hasPlayedAddBullet = false;
        isAnimating = false;
        needRestoreCameraAfterCardShow = false;
        wasShowingCards = false;
        Debug.Log("[AnimatorState] 重置动画状态");
    }
}

