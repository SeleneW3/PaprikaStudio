using UnityEngine;
using TMPro;
using Febucci.UI;
using System.Collections;

public class DialogManager : MonoBehaviour
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
    public string[] dialogLines;

    [Header("Typing Settings")]
    public float typingSpeed = 0.05f;

    [Header("Auto‑Advance Settings")]
    public bool autoAdvance = true;
    public float autoAdvanceDelay = 1.5f;

    public  int currentLineIndex;
    private int endLineIndex;
    private bool isTyping;
    private Coroutine autoAdvanceCoroutine;

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
            animatorPlayer.onTextShowed.AddListener(OnTypingComplete);
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

        currentLineIndex = startIndex;
        endLineIndex = endIndex;

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

        string line = dialogLines[currentLineIndex];
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

    private void OnTypingComplete()
    {
        isTyping = false;
        StartAutoAdvance();
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

        if (animatorPlayer != null)
        {
            animatorPlayer.SkipTypewriter();
        }
        else
        {
            dialogText.text = dialogLines[currentLineIndex];
        }
        isTyping = false;
    }

    private void EndDialog()
    {
        if (autoAdvanceCoroutine != null)
            StopCoroutine(autoAdvanceCoroutine);
        autoAdvanceCoroutine = null;

        dialogPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (animatorPlayer != null)
            animatorPlayer.onTextShowed.RemoveListener(OnTypingComplete);
    }
    #endregion
}
