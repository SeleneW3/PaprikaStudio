using UnityEngine;
using TMPro;
using Febucci.UI;
using System.Collections;
using Unity.Netcode;

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

    // 对话内容定义
    private static readonly string[] Level3A_Dialog_Player1 = new string[]
    {
        "玩家1，你现在已经欺骗了xx次",
        "在整局游戏中，你如果总共能够能达到15次欺骗...",
        "你最后会获得15分的额外加分。"
    };

    private static readonly string[] Level3A_Dialog_Player2 = new string[]
    {
        "玩家2，你现在已经合作了xx次",
        "在整局游戏中，你如果总共能够能达到12次合作...",
        "你最后会获得15分的额外加分。"
    };

    private static readonly string[] Level3B_Dialog_Player1 = new string[]
    {
        "玩家1，你现在已经合作了xx次",
        "在整局游戏中，你如果总共能够能达到12次合作...",
        "你最后会获得15分的额外加分。"
    };

    private static readonly string[] Level3B_Dialog_Player2 = new string[]
    {
        "玩家2，你现在已经欺骗了xx次",
        "在整局游戏中，你如果总共能够能达到15次欺骗...",
        "你最后会获得15分的额外加分。"
    };

//---------------------------Level4--------------------------------
    private static readonly string[] Level4A_Dialog_Player1 = new string[]
    {
        "玩家1，本轮如果双方总得分能够控制在15分以内...",
        "你会获得10分的额外加分。"
    };

    private static readonly string[] Level4A_Dialog_Player2 = new string[]
    {
        "玩家2，本轮如果双方总得分能够达到20分...",
        "你会获得10分的额外加分。"
    };

    private static readonly string[] Level4B_Dialog_Player1 = new string[]
    {
        "玩家1，本轮如果双方总得分能够达到20分...",
        "你会获得10分的额外加分。"
    }; 

    private static readonly string[] Level4B_Dialog_Player2 = new string[]
    {
        "玩家2，本轮如果双方总得分能够控制在15分以内...",
        "你会获得10分的额外加分。"
    };

//---------------------------Level5--------------------------------
    private static readonly string[] Level5A_Dialog_Player1 = new string[]
    {
        "玩家1，本轮要是你被打死了...",
        "你会获得15分作为补偿。",
        "权衡利弊吧。"
    };

    private static readonly string[] Level5A_Dialog_Player2 = new string[]
    {
        "玩家2，本轮要是对方分数比你高...",
        "你会获得15分作为补偿。",
        "权衡利弊吧。"
    };

    private static readonly string[] Level5B_Dialog_Player1 = new string[]
    {
        "玩家1，本轮要是对方分数比你高...",
        "你会获得15分作为补偿。",
        "权衡利弊吧。"

    };

    private static readonly string[] Level5B_Dialog_Player2 = new string[]
    {   
        "玩家2，本轮要是你被打死了...",
        "你会获得15分作为补偿。",
        "权衡利弊吧。"
    };

//---------------------------LevelFinal--------------------------------

    private static readonly string[] LevelFinal_Dialog_Player1 = new string[]
    {  
        "最后一轮了...",
        "打死对方,获得双方总分。",
        "别留情面。"
    };

    private static readonly string[] LevelFinal_Dialog_Player2 = new string[]
    {
        "最后一轮了...",
        "打死对方,获得双方总分。",
        "别留情面。"
    };
    

    private string[] currentDialog;
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

        currentDialog = dialogLines;  // 使用教程对话内容
        currentLineIndex = startIndex;
        endLineIndex = endIndex;

        dialogPanel.SetActive(true);
        DisplayCurrentLine();
    }

    public void PlayLevelDialog(LevelManager.Level level, bool isPlayer1)
    {
        string[] levelDialog;
        switch (level)
        {
            case LevelManager.Level.Level3A:
                levelDialog = isPlayer1 ? Level3A_Dialog_Player1 : Level3A_Dialog_Player2;
                break;
            case LevelManager.Level.Level3B:
                levelDialog = isPlayer1 ? Level3B_Dialog_Player1 : Level3B_Dialog_Player2;
                break;
            default:
                Debug.LogError($"DialogManager: 未知关卡 {level}");
                return;
        }

        currentDialog = levelDialog;
        currentLineIndex = 0;
        endLineIndex = levelDialog.Length - 1;

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
            dialogText.text = currentDialog[currentLineIndex];
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
