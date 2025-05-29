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

    public static UIManager Instance { get; private set; }  // 单例模式

    [Header("Screen Space UI")]

    [Header("Score UI")]
    public TextMeshProUGUI player1ScoreText;
    public TextMeshProUGUI player2ScoreText;
    
    [Header("Score Position References")]
    public Transform player1ScoreAnchor; // 天平上的第一个空子物体
    public Transform player2ScoreAnchor; // 天平上的第二个空子物体

    [Header("Debug Text UI")]
    public TextMeshProUGUI player1DebugText;
    public TextMeshProUGUI player2DebugText;
    public Transform player1DebugAnchor; // 玩家1 debug text 显示锚点
    public Transform player2DebugAnchor; // 玩家2 debug text 显示锚点
    public Vector2 debugTextOffset = new Vector2(0, 30); // debug text 的位置偏移

    [Header("Bullets UI")]
    public TextMeshProUGUI player1BulletsText; // 显示玩家1剩余子弹的文本
    public TextMeshProUGUI player2BulletsText; // 显示玩家2剩余子弹的文本
    public Transform player1BulletsAnchor; // 玩家1子弹数显示锚点
    public Transform player2BulletsAnchor; // 玩家2子弹数显示锚点

    [Header("Movement Settings")]
    [Range(1f, 50f)]
    public float smoothSpeed = 15f; // 更高的平滑速度，减少滞后感
    public Vector2 scoreOffset = new Vector2(0, 50); // 位置偏移
    public Vector2 bulletsOffset = new Vector2(0, 0); // 子弹数显示的位置偏移
    public Vector2 roundOffset = new Vector2(0, 20); // 回合显示的位置偏移
    
    // 最小移动阈值，低于此值就直接设置到目标位置
    public float snapThreshold = 2.0f; 
    private Canvas parentCanvas;

    private GunController gun1;
    private GunController gun2;
    private RoundManager roundManager;

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
    // public TextMeshProUGUI levelText1;  // 玩家1的关卡显示
    // public TextMeshProUGUI levelText2;  // 玩家2的关卡显示
    public TextMeshProUGUI roundText1;  // 玩家1的回合显示
    public TextMeshProUGUI roundText2;  // 玩家2的回合显示
    public TextMeshProUGUI player1TargetText;
    public TextMeshProUGUI player2TargetText;

    // Text Animator Player组件引用
    // private TextAnimatorPlayer levelText1Animator;
    // private TextAnimatorPlayer levelText2Animator;
    private TextAnimatorPlayer roundText1Animator;
    private TextAnimatorPlayer roundText2Animator;
    private TextAnimatorPlayer player1TargetTextAnimator;
    private TextAnimatorPlayer player2TargetTextAnimator;

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
        if (roundText1 != null)
            roundText1Animator = roundText1.GetComponent<TextAnimatorPlayer>();
        if (roundText2 != null)
            roundText2Animator = roundText2.GetComponent<TextAnimatorPlayer>();
        if (player1TargetText != null)
            player1TargetTextAnimator = player1TargetText.GetComponent<TextAnimatorPlayer>();
        if (player2TargetText != null)
            player2TargetTextAnimator = player2TargetText.GetComponent<TextAnimatorPlayer>();
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

        // 初始化时隐藏World Space回合文本
        if (roundText1 != null || roundText2 != null)
        {
            roundText1.gameObject.SetActive(false);
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
            player1ChoiceStatusText.text = "...";
            player1ChoiceStatusText.color = Color.white;
        }
        
        if (player2ChoiceStatusText != null)
        {
            player2ChoiceStatusText.gameObject.SetActive(true);
            player2ChoiceStatusText.text = "...";
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
        if (roundText1 != null)
        {
            roundText1.text = "ROUND 1/5";
            roundText1.gameObject.SetActive(true);
        }
        if (roundText2 != null)
        {
            roundText2.text = "ROUND 1/5";
            roundText2.gameObject.SetActive(true);
        }

        // 获取RoundManager引用
        roundManager = FindObjectOfType<RoundManager>();
        if (roundManager == null)
        {
            Debug.LogWarning("RoundManager not found!");
        }

        // 订阅棋子动画完成事件
        ChessLogic.OnBothChessAnimationComplete += OnChessAnimationComplete;

        // 强制UI可见性
        ForceUIVisibility();
    }

    public override void OnDestroy()
    {
        // 取消订阅事件
        if (networkManager != null)
        {
            networkManager.OnClientConnectedCallback -= (id) => {
                ForceUIVisibility();
            };
        }
        ChessLogic.OnBothChessAnimationComplete -= OnChessAnimationComplete;
        
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
        
        // 启动状态机
        if (IsServer)
        {
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

    // 修改UpdateDebugInfo方法，添加网络同步
    public void UpdateDebugInfo(string player1Debug, string player2Debug)
    {
        if (!IsClient) return;

        // 如果是服务器，通知所有客户端更新debug text
        if (IsServer)
        {
            UpdateDebugInfoClientRpc(player1Debug, player2Debug);
        }
    }

    [ClientRpc]
    private void UpdateDebugInfoClientRpc(string player1Debug, string player2Debug)
    {
        try
        {
            // 更新debug text内容
            if (player1DebugText != null)
            {
                player1DebugText.text = player1Debug;
                player1DebugText.gameObject.SetActive(false);
                SetTextAlpha(player1DebugText, 1f);
            }
            if (player2DebugText != null)
            {
                player2DebugText.text = player2Debug;
                player2DebugText.gameObject.SetActive(false);
                SetTextAlpha(player2DebugText, 1f);
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

        UpdateUIPositions();

        if (roundManager != null)
        {
            UpdateRoundText(roundManager.currentRound.Value, roundManager.totalRounds);
        }
        
        if (GameManager.Instance != null && GameManager.Instance.playerComponents.Count >= 2)
        {
            UpdateScoreText();
            UpdateBulletsText();
        }
    }

    void UpdateScoreText()
    {
        if (!IsClient) return;

        try
        {
            if (GameManager.Instance == null || GameManager.Instance.playerComponents == null || 
                GameManager.Instance.playerComponents.Count < 2)
            {
                Debug.LogWarning("GameManager or player components not ready");
                return;
            }

            float player1Score = GameManager.Instance.playerComponents[0].point.Value;
            float player2Score = GameManager.Instance.playerComponents[1].point.Value;
            
            // 更新分数显示
            if (player1ScoreText != null)
            {
                player1ScoreText.text = $"Player 1: {player1Score}";
            }
            if (player2ScoreText != null)
            {
                player2ScoreText.text = $"Player 2: {player2Score}";
            }

            // 更新天平
            BalanceScale balanceScale = FindObjectOfType<BalanceScale>();
            if (balanceScale != null)
            {
                balanceScale.UpdateScore(player1Score, player2Score);
            }

            // 如果分数发生变化，生成金币
            if (Mathf.Abs(player1Score - lastPlayer1Score) > 0.01f || 
                Mathf.Abs(player2Score - lastPlayer2Score) > 0.01f)
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

                    // 修改结算面板显示条件
                    if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && 
                        roundManager != null && 
                        roundManager.currentRound.Value > roundManager.totalRounds)
                    {
                        StartCoroutine(DelayShowSettlement());
                    }
                }
            }

            // 更新上一次的分数记录
            lastPlayer1Score = player1Score;
            lastPlayer2Score = player2Score;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error updating score text: {e.Message}");
        }
    }

    private IEnumerator DelayShowSettlement()
    {
        // 等待一帧确保所有分数更新完成
        yield return null;
        
        // 修改判断条件：只有在完成最后一轮后才显示结算面板
        if (roundManager != null && roundManager.currentRound.Value > roundManager.totalRounds)
        {
            ShowSettlementPanel();
        }
    }
    
    void UpdateBulletsText()
    {
        // 确保我们有所有需要的组件
        if (player1BulletsText == null || player2BulletsText == null || gun1 == null || gun2 == null)
            return;
            
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
                player1BulletsText.text = $"{gun1UsedChances}/6"; // 显示格式：已消耗/总数
                player2BulletsText.text = $"{gun1UsedChances}/6"; // 玩家1的子弹数显示在玩家2头顶
            }
            else // 玩家2视角
            {
                player1BulletsText.text = $"{gun2UsedChances}/6"; // 玩家2的子弹数显示在玩家1头顶
                player2BulletsText.text = $"{gun2UsedChances}/6";
            }
        }
        else
        {
            // 单机测试模式
            player1BulletsText.text = $"{gun1UsedChances}/6";
            player2BulletsText.text = $"{gun2UsedChances}/6";
        }
    }

    // 使用统一的位置更新方法，无论Canvas模式如何
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
        
        // 更新子弹数文本位置（使用新的锚点）
        if (player1BulletsText != null && player2BulletsText != null)
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
        
        // 初始化分数文本
        if (player1ScoreText != null)
        {
            player1ScoreText.gameObject.SetActive(true);
            player1ScoreText.text = "Player 1: 0";
            player1ScoreText.color = Color.red;
        }

        // 初始化 debug text
        if (player1DebugText != null)
        {
            player1DebugText.gameObject.SetActive(true);
            player1DebugText.text = "+0";
            player1DebugText.color = Color.white;
        }
        
        if (player2DebugText != null)
        {
            player2DebugText.gameObject.SetActive(true);
            player2DebugText.text = "+0";
            player2DebugText.color = Color.white;
        }
        
        // 同样初始化子弹数文本
        if (player1BulletsText != null)
        {
            player1BulletsText.gameObject.SetActive(true);
            player1BulletsText.text = "0/6";
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
        }
        
        // 初始化World Space回合显示文本
        if (roundText1 != null)
        {
            roundText1.gameObject.SetActive(true);
            roundText1.text = "ROUND 1/5";
            roundText1.color = Color.white;
        }
        if (roundText2 != null)
        {
            roundText2.gameObject.SetActive(true);
            roundText2.text = "ROUND 1/5";
            roundText2.color = Color.white;
        }
    }

    // 修改更新回合文本的方法
    public void UpdateRoundText(int currentRound, int totalRounds = 5)
    {
        // 只在回合数真正改变时才更新文本
        if ((roundText1 != null || roundText2 != null) && 
            (currentRound != lastDisplayedRound || totalRounds != lastDisplayedTotalRounds))
        {
            lastDisplayedRound = currentRound;
            lastDisplayedTotalRounds = totalRounds;

            string roundInfo = $"ROUND {currentRound}/{totalRounds}";
            
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
            player1ChoiceStatusText.text = player1Selected ? "✓" : "...";
            player1ChoiceStatusText.color = player1Selected ? Color.green : Color.white;
        }
        
        if (player2ChoiceStatusText != null)
        {
            player2ChoiceStatusText.text = player2Selected ? "✓" : "...";
            player2ChoiceStatusText.color = player2Selected ? Color.green : Color.white;
        }
    }

    /// <summary>
    /// 设置默认鼠标图案
    /// </summary>
    public void SetDefaultCursor()
    {
        if (defaultCursor != null)
        {
            Cursor.SetCursor(defaultCursor, cursorHotspot, CursorMode.Auto);
        }
    }

    /// <summary>
    /// 设置手牌上的鼠标图案
    /// </summary>
    public void SetHandCardCursor()
    {
        if (handCardCursor != null)
        {
            Cursor.SetCursor(handCardCursor, cursorHotspot, CursorMode.Auto);
        }
    }

    /// <summary>
    /// 设置已选中卡牌上的鼠标图案
    /// </summary>
    public void SetSelectedCardCursor()
    {
        if (selectedCardCursor != null)
        {
            Cursor.SetCursor(selectedCardCursor, cursorHotspot, CursorMode.Auto);
        }
    }

    /// <summary>
    /// 重置为系统默认鼠标图案
    /// </summary>
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

    // 显示结算面板
    public void ShowSettlementPanel()
    {
        if (!IsServer) return;
        ShowSettlementPanelClientRpc();
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
        try
        {
            if (settlementPanel == null || GameManager.Instance == null || 
                GameManager.Instance.playerComponents == null || 
                GameManager.Instance.playerComponents.Count < 2)
            {
                Debug.LogError("Required components missing for settlement panel");
                return;
            }

            // 等待一帧确保分数已更新
            StartCoroutine(UpdateSettlementPanelWithDelay());
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error showing settlement panel: {e.Message}");
        }
    }

    private IEnumerator UpdateSettlementPanelWithDelay()
    {
        // 等待一帧确保所有分数更新完成
        yield return new WaitForEndOfFrame();

        settlementPanel.SetActive(true);

        // 启用按钮
        if (continueButton != null)
            continueButton.interactable = true;
        if (exitButton != null)
            exitButton.interactable = true;

        // 更新结算面板显示
        if (settlementScoreText != null)
        {
            int player1Score = (int)GameManager.Instance.playerComponents[0].point.Value;
            int player2Score = (int)GameManager.Instance.playerComponents[1].point.Value;
            
            string winner = player1Score > player2Score ? "Player 1" :
                          player2Score > player1Score ? "Player 2" : "No one";
            
            settlementScoreText.text = $"Phase Complete!\n\n" +
                                     $"Player 1: {player1Score}\n" +
                                     $"Player 2: {player2Score}\n\n" +
                                     $"{winner} wins this Phase!\n\n" +
                                     "Continue to Next Phase?";
        }
    }

    // 修改继续按钮的点击处理方法
    public void OnContinueButtonClick()
    {
        if (!IsClient) return;

        if (IsServer)
        {
            // 服务器端发起场景加载
            LoadNextSceneServerRpc();
        }
        else
        {
            // 客户端请求服务器加载场景
            RequestLoadNextSceneServerRpc();
        }
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
        // 在服务器端执行场景加载
        LoadNextSceneClientRpc();
    }

    [ClientRpc]
    private void LoadNextSceneClientRpc()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadScene("ModeScene");
        }
    }

    private void PullInitialValues()
    {
        if (!IsClient) return;

        Debug.Log("[UIManager] PullInitialValues called");
        Debug.Log($"[UIManager] Initial values - GameManager exists: {GameManager.Instance != null}, " +
                 $"Players count: {GameManager.Instance?.playerComponents.Count}");

        // 更新分数
        UpdateScoreText();

        // 更新子弹
        UpdateBulletsText();

        // 更新回合
        if (roundManager != null)
        {
            UpdateRoundText(roundManager.currentRound.Value);
            Debug.Log($"[UIManager] Round updated to: {roundManager.currentRound.Value}");
        }
        else
        {
            Debug.LogError("[UIManager] RoundManager is null in PullInitialValues!");
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
    }

    [ServerRpc(RequireOwnership = false)]
    public void TriggerSettlementServerRpc()
    {
        ShowSettlementPanelClientRpc();
    }

    // 添加回合文本更新方法
    public void RequestRoundTextUpdate(int currentRound, int totalRounds)
    {
        if (!IsServer) return;
        UpdateRoundTextClientRpc(currentRound, totalRounds);
    }

    [ClientRpc]
    private void UpdateRoundTextClientRpc(int currentRound, int totalRounds)
    {
        if (roundText1 != null)
        {
            if (roundText1Animator != null)
            {
                roundText1.gameObject.SetActive(true);
                roundText1Animator.ShowText($"ROUND {currentRound}/{totalRounds}");
            }
            else
            {
                roundText1.text = $"ROUND {currentRound}/{totalRounds}";
                roundText1.gameObject.SetActive(true);
            }
        }
        
        if (roundText2 != null)
        {
            if (roundText2Animator != null)
            {
                roundText2.gameObject.SetActive(true);
                roundText2Animator.ShowText($"ROUND {currentRound}/{totalRounds}");
            }
            else
            {
                roundText2.text = $"ROUND {currentRound}/{totalRounds}";
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

        State nextState = currentState.Value switch
        {
            State.Idle => State.DebugText,
            State.DebugText => State.ScoreAndCoin,
            State.ScoreAndCoin => State.FireAnimation,
            State.FireAnimation => State.BulletUI,
            State.BulletUI => State.RoundText,
            State.RoundText => State.Settlement,
            State.Settlement => State.Idle,
            _ => State.Idle
        };

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
        switch (currentState.Value)
        {
            case State.DebugText:
                yield return StartCoroutine(HandleDebugTextState());
                break;
            case State.ScoreAndCoin:
                yield return StartCoroutine(HandleScoreAndCoinState());
                break;
            case State.FireAnimation:
                yield return StartCoroutine(HandleFireAnimationState());
                break;
            case State.BulletUI:
                yield return StartCoroutine(HandleBulletUIState());
                break;
            case State.RoundText:
                yield return StartCoroutine(HandleRoundTextState());
                break;
            case State.Settlement:
                yield return StartCoroutine(HandleSettlementState());
                break;
        }

        if (IsServer)
        {
            // 如果不是Settlement状态，继续转换到下一个状态
            if (currentState.Value != State.Settlement)
            {
                TransitionToNextState();
            }
            // 如果是Settlement状态，且不是游戏最后一轮，重置状态机
            else if (roundManager != null && roundManager.currentRound.Value < roundManager.totalRounds)
            {
                ResetStateMachine();
            }
        }
    }

    private IEnumerator HandleDebugTextState()
    {
        if (player1DebugText != null && player2DebugText != null)
        {
            player1DebugText.gameObject.SetActive(true);
            player2DebugText.gameObject.SetActive(true);
            StartCoroutine(PlayDebugTextAnimation());
        }
        yield return new WaitForSeconds(2f);
    }

    private IEnumerator HandleScoreAndCoinState()
    {
        if (GameManager.Instance != null && GameManager.Instance.playerComponents.Count >= 2)
        {
            UpdateScoreText();
        }
        yield return new WaitForSeconds(2f);
    }

    private IEnumerator HandleFireAnimationState()
    {
        
        // 等待动画完成的时间
        yield return new WaitForSeconds(3f);
    }

    private IEnumerator HandleBulletUIState()
    {
        // 更新子弹UI
        UpdateBulletsText();
        yield return new WaitForSeconds(1f);
    }

    private IEnumerator HandleRoundTextState()
    {
        // 更新回合文本
        if (roundManager != null)
        {
            UpdateRoundText(roundManager.currentRound.Value, roundManager.totalRounds);
        }
        yield return new WaitForSeconds(1f);
    }

    private IEnumerator HandleSettlementState()
    {
        // 只在服务器端判断是否显示结算面板
        if (IsServer && roundManager != null)
        {
            // 修改判断条件：只有在完成最后一轮后才显示结算面板
            if (roundManager.currentRound.Value > roundManager.totalRounds)
            {
                ShowSettlementPanelClientRpc();
            }
        }
        yield return new WaitForSeconds(0.5f);
    }

    // 开始状态机
    public void StartStateMachine()
    {
        if (IsServer)
        {
            currentState.Value = State.Idle;
            TransitionToNextState();
        }
    }

    // 重置状态机
    public void ResetStateMachine()
    {
        if (IsServer)
        {
            currentState.Value = State.Idle;
        }
    }

    public void OnExitButtonClick()
    {
        if (IsClient)
        {
            Application.Quit();
        }
    }

    // 添加公共方法用于在回合结算时启动状态机
    public void StartRoundSettlementUI()
    {
        if (IsServer)
        {
            currentState.Value = State.Idle;
            TransitionToNextState();
        }
    }

    // 添加方法检查状态机是否正在运行
    public bool IsStateMachineRunning()
    {
        return currentState.Value != State.Idle;
    }

    [ClientRpc]
    private void HideSettlementPanelClientRpc()
    {
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
} 