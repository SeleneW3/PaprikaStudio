using UnityEngine;
using TMPro;
using Febucci.UI;  // 确保添加这个引用

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

    private int currentLineIndex = 0;         // 当前显示的文本索引
    private bool isDialogActive = false;      // 对话是否激活
    private bool isTyping = false;            // 是否正在打字
    private string currentLine = "";          // 当前完整的文本行

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
                else
                {
                    // 显示下一行
                    DisplayNextLine();
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
        currentLine = line;
        isTyping = true;

        if (textAnimatorPlayer != null)
        {
            // 使用 TextAnimatorPlayer 显示文本
            textAnimatorPlayer.ShowText(line);
            isTyping = true;
        }
        else if (textAnimator != null)
        {
            // 使用 TextAnimator 显示文本
            textAnimator.SetText(line, true);
            isTyping = true;
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
        isDialogActive = false;
        dialogPanel.SetActive(false);
    }

    // 外部调用的显示对话方法
    public void ShowDialog(string[] lines)
    {
        dialogLines = lines;
        StartDialog();
    }
}
