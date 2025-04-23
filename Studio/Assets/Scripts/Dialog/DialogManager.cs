using UnityEngine;
using TMPro;
using Febucci.UI;  // 确保添加这个引用
using System.Collections;

public class DialogManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI dialogText;        // 对话文本显示组件
    public GameObject dialogPanel;             // 对话面板
    private TextAnimator textAnimator;  // TextAnimator 组件引用
    private TextAnimatorPlayer textAnimatorPlayer;  // TextAnimatorPlayer 组件引用

    [Header("Dialog Settings")]
    [TextArea(3, 10)]
    public string[] dialogLines;              // 对话文本数组
    public float typingSpeed = 0.05f;         // 文字显示速度
    
    [Header("Auto Advance Settings")]
    public bool autoAdvance = true;           // 是否自动前进到下一个对话
    public float autoAdvanceDelay = 1.5f;     // 打字完成后自动前进的延迟时间

    public int currentLineIndex = 0;         // 当前显示的文本索引
    private bool isDialogActive = false;      // 对话是否激活
    private bool isTyping = false;            // 是否正在打字
    private string currentLine = "";          // 当前完整的文本行
    private Coroutine autoAdvanceCoroutine;   // 自动前进的协程

    void Start()
    {
        // 初始化时隐藏对话面板
        if (dialogPanel != null)
            dialogPanel.SetActive(false);

        // 设置文本对齐方式
        if (dialogText != null)
        {
            dialogText.alignment = TextAlignmentOptions.Center;
            dialogText.enableWordWrapping = true;  // 启用自动换行
        }

        // 获取 TextAnimator 组件
        textAnimator = dialogText?.GetComponent<TextAnimator>();
        textAnimatorPlayer = dialogText?.GetComponent<TextAnimatorPlayer>();

        if (textAnimator == null)
            Debug.LogError("TextAnimator component not found!");
            
        // 设置TextAnimatorPlayer的事件监听
        if (textAnimatorPlayer != null)
        {
            // 注册打字完成事件
            textAnimatorPlayer.onTextShowed.AddListener(OnTypewriterComplete);
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            if (isDialogActive)
            {
                if (isTyping)
                {
                    // 如果正在打字，则完成当前行
                    CompleteLine();
                }
                // 移除了点击跳转到下一行的功能
                // 只保留自动前进
            }
        }
    }

    // 开始显示对话
    public void StartDialog()
    {
        if (dialogLines.Length == 0) return;

        isDialogActive = true;
        currentLineIndex = 0;
        dialogPanel.SetActive(true);
        DisplayLine(dialogLines[currentLineIndex]);
    }

    // 显示下一行文本
    private void DisplayNextLine()
    {
        currentLineIndex++;
        if (currentLineIndex < dialogLines.Length)
        {
            DisplayLine(dialogLines[currentLineIndex]);
        }
        else
        {
            // 所有文本显示完毕，关闭对话
            EndDialog();
        }
    }

    // 显示指定的文本行
    private void DisplayLine(string line)
    {
        // 如果有自动前进协程正在运行，取消它
        if (autoAdvanceCoroutine != null)
        {
            StopCoroutine(autoAdvanceCoroutine);
            autoAdvanceCoroutine = null;
        }
        
        currentLine = line;
        isTyping = true;

        if (textAnimatorPlayer != null)
        {
            // 使用 TextAnimatorPlayer 显示文本
            textAnimatorPlayer.ShowText(line);
            // 事件监听会处理打字完成
        }
        else if (textAnimator != null)
        {
            // 使用 TextAnimator 显示文本
            textAnimator.SetText(line, true);
            // 对于TextAnimator，我们需要估算时间
            StartCoroutine(EstimateTypewritingCompletion());
        }
        else
        {
            // 降级为普通文本显示
            dialogText.text = line;
            isTyping = false;
            
            // 如果启用了自动前进，开始自动前进倒计时
            if (autoAdvance)
            {
                autoAdvanceCoroutine = StartCoroutine(AutoAdvanceDialog());
            }
        }

        // 确保每次显示新行时文本都是居中的
        dialogText.alignment = TextAlignmentOptions.Center;
    }
    
    // 估算打字完成时间的协程（仅用于TextAnimator，不用于TextAnimatorPlayer）
    private IEnumerator EstimateTypewritingCompletion()
    {
        // 估算打字完成所需时间
        float estimatedTime = currentLine.Length * typingSpeed;
        
        // 等待估算的时间
        yield return new WaitForSeconds(estimatedTime);
        
        // 如果此时仍在打字(用户没有点击跳过)，则标记打字完成
        if (isTyping)
        {
            OnTypewriterComplete();
        }
    }
    
    // 打字效果完成后的回调
    private void OnTypewriterComplete()
    {
        isTyping = false;
        
        // 如果启用了自动前进，开始自动前进倒计时
        if (autoAdvance)
        {
            autoAdvanceCoroutine = StartCoroutine(AutoAdvanceDialog());
        }
    }
    
    // 自动前进到下一个对话的协程
    private IEnumerator AutoAdvanceDialog()
    {
        // 等待设定的延迟时间
        yield return new WaitForSeconds(autoAdvanceDelay);
        
        // 显示下一行文本
        DisplayNextLine();
        
        // 清空协程引用
        autoAdvanceCoroutine = null;
    }

    // 立即完成当前行的显示
    private void CompleteLine()
    {
        if (textAnimatorPlayer != null)
        {
            textAnimatorPlayer.SkipTypewriter();
        }
        else if (textAnimator != null)
        {
            textAnimator.SetText(currentLine, false);
        }
        else
        {
            dialogText.text = currentLine;
        }
        isTyping = false;
    }

    // 结束对话
    private void EndDialog()
    {
        // 如果有自动前进协程正在运行，取消它
        if (autoAdvanceCoroutine != null)
        {
            StopCoroutine(autoAdvanceCoroutine);
            autoAdvanceCoroutine = null;
        }
        
        isDialogActive = false;
        dialogPanel.SetActive(false);
    }

    // 防止内存泄漏
    private void OnDestroy()
    {
        if (textAnimatorPlayer != null)
        {
            textAnimatorPlayer.onTextShowed.RemoveListener(OnTypewriterComplete);
        }
    }

    // 外部调用的显示对话方法
    public void ShowDialog(string[] lines)
    {
        dialogLines = lines;
        StartDialog();
    }
    
    // 设置是否自动前进
    public void SetAutoAdvance(bool enable)
    {
        autoAdvance = enable;
        
        // 如果禁用自动前进，取消正在运行的自动前进协程
        if (!autoAdvance && autoAdvanceCoroutine != null)
        {
            StopCoroutine(autoAdvanceCoroutine);
            autoAdvanceCoroutine = null;
        }
    }
}
