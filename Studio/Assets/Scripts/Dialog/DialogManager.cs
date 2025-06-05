using UnityEngine;
using TMPro;
using Febucci.UI;
using System.Collections;
using Unity.Netcode;
using System;

public class DialogManager : NetworkBehaviour
{
    #region Singleton
    private static DialogManager _instance;
    public static DialogManager Instance => _instance;
    #endregion

    [Header("UI References")]
    public TextMeshProUGUI dialogText;
    public GameObject dialogPanel;

    [Header("Dialog Content")]
    [TextArea(3, 10)]
    public string[] dialogLines;  // 用于教程对话

    [Header("Typing Settings")]
    [Header("Sound Effects")]
    [SerializeField] private string typeSoundName = "Type";       // 打字音效名称
    [SerializeField] private string typeConfirmSoundName = "TypeConfirm"; // 打字完成音效名称
    [SerializeField] private bool useTypingSound = true;         // 是否使用打字音效

    // 对话内容定义
    

    private string[] currentDialog;
    public float typingSpeed = 0.05f;

    [Header("Auto‑Advance Settings")]
    public bool autoAdvance = true;
    public float autoAdvanceDelay = 1.5f;

    public  int currentLineIndex;
    private int endLineIndex;
    private bool isTyping;
    private Coroutine autoAdvanceCoroutine;
    private Action dialogCompleteCallback; // 添加回调字段

    private TextAnimatorPlayer animatorPlayer;

    #region Unity Life‑cycle
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        // 自动挂载到GameManager（如果你有需要）
        //GameManager.Instance.dialogManager = this;
    }

    private void Start()
    {
        if (dialogPanel != null) dialogPanel.SetActive(false);
        if (dialogText != null)
        {
            dialogText.alignment = TextAlignmentOptions.Center;
            dialogText.enableWordWrapping = true;
            animatorPlayer = dialogText.GetComponent<TextAnimatorPlayer>();
        }
        if (animatorPlayer != null)
        {
            animatorPlayer.onTextShowed.AddListener(OnTypingComplete);
            animatorPlayer.onTypewriterStart.AddListener(OnTypewriterStart);
        }
    }

    private void Update()
    {
        if (isTyping && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
        {
            CompleteLine();
        }
    }
    #endregion

    #region Public API
    /// <summary>
    /// 播放对话行区间 [startIndex, endIndex]，闭区间
    /// 当两者相等时仅播放单句。
    /// </summary>
    public void PlayRange(int startIndex, int endIndex)
    {
        if (dialogLines == null || dialogLines.Length == 0)
        {
            Debug.LogError("DialogManager: dialogLines 为空！");
            return;
        }

        // 边界检查
        if (startIndex < 0 || endIndex >= dialogLines.Length || startIndex > endIndex)
        {
            Debug.LogError($"DialogManager: 无效区间 [{startIndex}, {endIndex}]，范围 0‑{dialogLines.Length - 1}");
            return;
        }

        currentDialog = dialogLines;  // 使用教程对话内容
        currentLineIndex = startIndex;
        endLineIndex = endIndex;
        dialogCompleteCallback = null; // 清除之前的回调

        dialogPanel.SetActive(true);
        DisplayCurrentLine();
    }

    public void OnSceneUnload()
    {
        StopAllCoroutines();
        if (SoundManager.Instance != null && useTypingSound)
        {
            SoundManager.Instance.StopSFX(typeSoundName);
        }
        // ... 其他清理代码
    }

    public void PlayCustomDialog(string player1Text, string player2Text, Action onComplete = null)
    {
        // 创建临时对话数组
        currentDialog = new string[] { player1Text, player2Text };
        currentLineIndex = 0;
        endLineIndex = 1;
        
        // 存储回调
        dialogCompleteCallback = onComplete;
        
        // 显示对话面板
        dialogPanel.SetActive(true);
        DisplayCurrentLine();
    }

    #endregion

    #region Core Logic
    private void DisplayCurrentLine()
    {
        if (autoAdvanceCoroutine != null)
        {
            StopCoroutine(autoAdvanceCoroutine);
            autoAdvanceCoroutine = null;
        }

        string line = currentDialog[currentLineIndex];
        isTyping = true;

        if (animatorPlayer != null)
        {
            animatorPlayer.ShowText(line);
        }
        else
        {
            dialogText.text = line;
            isTyping = false;
            StartAutoAdvance();
        }
    }

    private void OnTypewriterStart()
    {
        if (SoundManager.Instance != null && useTypingSound)
        {
            SoundManager.Instance.PlaySFX(typeSoundName);
        }
    }

    private void OnTypingComplete()
    {
        isTyping = false;
        
        if (SoundManager.Instance != null && useTypingSound)
        {
            SoundManager.Instance.StopSFX(typeSoundName);
            StartCoroutine(PlayTypeConfirmWithDelay(0.3f));
        }
        
        StartAutoAdvance();
    }

    private IEnumerator PlayTypeConfirmWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SoundManager.Instance.PlaySFX(typeConfirmSoundName);
    }

    private void StartAutoAdvance()
    {
        if (autoAdvance)
        {
            autoAdvanceCoroutine = StartCoroutine(AutoAdvance());
        }
    }

    private IEnumerator AutoAdvance()
    {
        yield return new WaitForSeconds(autoAdvanceDelay);
        ProceedToNextLine();
    }

    private void ProceedToNextLine()
    {
        if (currentLineIndex < endLineIndex)
        {
            currentLineIndex++;
            DisplayCurrentLine();
        }
        else
        {
            EndDialog();
        }
    }

    private void CompleteLine()
    {
        if (!isTyping) return;

        if (SoundManager.Instance != null && useTypingSound)
        {
            SoundManager.Instance.StopSFX(typeSoundName);
            SoundManager.Instance.PlaySFX(typeConfirmSoundName);
        }

        if (animatorPlayer != null)
        {
            animatorPlayer.SkipTypewriter();
        }
        else
        {
            dialogText.text = currentDialog[currentLineIndex];
        }
        isTyping = false;
    }

    private void EndDialog()
    {
        if (autoAdvanceCoroutine != null)
            StopCoroutine(autoAdvanceCoroutine);
        autoAdvanceCoroutine = null;

        if (SoundManager.Instance != null && useTypingSound)
        {
            SoundManager.Instance.StopSFX(typeSoundName);
        }

        dialogPanel.SetActive(false);
        
        // 调用回调
        if (dialogCompleteCallback != null)
        {
            Action callback = dialogCompleteCallback;
            dialogCompleteCallback = null; // 清除引用
            callback.Invoke();
        }
    }

    private void OnDestroy()
    {
        if (animatorPlayer != null)
        {
            animatorPlayer.onTextShowed.RemoveListener(OnTypingComplete);
            animatorPlayer.onTypewriterStart.RemoveListener(OnTypewriterStart);
        }
        
        if (SoundManager.Instance != null && useTypingSound)
        {
            SoundManager.Instance.StopSFX(typeSoundName);
        }
    }
    #endregion
}
