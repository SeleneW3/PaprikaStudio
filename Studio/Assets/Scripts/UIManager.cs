using UnityEngine;
using TMPro;
using UnityEngine.UI;  // 用于 CanvasScaler 等 UI 组件
using Unity.Netcode;
using System.Collections;
using Febucci.UI;

public class UIManager : NetworkBehaviour
{
    public enum State
    {
        Idle,           // 初始状态
        DebugText,      // 更新DebugText
        ScoreAndCoin,   // 更新分数、金币和天平
        FireAnimation,  // 开枪动画
        BulletUI,      // 更新子弹UI
        RoundText,      // 更新回合文本
        Settlement     // 结算面板
    }

    private NetworkVariable<State> currentState = new NetworkVariable<State>(State.Idle);
    private Coroutine stateCoroutine;
    private GunController gun1;
    private GunController gun2;
    private RoundManager roundManager;

    public static UIManager Instance { get; private set; }  // 单例模式

    [Header("Screen Space UI")]

    [Header("Score UI")]
    public TextMeshProUGUI player1ScoreText;
    public TextMeshProUGUI player2ScoreText;
    private TextAnimatorPlayer player1ScoreTextAnimator; // 添加TextAnimator引用
    private TextAnimatorPlayer player2ScoreTextAnimator; // 添加TextAnimator引用
    
    [Header("Score Position References")]
    public Transform player1ScoreAnchor; // 天平上的第一个空子物体
    public Transform player2ScoreAnchor; // 天平上的第二个空子物体

    [Header("Debug Text UI")]
    public TextMeshProUGUI player1DebugText;
    public TextMeshProUGUI player2DebugText;
    public Transform player1DebugAnchor; // 玩家1 debug text 显示锚点
    public Transform player2DebugAnchor; // 玩家2 debug text 显示锚点
    public Vector2 debugTextOffset = new Vector2(0, 30); // debug text 的位置偏移
    private TextAnimatorPlayer player1DebugTextAnimator; // 添加TextAnimator引用
    private TextAnimatorPlayer player2DebugTextAnimator; // 添加TextAnimator引用

    [Header("Bullets UI")]
    public TextMeshProUGUI player1BulletsText; // 显示玩家1剩余子弹的文本
    public TextMeshProUGUI player2BulletsText; // 显示玩家2剩余子弹的文本
    public Transform player1BulletsAnchor; // 玩家1子弹数显示锚点
    public Transform player2BulletsAnchor; // 玩家2子弹数显示锚点
    private TextAnimatorPlayer player1BulletsTextAnimator; // 添加TextAnimator引用
    private TextAnimatorPlayer player2BulletsTextAnimator; // 添加TextAnimator引用

    [Header("Movement Settings")]
    [Range(1f, 50f)]
    public float smoothSpeed = 15f; // 更高的平滑速度，减少滞后感
    public Vector2 scoreOffset = new Vector2(0, 50); // 位置偏移
    public Vector2 bulletsOffset = new Vector2(0, 0); // 子弹数显示的位置偏移
    public Vector2 roundOffset = new Vector2(0, 20); // 回合显示的位置偏移
    
    // 最小移动阈值，低于此值就直接设置到目标位置
    public float snapThreshold = 2.0f; 
    private Canvas parentCanvas;

    [Header("Choice Status UI")]
    public TextMeshProUGUI player1ChoiceStatusText; // 玩家1选择状态
    public TextMeshProUGUI player2ChoiceStatusText; // 玩家2选择状态
    public Transform player1ChoiceStatusAnchor; // 玩家1状态显示锚点
    public Transform player2ChoiceStatusAnchor; // 玩家2状态显示锚点
    public Vector2 choiceStatusOffset = new Vector2(0, 30); // 状态显示的位置偏移

    [Header("Debug Text Animation")]
    public float debugTextAnimDuration = 0.5f;
    public float scaleAmount = 1.5f;
    public float showDuration = 1.5f;  // 显示持续时间
    public float fadeOutDuration = 0.3f;  // 淡出动画时间
    private Vector3 debugTextOriginalScale1;
    private Vector3 debugTextOriginalScale2;

    private System.Action onDebugTextAnimationComplete;

    [Header("Cursor Settings")]
    public Texture2D defaultCursor;     // 默认鼠标图案
    public Texture2D handCardCursor;    // 在手牌上的鼠标图案
    public Texture2D selectedCardCursor; // 在已选中卡牌上的鼠标图案
    public Vector2 cursorHotspot = Vector2.zero;  // 鼠标热点位置

    [Header("World Space UI")]
    public TextMeshProUGUI levelText1;  // 玩家1的关卡显示
    public TextMeshProUGUI levelText2;  // 玩家2的关卡显示
    public TextMeshProUGUI roundText1;  // 玩家1的回合显示
    public TextMeshProUGUI roundText2;  // 玩家2的回合显示
    public TextMeshProUGUI player1TargetText;
    public TextMeshProUGUI player2TargetText;
    public TextMeshProUGUI player1TotalScoreText; // 玩家1的总分显示
    public TextMeshProUGUI player2TotalScoreText; // 玩家2的总分显示
    private TextAnimatorPlayer levelText1Animator;
    private TextAnimatorPlayer levelText2Animator;
    private TextAnimatorPlayer roundText1Animator;
    private TextAnimatorPlayer roundText2Animator;
    private TextAnimatorPlayer player1TargetTextAnimator;
    private TextAnimatorPlayer player2TargetTextAnimator;
    private TextAnimatorPlayer player1TotalScoreTextAnimator;
    private TextAnimatorPlayer player2TotalScoreTextAnimator;

    private int lastDisplayedRound = -1; // 用于跟踪上一次显示的回合数
    private int lastDisplayedTotalRounds = -1; // 用于跟踪上一次显示的总回合数

    [Header("Settlement Panel")]
    public GameObject settlementPanel;
    public Button continueButton;
    public Button exitButton;
    public TextMeshProUGUI settlementScoreText;
    private NetworkManager networkManager;
    private float lastPlayer1Score = 0f;
    private float lastPlayer2Score = 0f;

    private bool isSettlementFromDeath = false;  // 添加标记

    [Header("Bonus Text UI")]
    public TextMeshProUGUI player1BonusText; // 玩家1奖励文本
    public TextMeshProUGUI player2BonusText; // 玩家2奖励文本
    private TextAnimatorPlayer player1BonusTextAnimator; // 添加TextAnimator引用
    private TextAnimatorPlayer player2BonusTextAnimator; // 添加TextAnimator引用

    // 分数动画相关变量
    private float displayedPlayer1Score = 0f;  // 当前显示的玩家1分数
    private float displayedPlayer2Score = 0f;  // 当前显示的玩家2分数
    private float displayedPlayer1TotalScore = 0f;  // 当前显示的玩家1总分数
    private float displayedPlayer2TotalScore = 0f;  // 当前显示的玩家2总分数
    private Coroutine player1ScoreAnimation = null;  // 玩家1分数动画协程
    private Coroutine player2ScoreAnimation = null;  // 玩家2分数动画协程
    private Coroutine player1TotalScoreAnimation = null;  // 玩家1总分数动画协程
    private Coroutine player2TotalScoreAnimation = null;  // 玩家2总分数动画协程
    [Range(1f, 10f)]
    public float scoreAnimationSpeed = 7f;  // 分数变化速度，每秒增加/减少的分数

    private bool showingFinalDialog = false;  // 添加标记，表示是否正在显示最终对话

    // 添加网络变量来跟踪玩家点击状态
    public NetworkVariable<bool> player1ClickedContinue = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> player2ClickedContinue = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> player1ClickedExit = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> player2ClickedExit = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // 添加用于显示等待对方确认的文本
    public TextMeshProUGUI waitingConfirmationText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 保持UI管理器在场景切换时不被销毁
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 获取NetworkManager引用
        networkManager = NetworkManager.Singleton;

        // 获取Text Animator组件
        if (levelText1 != null)
            levelText1Animator = levelText1.GetComponent<TextAnimatorPlayer>();
        if (levelText2 != null)
            levelText2Animator = levelText2.GetComponent<TextAnimatorPlayer>();
        if (roundText1 != null)
            roundText1Animator = roundText1.GetComponent<TextAnimatorPlayer>();
        if (roundText2 != null)
            roundText2Animator = roundText2.GetComponent<TextAnimatorPlayer>();
        if (player1TargetText != null)
            player1TargetTextAnimator = player1TargetText.GetComponent<TextAnimatorPlayer>();
        if (player2TargetText != null)
            player2TargetTextAnimator = player2TargetText.GetComponent<TextAnimatorPlayer>();
        if (player1TotalScoreText != null)
            player1TotalScoreTextAnimator = player1TotalScoreText.GetComponent<TextAnimatorPlayer>();
        if (player2TotalScoreText != null)
            player2TotalScoreTextAnimator = player2TotalScoreText.GetComponent<TextAnimatorPlayer>();
        // 获取奖励文本的TextAnimator组件
        if (player1BonusText != null)
            player1BonusTextAnimator = player1BonusText.GetComponent<TextAnimatorPlayer>();
        if (player2BonusText != null)
            player2BonusTextAnimator = player2BonusText.GetComponent<TextAnimatorPlayer>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Debug.Log($"[UIManager] OnNetworkSpawn called. IsClient={IsClient}, IsServer={IsServer}, IsOwner={IsOwner}, NetworkObjectId={NetworkObjectId}");

        // 缓存引用
        roundManager = FindObjectOfType<RoundManager>();
        gun1 = GameObject.Find("Gun1")?.GetComponent<GunController>();
        gun2 = GameObject.Find("Gun2")?.GetComponent<GunController>();

        if (IsClient)
        {
            // 请求服务器初始化UI
            RequestUIInitializationServerRpc();

            // 订阅事件
            ChessLogic.OnBothChessAnimationComplete += OnChessAnimationComplete;
        }

        // 添加网络变量更改监听
        if (IsClient)
        {
            player1ClickedContinue.OnValueChanged += OnPlayerContinueStatusChanged;
            player2ClickedContinue.OnValueChanged += OnPlayerContinueStatusChanged;
            player1ClickedExit.OnValueChanged += OnPlayerExitStatusChanged;
            player2ClickedExit.OnValueChanged += OnPlayerExitStatusChanged;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (IsClient)
        {
            // 取消订阅事件
            ChessLogic.OnBothChessAnimationComplete -= OnChessAnimationComplete;

            // 移除网络变量监听
            player1ClickedContinue.OnValueChanged -= OnPlayerContinueStatusChanged;
            player2ClickedContinue.OnValueChanged -= OnPlayerContinueStatusChanged;
            player1ClickedExit.OnValueChanged -= OnPlayerExitStatusChanged;
            player2ClickedExit.OnValueChanged -= OnPlayerExitStatusChanged;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestUIInitializationServerRpc(ServerRpcParams serverRpcParams = default)
    {
        // 服务器收到请求后，通知所有客户端初始化UI
        InitializeUIClientRpc();
    }

    [ClientRpc]
    private void InitializeUIClientRpc()
    {
        Debug.Log($"[UIManager] InitializeUIClientRpc called on client {NetworkManager.Singleton.LocalClientId}");
        
        // 初始化UI
        InitializeUI();
        
        // 拉取初始状态
        PullInitialValues();
        
        // 强制更新UI可见性
        ForceUIVisibility();
    }

    private void InitializeUI()
    {
        Debug.Log("[UIManager] InitializeUI called");

        // 确保有Canvas引用
        if (parentCanvas == null)
        {
            InitializeCanvasReference();
            if (parentCanvas == null)
            {
                Debug.LogError("[UIManager] Cannot initialize UI: No Canvas found!");
                return;
            }
        }

        // 初始化显示分数变量
        if (GameManager.Instance != null && GameManager.Instance.playerComponents.Count >= 2)
        {
            displayedPlayer1Score = GameManager.Instance.playerComponents[0].point.Value;
            displayedPlayer2Score = GameManager.Instance.playerComponents[1].point.Value;
        }
        else
        {
            displayedPlayer1Score = 0f;
            displayedPlayer2Score = 0f;
        }

        // 初始化总分显示
        if (LevelManager.Instance != null)
        {
            displayedPlayer1TotalScore = LevelManager.Instance.player1TotalPoint.Value;
            displayedPlayer2TotalScore = LevelManager.Instance.player2TotalPoint.Value;
        }
        else
        {
            displayedPlayer1TotalScore = 0f;
            displayedPlayer2TotalScore = 0f;
        }

        // 获取RoundManager引用 - 移到方法开始处
        roundManager = FindObjectOfType<RoundManager>();
        if (roundManager == null)
        {
            Debug.LogWarning("[UIManager] RoundManager not found in InitializeUI!");
        }
        else
        {
            Debug.Log($"[UIManager] RoundManager found, totalRounds: {roundManager.totalRounds.Value}");
        }

        // 获取Text Animator组件
        if (levelText1 != null)
            levelText1Animator = levelText1.GetComponent<TextAnimatorPlayer>();
        if (levelText2 != null)
            levelText2Animator = levelText2.GetComponent<TextAnimatorPlayer>();
        if (roundText1 != null)
            roundText1Animator = roundText1.GetComponent<TextAnimatorPlayer>();
        if (roundText2 != null)
            roundText2Animator = roundText2.GetComponent<TextAnimatorPlayer>();
        if (player1TargetText != null)
            player1TargetTextAnimator = player1TargetText.GetComponent<TextAnimatorPlayer>();
        if (player2TargetText != null)
            player2TargetTextAnimator = player2TargetText.GetComponent<TextAnimatorPlayer>();
        if (player1TotalScoreText != null)
            player1TotalScoreTextAnimator = player1TotalScoreText.GetComponent<TextAnimatorPlayer>();
        if (player2TotalScoreText != null)
            player2TotalScoreTextAnimator = player2TotalScoreText.GetComponent<TextAnimatorPlayer>();
            
        // 获取DebugText的TextAnimator组件
        if (player1DebugText != null)
            player1DebugTextAnimator = player1DebugText.GetComponent<TextAnimatorPlayer>();
        if (player2DebugText != null)
            player2DebugTextAnimator = player2DebugText.GetComponent<TextAnimatorPlayer>();
            
        // 获取ScoreText的TextAnimator组件
        if (player1ScoreText != null)
            player1ScoreTextAnimator = player1ScoreText.GetComponent<TextAnimatorPlayer>();
        if (player2ScoreText != null)
            player2ScoreTextAnimator = player2ScoreText.GetComponent<TextAnimatorPlayer>();
            
        // 获取BulletsText的TextAnimator组件
        if (player1BulletsText != null)
            player1BulletsTextAnimator = player1BulletsText.GetComponent<TextAnimatorPlayer>();
        if (player2BulletsText != null)
            player2BulletsTextAnimator = player2BulletsText.GetComponent<TextAnimatorPlayer>();
            
        // 获取BonusText的TextAnimator组件
        if (player1BonusText != null)
            player1BonusTextAnimator = player1BonusText.GetComponent<TextAnimatorPlayer>();
        if (player2BonusText != null)
            player2BonusTextAnimator = player2BonusText.GetComponent<TextAnimatorPlayer>();

        // 初始化时隐藏奖励文本
        if (player1BonusText != null)
            player1BonusText.gameObject.SetActive(false);
        if (player2BonusText != null)
            player2BonusText.gameObject.SetActive(false);

        // 初始化时隐藏World Space回合文本
        if (roundText1 != null)
        {
            roundText1.gameObject.SetActive(false);
        }
        if (roundText2 != null)
        {
            roundText2.gameObject.SetActive(false);
        }
        
        // 获取GunController引用
        GameObject gun1Obj = GameObject.Find("Gun1");
        GameObject gun2Obj = GameObject.Find("Gun2");
        
        if (gun1Obj) gun1 = gun1Obj.GetComponent<GunController>();
        if (gun2Obj) gun2 = gun2Obj.GetComponent<GunController>();
        

                    // 初始化选择状态文本
        if (player1ChoiceStatusText != null)
        {
            player1ChoiceStatusText.gameObject.SetActive(true);
            player1ChoiceStatusText.text = "决策中...";
            player1ChoiceStatusText.color = Color.white;
        }
        
        if (player2ChoiceStatusText != null)
        {
            player2ChoiceStatusText.gameObject.SetActive(true);
            player2ChoiceStatusText.text = "决策中...";
            player2ChoiceStatusText.color = Color.white;
        }

        // 保存原始缩放值
        if (player1DebugText != null)
        {
            debugTextOriginalScale1 = player1DebugText.transform.localScale;
            player1DebugText.gameObject.SetActive(false);
        }
        if (player2DebugText != null)
        {
            debugTextOriginalScale2 = player2DebugText.transform.localScale;
            player2DebugText.gameObject.SetActive(false);
        }

        // 设置默认鼠标图案
        SetDefaultCursor();

        // 初始化World Space回合文本
        try {
            // 从RoundManager获取总回合数，如果可用
            int initialTotalRounds = 5; // 默认值
            
            if (roundManager != null)
            {
                // 直接使用RoundManager中的值，确保客户端和服务器一致
                initialTotalRounds = roundManager.totalRounds.Value;
                Debug.Log($"[UIManager] InitializeUI: 从RoundManager获取总回合数: {initialTotalRounds}");
            }
            else
            {
                // 如果RoundManager不可用，尝试从LevelManager获取
                if (LevelManager.Instance != null && LevelManager.Instance.currentMode.Value == LevelManager.Mode.Tutor)
                {
                    initialTotalRounds = 4;
                }
                Debug.Log($"[UIManager] InitializeUI: 从LevelManager获取总回合数: {initialTotalRounds}");
            }
            
            string roundInfo = $"回合：1/{initialTotalRounds}";
            
            if (roundText1 != null) 
            {
                roundText1.text = roundInfo;
                roundText1.gameObject.SetActive(true);
            }
            
            if (roundText2 != null)
            {
                roundText2.text = roundInfo;
                roundText2.gameObject.SetActive(true);
            }
            
            Debug.Log($"[UIManager] 初始化回合显示为: {roundInfo}");
        } 
        catch (System.Exception e) 
        {
            Debug.LogError($"[UIManager] 初始化回合文本时出错: {e.Message}\n{e.StackTrace}");
        }

        // 初始化Level文本
        try {
            if (LevelManager.Instance != null)
            {
                // 只有当LevelText对象存在时才更新它们
                if (levelText1 != null || levelText2 != null)
                {
                    UpdateLevelText(LevelManager.Instance.currentLevel.Value);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UIManager] 初始化Level文本时出错: {e.Message}");
        }

        // 初始化玩家总分文本
        if (player1TotalScoreText != null && player2TotalScoreText != null && LevelManager.Instance != null)
        {
            UpdatePlayerTotalScoreText();
        }

        // 强制UI可见性
        ForceUIVisibility();
    }

    public override void OnDestroy()
    {
        // 确保停止所有分数动画
        StopAllScoreAnimations();
        
        // 取消订阅事件
        if (networkManager != null)
        {
            networkManager.OnClientConnectedCallback -= (id) => {
                ForceUIVisibility();
            };
        }
        
        if (IsClient)
        {
            ChessLogic.OnBothChessAnimationComplete -= OnChessAnimationComplete;
        }
        
        base.OnDestroy();

        // 取消事件订阅
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private void OnChessAnimationComplete()
    {
        if (!IsClient) return;
        
        // 只在服务器端启动状态机
        if (IsServer)
        {
            Debug.Log("[UIManager] Starting state machine after chess animation");
            StartStateMachine();
        }
    }

    private IEnumerator PlayDebugTextAnimation()
    {
        // 确保debug text是可见的，并重置透明度
        if (player1DebugText != null)
        {
            player1DebugText.gameObject.SetActive(true);
            SetTextAlpha(player1DebugText, 1f);
        }
        
        if (player2DebugText != null)
        {
            player2DebugText.gameObject.SetActive(true);
            SetTextAlpha(player2DebugText, 1f);
        }

        // 缩放动画
        float elapsed = 0f;
        while (elapsed < debugTextAnimDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / debugTextAnimDuration;
            
            float scale = 1f + Mathf.Sin(progress * Mathf.PI) * (scaleAmount - 1f);
            
            if (player1DebugText != null)
                player1DebugText.transform.localScale = debugTextOriginalScale1 * scale;
            if (player2DebugText != null)
                player2DebugText.transform.localScale = debugTextOriginalScale2 * scale;

            yield return null;
        }

        // 确保回到原始大小
        if (player1DebugText != null)
            player1DebugText.transform.localScale = debugTextOriginalScale1;
        if (player2DebugText != null)
            player2DebugText.transform.localScale = debugTextOriginalScale2;

        // 等待显示时间
        yield return new WaitForSeconds(showDuration);

        // 淡出动画
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / fadeOutDuration);  // 从1渐变到0
            
            if (player1DebugText != null)
                SetTextAlpha(player1DebugText, alpha);
            if (player2DebugText != null)
                SetTextAlpha(player2DebugText, alpha);

            yield return null;
        }

        // 完全隐藏
        if (player1DebugText != null)
        {
            SetTextAlpha(player1DebugText, 0f);
            player1DebugText.gameObject.SetActive(false);
        }
        if (player2DebugText != null)
        {
            SetTextAlpha(player2DebugText, 0f);
            player2DebugText.gameObject.SetActive(false);
        }

        // 调用回调
        if (onDebugTextAnimationComplete != null)
        {
            onDebugTextAnimationComplete.Invoke();
            onDebugTextAnimationComplete = null;  // 清除回调
        }
    }

    // 修改UpdateDebugInfo方法，添加延迟
    public void UpdateDebugInfo(string player1Debug, string player2Debug)
    {
        if (!IsClient) return;

        // 如果是服务器，通知所有客户端更新debug text
        if (IsServer)
        {
            // 存储debug信息，稍后使用
            string p1Debug = player1Debug;
            string p2Debug = player2Debug;
            
            // 启动协程延迟1秒后更新
            StartCoroutine(DelayedUpdateDebugInfo(p1Debug, p2Debug));
        }
    }
    
    // 添加延迟更新debug信息的协程
    private IEnumerator DelayedUpdateDebugInfo(string player1Debug, string player2Debug)
    {
        // 等待1秒 
        yield return new WaitForSeconds(1.3f);
        
        // 调用ClientRpc更新所有客户端
            UpdateDebugInfoClientRpc(player1Debug, player2Debug);
    }

    [ClientRpc]
    private void UpdateDebugInfoClientRpc(string player1Debug, string player2Debug)
    {
        try
        {
            // 播放AddScore音效
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX("AddScore");
            }
            
            // 更新debug text内容并使用TextAnimator显示
            if (player1DebugText != null)
            {
                player1DebugText.gameObject.SetActive(true);
                SetTextAlpha(player1DebugText, 1f);
                
                // 使用TextAnimator显示文本
                if (player1DebugTextAnimator != null)
                {
                    player1DebugTextAnimator.ShowText(player1Debug);
                }
                else
                {
                    player1DebugText.text = player1Debug;
                }
            }
            
            if (player2DebugText != null)
            {
                player2DebugText.gameObject.SetActive(true);
                SetTextAlpha(player2DebugText, 1f);
                
                // 使用TextAnimator显示文本
                if (player2DebugTextAnimator != null)
                {
                    player2DebugTextAnimator.ShowText(player2Debug);
                }
                else
                {
                    player2DebugText.text = player2Debug;
                }
            }

            // 播放动画
            StartCoroutine(PlayDebugTextAnimation());

            // 启动状态机
            if (IsServer)
            {
                StartStateMachine();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in UpdateDebugInfoClientRpc: {e.Message}");
        }
    }

    // 修改PlayDebugTextAnimationWithCallback方法
    public void PlayDebugTextAnimationWithCallback(System.Action callback)
    {
        if (!IsClient) return;
        onDebugTextAnimationComplete = callback;
        if (IsServer)
        {
            PlayDebugTextAnimationClientRpc();
        }
    }

    [ClientRpc]
    private void PlayDebugTextAnimationClientRpc()
    {
        StartCoroutine(PlayDebugTextAnimation());
    }

    // 辅助方法：设置文本透明度
    private void SetTextAlpha(TextMeshProUGUI text, float alpha)
    {
        Color color = text.color;
        color.a = alpha;
        text.color = color;
    }

    void Update()
    {
        if (parentCanvas == null) return;

        // 只在Update中更新UI位置
        UpdateUIPositions();
    }

    void UpdateScoreText()
    {
        if (!IsClient) return;

        try
        {
            // 详细的错误检查
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("[UIManager] UpdateScoreText: GameManager.Instance is null");
                return;
            }

            if (GameManager.Instance.playerComponents == null)
            {
                Debug.LogWarning("[UIManager] UpdateScoreText: GameManager.Instance.playerComponents is null");
                return;
            }

            if (GameManager.Instance.playerComponents.Count < 2)
            {
                Debug.LogWarning($"[UIManager] UpdateScoreText: Not enough players. Current count: {GameManager.Instance.playerComponents.Count}");
                return;
            }

            // 检查玩家组件
            var player1Component = GameManager.Instance.playerComponents[0];
            var player2Component = GameManager.Instance.playerComponents[1];

            if (player1Component == null || player2Component == null)
            {
                Debug.LogWarning("[UIManager] UpdateScoreText: One or both player components are null");
                return;
            }

            if (player1Component.point == null || player2Component.point == null)
            {
                Debug.LogWarning("[UIManager] UpdateScoreText: One or both player point variables are null");
                return;
            }

            float player1Score = player1Component.point.Value;
            float player2Score = player2Component.point.Value;
            
            // 检查分数是否发生变化
            bool player1ScoreChanged = Mathf.Abs(player1Score - lastPlayer1Score) > 0.01f;
            bool player2ScoreChanged = Mathf.Abs(player2Score - lastPlayer2Score) > 0.01f;
            bool scoreChanged = player1ScoreChanged || player2ScoreChanged;
            
            // 更新Player 1的分数显示
            if (player1ScoreText != null && player1ScoreChanged)
            {
                // 如果有正在运行的动画，停止它
                if (player1ScoreAnimation != null)
                {
                    StopCoroutine(player1ScoreAnimation);
                    player1ScoreAnimation = null;
                }
                
                // 启动新的动画
                player1ScoreAnimation = StartCoroutine(AnimateScoreChange(
                    displayedPlayer1Score, 
                    player1Score, 
                    (value) =>
                    {
                        displayedPlayer1Score = value;
                        // 在累加动画过程中使用普通文本，不使用shake效果
                        string scoreText = $"玩家1: {displayedPlayer1Score}";
                        
                        // 使用TextAnimator显示文本
                        if (player1ScoreTextAnimator != null)
                        {
                            player1ScoreTextAnimator.ShowText(scoreText);
                        }
                        else
                        {
                            player1ScoreText.text = scoreText;
                        }
                    },
                    null,  // 无需额外回调
                    true   // 累加完成后播放放大特效
                ));
            }
            else if (player1ScoreText != null)
            {
                Debug.LogWarning("[UIManager] UpdateScoreText: player1ScoreText is null");
            }

            // 更新Player 2的分数显示
            if (player2ScoreText != null && player2ScoreChanged)
            {
                // 如果有正在运行的动画，停止它
                if (player2ScoreAnimation != null)
                {
                    StopCoroutine(player2ScoreAnimation);
                    player2ScoreAnimation = null;
                }
                
                // 启动新的动画
                player2ScoreAnimation = StartCoroutine(AnimateScoreChange(
                    displayedPlayer2Score, 
                    player2Score, 
                    (value) =>
                    {
                        displayedPlayer2Score = value;
                        // 在累加动画过程中使用普通文本，不使用shake效果
                        string scoreText = $"玩家2: {displayedPlayer2Score}";
                        
                        // 使用TextAnimator显示文本
                        if (player2ScoreTextAnimator != null)
                        {
                            player2ScoreTextAnimator.ShowText(scoreText);
                        }
                        else
                        {
                            player2ScoreText.text = scoreText;
                        }
                    },
                    null,  // 无需额外回调
                    true   // 累加完成后播放放大特效
                ));
            }
            else if (player2ScoreText != null)
            {
                Debug.LogWarning("[UIManager] UpdateScoreText: player2ScoreText is null");
            }

            // 如果是服务器，更新天平
            if (IsServer)
            {
                BalanceScale balanceScale = FindObjectOfType<BalanceScale>();
                if (balanceScale != null)
                {
                    // 直接设置分数差值
                    balanceScale.SetScoreDiffServerRpc(player1Score - player2Score);
                }

                // 如果分数发生变化，生成金币
                if (scoreChanged)
                {
                    Coin coin = FindObjectOfType<Coin>();
                    if (coin != null && player1ScoreAnchor != null && player2ScoreAnchor != null)
                    {
                        int p1Added = Mathf.FloorToInt(player1Score - lastPlayer1Score);
                        int p2Added = Mathf.FloorToInt(player2Score - lastPlayer2Score);
                        
                        if (p1Added > 0)
                            coin.RequestSpawnCoins(player1ScoreAnchor.position, p1Added);
                        if (p2Added > 0)
                            coin.RequestSpawnCoins(player2ScoreAnchor.position, p2Added);
                    }
                    else
                    {
                        if (coin == null) Debug.LogWarning("[UIManager] UpdateScoreText: Coin component not found");
                        if (player1ScoreAnchor == null) Debug.LogWarning("[UIManager] UpdateScoreText: player1ScoreAnchor is null");
                        if (player2ScoreAnchor == null) Debug.LogWarning("[UIManager] UpdateScoreText: player2ScoreAnchor is null");
                    }
                }
            }

            // 更新上一次的分数记录
            lastPlayer1Score = player1Score;
            lastPlayer2Score = player2Score;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UIManager] Error in UpdateScoreText: {e.Message}\nStack trace: {e.StackTrace}");
        }
    }

    void UpdateBulletsText()
    {
        // 确保我们有所有需要的组件
        if (player1BulletsText == null || player2BulletsText == null || gun1 == null || gun2 == null)
            return;
            
        // 检查当前游戏模式
        if (LevelManager.Instance != null && 
            (LevelManager.Instance.currentMode.Value == LevelManager.Mode.Tutor || 
             LevelManager.Instance.currentMode.Value == LevelManager.Mode.OnlyCard))
        {
            // 在Tutor或OnlyCard模式下隐藏子弹文本
            player1BulletsText.gameObject.SetActive(false);
            player2BulletsText.gameObject.SetActive(false);
            return;
        }
            
        // 计算已消耗的机会数（总共6次机会）
        int gun1UsedChances = 6 - gun1.remainingChances.Value; // 已消耗机会数 = 总机会 - 剩余机会
        int gun2UsedChances = 6 - gun2.remainingChances.Value;
        
        // 显示已消耗机会数/总机会数
        if (NetworkManager.Singleton != null)
        {
            // 判断当前是哪个玩家视角
            ulong localClientId = NetworkManager.Singleton.LocalClientId;
            
            if (localClientId == 0) // 玩家1视角
            {
                string bulletsText = $"<color=yellow>{gun1UsedChances}</color>/<shake a=0.05 f=0.2>6</shake>";
                
                // 使用TextAnimator显示文本
                if (player1BulletsTextAnimator != null)
                {
                    // 添加打字机效果事件监听
                    player1BulletsTextAnimator.onTypewriterStart.RemoveListener(OnBulletsTextTypewriterStart);
                    player1BulletsTextAnimator.onTextShowed.RemoveListener(OnBulletsTextTypewriterEnd);
                    player1BulletsTextAnimator.onTypewriterStart.AddListener(OnBulletsTextTypewriterStart);
                    player1BulletsTextAnimator.onTextShowed.AddListener(OnBulletsTextTypewriterEnd);
                    
                    player1BulletsText.gameObject.SetActive(true);
                    player1BulletsTextAnimator.ShowText(bulletsText);
                }
                else
                {
                    player1BulletsText.text = bulletsText;
                }
                
                // 玩家2的文本（显示在玩家1头顶）
                if (player2BulletsTextAnimator != null)
                {
                    player2BulletsText.gameObject.SetActive(true);
                    player2BulletsTextAnimator.ShowText(bulletsText);
                }
                else
                {
                    player2BulletsText.text = bulletsText;
                }
            }
            else // 玩家2视角
            {
                string bulletsText = $"<color=yellow>{gun2UsedChances}</color>/<shake a=0.05 f=0.2>6</shake>";
                
                // 使用TextAnimator显示文本
                if (player1BulletsTextAnimator != null)
                {
                    // 添加打字机效果事件监听
                    player1BulletsTextAnimator.onTypewriterStart.RemoveListener(OnBulletsTextTypewriterStart);
                    player1BulletsTextAnimator.onTextShowed.RemoveListener(OnBulletsTextTypewriterEnd);
                    player1BulletsTextAnimator.onTypewriterStart.AddListener(OnBulletsTextTypewriterStart);
                    player1BulletsTextAnimator.onTextShowed.AddListener(OnBulletsTextTypewriterEnd);
                    
                    player1BulletsText.gameObject.SetActive(true);
                    player1BulletsTextAnimator.ShowText(bulletsText);
                }
                else
                {
                    player1BulletsText.text = bulletsText;
                }
                
                // 玩家2的文本
                if (player2BulletsTextAnimator != null)
                {
                    player2BulletsText.gameObject.SetActive(true);
                    player2BulletsTextAnimator.ShowText(bulletsText);
                }
                else
                {
                    player2BulletsText.text = bulletsText;
                }
            }
        }
    }
    
    // 添加BulletsText的打字机效果事件处理方法
    private void OnBulletsTextTypewriterStart()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("TypeConfirm");
            }
    }
    
    private void OnBulletsTextTypewriterEnd()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopSFX("TypeConfirm");
        }
    }

    void UpdateUIPositions()
    {
        if (player1ScoreAnchor == null || player2ScoreAnchor == null || Camera.main == null ||
            player1ScoreText == null || player2ScoreText == null || parentCanvas == null)
        {
            return;
        }

        // 计算天平锚点的屏幕坐标
        Vector3 player1ScreenPos = Camera.main.WorldToScreenPoint(player1ScoreAnchor.position);
        Vector3 player2ScreenPos = Camera.main.WorldToScreenPoint(player2ScoreAnchor.position);
        
        // 处理玩家1分数文本位置
        UpdateTextPosition(player1ScoreText, player1ScreenPos, scoreOffset, player1ScreenPos.z < 0);
        
        // 处理玩家2分数文本位置
        UpdateTextPosition(player2ScoreText, player2ScreenPos, scoreOffset, player2ScreenPos.z < 0);

        // 更新 debug text 位置
        if (player1DebugText != null && player2DebugText != null && 
            player1DebugAnchor != null && player2DebugAnchor != null)
        {
            Vector3 debug1ScreenPos = Camera.main.WorldToScreenPoint(player1DebugAnchor.position);
            Vector3 debug2ScreenPos = Camera.main.WorldToScreenPoint(player2DebugAnchor.position);
            
            UpdateTextPosition(player1DebugText, debug1ScreenPos, debugTextOffset, debug1ScreenPos.z < 0);
            UpdateTextPosition(player2DebugText, debug2ScreenPos, debugTextOffset, debug2ScreenPos.z < 0);
        }
    
        
        // 更新子弹数文本位置（使用新的锚点）- 仅在非Tutor和非OnlyCard模式下更新
        if (player1BulletsText != null && player2BulletsText != null && 
            LevelManager.Instance != null && 
            LevelManager.Instance.currentMode.Value != LevelManager.Mode.Tutor && 
            LevelManager.Instance.currentMode.Value != LevelManager.Mode.OnlyCard)
        {
            // 使用专门的子弹锚点计算屏幕位置
            Vector3 bullets1ScreenPos = Camera.main.WorldToScreenPoint(player1BulletsAnchor.position);
            Vector3 bullets2ScreenPos = Camera.main.WorldToScreenPoint(player2BulletsAnchor.position);
            
            UpdateTextPosition(player1BulletsText, bullets1ScreenPos, bulletsOffset, bullets1ScreenPos.z < 0);
            UpdateTextPosition(player2BulletsText, bullets2ScreenPos, bulletsOffset, bullets2ScreenPos.z < 0);
        }
        
        // 更新选择状态文本位置
        if (player1ChoiceStatusText != null && player2ChoiceStatusText != null)
        {
            Vector3 status1ScreenPos = Camera.main.WorldToScreenPoint(player1ChoiceStatusAnchor.position);
            Vector3 status2ScreenPos = Camera.main.WorldToScreenPoint(player2ChoiceStatusAnchor.position);
            
            UpdateTextPosition(player1ChoiceStatusText, status1ScreenPos, choiceStatusOffset, status1ScreenPos.z < 0);
            UpdateTextPosition(player2ChoiceStatusText, status2ScreenPos, choiceStatusOffset, status2ScreenPos.z < 0);
        }
    }
    
    // 提取更新UI文本位置的通用方法
    void UpdateTextPosition(TextMeshProUGUI textElement, Vector3 screenPos, Vector2 offset, bool isBehindCamera)
    {
        if (textElement == null) return;
        
        if (isBehindCamera)
        {
            textElement.gameObject.SetActive(false);
        }
        else
        {
            textElement.gameObject.SetActive(true);
            
            // 创建目标屏幕位置（包括偏移）
            Vector3 targetScreenPos = new Vector3(
                screenPos.x + offset.x, 
                screenPos.y + offset.y,
                0);
            
            // 转换为世界坐标（适用于Screen Space - Camera）
            if (parentCanvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                // 计算从屏幕到世界的射线
                Ray ray = parentCanvas.worldCamera.ScreenPointToRay(targetScreenPos);
                
                // 计算射线与Canvas平面的交点
                float distance = parentCanvas.planeDistance;
                Vector3 worldPos = ray.origin + ray.direction * distance;
                
                // 将世界坐标应用到UI元素
                textElement.transform.position = Vector3.Lerp(
                    textElement.transform.position,
                    worldPos,
                    Time.deltaTime * smoothSpeed
                );
            }
            else // Screen Space - Overlay
            {
                // 直接使用屏幕坐标
                textElement.transform.position = Vector3.Lerp(
                    textElement.transform.position,
                    targetScreenPos,
                    Time.deltaTime * smoothSpeed
                );
            }
        }
    }

    // 初始化位置方法也做相应调整
    void ForceUIVisibility()
    {
        if (parentCanvas == null)
        {
            Debug.LogError("Cannot force UI visibility: parentCanvas is null");
            return;
        }

        // 初始化分数显示变量
        if (GameManager.Instance != null && GameManager.Instance.playerComponents.Count >= 2)
        {
            displayedPlayer1Score = GameManager.Instance.playerComponents[0].point.Value;
            displayedPlayer2Score = GameManager.Instance.playerComponents[1].point.Value;
        }
        else
        {
            displayedPlayer1Score = 0f;
            displayedPlayer2Score = 0f;
        }

        // 初始化总分显示
        if (LevelManager.Instance != null)
        {
            displayedPlayer1TotalScore = LevelManager.Instance.player1TotalPoint.Value;
            displayedPlayer2TotalScore = LevelManager.Instance.player2TotalPoint.Value;
        }
        else
        {
            displayedPlayer1TotalScore = 0f;
            displayedPlayer2TotalScore = 0f;
        }
        
        // 初始化分数文本
        if (player1ScoreText != null)
        {
            player1ScoreText.gameObject.SetActive(true);
            string scoreText = $"玩家1: {displayedPlayer1Score}";
            
            // 使用TextAnimator显示文本
            if (player1ScoreTextAnimator != null)
            {
                player1ScoreTextAnimator.ShowText(scoreText);
            }
            else
            {
                player1ScoreText.text = scoreText;
            }
            player1ScoreText.color = Color.red;
        }
        
        if (player2ScoreText != null)
        {
            player2ScoreText.gameObject.SetActive(true);
            string scoreText = $"玩家2: {displayedPlayer2Score}";
            
            // 使用TextAnimator显示文本
            if (player2ScoreTextAnimator != null)
            {
                player2ScoreTextAnimator.ShowText(scoreText);
            }
            else
            {
                player2ScoreText.text = scoreText;
            }
            player2ScoreText.color = Color.red;
        }

        // 初始化 debug text
        if (player1DebugText != null)
        {
            player1DebugText.gameObject.SetActive(true);
            string debugText = "+0";
            
            // 使用TextAnimator显示文本
            if (player1DebugTextAnimator != null)
            {
                player1DebugTextAnimator.ShowText(debugText);
            }
            else
            {
                player1DebugText.text = debugText;
            }
            player1DebugText.color = Color.white;
        }
        
        if (player2DebugText != null)
        {
            player2DebugText.gameObject.SetActive(true);
            string debugText = "+0";
            
            // 使用TextAnimator显示文本
            if (player2DebugTextAnimator != null)
            {
                player2DebugTextAnimator.ShowText(debugText);
            }
            else
            {
                player2DebugText.text = debugText;
            }
            player2DebugText.color = Color.white;
        }
        
        // 初始化子弹数文本 - 根据游戏模式决定是否显示
        if (player1BulletsText != null && player2BulletsText != null)
        {
            // 检查当前游戏模式
            if (LevelManager.Instance != null && 
                (LevelManager.Instance.currentMode.Value == LevelManager.Mode.Tutor || 
                 LevelManager.Instance.currentMode.Value == LevelManager.Mode.OnlyCard))
        {
                // 在Tutor或OnlyCard模式下隐藏子弹文本
                player1BulletsText.gameObject.SetActive(false);
                player2BulletsText.gameObject.SetActive(false);
            }
            else
            {
                // 在有枪的模式下显示子弹文本
            player1BulletsText.gameObject.SetActive(true);
                string bulletsText = "0/6";
                
                // 使用TextAnimator显示文本
                if (player1BulletsTextAnimator != null)
                {
                    player1BulletsTextAnimator.ShowText(bulletsText);
                }
                else
                {
                    player1BulletsText.text = bulletsText;
                }
            player1BulletsText.color = Color.yellow;
            
            if (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                player1BulletsText.transform.position = new Vector3(Screen.width / 2, Screen.height / 2 + 120, 0);
            }
            else // Screen Space - Camera
            {
                Vector3 screenCenter = new Vector3(Screen.width / 2, Screen.height / 2 + 120, 0);
                Ray ray = parentCanvas.worldCamera.ScreenPointToRay(screenCenter);
                float distance = parentCanvas.planeDistance;
                Vector3 worldPos = ray.origin + ray.direction * distance;
                
                player1BulletsText.transform.position = worldPos;
                }
                
                player2BulletsText.gameObject.SetActive(true);
                
                // 使用TextAnimator显示文本
                if (player2BulletsTextAnimator != null)
                {
                    player2BulletsTextAnimator.ShowText(bulletsText);
                }
                else
                {
                    player2BulletsText.text = bulletsText;
                }
                player2BulletsText.color = Color.yellow;
            }
        }
        
        // 初始化World Space回合显示文本
        if (roundText1 != null && roundText2 != null)
        {
            // 从RoundManager获取总回合数，如果可用
            int initialTotalRounds = 5; // 默认值
            
            if (roundManager != null)
            {
                // 直接使用RoundManager中的值，确保客户端和服务器一致
                initialTotalRounds = roundManager.totalRounds.Value;
                Debug.Log($"[UIManager] ForceUIVisibility: 从RoundManager获取总回合数: {initialTotalRounds}");
            }
            else
            {
                // 如果RoundManager不可用，尝试从LevelManager获取
                if (LevelManager.Instance != null && LevelManager.Instance.currentMode.Value == LevelManager.Mode.Tutor)
                {
                    initialTotalRounds = 4;
                }
                Debug.Log($"[UIManager] ForceUIVisibility: 从LevelManager获取总回合数: {initialTotalRounds}");
            }
            
            string roundInfo = $"回合：1/{initialTotalRounds}";
            roundText1.text = roundInfo;
            roundText2.text = roundInfo;
            roundText1.gameObject.SetActive(true);
            roundText2.gameObject.SetActive(true);
            
            Debug.Log($"[UIManager] 初始化回合显示为: {roundInfo}");
        }
    }

    // 修改更新回合文本的方法
    public void UpdateRoundText(int currentRound, int totalRounds = 5)
    {
        // 确保显示的回合数不超过总回合数
        int displayRound = Mathf.Min(currentRound, totalRounds);
        
        // 只在回合数真正改变时才更新文本
        if ((roundText1 != null || roundText2 != null) && 
            (displayRound != lastDisplayedRound || totalRounds != lastDisplayedTotalRounds))
        {
            lastDisplayedRound = displayRound;
            lastDisplayedTotalRounds = totalRounds;

            string roundInfo = $"回合：<wave a=0.2 f=0.8>{displayRound}</wave>/{totalRounds}";

            UpdatePlayerTargetText();
            
            // 更新玩家1的回合显示
            if (roundText1 != null)
            {
                if (roundText1Animator != null)
                {
                    roundText1.gameObject.SetActive(true);
                    roundText1Animator.ShowText(roundInfo);
                }
                else
                {
                    roundText1.text = roundInfo;
                    roundText1.gameObject.SetActive(true);
                }
            }
            
            // 更新玩家2的回合显示
            if (roundText2 != null)
            {
                if (roundText2Animator != null)
                {
                    roundText2.gameObject.SetActive(true);
                    roundText2Animator.ShowText(roundInfo);
                }
                else
                {
                    roundText2.text = roundInfo;
                    roundText2.gameObject.SetActive(true);
                }
            }
        }
    }

    // 更新选择状态的方法
    public void UpdateChoiceStatus(bool player1Selected, bool player2Selected)
    {
        if (player1ChoiceStatusText != null)
        {
            player1ChoiceStatusText.text = player1Selected ? "已选择" : "决策中...";
            player1ChoiceStatusText.color = player1Selected ? Color.green : Color.white;
        }
        
        if (player2ChoiceStatusText != null)
        {
            player2ChoiceStatusText.text = player2Selected ? "已选择" : "决策中...";
            player2ChoiceStatusText.color = player2Selected ? Color.green : Color.white;
        }
    }


    /// 设置默认鼠标图案
    public void SetDefaultCursor()
    {
        if (defaultCursor != null)
        {
            Cursor.SetCursor(defaultCursor, cursorHotspot, CursorMode.Auto);
        }
    }

    /// 设置手牌上的鼠标图案
    public void SetHandCardCursor()
    {
        if (handCardCursor != null)
        {
            Cursor.SetCursor(handCardCursor, cursorHotspot, CursorMode.Auto);
        }
    }

    /// 设置已选中卡牌上的鼠标图案
    public void SetSelectedCardCursor()
    {
        if (selectedCardCursor != null)
        {
            Cursor.SetCursor(selectedCardCursor, cursorHotspot, CursorMode.Auto);
        }
    }

    /// 重置为系统默认鼠标图案
    public void ResetToSystemCursor()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    private bool IsMouseOverClickableUI()
    {
        // 获取鼠标位置的射线
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D[] hits = Physics2D.RaycastAll(ray.origin, ray.direction);

        foreach (RaycastHit2D hit in hits)
        {
            // 检查是否击中了按钮或其他可交互的UI元素
            Button button = hit.collider?.GetComponent<Button>();
            if (button != null && button.interactable)
            {
                return true;
            }
        }

        return false;
    }

    // 修改隐藏结算面板的方法
    public void HideSettlementPanelAndReset()
    {
        if (!IsServer) return;
        HideSettlementPanelClientRpc();
    }

    [ClientRpc]
    private void ShowSettlementPanelClientRpc()
    {
        Debug.Log($"[UIManager] 收到显示结算面板的 RPC 调用，客户端ID: {NetworkManager.Singleton.LocalClientId}");
        try
        {
            // 确保面板对象存在
            if (settlementPanel == null)
            {
                Debug.LogError("[UIManager] Settlement panel is null!");
                return;
            }

            Debug.Log($"[UIManager] 结算面板引用存在，当前激活状态: {settlementPanel.activeSelf}");
            
            // 直接启动协程更新面板
            StartCoroutine(UpdateSettlementPanelWithDelay());
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error showing settlement panel: {e.Message}\nStack trace: {e.StackTrace}");
        }
    }

    private IEnumerator UpdateSettlementPanelWithDelay()
    {
        Debug.Log("[UIManager] 开始执行UpdateSettlementPanelWithDelay协程");
        
        // 等待足够长的时间确保所有状态都执行完毕
        yield return new WaitForSeconds(0.5f);

        if (settlementPanel == null)
        {
            Debug.LogError("[UIManager] Settlement panel is null!");
            yield break;
        }

        Debug.Log("[UIManager] 准备激活结算面板，当前激活状态: " + settlementPanel.activeSelf);
        settlementPanel.SetActive(true);
        Debug.Log("[UIManager] 结算面板激活后状态: " + settlementPanel.activeSelf);

        // 重置按钮点击状态
        if (IsServer)
        {
            ResetButtonClickStates();
        }
        else
        {
            ResetButtonClickStatesServerRpc();
        }

        // 启用按钮
        if (continueButton != null)
            continueButton.interactable = true;
            
        if (exitButton != null)
            exitButton.interactable = true;

        // 隐藏等待确认文本
        if (waitingConfirmationText != null)
            waitingConfirmationText.gameObject.SetActive(false);

        // 更新结算面板显示
        if (settlementScoreText != null && GameManager.Instance != null)
        {
            try 
            {
                int player1Score = (int)GameManager.Instance.playerComponents[0].point.Value;
                int player2Score = (int)GameManager.Instance.playerComponents[1].point.Value;
                float player1TotalScore = 0f;
                float player2TotalScore = 0f;
                
                // 获取总分（如果是最终关卡，使用总分；否则使用本轮分数）
                bool isFinalLevel = false;
                if (LevelManager.Instance != null)
                {
                    player1TotalScore = LevelManager.Instance.player1TotalPoint.Value;
                    player2TotalScore = LevelManager.Instance.player2TotalPoint.Value;
                    isFinalLevel = LevelManager.Instance.IsFinalLevel();
                }
                
                string winner;
                
                // 获取枪控制器引用
                GunController gun1 = GameObject.Find("Gun1")?.GetComponent<GunController>();
                GunController gun2 = GameObject.Find("Gun2")?.GetComponent<GunController>();
                
                // 修复被击中者判断逻辑
                // gun1是玩家1的枪，gun1.gameEnded.Value为true表示玩家1的枪射出了真子弹，击中了玩家2
                // gun2是玩家2的枪，gun2.gameEnded.Value为true表示玩家2的枪射出了真子弹，击中了玩家1
                bool gun1FiredRealBullet = gun1 != null && gun1.gameEnded.Value; // 玩家1的枪射出真子弹（击中玩家2）
                bool gun2FiredRealBullet = gun2 != null && gun2.gameEnded.Value; // 玩家2的枪射出真子弹（击中玩家1）
                
                string resultMessage = "";
                
                if (gun1FiredRealBullet && gun2FiredRealBullet)
                {
                    // 两名玩家都被击中
                    winner = "无人";
                    isSettlementFromDeath = true;
                    resultMessage = "双方都被击中！";
                    Debug.Log("[UIManager] Game ended with both players shot, no winner");
                }
                else if (gun2FiredRealBullet) // 玩家2的枪射出真子弹，击中了玩家1
                {
                    // 玩家1被击中，玩家2获胜
                    winner = "玩家2";
                    isSettlementFromDeath = true;
                    resultMessage = "玩家2击中了玩家1！";
                    Debug.Log("[UIManager] Game ended due to Player 1 being shot by Player 2");
                }
                else if (gun1FiredRealBullet) // 玩家1的枪射出真子弹，击中了玩家2
                {
                    // 玩家2被击中，玩家1获胜
                    winner = "玩家1";
                    isSettlementFromDeath = true;
                    resultMessage = "玩家1击中了玩家2！";
                    Debug.Log("[UIManager] Game ended due to Player 2 being shot by Player 1");
                }
                else
                {
                    // 没有玩家被击中，比较分数
                    if (isFinalLevel)
                    {
                        // 最终关卡使用总分比较
                        if (player1TotalScore > player2TotalScore)
                        {
                            winner = "玩家1";
                        }
                        else if (player2TotalScore > player1TotalScore)
                        {
                            winner = "玩家2";
                        }
                        else
                        {
                            winner = "无人"; // 平局
                        }
                    }
                    else
                    {
                        // 非最终关卡使用本轮分数比较
                        if (player1Score > player2Score)
                        {
                            winner = "玩家1";
                        }
                        else if (player2Score > player1Score)
                        {
                            winner = "玩家2";
                        }
                        else
                        {
                            winner = "无人"; // 平局
                        }
                    }
                    isSettlementFromDeath = false;
                    resultMessage = $"本局{winner}胜利！";
                    Debug.Log("[UIManager] Game ended normally, winner: " + winner);
                }
                
                // 根据是否是最终关卡，显示不同的结算面板内容
                if (isFinalLevel)
                {
                    // 在最终关卡显示总分
                    settlementScoreText.text = $"游戏结束！\n\n" +
                                              $"玩家1最终金币: {player1TotalScore}\n" +
                                              $"玩家2最终金币: {player2TotalScore}\n\n" +
                                              $"{resultMessage}\n\n" +
                                              "是否要继续游戏？\n\n" +
                                              "注意：继续或退出需要双方都确认";
                    
                    Debug.Log($"[UIManager] 最终结算面板文本已更新 - P1总分: {player1TotalScore}, P2总分: {player2TotalScore}, Winner: {winner}");
                }
                else
                {
                    // 在非最终关卡显示本轮分数
                    settlementScoreText.text = $"本局游戏结束\n\n" +
                                              $"玩家1: {player1Score}\n" +
                                              $"玩家2: {player2Score}\n\n" +
                                              $"{resultMessage}\n\n" +
                                              "玩家们，要继续加大赌注吗？\n\n" +
                                              "注意：继续或退出需要双方都确认";
                    
                    Debug.Log($"[UIManager] 结算面板文本已更新 - P1: {player1Score}, P2: {player2Score}, Winner: {winner}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UIManager] Error updating settlement panel text: {e.Message}");
            }
        }
        else
        {
            Debug.LogError($"[UIManager] Settlement score text or GameManager is null! ScoreText存在: {(settlementScoreText != null)}, GameManager存在: {(GameManager.Instance != null)}");
        }
    }

    // 修改继续按钮的点击处理方法
    public void OnContinueButtonClick()
    {
        if (!IsClient) return;

        // 播放按钮点击音效
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("ButtonClick");
        }

        // 如果是因为死亡导致的结算，调用 CancelEffect
        if (isSettlementFromDeath)
        {
            HitScreen hitScreen = FindObjectOfType<HitScreen>();
            if (hitScreen != null)
            {
                hitScreen.CancelEffect();
            }
        }

        // 获取本地玩家ID和当前点击状态
        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        bool currentStatus = localClientId == 0 ? player1ClickedContinue.Value : player2ClickedContinue.Value;
        
        // 更新按钮状态 - 切换选中状态
        PlayerClickedContinueServerRpc(localClientId, !currentStatus);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestLoadNextSceneServerRpc(ServerRpcParams serverRpcParams = default)
    {
        // 服务器收到客户端请求后，执行场景加载
        LoadNextSceneServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void LoadNextSceneServerRpc()
    {
        // 更新解锁的关卡
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.UpdateUnlockedLevelServerRpc();
        }
        
        // 在服务器端执行场景加载
        LoadNextSceneClientRpc();
    }

    [ClientRpc]
    private void LoadNextSceneClientRpc()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadScene("LevelScene");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestPlayFinalDialogServerRpc(ServerRpcParams serverRpcParams = default)
    {
        // 服务器收到客户端请求后，执行播放最终对话
        PlayFinalDialogServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayFinalDialogServerRpc()
    {
        // 标记正在显示最终对话
        showingFinalDialog = true;
        
        // 通知所有客户端播放最终对话
        PlayFinalDialogClientRpc();
    }

    [ClientRpc]
    private void PlayFinalDialogClientRpc()
    {
        // 标记正在显示最终对话
        showingFinalDialog = true;
        
        // 播放最终对话
        if (DialogManager.Instance != null)
        {
            Debug.Log("[UIManager] 播放最终对话");
            
            // 播放ID为35-40的对话
            DialogManager.Instance.PlayRange(36, 38, OnFinalDialogComplete);
        }
        else
        {
            Debug.LogError("[UIManager] DialogManager实例不存在，无法播放最终对话");
            // 如果找不到DialogManager，直接退出游戏
            ExitGame();
        }
    }

    // 最终对话完成后的回调
    private void OnFinalDialogComplete()
    {
        Debug.Log("[UIManager] 最终对话播放完成，准备退出游戏");
        
        // 延迟2秒后退出游戏，给玩家时间阅读最后的对话
        StartCoroutine(DelayedExit(2.0f));
    }

    private IEnumerator DelayedExit(float delay)
    {
        yield return new WaitForSeconds(delay);
        ExitGame();
    }

    // 退出游戏
    private void ExitGame()
    {
        Debug.Log("[UIManager] 退出游戏");
        Application.Quit();
        
        // 在编辑器中，以下代码会在运行时关闭Play模式
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    private void PullInitialValues()
    {
        if (!IsClient) return;

        Debug.Log("[UIManager] PullInitialValues called");
        Debug.Log($"[UIManager] Initial values - GameManager exists: {GameManager.Instance != null}, " +
                 $"Players count: {GameManager.Instance?.playerComponents.Count}");

        // 更新分数
        UpdateScoreText();

        // 更新子弹 - 根据游戏模式决定是否显示
        if (LevelManager.Instance == null || 
            (LevelManager.Instance.currentMode.Value != LevelManager.Mode.Tutor && 
             LevelManager.Instance.currentMode.Value != LevelManager.Mode.OnlyCard))
        {
        UpdateBulletsText();
        }
        else
        {
            // 在Tutor或OnlyCard模式下隐藏子弹文本
            if (player1BulletsText != null)
                player1BulletsText.gameObject.SetActive(false);
            if (player2BulletsText != null)
                player2BulletsText.gameObject.SetActive(false);
        }

        // 更新回合
        if (roundManager != null)
        {
            // 确保使用正确的总回合数
            int totalRounds = roundManager.totalRounds.Value;
            int currentRound = roundManager.currentRound.Value;
            UpdateRoundText(currentRound, totalRounds);
            Debug.Log($"[UIManager] PullInitialValues: 回合更新为 {currentRound}/{totalRounds}, 客户端ID: {NetworkManager.Singleton.LocalClientId}");
        }
        else
        {
            Debug.LogError("[UIManager] RoundManager is null in PullInitialValues! 尝试重新获取");
            roundManager = FindObjectOfType<RoundManager>();
            if (roundManager != null)
            {
                int totalRounds = roundManager.totalRounds.Value;
                int currentRound = roundManager.currentRound.Value;
                UpdateRoundText(currentRound, totalRounds);
                Debug.Log($"[UIManager] PullInitialValues: 重新获取后回合更新为 {currentRound}/{totalRounds}");
            }
            else
            {
                Debug.LogError("[UIManager] 重新获取RoundManager仍然失败!");
            }
        }

        // 隐藏结算面板
        if (settlementPanel != null)
        {
            settlementPanel.SetActive(false);
            Debug.Log("[UIManager] Settlement panel hidden");
        }
        else
        {
            Debug.LogError("[UIManager] Settlement panel is null!");
        }

        // 更新关卡显示
        if (LevelManager.Instance != null)
        {
            UpdateLevelText(LevelManager.Instance.currentLevel.Value);
        }

        // 更新玩家总分和统计数据
        UpdatePlayerTotalScoreText();
        UpdatePlayerStatsText();
    }

    // 添加回合文本更新方法
    public void RequestRoundTextUpdate(int currentRound, int totalRounds)
    {
        if (!IsServer) return;
        UpdateRoundTextClientRpc(currentRound, totalRounds);
    }

    // 添加事件处理方法
    private void OnRoundText1TypewriterStart()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("Type");
        }
    }
    
    private void OnRoundText1TextShowed()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopSFX("Type");
        }
    }
    
    [ClientRpc]
    private void UpdateRoundTextClientRpc(int currentRound, int totalRounds)
    {
        // 确保显示的回合数不超过总回合数
        int displayRound = Mathf.Min(currentRound, totalRounds);
        string roundInfo = $"回合：<wave a=0.2 f=0.8>{displayRound}</wave>/{totalRounds}";
        UpdatePlayerTargetTextClientRpc();
        
        Debug.Log($"[UIManager] UpdateRoundTextClientRpc: 更新回合显示为 {roundInfo}, 客户端ID: {NetworkManager.Singleton.LocalClientId}");
        
        if (roundText1 != null)
        {
            if (roundText1Animator != null)
            {
                // 移除之前可能添加的监听器，避免重复
                roundText1Animator.onTypewriterStart.RemoveListener(OnRoundText1TypewriterStart);
                roundText1Animator.onTextShowed.RemoveListener(OnRoundText1TextShowed);
                
                // 添加新的监听器
                roundText1Animator.onTypewriterStart.AddListener(OnRoundText1TypewriterStart);
                roundText1Animator.onTextShowed.AddListener(OnRoundText1TextShowed);
                
                roundText1.gameObject.SetActive(true);
                
                // 不需要在这里直接播放音效，因为onTypewriterStart事件会触发播放
                // 音效将在打字效果开始时通过事件监听器播放
                
                roundText1Animator.ShowText(roundInfo);
            }
            else
            {
                roundText1.text = roundInfo;
                roundText1.gameObject.SetActive(true);
            }
        }
        
        if (roundText2 != null)
        {
            if (roundText2Animator != null)
            {
                // 第二个文本不需要播放音效，因为它们是同步的
                roundText2.gameObject.SetActive(true);
                roundText2Animator.ShowText(roundInfo);
            }
            else
            {
                roundText2.text = roundInfo;
                roundText2.gameObject.SetActive(true);
            }
        }
    }

    private void InitializeCanvasReference()
    {
        // 首先尝试获取父级Canvas
        parentCanvas = GetComponentInParent<Canvas>();
        
        // 如果在父级找不到，尝试在场景中查找
        if (parentCanvas == null)
        {
            parentCanvas = FindObjectOfType<Canvas>();
        }
        
        // 如果还是找不到，在当前对象所在的层级查找
        if (parentCanvas == null)
        {
            var canvasObj = GameObject.Find("Canvas");
            if (canvasObj != null)
            {
                parentCanvas = canvasObj.GetComponent<Canvas>();
            }
        }

        if (parentCanvas != null)
        {
            Debug.Log($"[UIManager] Canvas found with render mode: {parentCanvas.renderMode}");
            parentCanvas.sortingOrder = 100;
        }
        else
        {
            Debug.LogError("[UIManager] No Canvas found in the scene! UI elements will not be visible!");
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"[UIManager] Client {clientId} connected");
        
        // 当新客户端连接时，请求UI初始化
        if (IsClient)
        {
            RequestUIInitializationServerRpc();
        }
    }

    // 状态转换方法
    private void TransitionToNextState()
    {
        if (!IsServer) return;

        State nextState;

        // 当前状态是ScoreAndCoin时，检查是否有人可以开枪
        if (currentState.Value == State.ScoreAndCoin)
        {
            // 确保引用已初始化
            if (roundManager == null) roundManager = FindObjectOfType<RoundManager>();
            
            // 检查是否有人可以开枪
            bool anyoneCanFire = false;
            if (roundManager != null)
            {
                anyoneCanFire = roundManager.player1CanFire.Value || roundManager.player2CanFire.Value;
            }
            
            // 如果没人可以开枪，直接跳到BulletUI状态
            nextState = anyoneCanFire ? State.FireAnimation : State.BulletUI;
            
            Debug.Log($"[UIManager] State transition from ScoreAndCoin: anyoneCanFire={anyoneCanFire}, nextState={nextState}");
        }
        else
        {
            // 其他正常状态转换
            nextState = currentState.Value switch
            {
                State.Idle => State.DebugText,
                State.DebugText => State.ScoreAndCoin,
                State.FireAnimation => State.BulletUI,
                State.BulletUI => State.RoundText,
                State.RoundText => State.Settlement,
                State.Settlement => State.Idle,
                _ => State.Idle
            };
        }

        currentState.Value = nextState;
        ExecuteStateLogicClientRpc();
    }

    [ClientRpc]
    private void ExecuteStateLogicClientRpc()
    {
        if (stateCoroutine != null)
        {
            StopCoroutine(stateCoroutine);
        }
        stateCoroutine = StartCoroutine(ExecuteStateLogic());
    }

    private IEnumerator ExecuteStateLogic()
    {
        float waitTime = currentState.Value switch
        {
            State.Idle => 1.2f,
            State.DebugText => 0.5f,
            State.ScoreAndCoin => 1.2f,
            State.FireAnimation => 2.5f,
            State.BulletUI => 0.5f,
            State.RoundText => 0.5f,
            State.Settlement => 0.5f,
            _ => 0f
        };

        // Debug.Log($"[UIManager State Machine] Entering state: {currentState.Value}, Wait time: {waitTime}s");
        
        // 在状态机开始时获取引用
        if (gun1 == null) gun1 = GameObject.Find("Gun1")?.GetComponent<GunController>();
        if (gun2 == null) gun2 = GameObject.Find("Gun2")?.GetComponent<GunController>();
        if (roundManager == null) roundManager = FindObjectOfType<RoundManager>();
        
        switch (currentState.Value)
        {
            case State.DebugText:
                // Debug.Log("[UIManager State Machine] DebugText: Starting debug text animation");
                break;
            case State.ScoreAndCoin:
                // Debug.Log("[UIManager State Machine] ScoreAndCoin: Updating score display");
                UpdateScoreText();
                // 更新玩家总分
                UpdatePlayerTotalScoreText();
                break;
            case State.FireAnimation:
                //Debug.Log("[UIManager State Machine] FireAnimation: Checking and executing gun firing");
                if (roundManager != null)
                {
                    // 检查并执行开枪
                    if (roundManager.player1CanFire.Value && gun1 != null)
                    {
                        gun1.FireGun();
                        roundManager.player1CanFire.Value = false;
                    }
                    if (roundManager.player2CanFire.Value && gun2 != null)
                    {
                        gun2.FireGun();
                        roundManager.player2CanFire.Value = false;
                    }
                }
                break;
            case State.BulletUI:
                //Debug.Log("[UIManager State Machine] BulletUI: Updating bullet count display");
                // 只在非Tutor和非OnlyCard模式下更新子弹UI
                if (LevelManager.Instance == null || 
                    (LevelManager.Instance.currentMode.Value != LevelManager.Mode.Tutor && 
                     LevelManager.Instance.currentMode.Value != LevelManager.Mode.OnlyCard))
                {
                UpdateBulletsText();
                }
                break;
            case State.RoundText:
                //Debug.Log("[UIManager State Machine] RoundText: Updating round information");
                if (roundManager != null)
                {
                    // 确保这里正确地更新回合显示，因为RoundManager不再直接更新UI
                    UpdateRoundText(roundManager.currentRound.Value, roundManager.totalRounds.Value);
                    Debug.Log($"[UIManager State Machine] Round updated to: {roundManager.currentRound.Value}/{roundManager.totalRounds.Value}");
                    
                    // 更新玩家统计数据
                    UpdatePlayerStatsText();
                }
                break;
            case State.Settlement:
                //Debug.Log("[UIManager State Machine] Settlement: Checking settlement conditions");
                bool shouldShowSettlement = false;
                
                // 检查是否有玩家被枪打死
                bool gun1FiredRealBullet = gun1 != null && gun1.gameEnded.Value; // 玩家1的枪射出真子弹（击中玩家2）
                bool gun2FiredRealBullet = gun2 != null && gun2.gameEnded.Value; // 玩家2的枪射出真子弹（击中玩家1）
                
                if (gun1FiredRealBullet || gun2FiredRealBullet)
                {
                    //Debug.Log("[UIManager State Machine] Settlement: Game ended due to gunshot");
                    shouldShowSettlement = true;
                    isSettlementFromDeath = true;
                }
                // 再检查是否达到总回合数
                else if (roundManager != null && roundManager.currentRound.Value > roundManager.totalRounds.Value)
                {
                    //Debug.Log($"[UIManager State Machine] Settlement: Game ended due to round limit. Current round: {roundManager.currentRound.Value}, Total rounds: {roundManager.totalRounds.Value}");
                    shouldShowSettlement = true;
                    isSettlementFromDeath = false;
                }
                else
                {
                    //Debug.Log($"[UIManager State Machine] Settlement: No end condition met. Current round: {roundManager?.currentRound.Value}, Total rounds: {roundManager?.totalRounds.Value}");
                }

                if (shouldShowSettlement && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                {
                    //Debug.Log("[UIManager State Machine] Settlement: Starting DelayShowSettlement coroutine");
                    StartCoroutine(DelayShowSettlement());
                }
                break;
        }

        yield return new WaitForSeconds(waitTime);
        //Debug.Log($"[UIManager State Machine] Completed state: {currentState.Value}");

        if (IsServer)
        {
            if (currentState.Value != State.Settlement)
            {
                //Debug.Log($"[UIManager State Machine] Transitioning from {currentState.Value} to next state");
                TransitionToNextState();
            }
            else if (roundManager != null && roundManager.currentRound.Value < roundManager.totalRounds.Value)
            {
                //Debug.Log("[UIManager State Machine] Resetting state machine for next round");
                ResetStateMachine();
            }
        }
    }

    // 添加延迟显示结算面板的协程
    private IEnumerator DelayShowSettlement()
    {
        //Debug.Log("[UIManager] DelayShowSettlement: 等待1秒后显示结算面板");
        yield return new WaitForSeconds(1f);
        ShowSettlementPanel();
    }
    
    // 显示结算面板的方法
    public void ShowSettlementPanel()
    {
        //Debug.Log("[UIManager] ShowSettlementPanel: 准备显示结算面板");
        if (!IsServer) 
        {
            //Debug.LogWarning("[UIManager] ShowSettlementPanel: 非服务器端调用，已忽略");
            return;
        }
        ShowSettlementPanelClientRpc();
    }

    public void StartStateMachine()
    {
        if (IsServer)
        {
            currentState.Value = State.Idle;
            TransitionToNextState();
        }
    }

    public void ResetStateMachine()
    {
        if (IsServer)
        {
            currentState.Value = State.Idle;
        }
    }

    public bool IsStateMachineRunning()
    {
        return currentState.Value != State.Idle;
    }

    [ClientRpc]
    private void HideSettlementPanelClientRpc()
    {
        Debug.Log("[UIManager] 隐藏结算面板，客户端ID: " + NetworkManager.Singleton.LocalClientId);
        
        // 在所有客户端都隐藏结算面板
        if (settlementPanel != null)
        {
            settlementPanel.SetActive(false);
        }

        // 只在服务器端执行重置逻辑
        if (IsServer)
        {
            // 重置回合和分数
            if (roundManager != null)
            {
                roundManager.ResetRound();
                
                // 重置玩家分数
                roundManager.player1.point.Value = 0f;
                roundManager.player2.point.Value = 0f;
            }
            
            // 重置游戏状态
            if (GameManager.Instance != null)
            {
                GameManager.Instance.currentGameState = GameManager.GameState.Ready;
            }

            // 重置状态机
            ResetStateMachine();
        }
    }
    public void StartRoundSettlementUI()
    {
        if (IsServer)
        {
            currentState.Value = State.Idle;
            TransitionToNextState();
        }
    }

    public void OnExitButtonClick()
    {
        if (!IsClient) return;

        // 播放按钮点击音效
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("ButtonClick");
        }

        // 获取本地玩家ID和当前点击状态
        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        bool currentStatus = localClientId == 0 ? player1ClickedExit.Value : player2ClickedExit.Value;
        
        // 更新按钮状态 - 切换选中状态
        PlayerClickedExitServerRpc(localClientId, !currentStatus);
    }

    public void UpdatePlayerTargetText()
    {
        UpdatePlayerStatsText();
    }

    [ClientRpc]
    public void UpdatePlayerTargetTextClientRpc()
    {
        UpdatePlayerStatsText();
    }

    // 添加更新关卡文本的方法
    public void UpdateLevelText(LevelManager.Level level)
    {
        try
        {
            string levelNumber = "";
            
            // 确定关卡数字
            switch (level)
            {
                case LevelManager.Level.Tutorial:
                    levelNumber = "零";
                    break;
                case LevelManager.Level.Level1:
                    levelNumber = "一";
                    break;
                case LevelManager.Level.Level2:
                    levelNumber = "二";
                    break;
                case LevelManager.Level.Level3A:
                case LevelManager.Level.Level3B:
                    levelNumber = "三";
                    break;
                case LevelManager.Level.Level4A:
                case LevelManager.Level.Level4B:
                case LevelManager.Level.Level4C:
                    levelNumber = "四";
                    break;
                case LevelManager.Level.Level5A:
                case LevelManager.Level.Level5B:
                    levelNumber = "五";
                    break;
                case LevelManager.Level.Level6A:
                case LevelManager.Level.Level6B:
                    levelNumber = "六";
                    break;
            }

            string levelInfo = $"关卡<wave a=0.2 f=0.8>{levelNumber}</wave>";

            // 更新玩家1的关卡显示
            if (levelText1 != null)
            {
                if (levelText1Animator != null)
                {
                    levelText1.gameObject.SetActive(true);
                    levelText1Animator.ShowText(levelInfo);
                }
                else
                {
                    levelText1.text = levelInfo;
                    levelText1.gameObject.SetActive(true);
                }
            }
            
            // 更新玩家2的关卡显示
            if (levelText2 != null)
            {
                if (levelText2Animator != null)
                {
                    levelText2.gameObject.SetActive(true);
                    levelText2Animator.ShowText(levelInfo);
                }
                else
                {
                    levelText2.text = levelInfo;
                    levelText2.gameObject.SetActive(true);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UIManager] 更新关卡文本时出错: {e.Message}");
        }
    }

    // 添加更新玩家总分的方法
    public void UpdatePlayerTotalScoreText()
    {
        if (player1TotalScoreText == null || player2TotalScoreText == null || LevelManager.Instance == null) return;

        float player1TotalScore = LevelManager.Instance.player1TotalPoint.Value;
        float player2TotalScore = LevelManager.Instance.player2TotalPoint.Value;

        Debug.Log($"[UIManager] 更新玩家总分：玩家1={player1TotalScore}，玩家2={player2TotalScore}，当前显示：玩家1={displayedPlayer1TotalScore}，玩家2={displayedPlayer2TotalScore}");

        // 检查分数是否发生变化
        bool player1TotalScoreChanged = Mathf.Abs(player1TotalScore - displayedPlayer1TotalScore) > 0.01f;
        bool player2TotalScoreChanged = Mathf.Abs(player2TotalScore - displayedPlayer2TotalScore) > 0.01f;
        
        // 更新玩家1的总分显示
        if (player1TotalScoreChanged)
        {
            // 如果有正在运行的动画，停止它
            if (player1TotalScoreAnimation != null)
            {
                StopCoroutine(player1TotalScoreAnimation);
                player1TotalScoreAnimation = null;
            }
            
            // 播放得分音效
            if (SoundManager.Instance != null && player1TotalScore > displayedPlayer1TotalScore)
            {
                SoundManager.Instance.PlaySFX("AddScore");
            }
            
            // 启动新的动画
            player1TotalScoreAnimation = StartCoroutine(AnimateScoreChange(
                displayedPlayer1TotalScore, 
                player1TotalScore, 
                (value) =>
                {
                    displayedPlayer1TotalScore = value;
                    // 在累加动画过程中使用普通文本，不使用shake效果
                    string scoreText = $"金币数：{displayedPlayer1TotalScore}";
                    
                    // 使用TextAnimator显示文本
                    if (player1TotalScoreTextAnimator != null)
                    {
                        player1TotalScoreText.gameObject.SetActive(true);
                        player1TotalScoreTextAnimator.ShowText(scoreText);
                    }
                    else
                    {
                        player1TotalScoreText.text = scoreText;
                        player1TotalScoreText.gameObject.SetActive(true);
                    }
                },
                null,  // 无需额外回调
                true   // 累加完成后播放放大特效
            ));
        }
        
        // 更新玩家2的总分显示
        if (player2TotalScoreChanged)
        {
            // 如果有正在运行的动画，停止它
            if (player2TotalScoreAnimation != null)
            {
                StopCoroutine(player2TotalScoreAnimation);
                player2TotalScoreAnimation = null;
            }
            
            // 播放得分音效
            if (SoundManager.Instance != null && player2TotalScore > displayedPlayer2TotalScore)
            {
                SoundManager.Instance.PlaySFX("AddScore");
            }
            
            // 启动新的动画
            player2TotalScoreAnimation = StartCoroutine(AnimateScoreChange(
                displayedPlayer2TotalScore, 
                player2TotalScore, 
                (value) =>
                {
                    displayedPlayer2TotalScore = value;
                    // 在累加动画过程中使用普通文本，不使用shake效果
                    string scoreText = $"金币数：{displayedPlayer2TotalScore}";
                    
                    // 使用TextAnimator显示文本
                    if (player2TotalScoreTextAnimator != null)
                    {
                        player2TotalScoreText.gameObject.SetActive(true);
                        player2TotalScoreTextAnimator.ShowText(scoreText);
                    }
                    else
                    {
                        player2TotalScoreText.text = scoreText;
                        player2TotalScoreText.gameObject.SetActive(true);
                    }
                },
                null,  // 无需额外回调
                true   // 累加完成后播放放大特效
            ));
        }
    }

    // 修改UpdatePlayerTargetText方法以显示玩家统计数据
    public void UpdatePlayerStatsText()
    {
        if (player1TargetText == null || player2TargetText == null || LevelManager.Instance == null) return;

        // 获取玩家欺骗和合作次数
        int p1CoopCount = LevelManager.Instance.player1CoopTimes.Value;
        int p1CheatCount = LevelManager.Instance.player1CheatTimes.Value;
        int p2CoopCount = LevelManager.Instance.player2CoopTimes.Value;
        int p2CheatCount = LevelManager.Instance.player2CheatTimes.Value;

        // 获取玩家身份
        LevelManager.PlayerIdentity p1Identity = LevelManager.Instance.player1Identity.Value;
        LevelManager.PlayerIdentity p2Identity = LevelManager.Instance.player2Identity.Value;

        // 准备显示文本
        string player1StatsInfo = $"合作：{p1CoopCount} | 欺骗：{p1CheatCount}";
        string player2StatsInfo = $"合作：{p2CoopCount} | 欺骗：{p2CheatCount}";

        // 根据游戏模式决定是否显示统计数据
        if (LevelManager.Instance.currentMode.Value == LevelManager.Mode.Tutor)
        {
            player1StatsInfo = "...";
            player2StatsInfo = "...";
        }

        // 更新玩家1的统计显示
        if (player1TargetTextAnimator != null)
        {
            player1TargetText.gameObject.SetActive(true);
            player1TargetTextAnimator.ShowText(player1StatsInfo);
        }
        else
        {
            player1TargetText.text = player1StatsInfo;
            player1TargetText.gameObject.SetActive(true);
        }
        
        // 更新玩家2的统计显示
        if (player2TargetTextAnimator != null)
        {
            player2TargetText.gameObject.SetActive(true);
            player2TargetTextAnimator.ShowText(player2StatsInfo);
        }
        else
        {
            player2TargetText.text = player2StatsInfo;
            player2TargetText.gameObject.SetActive(true);
        }
    }

    // 添加更新关卡文本的RPC方法
    [ClientRpc]
    public void UpdateLevelTextClientRpc(LevelManager.Level level)
    {
        UpdateLevelText(level);
    }

    // 添加更新玩家总分的RPC方法
    [ClientRpc]
    public void UpdatePlayerTotalScoreTextClientRpc()
    {
        UpdatePlayerTotalScoreText();
    }

    // 显示奖励文本
    public void ShowBonusText(string player1Text, string player2Text)
    {
        if (!IsClient) return;
        
        Debug.Log($"[UIManager] 显示奖励文本：玩家1='{player1Text}', 玩家2='{player2Text}'");
        
        // 首先更新玩家总分显示
        UpdatePlayerTotalScoreText();
        
        // 更新玩家1的奖励文本
        if (player1BonusText != null && !string.IsNullOrEmpty(player1Text))
        {
            player1BonusText.gameObject.SetActive(true);
            SetTextAlpha(player1BonusText, 1f);
            
            // 使用TextAnimator显示文本
            if (player1BonusTextAnimator != null)
            {
                player1BonusTextAnimator.ShowText(player1Text);
            }
            else
            {
                player1BonusText.text = player1Text;
            }
        }
        
        // 更新玩家2的奖励文本
        if (player2BonusText != null && !string.IsNullOrEmpty(player2Text))
        {
            player2BonusText.gameObject.SetActive(true);
            SetTextAlpha(player2BonusText, 1f);
            
            // 使用TextAnimator显示文本
            if (player2BonusTextAnimator != null)
            {
                player2BonusTextAnimator.ShowText(player2Text);
            }
            else
            {
                player2BonusText.text = player2Text;
            }
        }
        
        // 播放动画
        StartCoroutine(PlayBonusTextAnimation());
        
        // 在动画播放完成后再次更新总分显示
        StartCoroutine(UpdateTotalScoreAfterDelay());
    }
    
    // 在延迟后更新总分显示
    private IEnumerator UpdateTotalScoreAfterDelay()
    {
        // 等待足够长的时间，确保动画和网络变量同步都已完成
        yield return new WaitForSeconds(showDuration + 0.5f);
        
        // 再次更新总分显示
        UpdatePlayerTotalScoreText();
    }
    
    // 播放奖励文本动画
    private IEnumerator PlayBonusTextAnimation()
    {
        // 保存原始缩放值
        Vector3 originalScale1 = player1BonusText != null ? player1BonusText.transform.localScale : Vector3.one;
        Vector3 originalScale2 = player2BonusText != null ? player2BonusText.transform.localScale : Vector3.one;
        
        // 在动画开始时更新玩家总分
        UpdatePlayerTotalScoreText();
        
        // 缩放动画
        float elapsed = 0f;
        while (elapsed < debugTextAnimDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / debugTextAnimDuration;
            
            float scale = 1f + Mathf.Sin(progress * Mathf.PI) * (scaleAmount - 1f);
            
            if (player1BonusText != null && player1BonusText.gameObject.activeSelf)
                player1BonusText.transform.localScale = originalScale1 * scale;
            if (player2BonusText != null && player2BonusText.gameObject.activeSelf)
                player2BonusText.transform.localScale = originalScale2 * scale;
                
            yield return null;
        }
        
        // 确保回到原始大小
        if (player1BonusText != null && player1BonusText.gameObject.activeSelf)
            player1BonusText.transform.localScale = originalScale1;
        if (player2BonusText != null && player2BonusText.gameObject.activeSelf)
            player2BonusText.transform.localScale = originalScale2;
        
        // 在缩放动画结束后再次更新总分显示
        UpdatePlayerTotalScoreText();
            
        // 等待显示时间
        yield return new WaitForSeconds(showDuration);
        
        // 在显示结束前再次更新总分
        UpdatePlayerTotalScoreText();
        
        // 淡出动画
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / fadeOutDuration);  // 从1渐变到0
            
            if (player1BonusText != null && player1BonusText.gameObject.activeSelf)
                SetTextAlpha(player1BonusText, alpha);
            if (player2BonusText != null && player2BonusText.gameObject.activeSelf)
                SetTextAlpha(player2BonusText, alpha);
                
            yield return null;
        }
        
        // 完全隐藏
        if (player1BonusText != null)
        {
            SetTextAlpha(player1BonusText, 0f);
            player1BonusText.gameObject.SetActive(false);
        }
        if (player2BonusText != null)
        {
            SetTextAlpha(player2BonusText, 0f);
            player2BonusText.gameObject.SetActive(false);
        }
        
        // 动画完全结束后再次更新总分
        UpdatePlayerTotalScoreText();
    }

    // 添加分数动画协程
    private IEnumerator AnimateScoreChange(float startValue, float targetValue, System.Action<float> updateAction, System.Action onComplete = null, bool playScaleEffectAfter = true)
    {
        float currentValue = startValue;
        float duration = Mathf.Abs(targetValue - startValue) / scoreAnimationSpeed;
        float elapsedTime = 0f;

        // 如果分数差异太小，直接设置为目标值
        if (Mathf.Abs(targetValue - startValue) < 0.1f)
        {
            updateAction(targetValue);
            if (onComplete != null) onComplete();
            yield break;
        }

        // 先使用没有shake效果的纯数字
        bool isPlayer1Score = false;
        bool isPlayer2Score = false;
        bool isPlayer1TotalScore = false;
        bool isPlayer2TotalScore = false;
        string originalText = "";
        TextMeshProUGUI textToAnimate = null;
        Vector3 originalScale = Vector3.one;
        
        // 确定是哪个分数文本
        if (updateAction.Target is UIManager)
        {
            if (player1ScoreText != null && updateAction.Method.Name.Contains("player1"))
            {
                isPlayer1Score = true;
                textToAnimate = player1ScoreText;
                originalText = player1ScoreText.text;
                originalScale = player1ScoreText.transform.localScale;
            }
            else if (player2ScoreText != null && updateAction.Method.Name.Contains("player2"))
            {
                isPlayer2Score = true;
                textToAnimate = player2ScoreText;
                originalText = player2ScoreText.text;
                originalScale = player2ScoreText.transform.localScale;
            }
            else if (player1TotalScoreText != null && updateAction.Method.Name.Contains("player1TotalScore"))
            {
                isPlayer1TotalScore = true;
                textToAnimate = player1TotalScoreText;
                originalText = player1TotalScoreText.text;
                originalScale = player1TotalScoreText.transform.localScale;
            }
            else if (player2TotalScoreText != null && updateAction.Method.Name.Contains("player2TotalScore"))
            {
                isPlayer2TotalScore = true;
                textToAnimate = player2TotalScoreText;
                originalText = player2TotalScoreText.text;
                originalScale = player2TotalScoreText.transform.localScale;
            }
        }

        // 数字递增动画
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            
            // 使用线性插值计算当前值
            currentValue = Mathf.Lerp(startValue, targetValue, t);
            
            // 对于整数显示，我们可以使用Mathf.Floor确保逐个整数显示
            float displayValue = Mathf.Floor(currentValue);
            updateAction(displayValue);
            
            yield return null;
        }

        // 确保最终值正确
        updateAction(targetValue);
        
        // 在累加完成后，如果需要，播放放大特效
        if (playScaleEffectAfter && (isPlayer1Score || isPlayer2Score || isPlayer1TotalScore || isPlayer2TotalScore) && textToAnimate != null)
        {
            // 播放放大缩小动画
            float scaleAnimDuration = 0.3f;
            float maxScale = 1.3f;
            elapsedTime = 0f;
            
            // 放大阶段
            while (elapsedTime < scaleAnimDuration / 2)
            {
                elapsedTime += Time.deltaTime;
                float scale = Mathf.Lerp(1f, maxScale, elapsedTime / (scaleAnimDuration / 2));
                textToAnimate.transform.localScale = originalScale * scale;
                yield return null;
            }
            
            // 缩小阶段
            elapsedTime = 0f;
            while (elapsedTime < scaleAnimDuration / 2)
            {
                elapsedTime += Time.deltaTime;
                float scale = Mathf.Lerp(maxScale, 1f, elapsedTime / (scaleAnimDuration / 2));
                textToAnimate.transform.localScale = originalScale * scale;
                yield return null;
            }
            
            // 确保恢复原始缩放
            textToAnimate.transform.localScale = originalScale;
            
            // 放大特效结束后，应用带有shake效果的文本
            if (isPlayer1Score)
            {
                string finalText = $"玩家1: <shake a=0.1 f=0.1>{targetValue}</shake>";
                if (player1ScoreTextAnimator != null)
                {
                    player1ScoreTextAnimator.ShowText(finalText);
                }
                else
                {
                    player1ScoreText.text = finalText;
                }
            }
            else if (isPlayer2Score)
            {
                string finalText = $"玩家2: <shake a=0.1 f=0.1>{targetValue}</shake>";
                if (player2ScoreTextAnimator != null)
                {
                    player2ScoreTextAnimator.ShowText(finalText);
                }
                else
                {
                    player2ScoreText.text = finalText;
                }
            }
            else if (isPlayer1TotalScore)
            {
                string finalText = $"金币数：<shake a=0.1 f=0.1>{targetValue}</shake>";
                if (player1TotalScoreTextAnimator != null)
                {
                    player1TotalScoreTextAnimator.ShowText(finalText);
                }
                else
                {
                    player1TotalScoreText.text = finalText;
                }
            }
            else if (isPlayer2TotalScore)
            {
                string finalText = $"金币数：<shake a=0.1 f=0.1>{targetValue}</shake>";
                if (player2TotalScoreTextAnimator != null)
                {
                    player2TotalScoreTextAnimator.ShowText(finalText);
                }
                else
                {
                    player2TotalScoreText.text = finalText;
                }
            }
        }
        
        if (onComplete != null) onComplete();
    }

    // 停止所有分数动画
    private void StopAllScoreAnimations()
    {
        // 停止玩家1分数动画
        if (player1ScoreAnimation != null)
        {
            StopCoroutine(player1ScoreAnimation);
            player1ScoreAnimation = null;
        }
        
        // 停止玩家2分数动画
        if (player2ScoreAnimation != null)
        {
            StopCoroutine(player2ScoreAnimation);
            player2ScoreAnimation = null;
        }
        
        // 停止玩家1总分动画
        if (player1TotalScoreAnimation != null)
        {
            StopCoroutine(player1TotalScoreAnimation);
            player1TotalScoreAnimation = null;
        }
        
        // 停止玩家2总分动画
        if (player2TotalScoreAnimation != null)
        {
            StopCoroutine(player2TotalScoreAnimation);
            player2TotalScoreAnimation = null;
        }
    }

    // 监听玩家Continue状态变化
    private void OnPlayerContinueStatusChanged(bool oldValue, bool newValue)
    {
        UpdateSettlementButtonsState();
    }

    // 监听玩家Exit状态变化
    private void OnPlayerExitStatusChanged(bool oldValue, bool newValue)
    {
        UpdateSettlementButtonsState();
    }

    // 更新结算面板按钮状态和文本
    private void UpdateSettlementButtonsState()
    {
        if (!IsClient || settlementPanel == null) return;

        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        bool isPlayer1 = localClientId == 0;
        bool localPlayerClickedContinue = isPlayer1 ? player1ClickedContinue.Value : player2ClickedContinue.Value;
        bool otherPlayerClickedContinue = isPlayer1 ? player2ClickedContinue.Value : player1ClickedContinue.Value;
        bool localPlayerClickedExit = isPlayer1 ? player1ClickedExit.Value : player2ClickedExit.Value;
        bool otherPlayerClickedExit = isPlayer1 ? player2ClickedExit.Value : player1ClickedExit.Value;

        // 更新等待确认文本
        if (waitingConfirmationText != null)
        {
            if (localPlayerClickedContinue && !otherPlayerClickedContinue)
            {
                waitingConfirmationText.text = "等待对方玩家确认继续...\n(再次点击可取消)";
                waitingConfirmationText.gameObject.SetActive(true);
            }
            else if (localPlayerClickedExit && !otherPlayerClickedExit)
            {
                waitingConfirmationText.text = "等待对方玩家确认退出...\n(再次点击可取消)";
                waitingConfirmationText.gameObject.SetActive(true);
            }
            else
            {
                waitingConfirmationText.gameObject.SetActive(false);
            }
        }

        // 根据状态更新按钮外观
        if (continueButton != null)
        {
            // 保持按钮可交互，但改变视觉状态
            continueButton.interactable = true;
            
            // 获取按钮的颜色组件
            ColorBlock colors = continueButton.colors;
            
            if (localPlayerClickedContinue)
            {
                // 设置为选中状态的颜色
                colors.normalColor = new Color(0.7f, 1.0f, 0.7f); // 淡绿色表示选中
            }
            else
            {
                // 恢复默认颜色
                colors.normalColor = new Color(1.0f, 1.0f, 1.0f);
            }
            
            continueButton.colors = colors;
        }
        
        if (exitButton != null)
        {
            // 保持按钮可交互，但改变视觉状态
            exitButton.interactable = true;
            
            // 获取按钮的颜色组件
            ColorBlock colors = exitButton.colors;
            
            if (localPlayerClickedExit)
            {
                // 设置为选中状态的颜色
                colors.normalColor = new Color(1.0f, 0.7f, 0.7f); // 淡红色表示选中
            }
            else
            {
                // 恢复默认颜色
                colors.normalColor = new Color(1.0f, 1.0f, 1.0f);
            }
            
            exitButton.colors = colors;
        }

        // 检查是否两个玩家都点击了同一按钮
        bool bothClickedContinue = player1ClickedContinue.Value && player2ClickedContinue.Value;
        bool bothClickedExit = player1ClickedExit.Value && player2ClickedExit.Value;

        // 如果双方都点了Continue按钮，继续游戏
        if (bothClickedContinue && IsServer)
        {
            // 重置点击状态
            ResetButtonClickStates();
            
            // 通知所有客户端隐藏结算面板
            HideSettlementPanelClientRpc();
            
            // 检查是否是最终关卡
            if (LevelManager.Instance != null && LevelManager.Instance.isFinalLevel.Value)
            {
                // 播放最终对话
                PlayFinalDialogServerRpc();
            }
            else
            {
                // 加载下一个场景
                LoadNextSceneServerRpc();
            }
        }
        
        // 如果双方都点了Exit按钮，退出游戏
        if (bothClickedExit && IsServer)
        {
            // 重置点击状态
            ResetButtonClickStates();
            
            // 退出游戏
            ExitGameClientRpc();
        }
    }

    [ClientRpc]
    private void ExitGameClientRpc()
    {
        // 在所有客户端执行退出游戏
        ExitGame();
    }

    // 重置按钮点击状态
    [ServerRpc(RequireOwnership = false)]
    private void ResetButtonClickStatesServerRpc()
    {
        if (!IsServer) return;
        
        player1ClickedContinue.Value = false;
        player2ClickedContinue.Value = false;
        player1ClickedExit.Value = false;
        player2ClickedExit.Value = false;
    }

    // 在服务器端重置按钮点击状态
    private void ResetButtonClickStates()
    {
        if (!IsServer) return;
        
        player1ClickedContinue.Value = false;
        player2ClickedContinue.Value = false;
        player1ClickedExit.Value = false;
        player2ClickedExit.Value = false;
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayerClickedContinueServerRpc(ulong clientId, bool newStatus, ServerRpcParams serverRpcParams = default)
    {
        if (!IsServer) return;
        
        if (clientId == 0)
        {
            player1ClickedContinue.Value = newStatus;
            // 如果取消选中Continue，确保Exit也是未选中状态
            if (!newStatus)
            {
                player1ClickedExit.Value = false;
            }
            else if (newStatus)
            {
                // 如果选中Continue，确保Exit是未选中状态
                player1ClickedExit.Value = false;
            }
        }
        else if (clientId == 1)
        {
            player2ClickedContinue.Value = newStatus;
            // 如果取消选中Continue，确保Exit也是未选中状态
            if (!newStatus)
            {
                player2ClickedExit.Value = false;
            }
            else if (newStatus)
            {
                // 如果选中Continue，确保Exit是未选中状态
                player2ClickedExit.Value = false;
            }
        }
        
        string action = newStatus ? "选中" : "取消选中";
        Debug.Log($"[UIManager] 玩家 {clientId} {action}了Continue按钮。P1状态: {player1ClickedContinue.Value}, P2状态: {player2ClickedContinue.Value}");
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayerClickedExitServerRpc(ulong clientId, bool newStatus, ServerRpcParams serverRpcParams = default)
    {
        if (!IsServer) return;
        
        if (clientId == 0)
        {
            player1ClickedExit.Value = newStatus;
            // 如果取消选中Exit，确保Continue也是未选中状态
            if (!newStatus)
            {
                player1ClickedContinue.Value = false;
            }
            else if (newStatus)
            {
                // 如果选中Exit，确保Continue是未选中状态
                player1ClickedContinue.Value = false;
            }
        }
        else if (clientId == 1)
        {
            player2ClickedExit.Value = newStatus;
            // 如果取消选中Exit，确保Continue也是未选中状态
            if (!newStatus)
            {
                player2ClickedContinue.Value = false;
            }
            else if (newStatus)
            {
                // 如果选中Exit，确保Continue是未选中状态
                player2ClickedContinue.Value = false;
            }
        }
        
        string action = newStatus ? "选中" : "取消选中";
        Debug.Log($"[UIManager] 玩家 {clientId} {action}了Exit按钮。P1状态: {player1ClickedExit.Value}, P2状态: {player2ClickedExit.Value}");
    }
} 