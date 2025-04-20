using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BalanceScale : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Animator animator;      // 天平整体的动画器
    [SerializeField] private float transitionSpeed = 1f;
    [SerializeField] private float maxScoreDifference = 10f;
    [SerializeField] private float minScoreDifference = 1f;

    [Header("Animation State Names")]
    [SerializeField] private string rotateStateName = "Rotate";    // 横梁动画状态名
    [SerializeField] private string player1StateName = "Player1";  // 托盘1动画状态名
    [SerializeField] private string player2StateName = "Player2";  // 托盘2动画状态名

    [Header("Debug Testing")]
    [SerializeField] private bool enableTesting = true;  // 是否启用测试
    [SerializeField] private KeyCode testPlayer1WinKey = KeyCode.Alpha1;  // 按1测试玩家1领先
    [SerializeField] private KeyCode testPlayer2WinKey = KeyCode.Alpha2;  // 按2测试玩家2领先
    [SerializeField] private KeyCode testNeutralKey = KeyCode.Alpha0;     // 按0测试平衡状态
    [SerializeField] private float testScoreAmount = 5f;  // 测试分数差值

    private float currentFrame = 0f;
    private float targetFrame = 0f;

    private void Start()
    {
        // 初始化所有动画到第0帧
        SetAllAnimationsFrame(0);
    }

    private void SetAllAnimationsFrame(float normalizedTime)
    {
        if (animator != null)
        {
            try
        {
            // 直接在每个层上强制播放动画状态
            animator.Play(rotateStateName, 0, normalizedTime);
            animator.Play(player1StateName, 1, normalizedTime);
            animator.Play(player2StateName, 2, normalizedTime);
            
            // 确保动画立即更新
            animator.Update(0);
            animator.speed = 0;

            // 添加调试信息
            Debug.Log($"设置动画 - normalizedTime: {normalizedTime}");
            Debug.Log($"Layer 0 (Rotate): {animator.GetCurrentAnimatorStateInfo(0).normalizedTime}");
            Debug.Log($"Layer 1 (Player1): {animator.GetCurrentAnimatorStateInfo(1).normalizedTime}");
            Debug.Log($"Layer 2 (Player2): {animator.GetCurrentAnimatorStateInfo(2).normalizedTime}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"设置动画帧时出错: {e.Message}");
        }
    }
    else
    {
        Debug.LogError("Animator未设置!");
    }
    }

    public void UpdateScore(float player1Score, float player2Score)
    {
        float scoreDifference = player1Score - player2Score;
        
        // 计算目标帧
        if (Mathf.Abs(scoreDifference) < minScoreDifference)
        {
            // 分差小于1分，回到中立状态（0帧）
            targetFrame = 0;
        }
        else if (scoreDifference > 0)
        {
            // 玩家1分数更高，使用1-40帧
            float normalizedScore = Mathf.Clamp01((scoreDifference - minScoreDifference) / 
                                                (maxScoreDifference - minScoreDifference));
            targetFrame = Mathf.Lerp(1, 40, normalizedScore);
        }
        else
        {
            // 玩家2分数更高，使用51-90帧
            float normalizedScore = Mathf.Clamp01((-scoreDifference - minScoreDifference) / 
                                                (maxScoreDifference - minScoreDifference));
            targetFrame = Mathf.Lerp(51, 90, normalizedScore);
        }
    }

    private void Update()
    {
        if (!Mathf.Approximately(currentFrame, targetFrame))
        {
            // 计算新的当前帧
            currentFrame = Mathf.MoveTowards(currentFrame, targetFrame, 
                                           transitionSpeed * Time.deltaTime * 60);

            // 将帧数转换为动画时间（normalizedTime）
            float normalizedTime = currentFrame / 100f;
            
            // 添加调试信息
            Debug.Log($"更新动画 - 当前帧: {currentFrame}, 目标帧: {targetFrame}, 正规化时间: {normalizedTime}");
            
            SetAllAnimationsFrame(normalizedTime);
        }

        // 测试逻辑
        if (enableTesting)
        {
            // 测试玩家1领先
            if (Input.GetKeyDown(testPlayer1WinKey))
            {
                Debug.Log($"测试：玩家1领先 {testScoreAmount} 分");
                UpdateScore(testScoreAmount, 0);
            }
            // 测试玩家2领先
            else if (Input.GetKeyDown(testPlayer2WinKey))
            {
                Debug.Log($"测试：玩家2领先 {testScoreAmount} 分");
                UpdateScore(0, testScoreAmount);
            }
            // 测试平衡状态
            else if (Input.GetKeyDown(testNeutralKey))
            {
                Debug.Log("测试：分数相等");
                UpdateScore(0, 0);
            }

            // 显示当前帧信息
            if (Input.GetKey(KeyCode.Tab))
            {
                Debug.Log($"当前帧: {currentFrame}, 目标帧: {targetFrame}");
            }
        }
    }

    // 调试用：直接设置特定帧
    public void SetFrame(float frame)
    {
        currentFrame = frame;
        SetAllAnimationsFrame(frame / 100f);
    }

    // 可选：添加缓动效果
    private float SmoothStep(float current, float target, float speed)
    {
        float change = target - current;
        float step = change * Mathf.Clamp01(speed * Time.deltaTime);
        return current + step;
    }
}