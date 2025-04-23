using UnityEngine;
using TMPro;
using Febucci.UI;  // 确保添加这个引用
using System.Collections;

public class DialogManager : MonoBehaviour
{
    // 单例实例
    private static DialogManager _instance;
    public static DialogManager Instance { get { return _instance; } }

    [Header("UI References")]
    public TextMeshProUGUI dialogText;        // 对话文本显示组件
    public GameObject dialogPanel;             // 对话面板
    private TextAnimator textAnimator;  // TextAnimator 组件引用
    private TextAnimatorPlayer textAnimatorPlayer;  // TextAnimatorPlayer 组件引用

    [Header("Dialog Settings")]
    [TextArea(3, 10)]
    public string[] dialogLines;              // 对话文本数组
    public float typingSpeed = 0.05f;         // 文字显示速度

    [Header("Auto Play Settings")]
    public bool autoPlay = true;              // 是否自动播放
    public float[] dialogDisplayTimes;        // 每个对话元素的显示时间
    public float defaultDisplayTime = 1.5f;   // 如果未设置时间，使用的默认显示时间(从3秒减少到1.5秒)
    public float timeAfterTyping = 0.5f;      // 打字完成后的额外等待时间(从1.5秒减少到0.5秒)

    public int currentLineIndex = 0;         // 当前显示的文本索引
    private bool isDialogActive = false;      // 对话是否激活
    private bool isTyping = false;            // 是否正在打字
    private string currentLine = "";          // 当前完整的文本行
    private Coroutine autoPlayCoroutine;      // 自动播放协程

    private void Awake()
    {
        // 单例初始化
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        // 如果需要在场景切换时保留，取消注释下面的行
        // DontDestroyOnLoad(gameObject);
    }

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

        // 确保dialogDisplayTimes数组长度与dialogLines一致
        if (dialogDisplayTimes == null || dialogDisplayTimes.Length != dialogLines.Length)
        {
            dialogDisplayTimes = new float[dialogLines.Length];
            for (int i = 0; i < dialogLines.Length; i++)
            {
                dialogDisplayTimes[i] = defaultDisplayTime;
            }
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
                else if (!autoPlay)
                {
                    // 只有在手动模式下才显示下一行
                    DisplayNextLine();
                }
                else
                {
                    // 在自动模式下，点击会跳过当前的等待时间，立即显示下一行
                    if (autoPlayCoroutine != null)
                    {
                        StopCoroutine(autoPlayCoroutine);
                        DisplayNextLine();
                    }
                }
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

        // 如果设置为自动播放，启动自动播放协程
        if (autoPlay)
        {
            if (autoPlayCoroutine != null)
                StopCoroutine(autoPlayCoroutine);

            autoPlayCoroutine = StartCoroutine(AutoPlayDialog());
        }
    }

    // 自动播放对话协程
    private IEnumerator AutoPlayDialog()
    {
        while (isDialogActive && currentLineIndex < dialogLines.Length)
        {
            // 等待打字效果完成
            while (isTyping)
            {
                yield return null;
            }

            // 计算当前对话应该显示的时间
            float displayTime = currentLineIndex < dialogDisplayTimes.Length ?
                                dialogDisplayTimes[currentLineIndex] : defaultDisplayTime;

            // 等待显示时间
            yield return new WaitForSeconds(displayTime + timeAfterTyping);

            // 显示下一行
            DisplayNextLine();
        }
    }

    // 显示下一行文本
    private void DisplayNextLine()
    {
        currentLineIndex++;
        if (currentLineIndex < dialogLines.Length)
        {
            DisplayLine(dialogLines[currentLineIndex]);

            // 如果是自动播放模式，重新启动自动播放协程
            if (autoPlay && autoPlayCoroutine == null)
            {
                autoPlayCoroutine = StartCoroutine(AutoPlayDialog());
            }
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
        currentLine = line;
        isTyping = true;

        if (textAnimatorPlayer != null)
        {
            // 使用 TextAnimatorPlayer 显示文本
            textAnimatorPlayer.ShowText(line);

            // 在自动播放模式下，启动检测打字完成的协程
            if (autoPlay)
            {
                StartCoroutine(CheckTypewriterCompletion());
            }
        }
        else if (textAnimator != null)
        {
            // 使用 TextAnimator 显示文本
            textAnimator.SetText(line, true);

            // 在自动播放模式下，启动检测打字完成的协程
            if (autoPlay)
            {
                StartCoroutine(CheckTypewriterCompletion());
            }
        }
        else
        {
            // 降级为普通文本显示
            dialogText.text = line;
            isTyping = false;
        }

        // 确保每次显示新行时文本都是居中的
        dialogText.alignment = TextAlignmentOptions.Center;
    }

    // 检测打字效果是否完成的协程
    private IEnumerator CheckTypewriterCompletion()
    {
        // 估算打字完成所需时间
        float estimatedTime = currentLine.Length * typingSpeed + 0.5f;

        // 等待估算的时间
        yield return new WaitForSeconds(estimatedTime);

        // 如果此时仍在打字(用户没有点击跳过)，则标记打字完成
        if (isTyping)
        {
            OnTypingComplete();
        }
    }

    // 打字效果完成后的回调
    private void OnTypingComplete()
    {
        isTyping = false;
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
            CancelInvoke("OnTypingComplete");
            isTyping = false;
        }
        else
        {
            dialogText.text = currentLine;
            isTyping = false;
        }
    }

    // 结束对话
    private void EndDialog()
    {
        isDialogActive = false;
        dialogPanel.SetActive(false);

        if (autoPlayCoroutine != null)
        {
            StopCoroutine(autoPlayCoroutine);
            autoPlayCoroutine = null;
        }
    }

    // 外部调用的显示对话方法
    public void ShowDialog(string[] lines, float[] displayTimes = null)
    {
        dialogLines = lines;

        if (displayTimes != null && displayTimes.Length == lines.Length)
        {
            dialogDisplayTimes = displayTimes;
        }
        else
        {
            dialogDisplayTimes = new float[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                dialogDisplayTimes[i] = defaultDisplayTime;
            }
        }

        StartDialog();
    }

    // 从指定索引开始显示对话
    public void ShowDialogAt(int startIndex)
    {
        if (startIndex < 0 || startIndex >= dialogLines.Length)
        {
            Debug.LogError($"Dialog index {startIndex} is out of range (0-{dialogLines.Length - 1})");
            return;
        }

        isDialogActive = true;
        currentLineIndex = startIndex;
        dialogPanel.SetActive(true);
        DisplayLine(dialogLines[currentLineIndex]);

        // 如果设置为自动播放，启动自动播放协程
        if (autoPlay)
        {
            if (autoPlayCoroutine != null)
                StopCoroutine(autoPlayCoroutine);

            autoPlayCoroutine = StartCoroutine(AutoPlayDialog());
        }
    }

    // 跳转到指定的对话行
    public void JumpToDialogLine(int lineIndex)
    {
        if (!isDialogActive)
        {
            Debug.LogWarning("Dialog is not active. Use ShowDialogAt instead.");
            return;
        }

        if (lineIndex < 0 || lineIndex >= dialogLines.Length)
        {
            Debug.LogError($"Dialog index {lineIndex} is out of range (0-{dialogLines.Length - 1})");
            return;
        }

        // 取消任何正在进行的自动播放
        if (autoPlayCoroutine != null)
        {
            StopCoroutine(autoPlayCoroutine);
        }

        // 设置新的索引并显示
        currentLineIndex = lineIndex;
        DisplayLine(dialogLines[currentLineIndex]);

        // 如果设置为自动播放，重新启动自动播放协程
        if (autoPlay)
        {
            autoPlayCoroutine = StartCoroutine(AutoPlayDialog());
        }
    }

    // 设置对话显示时间
    public void SetDialogTime(int dialogIndex, float displayTime)
    {
        if (dialogIndex >= 0 && dialogIndex < dialogDisplayTimes.Length)
        {
            dialogDisplayTimes[dialogIndex] = displayTime;
        }
    }

    // 切换自动/手动播放模式
    public void ToggleAutoPlay(bool autoPlayEnabled)
    {
        autoPlay = autoPlayEnabled;

        if (autoPlay && isDialogActive && !isTyping)
        {
            // 如果切换到自动模式且对话正在活动状态，启动自动播放
            autoPlayCoroutine = StartCoroutine(AutoPlayDialog());
        }
        else if (!autoPlay && autoPlayCoroutine != null)
        {
            // 如果切换到手动模式，停止自动播放
            StopCoroutine(autoPlayCoroutine);
            autoPlayCoroutine = null;
        }
    }
}
