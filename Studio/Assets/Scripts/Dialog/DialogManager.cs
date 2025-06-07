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
    public static DialogManager Instance 
    { 
        get 
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<DialogManager>();
            }
            return _instance;
        }
    }
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
        
        // 保持DontDestroyOnLoad，确保在所有场景中可用
        DontDestroyOnLoad(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log($"[DialogManager] OnNetworkSpawn被调用，IsServer={IsServer}, IsClient={IsClient}, OwnerClientId={OwnerClientId}");
        
        // 添加场景加载事件监听
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // 场景加载完成事件处理
    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        Debug.Log($"[DialogManager] 场景 {scene.name} 加载完成");
        
        // 在新场景中查找UI元素
        FindDialogUIElements();
    }

    // 查找对话UI元素
    private void FindDialogUIElements()
    {
        Debug.Log($"[DialogManager] 尝试查找对话UI元素，IsServer={IsServer}, IsClient={IsClient}");
        
        // 查找DialogCanvas
        GameObject dialogCanvas = GameObject.Find("DialogCanvas");
        if (dialogCanvas != null)
        {
            Debug.Log("[DialogManager] 找到DialogCanvas");
            
            // 查找Panel
            Transform panelTransform = dialogCanvas.transform.Find("Panel");
            if (panelTransform != null)
            {
                dialogPanel = panelTransform.gameObject;
                Debug.Log("[DialogManager] 找到Panel");
                
                // 查找DialogText
                Transform textTransform = panelTransform.Find("DialogText");
                if (textTransform != null)
                {
                    dialogText = textTransform.GetComponent<TextMeshProUGUI>();
                    Debug.Log("[DialogManager] 找到DialogText");
                    
                    // 设置文本组件属性
                    if (dialogText != null)
                    {
                        dialogText.alignment = TextAlignmentOptions.Center;
                        dialogText.enableWordWrapping = true;
                        
                        // 获取TextAnimatorPlayer组件
                        animatorPlayer = dialogText.GetComponent<TextAnimatorPlayer>();
                        if (animatorPlayer != null)
                        {
                            Debug.Log("[DialogManager] 找到TextAnimatorPlayer组件");
                            // 确保只添加一次监听器
                            animatorPlayer.onTextShowed.RemoveListener(OnTypingComplete);
                            animatorPlayer.onTypewriterStart.RemoveListener(OnTypewriterStart);
                            
                            animatorPlayer.onTextShowed.AddListener(OnTypingComplete);
                            animatorPlayer.onTypewriterStart.AddListener(OnTypewriterStart);
                        }
                        else
                        {
                            Debug.LogWarning("[DialogManager] DialogText上没有TextAnimatorPlayer组件");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("[DialogManager] 未找到DialogText");
                }
            }
            else
            {
                Debug.LogWarning("[DialogManager] 未找到Panel");
            }
        }
        else
        {
            Debug.LogWarning("[DialogManager] 未找到DialogCanvas");
        }
        
        // 如果找到了面板，默认设置为不可见
        if (dialogPanel != null)
        {
            dialogPanel.SetActive(false);
        }
    }

    private void Start()
    {
        // 初始化对话UI
        FindDialogUIElements();
        
        if (dialogPanel != null) 
        {
            dialogPanel.SetActive(false);
        }
        
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

    public override void OnNetworkDespawn()
    {
        Debug.Log("[DialogManager] OnNetworkDespawn被调用");
        
        // 移除场景加载事件监听
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        
        base.OnNetworkDespawn();
    }
    #endregion

    #region Public API
    /// <summary>
/// 播放对话行区间 [startIndex, endIndex]，闭区间
/// 当两者相等时仅播放单句。
/// </summary>
public void PlayRange(int startIndex, int endIndex, Action onComplete = null)
{
    Debug.Log($"[DialogManager] PlayRange({startIndex}, {endIndex})被调用，IsServer={IsServer}, IsClient={IsClient}");
    
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

    // 如果是服务器，同步到所有客户端
    if (IsServer)
    {
        PlayRangeClientRpc(startIndex, endIndex);
    }

    // 保存回调函数
    dialogCompleteCallback = onComplete;

    // 本地播放对话
    PlayRangeLocal(startIndex, endIndex);
}

    // 本地播放对话的方法
private void PlayRangeLocal(int startIndex, int endIndex)
{
    Debug.Log($"[DialogManager] PlayRangeLocal({startIndex}, {endIndex})被调用");
    
    currentDialog = dialogLines;  // 使用教程对话内容
    currentLineIndex = startIndex;
    endLineIndex = endIndex;
    // 不再在这里清除回调，而是在PlayRange中设置

    // 确保找到UI元素
    FindDialogUIElements();
    
    // 检查dialogPanel是否可用
    if (dialogPanel == null)
    {
        Debug.LogError("[DialogManager] PlayRangeLocal: dialogPanel为空！");
        return;
    }
    
    // 检查dialogText是否可用
    if (dialogText == null)
    {
        Debug.LogError("[DialogManager] PlayRangeLocal: dialogText为空！");
        return;
    }
    
    Debug.Log($"[DialogManager] 显示对话面板，当前行内容: \"{currentDialog[currentLineIndex]}\"");
    dialogPanel.SetActive(true);
    DisplayCurrentLine();
}

    // 同步对话内容到所有客户端
    [ClientRpc]
    private void PlayRangeClientRpc(int startIndex, int endIndex)
    {
        Debug.Log($"[DialogManager] PlayRangeClientRpc({startIndex}, {endIndex})被调用，IsServer={IsServer}, IsClient={IsClient}");
        
        // 如果是服务器，跳过（因为服务器已经在PlayRange中调用了PlayRangeLocal）
        if (IsServer) return;
        
        // 客户端播放对话
        PlayRangeLocal(startIndex, endIndex);
    }

    public void PlayCustomDialog(string player1Text, string player2Text, Action onComplete = null)
    {
        Debug.Log($"[DialogManager] PlayCustomDialog被调用，IsServer={IsServer}, IsClient={IsClient}");
        
        // 如果是服务器，同步到所有客户端
        if (IsServer)
        {
            PlayCustomDialogClientRpc(player1Text, player2Text);
        }
        
        // 本地播放自定义对话
        PlayCustomDialogLocal(player1Text, player2Text, onComplete);
    }

    // 同步自定义对话内容到所有客户端
    [ClientRpc]
    private void PlayCustomDialogClientRpc(string player1Text, string player2Text)
    {
        Debug.Log($"[DialogManager] PlayCustomDialogClientRpc被调用，IsServer={IsServer}, IsClient={IsClient}");
        
        // 如果是服务器，跳过（因为服务器已经在PlayCustomDialog中调用了PlayCustomDialogLocal）
        if (IsServer) return;
        
        // 根据客户端ID选择显示的文本
        ulong clientId = NetworkManager.Singleton.LocalClientId;
        string textToShow = clientId == 0 ? player1Text : player2Text;
        
        // 客户端播放自定义对话
        PlayCustomDialogLocal(new string[] { textToShow }, null); // 客户端不需要回调
    }
    
    // 本地播放自定义对话的方法
    private void PlayCustomDialogLocal(string player1Text, string player2Text, Action onComplete = null)
    {
        Debug.Log($"[DialogManager] PlayCustomDialogLocal被调用");
        
        // 根据客户端ID选择显示的文本
        ulong clientId = NetworkManager.Singleton.LocalClientId;
        string textToShow = clientId == 0 ? player1Text : player2Text;
        
        // 创建临时对话数组
        currentDialog = new string[] { textToShow };
        currentLineIndex = 0;
        endLineIndex = 0;
        
        // 存储回调
        dialogCompleteCallback = onComplete;
        
        // 确保找到UI元素
        FindDialogUIElements();
        
        // 检查dialogPanel是否可用
        if (dialogPanel == null)
        {
            Debug.LogError("[DialogManager] PlayCustomDialogLocal: dialogPanel为空！");
            return;
        }
        
        // 显示对话面板
        Debug.Log($"[DialogManager] 显示自定义对话面板，内容: \"{textToShow}\"");
        dialogPanel.SetActive(true);
        DisplayCurrentLine();
    }
    
    // 重载方法，接受字符串数组
    private void PlayCustomDialogLocal(string[] dialogLines, Action onComplete = null)
    {
        Debug.Log($"[DialogManager] PlayCustomDialogLocal(string[])被调用");
        
        // 创建临时对话数组
        currentDialog = dialogLines;
        currentLineIndex = 0;
        endLineIndex = currentDialog.Length - 1;
        
        // 存储回调
        dialogCompleteCallback = onComplete;
        
        // 确保找到UI元素
        FindDialogUIElements();
        
        // 检查dialogPanel是否可用
        if (dialogPanel == null)
        {
            Debug.LogError("[DialogManager] PlayCustomDialogLocal: dialogPanel为空！");
            return;
        }
        
        // 显示对话面板
        Debug.Log($"[DialogManager] 显示自定义对话面板，内容: {string.Join(", ", currentDialog)}");
        dialogPanel.SetActive(true);
        DisplayCurrentLine();
    }
    
    // 新增：播放分段对话的方法
    public void PlaySegmentedDialog(string[] segments, Action onComplete = null)
    {
        Debug.Log($"[DialogManager] PlaySegmentedDialog被调用，IsServer={IsServer}, IsClient={IsClient}");
        
        // 将字符串数组转换为单个字符串，用特殊分隔符"|^|"分隔
        string combinedText = string.Join("|^|", segments);
        
        // 如果是服务器，同步到所有客户端
        if (IsServer)
        {
            PlaySegmentedDialogClientRpc(combinedText);
        }
        
        // 本地播放分段对话
        string[] localSegments = combinedText.Split(new string[] { "|^|" }, StringSplitOptions.None);
        PlayCustomDialogLocal(localSegments, onComplete);
    }
    
    // 同步分段对话内容到所有客户端
    [ClientRpc]
    private void PlaySegmentedDialogClientRpc(string combinedText)
    {
        Debug.Log($"[DialogManager] PlaySegmentedDialogClientRpc被调用，IsServer={IsServer}, IsClient={IsClient}");
        
        // 如果是服务器，跳过（因为服务器已经在PlaySegmentedDialog中调用了PlayCustomDialogLocal）
        if (IsServer) return;
        
        // 将组合字符串分割回字符串数组
        string[] segments = combinedText.Split(new string[] { "|^|" }, StringSplitOptions.None);
        
        // 客户端播放分段对话
        PlayCustomDialogLocal(segments, null); // 客户端不需要回调
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

    #endregion

    #region Core Logic
    private void DisplayCurrentLine()
    {
        Debug.Log($"[DialogManager] DisplayCurrentLine被调用，当前行索引: {currentLineIndex}");
        
        if (autoAdvanceCoroutine != null)
        {
            StopCoroutine(autoAdvanceCoroutine);
            autoAdvanceCoroutine = null;
        }
        
        if (currentDialog == null)
        {
            Debug.LogError("[DialogManager] DisplayCurrentLine: currentDialog为空！");
            return;
        }
        
        if (currentLineIndex < 0 || currentLineIndex >= currentDialog.Length)
        {
            Debug.LogError($"[DialogManager] DisplayCurrentLine: 无效的行索引: {currentLineIndex}，范围: 0-{currentDialog.Length - 1}");
            return;
        }

        string line = currentDialog[currentLineIndex];
        Debug.Log($"[DialogManager] 显示对话内容: \"{line}\"");
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
