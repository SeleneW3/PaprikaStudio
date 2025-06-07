using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;

public class RoundManager : NetworkBehaviour
{
    public DeckLogic deckLogic;
    public float baseBet = 1f;
    public float betMultiplier = 2f;

    [Header("Choice Statistics")]
    // 当前回合的选择统计
    public NetworkVariable<int> player1CurrentRoundCoopCount = new NetworkVariable<int>(0);
    public NetworkVariable<int> player1CurrentRoundCheatCount = new NetworkVariable<int>(0);
    public NetworkVariable<int> player2CurrentRoundCoopCount = new NetworkVariable<int>(0);
    public NetworkVariable<int> player2CurrentRoundCheatCount = new NetworkVariable<int>(0);

    // 总的选择统计
    public NetworkVariable<int> player1TotalCoopCount = new NetworkVariable<int>(0);
    public NetworkVariable<int> player1TotalCheatCount = new NetworkVariable<int>(0);
    public NetworkVariable<int> player2TotalCoopCount = new NetworkVariable<int>(0);
    public NetworkVariable<int> player2TotalCheatCount = new NetworkVariable<int>(0);

    [Header("Game Settings")]
    public NetworkVariable<int> totalRounds = new NetworkVariable<int>(5); // 游戏总回合数

    public PlayerLogic player1;
    public PlayerLogic player2;

    public GameObject Gun1;  // 玩家1的枪对象
    public GameObject Gun2;  // 玩家2的枪对象

    public GameObject ShowCamera1;
    public GameObject ShowCamera2;
    public GameObject Player1Camera;
    public GameObject Player2Camera;

    [Header("Balance Scale")]
    [SerializeField] private BalanceScale balanceScale;  // 天平引用

    [Header("Coin Effects")]
    public GameObject coinPrefab; // 硬币预制体
    public Transform player1ScoreAnchor; // 玩家1分数锚点
    public Transform player2ScoreAnchor; // 玩家2分数锚点
    public Transform player1CoinRespawnPos;
    public Transform player2CoinRespawnPos;

    public NetworkVariable<int> currentRound = new NetworkVariable<int>(0);
    private bool gameEnded = false;

    public int tutorState = 0;
    public int dialogIndex = 0;

    public int breakPoint = 0;

    [Header("Bool")]
    public bool chessIsMoved = false;
    public NetworkVariable<bool> playerGotCard = new NetworkVariable<bool>(false);
    public bool showCard = false;

    [Header("Gun Control")]
    public NetworkVariable<bool> player1CanFire = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> player2CanFire = new NetworkVariable<bool>(false);
    //public bool isGunRound = false;

    void Start()
    {

        // 初始化回合
        currentRound.Value = 1;

        // 给UIManager设置枪支引用
        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager == null)
        {
            Debug.Log("UIManager未找到!");
        }

        if(LevelManager.Instance.currentMode.Value == LevelManager.Mode.Tutor)
        {
            totalRounds.Value = 4;
        }
        else
        {
            totalRounds.Value = 5;
        }

        // 根据游戏模式设置枪的可见性
        UpdateGunsVisibility();
    }

    private void OnEnable()
    {
        GameManager.OnPlayersReady += AssignPlayers;
        
        // 订阅LevelManager的模式更改事件
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.currentMode.OnValueChanged += OnGameModeChanged;
        }
    }

    private void OnDisable()
    {
        GameManager.OnPlayersReady -= AssignPlayers;
        
        // 取消订阅LevelManager的模式更改事件
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.currentMode.OnValueChanged -= OnGameModeChanged;
        }
    }
    
    // 当游戏模式改变时调用
    private void OnGameModeChanged(LevelManager.Mode previousMode, LevelManager.Mode newMode)
    {
        // 更新枪的可见性
        UpdateGunsVisibility();
    }

    void Update()
    {
        // 添加日志输出当前tutorState和游戏状态
        if (IsServer && LevelManager.Instance.currentMode.Value == LevelManager.Mode.Tutor && Time.frameCount % 300 == 0)
        {
            Debug.Log($"[RoundManager] 当前tutorState = {tutorState}, 游戏状态 = {GameManager.Instance.currentGameState}, 回合 = {currentRound.Value}/{totalRounds.Value}");
        }
        
        if(GameManager.Instance.currentGameState == GameManager.GameState.TutorReady)
        {
            if(tutorState == 0)
            {
                DialogManager.Instance.PlayRange(0, 3);
                tutorState++;
            }
            else if(tutorState == 1)
            {
                DialogManager.Instance.PlayRange(4, 5);
                tutorState++;

            }
            else if(tutorState == 2)
            {
                DialogManager.Instance.PlayRange(6, 9);
                tutorState++;

                CardManager cardManager = FindObjectOfType<CardManager>();
                if (cardManager != null)
                {
                    cardManager.StartDealCards(2);
                    if(NetworkManager.LocalClientId == 0)
                    {
                        playerGotCard.Value = true;
                    }
                }
                else
                {
                    Debug.Log("CardManager not found!");
                }

            }
            else if(tutorState == 3)
            {
                Debug.Log($"[RoundManager] 从tutorState 3准备进入状态4，确保DialogManager存在");
                // 在进入状态4前先确保DialogManager存在
                EnsureDialogManagerExists();
                tutorState++;
            }
            else if(tutorState == 4)
            {
                Debug.Log($"[RoundManager] 进入tutorState=4分支，当前回合 {currentRound.Value}/{totalRounds.Value}");
                // 确保DialogManager存在
                if (EnsureDialogManagerExists())
                {
                    Debug.Log($"[RoundManager] 准备播放教程对话10-12，当前回合 {currentRound.Value}/{totalRounds.Value}");
                    // 先启动协程，在协程完成后再增加状态
                    StartCoroutine(PlayDialogWithDelayAndIncrementState(10, 12, 2f));
                }
                else
                {
                    Debug.LogError("[RoundManager] 无法播放对话，DialogManager不存在！");
                    // 即使对话无法播放，也要增加状态以避免卡住
                    tutorState++;
                }
            }
            GameManager.Instance.currentGameState = GameManager.GameState.TutorPlayerTurn;
        }
        else if (GameManager.Instance.currentGameState == GameManager.GameState.TutorShowState)
        {
            if(showCard == false)
            {
                showCard = true;
                deckLogic.ShowSentCards();
            }
            else
            {
                GameManager.Instance.currentGameState = GameManager.GameState.TutorPlayerTurn;
            }
        }
        else if(GameManager.Instance.currentGameState == GameManager.GameState.TutorPlayerTurn)
        {
            if (player1.choice != PlayerLogic.playerChoice.None && player2.choice != PlayerLogic.playerChoice.None)
            {
                if (playerGotCard.Value == true)
                {
                    if (player1.usedCard.Value == true && player2.usedCard.Value == true)
                    {
                        MovePiecesToPositions();
                        GameManager.Instance.currentGameState = GameManager.GameState.TutorShowState;
                    }
                }
                else
                {
                    MovePiecesToPositions();
                    GameManager.Instance.currentGameState = GameManager.GameState.TutorCalculateTurn;
                }
            }

            bool player1Selected = player1.choice != PlayerLogic.playerChoice.None;
            bool player2Selected = player2.choice != PlayerLogic.playerChoice.None;
            
            UIManager uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.UpdateChoiceStatus(player1Selected, player2Selected);
            }
        }
        else if(GameManager.Instance.currentGameState == GameManager.GameState.TutorCalculateTurn)
        {
            CalculatePointWithoutGun();
        }
        else if(GameManager.Instance.currentGameState == GameManager.GameState.Ready)
        {
            CardManager cardManager = FindObjectOfType<CardManager>();
            if(LevelManager.Instance.currentMode.Value == LevelManager.Mode.OnlyCard || 
                              LevelManager.Instance.currentMode.Value == LevelManager.Mode.CardAndGun)
            {
                if (cardManager != null)
                {
                    cardManager.StartDealCards(5);
                    if (NetworkManager.LocalClientId == 0)
                    {
                        playerGotCard.Value = true;
                    }
                }
                else
                {
                    Debug.Log("CardManager not found!");
                }
            }

            GameManager.Instance.currentGameState = GameManager.GameState.PlayerTurn;
            
        }
        else if(GameManager.Instance.currentGameState == GameManager.GameState.PlayerTurn)
        {
            if(player1.choice != PlayerLogic.playerChoice.None && player2.choice != PlayerLogic.playerChoice.None)
            {
                if (playerGotCard.Value == true)
                {
                    if (player1.usedCard.Value == true && player2.usedCard.Value == true)
                    {
                        MovePiecesToPositions();
                        GameManager.Instance.currentGameState = GameManager.GameState.PlayerShowState;
                    }
                }
                else
                {
                    MovePiecesToPositions();
                    GameManager.Instance.currentGameState = GameManager.GameState.CalculateTurn;
                }
            }

            bool player1Selected = player1.choice != PlayerLogic.playerChoice.None;
            bool player2Selected = player2.choice != PlayerLogic.playerChoice.None;
            
            UIManager uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.UpdateChoiceStatus(player1Selected, player2Selected);
            }
        }
        else if (GameManager.Instance.currentGameState == GameManager.GameState.PlayerShowState)
        {
            if (showCard == false)
            {
                showCard = true;
                Debug.Log("I've got here");
                deckLogic.ShowSentCards();
            }
            else
            {
                GameManager.Instance.currentGameState = GameManager.GameState.PlayerTurn;
            }
        }
        else if(GameManager.Instance.currentGameState == GameManager.GameState.CalculateTurn)
        {
            Debug.Log($"[RoundManager] Current Mode: {LevelManager.Instance.currentMode}");
            if (LevelManager.Instance.currentMode.Value == LevelManager.Mode.OnlyGun||
                LevelManager.Instance.currentMode.Value == LevelManager.Mode.CardAndGun)
            {
                Debug.Log("[RoundManager] Using gun calculation mode");
                CalculatePointWithGun();
            }
            else
            {
                Debug.Log("[RoundManager] Using non-gun calculation mode");
                CalculatePointWithoutGun();
            }
            
            ChessMoveBack();
            ResetChess();
            ResetPlayersChoice();
            ResetPlayers();
            showCard = false;
            GameManager.Instance.currentGameState = GameManager.GameState.PlayerTurn;
        }
    }

    void MovePiecesToPositions()
    {
        foreach (var chess in GameManager.Instance.chessComponents)
        {
            chess.Move();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdateBalanceScaleServerRpc(float player1Score, float player2Score)
    {
        if (balanceScale != null)
        {
            balanceScale.SetScoreDiffServerRpc(player1Score - player2Score);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdateRoundServerRpc()
    {
        if (!gameEnded)
        {
            currentRound.Value++;
            UpdateRoundClientRpc();
        }
    }

    [ClientRpc]
    private void UpdateRoundClientRpc()
    {
        Debug.Log($"[RoundManager] 回合已更新: 当前回合 {currentRound.Value}/{totalRounds.Value}");
    }

    void CalculatePointWithoutGun()
    {
        Debug.Log("[RoundManager] Using CalculatePointWithoutGun");
        if (gameEnded)
        {
            return;
        }

        if (NetworkManager.LocalClientId == 0)
        {
            ApplyEffect();
            // 在应用卡牌效果后记录玩家的最终选择
            if (NetworkManager.Singleton.IsServer)
            {
                RecordPlayerChoices(player1.choice, player2.choice);
                
                // 检查任务完成情况
                if (LevelManager.Instance != null)
                {
                    // 检查身份任务是否完成
                    LevelManager.Instance.CheckAndApplyIdentityBonus();
                }
            }
            CalculatePoint(player1.choice, player2.choice);
        }
        ResetPlayersChoice();
        GameManager.Instance.ResetAllBlocksServerRpc();
        GameManager.Instance.chessComponents[0].backToOriginal = true;
        GameManager.Instance.chessComponents[1].backToOriginal = true;
        
        if(showCard == true)
        {
            if(NetworkManager.LocalClientId == 0)
            {
                GameManager.Instance.deck.ResetPlayerCardServerRpc();
            }
            showCard = false;
        }
        
        chessIsMoved = false;
        
        // 根据当前游戏模式设置正确的状态
        if(LevelManager.Instance.currentMode.Value == LevelManager.Mode.Tutor)
        {
            GameManager.Instance.currentGameState = GameManager.GameState.TutorReady;
        }
        else
        {
            GameManager.Instance.currentGameState = GameManager.GameState.Ready;
        }
    }

    void CalculatePointWithGun()
    {
        Debug.Log("[RoundManager] Using CalculatePointWithGun");
        if (gameEnded)
        {
            return;
        }
        UIManager uiManager = FindObjectOfType<UIManager>();

        //isGunRound = true;

        if (NetworkManager.LocalClientId == 0)
        {
            ApplyEffect();
            // 在应用卡牌效果后记录玩家的最终选择
            if (NetworkManager.Singleton.IsServer)
            {
                RecordPlayerChoices(player1.choice, player2.choice);
                
                // 检查任务完成情况
                if (LevelManager.Instance != null)
                {
                    // 检查身份任务是否完成
                    LevelManager.Instance.CheckAndApplyIdentityBonus();
                }
            }
            CalculatePoint(player1.choice, player2.choice);
            
            if (Gun1.GetComponent<GunController>().gameEnded.Value || 
                Gun2.GetComponent<GunController>().gameEnded.Value)
            {
                Debug.Log("Game ended due to gunshot!");
                gameEnded = true;
            }
        }

        ResetPlayersChoice();
        GameManager.Instance.ResetAllBlocksServerRpc();
        GameManager.Instance.chessComponents[0].backToOriginal = true;
        GameManager.Instance.chessComponents[1].backToOriginal = true;
        if (showCard == true)
        {
            if (NetworkManager.LocalClientId == 0)
            {
                GameManager.Instance.deck.ResetPlayerCardServerRpc();
            }
            showCard = false;
        }

        chessIsMoved = false;
        GameManager.Instance.currentGameState = GameManager.GameState.Ready;

        //isGunRound = false;
    }

    // 新增方法：记录玩家的最终选择（经过卡牌效果处理后）
    private void RecordPlayerChoices(PlayerLogic.playerChoice player1Choice, PlayerLogic.playerChoice player2Choice)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        
        // 记录玩家1的选择
        if (player1Choice == PlayerLogic.playerChoice.Cooperate)
        {
            LevelManager.Instance.AddPlayerCoopTime(0); // 0是玩家1的clientId
        }
        else if (player1Choice == PlayerLogic.playerChoice.Cheat)
        {
            LevelManager.Instance.AddPlayerCheatTime(0);
        }

        // 记录玩家2的选择
        if (player2Choice == PlayerLogic.playerChoice.Cooperate)
        {
            LevelManager.Instance.AddPlayerCoopTime(1); // 1是玩家2的clientId
        }
        else if (player2Choice == PlayerLogic.playerChoice.Cheat)
        {
            LevelManager.Instance.AddPlayerCheatTime(1);
        }
    }

    void CalculatePoint(PlayerLogic.playerChoice player1Choice, PlayerLogic.playerChoice player2Choice)
    {
        UIManager uiManager = FindObjectOfType<UIManager>();

        float player1CurrentRoundPoint;
        float player2CurrentRoundPoint;

        // 只有在OnlyGun和CardAndGun模式下才允许开枪
        bool canUseGun = (LevelManager.Instance.currentMode.Value == LevelManager.Mode.OnlyGun || 
                         LevelManager.Instance.currentMode.Value == LevelManager.Mode.CardAndGun);
        
        if (canUseGun)
        {
            if (player2Choice == PlayerLogic.playerChoice.Cheat && Gun1 != null)
            {
                player1CanFire.Value = true;
            }
            if (player1Choice == PlayerLogic.playerChoice.Cheat && Gun2 != null)
            {
                player2CanFire.Value = true;
            }
        }

        // 保存原始分数值
        float originalPlayer1Point = player1.point.Value;
        float originalPlayer2Point = player2.point.Value;

        // 计算得分
        if(player1Choice == PlayerLogic.playerChoice.Cooperate && player2Choice == PlayerLogic.playerChoice.Cooperate)
        {
            player1CurrentRoundPoint = player1.coopPoint.Value;
            player2CurrentRoundPoint = player2.coopPoint.Value;
        }
        else if (player1Choice == PlayerLogic.playerChoice.Cooperate && player2Choice == PlayerLogic.playerChoice.Cheat)
        {
            player1CurrentRoundPoint = player1.coopPoint.Value;
            player2CurrentRoundPoint = player2.cheatPoint.Value;
        }
        else if (player1Choice == PlayerLogic.playerChoice.Cheat && player2Choice == PlayerLogic.playerChoice.Cooperate)
        {
            player1CurrentRoundPoint = player1.cheatPoint.Value;
            player2CurrentRoundPoint = player2.coopPoint.Value;
        }
        else
        {
            player1CurrentRoundPoint = 0f;
            player2CurrentRoundPoint = 0f;
        }

        player1.point.Value += player1CurrentRoundPoint;
        player2.point.Value += player2CurrentRoundPoint;

        // 计算实际增加的分数（考虑卡牌效果可能的修改）
        float actualPlayer1Increase = player1.point.Value - originalPlayer1Point;
        float actualPlayer2Increase = player2.point.Value - originalPlayer2Point;

        // 更新总分，使用实际增加的分数
        LevelManager.Instance.AddPlayer1TotalPoint(actualPlayer1Increase);
        LevelManager.Instance.AddPlayer2TotalPointServerRpc(actualPlayer2Increase);

        // 修改这里：使用actualPlayerIncrease作为显示的分数
        string player1Debug = "+" + actualPlayer1Increase.ToString();
        string player2Debug = "+" + actualPlayer2Increase.ToString();

        if (uiManager != null)
        {
            // 更新debug信息并启动UI状态机
            uiManager.UpdateDebugInfo(player1Debug, player2Debug);
            
            // 如果状态机没有在运行，启动它
            if (!uiManager.IsStateMachineRunning())
            {
                uiManager.StartRoundSettlementUI();
            }
        }

        if (NetworkManager.Singleton.IsServer)
        {
            GameManager.Instance.playerComponents[0].debugInfo.Value = player1Debug;
            GameManager.Instance.playerComponents[1].debugInfo.Value = player2Debug;
        }

        // 修改这里：始终更新回合数，然后再检查是否结束游戏
        if (!gameEnded && NetworkManager.Singleton.IsServer)
        {
            UpdateRoundServerRpc();
            
            // 在更新回合数后检查是否达到游戏结束条件
            if (currentRound.Value > totalRounds.Value)
            {
                EndGame(uiManager);
            }
        }
    }

    private void ResetCurrentRoundStatistics()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        
        player1CurrentRoundCoopCount.Value = 0;
        player1CurrentRoundCheatCount.Value = 0;
        player2CurrentRoundCoopCount.Value = 0;
        player2CurrentRoundCheatCount.Value = 0;
    }

    public void ResetAllStatistics()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        ResetCurrentRoundStatistics();
        player1TotalCoopCount.Value = 0;
        player1TotalCheatCount.Value = 0;
        player2TotalCoopCount.Value = 0;
        player2TotalCheatCount.Value = 0;
    }

    private void EndGame(UIManager uiManager)
    {
        gameEnded = true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void FireGunServerRpc(ulong clientId)
    {
        if (clientId == 0 && player1CanFire.Value)
        {
            Gun1.GetComponent<GunController>().FireGun();
            player1CanFire.Value = false;
        }
        else if (clientId == 1 && player2CanFire.Value)
        {
            Gun2.GetComponent<GunController>().FireGun();
            player2CanFire.Value = false;
        }
    }

    void AssignPlayers()
    {
        foreach (var player in GameManager.Instance.playerComponents)
        {
            if (player.playerID == 1)
            {
                player1 = player;
            }
            else if (player.playerID == 2)
            {
                player2 = player;
            }
        }
    }

    void ResetPlayers()
    {
        player1.ResetToInitial();
        player2.ResetToInitial();
    }

    void ResetPlayersChoice()
    {
        player1.choice = PlayerLogic.playerChoice.None;
        player2.choice = PlayerLogic.playerChoice.None;
        player1.SetUsedCardServerRpc(false);
        player2.SetUsedCardServerRpc(false);
    }

    void ChessMoveBack()
    {
        foreach (var chess in GameManager.Instance.chessComponents)
        {

             chess.backToOriginal = true;

        }
    }

    void ResetChess()
    {
        foreach (var chess in GameManager.Instance.chessComponents)
        {
            chess.isOnGround = false;
        }
    }

    void ApplyEffect()
    {
        CardLogic p1Card = null;
        CardLogic p2Card = null;

        foreach (CardLogic cardLogic in deckLogic.cardLogics)
        {
            if (cardLogic.isOut)
            {
                if (cardLogic.belong.Value == CardLogic.Belong.Player1)
                {
                    p1Card = cardLogic;
                }
                else if (cardLogic.belong.Value == CardLogic.Belong.Player2)
                {
                    p2Card = cardLogic;
                }
            }
        }

        if (p1Card != null && p2Card != null)
        {
            ApplyCardEffects(p1Card, p2Card);

            p1Card.isOut = false;
            p2Card.isOut = false;
        }
    }

    private void ApplyCardEffects(CardLogic player1Card, CardLogic player2Card)
    {
        List<CardLogic> cards = new List<CardLogic>() { player1Card, player2Card };

        cards.Sort((cardA, cardB) =>
            CardLogic.GetEffectPriority(cardA.effect).CompareTo(
            CardLogic.GetEffectPriority(cardB.effect)));

        foreach (var card in cards)
        {
            card.OnEffect();
        }
    }

    public void ResetRound()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        gameEnded = false;
        currentRound.Value = 1;
        
        // 根据游戏模式重置总回合数
        if(LevelManager.Instance != null)
        {
            if(LevelManager.Instance.currentMode.Value == LevelManager.Mode.Tutor)
            {
                totalRounds.Value = 4;
            }
            else
            {
                totalRounds.Value = 5;
            }
        }
        
        ResetAllStatistics();
        
        Gun1.GetComponent<GunController>().ResetGun();
        Gun2.GetComponent<GunController>().ResetGun();

        player1.point.Value = 0;
        player2.point.Value = 0;

        foreach (Transform anchor in new Transform[] { player1ScoreAnchor, player2ScoreAnchor })
        {
            if (anchor != null)
            {
                for (int i = anchor.childCount - 1; i >= 0; i--)
                {
                    Transform container = anchor.GetChild(i);
                    NetworkObject netObj = container.GetComponent<NetworkObject>();
                    if (netObj != null && netObj.IsSpawned)
                    {
                        netObj.Despawn();
                    }
                }
            }
        }

        if (balanceScale != null)
        {
            UpdateBalanceScaleServerRpc(0, 0);
        }
        
        // 更新枪的可见性
        UpdateGunsVisibility();
    }

    // 添加一个方法来根据游戏模式控制枪的可见性
    void UpdateGunsVisibility()
    {
        if (Gun1 == null || Gun2 == null) return;

        bool shouldShowGuns = (LevelManager.Instance.currentMode.Value == LevelManager.Mode.OnlyGun || 
                              LevelManager.Instance.currentMode.Value == LevelManager.Mode.CardAndGun);

        // 在服务器端更新枪的可见性
        if (IsServer)
        {
            UpdateGunsVisibilityClientRpc(shouldShowGuns);
        }
    }

    [ClientRpc]
    void UpdateGunsVisibilityClientRpc(bool showGuns)
    {
        // 设置枪的可见性
        if (Gun1 != null)
        {
            foreach (Renderer renderer in Gun1.GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = showGuns;
            }
            // 禁用碰撞器以防止鼠标交互
            foreach (Collider collider in Gun1.GetComponentsInChildren<Collider>())
            {
                collider.enabled = showGuns;
            }
        }

        if (Gun2 != null)
        {
            foreach (Renderer renderer in Gun2.GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = showGuns;
            }
            // 禁用碰撞器以防止鼠标交互
            foreach (Collider collider in Gun2.GetComponentsInChildren<Collider>())
            {
                collider.enabled = showGuns;
            }
        }
    }

    // 延迟播放对话的协程
    private IEnumerator PlayDialogWithDelay(int startIndex, int endIndex, float delaySeconds)
    {
        Debug.Log($"[RoundManager] PlayDialogWithDelay开始等待 {delaySeconds} 秒，将播放对话索引 {startIndex}-{endIndex}");
        
        // 增加延迟时间，确保所有组件都已初始化
        yield return new WaitForSeconds(delaySeconds + 0.5f);
        
        // 确保DialogManager实例存在
        if (!EnsureDialogManagerExists())
        {
            Debug.LogError("[RoundManager] DialogManager.Instance为空，无法播放对话！尝试重新创建...");
            
            // 再次尝试确保DialogManager存在
            for (int i = 0; i < 3; i++)
            {
                yield return new WaitForSeconds(0.5f);
                if (EnsureDialogManagerExists())
                {
                    break;
                }
            }
            
            // 如果仍然无法找到DialogManager，则退出
            if (DialogManager.Instance == null)
            {
                Debug.LogError("[RoundManager] 多次尝试后仍无法找到DialogManager实例，放弃播放对话！");
                yield break;
            }
        }
        
        Debug.Log($"[RoundManager] 延迟结束，准备调用PlayDelayedDialogServerRpc，DialogManager.Instance存在: {DialogManager.Instance != null}");
        
        // 使用ServerRpc确保对话在所有客户端上播放
        PlayDelayedDialogServerRpc(startIndex, endIndex, 0.5f);
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayDelayedDialogServerRpc(int startIndex, int endIndex, float delaySeconds)
    {
        Debug.Log($"[RoundManager] PlayDelayedDialogServerRpc被调用，startIndex={startIndex}, endIndex={endIndex}");
        // Schedule the dialog to play after the delay on all clients
        StartCoroutine(DelayedDialogClientRpcCoroutine(startIndex, endIndex, delaySeconds));
    }

    private IEnumerator DelayedDialogClientRpcCoroutine(int startIndex, int endIndex, float delaySeconds)
    {
        Debug.Log($"[RoundManager] DelayedDialogClientRpcCoroutine开始等待 {delaySeconds} 秒");
        yield return new WaitForSeconds(delaySeconds);
        
        // 再次检查DialogManager是否存在，如果不存在则尝试创建
        bool dialogManagerExists = false;
        
        for (int i = 0; i < 3; i++) // 尝试最多3次
        {
            if (DialogManager.Instance != null)
            {
                dialogManagerExists = true;
                break;
            }
            
            Debug.LogWarning($"[RoundManager] 尝试第{i+1}次确保DialogManager存在");
            EnsureDialogManagerExists();
            yield return new WaitForSeconds(0.3f);
        }
        
        if (!dialogManagerExists)
        {
            Debug.LogError("[RoundManager] 在DelayedDialogClientRpcCoroutine中，多次尝试后DialogManager.Instance仍为空！");
            yield break;
        }
        
        Debug.Log($"[RoundManager] 准备调用PlayDialogClientRpc，DialogManager.Instance存在: {DialogManager.Instance != null}");
        PlayDialogClientRpc(startIndex, endIndex);
    }

    [ClientRpc]
    private void PlayDialogClientRpc(int startIndex, int endIndex)
    {
        Debug.Log($"[RoundManager] PlayDialogClientRpc被调用，startIndex={startIndex}, endIndex={endIndex}, DialogManager.Instance存在: {DialogManager.Instance != null}");
        
        if (DialogManager.Instance != null)
        {
            Debug.Log($"[RoundManager] 调用DialogManager.PlayRange({startIndex}, {endIndex})");
            DialogManager.Instance.PlayRange(startIndex, endIndex);
        }
        else
        {
            Debug.LogError("[RoundManager] DialogManager.Instance为空，无法播放对话！");
        }
    }
    
    // 播放对话并在完成后增加教程状态
    private IEnumerator PlayDialogWithDelayAndIncrementState(int startIndex, int endIndex, float delaySeconds)
    {
        Debug.Log($"[RoundManager] PlayDialogWithDelayAndIncrementState开始执行，对话{startIndex}-{endIndex}，游戏状态={GameManager.Instance.currentGameState}");
        
        // 创建一个完成标志
        bool dialogCompleted = false;
        
        // 启动对话协程
        StartCoroutine(PlayDialogWithDelayAndNotify(startIndex, endIndex, delaySeconds, () => {
            dialogCompleted = true;
            Debug.Log($"[RoundManager] 对话完成回调被触发，dialogCompleted = true");
        }));
        
        // 等待对话完成或超时
        float timeoutSeconds = 15f; // 增加到15秒
        float elapsedTime = 0f;
        
        while (!dialogCompleted && elapsedTime < timeoutSeconds)
        {
            if (elapsedTime > 0 && elapsedTime % 3 < 0.1f) 
            {
                Debug.Log($"[RoundManager] 等待对话完成中... 已等待{elapsedTime:F1}秒，DialogManager存在={DialogManager.Instance != null}");
            }
            yield return null;
            elapsedTime += Time.deltaTime;
        }
        
        // 无论对话是否成功完成，都增加状态以避免游戏卡住
        Debug.Log($"[RoundManager] 对话{startIndex}-{endIndex}播放{(dialogCompleted ? "完成" : "超时")}，增加tutorState，当前状态={tutorState}");
        tutorState++;
        Debug.Log($"[RoundManager] tutorState已增加到{tutorState}");
    }
    
    // 播放对话并通知完成
    private IEnumerator PlayDialogWithDelayAndNotify(int startIndex, int endIndex, float delaySeconds, System.Action onComplete)
    {
        yield return StartCoroutine(PlayDialogWithDelay(startIndex, endIndex, delaySeconds));
        
        // 额外等待一段时间，确保对话有足够时间显示
        yield return new WaitForSeconds(3f);
        
        // 通知完成
        if (onComplete != null)
        {
            onComplete();
        }
    }

    // 确保DialogManager实例存在
    private bool EnsureDialogManagerExists()
    {
        if (DialogManager.Instance != null)
        {
            return true;
        }
        
        Debug.LogWarning("[RoundManager] DialogManager.Instance为空，尝试查找或创建...");
        
        // 尝试查找场景中的DialogManager
        DialogManager dialogManager = FindObjectOfType<DialogManager>();
        if (dialogManager != null)
        {
            Debug.Log("[RoundManager] 在场景中找到了DialogManager");
            return true;
        }
        
        // 如果场景中没有DialogManager，尝试查找预制体并实例化
        GameObject dialogManagerPrefab = Resources.Load<GameObject>("Prefabs/DialogManager");
        if (dialogManagerPrefab != null)
        {
            Debug.Log("[RoundManager] 从Resources加载DialogManager预制体并实例化");
            Instantiate(dialogManagerPrefab);
            return DialogManager.Instance != null;
        }
        
        Debug.LogError("[RoundManager] 无法找到或创建DialogManager实例！");
        return false;
    }
}
