using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;

public class RoundManager : NetworkBehaviour
{
    public DeckLogic deckLogic;
    public DialogManager dialogManager;
    public TMP_Text roundText;
    public float baseBet = 1f;
    public float betMultiplier = 2f;
    public LevelManager levelManager;  // 添加LevelManager引用

    [Header("Game Settings")]
    public int totalRounds = 5; // 游戏总回合数

    public PlayerLogic player1;
    public PlayerLogic player2;

    public GameObject Gun1;  // 玩家1的枪对象
    public GameObject Gun2;  // 玩家2的枪对象

    [Header("Balance Scale")]
    [SerializeField] private BalanceScale balanceScale;  // 天平引用

    [Header("Coin Effects")]
    public GameObject coinPrefab; // 硬币预制体
    public Transform player1ScoreAnchor; // 玩家1分数锚点
    public Transform player2ScoreAnchor; // 玩家2分数锚点

    private int currentRound = 0;  // 当前回合计数器
    private bool gameEnded = false;

    public int tutorState = 0;
    public int dialogIndex = 0;

    public int breakPoint = 0;

    [Header("Bool")]
    public bool chessIsMoved = false;
    public bool playerGotCard = false;


    void Start()
    {
        // 初始化回合
        currentRound = 1;

        if (dialogManager != null)
        {
            Debug.Log("Dialog Manager found, but not starting dialog automatically");
            // 移除自动调用StartDialog的代码
            // dialogManager.StartDialog();
        }
        else
        {
            Debug.LogError("Dialog Manager is not assigned in the inspector!");
        }

        // 给UIManager设置枪支引用
        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null && Gun1 != null && Gun2 != null)
        {
            Debug.Log("正在设置UIManager的枪支引用");
            
            // 初始化回合显示，但默认隐藏
            if (uiManager.roundText != null)
            {
                uiManager.roundText.gameObject.SetActive(false);
            }
        }
        else if (uiManager == null)
        {
            Debug.LogError("UIManager未找到!");
        }
        
    }

    private void OnEnable()
    {
        GameManager.OnPlayersReady += AssignPlayers;
    }

    private void OnDisable()
    {
        GameManager.OnPlayersReady -= AssignPlayers;
    }

    void Update()
    {
        if(GameManager.Instance.currentGameState == GameManager.GameState.TutorReady)
        {
            if(tutorState == 0)
            {
                DialogManager.Instance.PlayRange(0, 3);
                tutorState++;
            }
            else if(tutorState == 1)
            {
                tutorState++;
            }
            else if(tutorState == 2)
            {
                tutorState++;
                DialogManager.Instance.PlayRange(4, 5);

                CardManager cardManager = FindObjectOfType<CardManager>();
                if (cardManager != null)
                {
                    cardManager.StartDealCards(2);
                    playerGotCard = true;
                }
                else
                {
                    Debug.LogError("CardManager not found!");
                }
                DialogManager.Instance.PlayRange(6, 9);
            }
            else if(tutorState == 3)
            {
          tutorState++;
            }
            else if(tutorState == 4)
            {
                tutorState++;
                DialogManager.Instance.PlayRange(10, 12);
                
                // 重置玩家分数，准备开始第一阶段
                if (NetworkManager.Singleton.IsServer)
                {
                    player1.point.Value = 0;
                    player2.point.Value = 0;
                }

                // 在教程结束后显示回合文本
                UIManager uiManager = FindObjectOfType<UIManager>();
                if (uiManager != null)
                {
                    if (uiManager.roundText != null)
                    {
                        uiManager.roundText.gameObject.SetActive(true);
                    }
                    uiManager.UpdateRoundText(currentRound, totalRounds);
                }
                
                GameManager.Instance.currentGameState = GameManager.GameState.Ready;
                return;
            }
            GameManager.Instance.currentGameState = GameManager.GameState.TutorPlayerTurn;
        }
        else if(GameManager.Instance.currentGameState == GameManager.GameState.TutorPlayerTurn)
        {
            if (player1.choice != PlayerLogic.playerChoice.None && player2.choice != PlayerLogic.playerChoice.None
                )
            {
               
                if (playerGotCard)
                {
                    if (player1.usedCard.Value == true && player2.usedCard.Value == true)
                    {

                        MovePiecesToPositions();

                        GameManager.Instance.currentGameState = GameManager.GameState.TutorCalculateTurn;
                    }
                    Debug.Log(tutorState);
                }
                else
                {
                    MovePiecesToPositions();
                    GameManager.Instance.currentGameState = GameManager.GameState.TutorCalculateTurn;
                }
            }

            // 获取玩家选择状态
            bool player1Selected = player1.choice != PlayerLogic.playerChoice.None;
            bool player2Selected = player2.choice != PlayerLogic.playerChoice.None;
            
            // 更新UI显示
            UIManager uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.UpdateChoiceStatus(player1Selected, player2Selected);
            }
        }
        else if(GameManager.Instance.currentGameState == GameManager.GameState.TutorCalculateTurn)
        {
            CalculatePointWithoutCardAndGun();
            
        }
        else if(GameManager.Instance.currentGameState == GameManager.GameState.Ready)
        {
            CardManager cardManager = FindObjectOfType<CardManager>();
            if (cardManager != null)
            {
                cardManager.StartDealCards(5);
                GameManager.Instance.currentGameState = GameManager.GameState.PlayerTurn;
            }
            else
            {
                Debug.LogError("CardManager not found!");
            }
        }
        else if(GameManager.Instance.currentGameState == GameManager.GameState.PlayerTurn)
        {
            if(player1.choice != PlayerLogic.playerChoice.None && player2.choice != PlayerLogic.playerChoice.None)
            {
                if (playerGotCard)
                {
                    if (player1.usedCard.Value == true && player2.usedCard.Value == true)
                    {

                        MovePiecesToPositions();

                        GameManager.Instance.currentGameState = GameManager.GameState.CalculateTurn;
                    }
                    Debug.Log(tutorState);
                }
                else
                {
                    MovePiecesToPositions();
                    GameManager.Instance.currentGameState = GameManager.GameState.CalculateTurn;
                }
            }

            // 获取玩家选择状态
            bool player1Selected = player1.choice != PlayerLogic.playerChoice.None;
            bool player2Selected = player2.choice != PlayerLogic.playerChoice.None;
            
            // 更新UI显示
            UIManager uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.UpdateChoiceStatus(player1Selected, player2Selected);
            }
        }
        else if(GameManager.Instance.currentGameState == GameManager.GameState.CalculateTurn)
        {
            // 根据是否是枪战模式选择计分方法
            if (levelManager != null && levelManager.IsGunfightMode())
            {
                CalculatePointWithGunAndCard();
            }
            else
            {
            CalculatePointWithCardButWithoutGun();
            }
            
            ChessMoveBack();
            ResetChess();
            ResetPlayersChoice();
            ResetPlayers();
            GameManager.Instance.currentGameState = GameManager.GameState.PlayerTurn;

            // 使用UIManager更新回合UI
            UIManager uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.UpdateRoundText(currentRound, totalRounds);
            }
            else if (roundText != null)
            {
                roundText.text = $"ROUND {currentRound}/{totalRounds}";
            }
        }
    }

    void MovePiecesToPositions()
    {
        // 只要双方都放置了棋子，调用棋子的移动方法
        foreach (var chess in GameManager.Instance.chessComponents)
        {
            chess.Move();
        }
    }

    // 用于清空调试信息的方法
    // 这个方法将在 Invoke 触发时被调用
    private void ClearDebug()
    {
        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            uiManager.ClearDebugInfo();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdateBalanceScaleServerRpc(float player1Score, float player2Score)
    {
        if (balanceScale != null)
        {
            UpdateBalanceScaleClientRpc(player1Score, player2Score);
        }
    }

    [ClientRpc]
    private void UpdateBalanceScaleClientRpc(float player1Score, float player2Score)
    {
        if (balanceScale != null)
        {
            balanceScale.UpdateScore(player1Score, player2Score);
        }
    }

    void CalculatePointWithCardButWithoutGun()
    {
        if (gameEnded)
        {
            return;
        }

        // 计算完成后，构造调试信息字符串
        string player1Debug = "+0";
        string player2Debug = "+0";

        if (NetworkManager.LocalClientId == 0)
        {
            ApplyEffect();

            if (player1.choice == PlayerLogic.playerChoice.Cooperate && player2.choice == PlayerLogic.playerChoice.Cooperate)
            {
                float p1PointsBefore = player1.point.Value;
                float p2PointsBefore = player2.point.Value;

                player1.point.Value += player1.coopPoint.Value;
                player2.point.Value += player2.coopPoint.Value;

                player1Debug = $"+{player1.coopPoint.Value}";
                player2Debug = $"+{player2.coopPoint.Value}";

                // 计算增加了多少分
                int p1PointsAdded = Mathf.FloorToInt(player1.point.Value - p1PointsBefore);
                int p2PointsAdded = Mathf.FloorToInt(player2.point.Value - p2PointsBefore);
                
                // 生成硬币
                Coin.SpawnCoins(coinPrefab, player1ScoreAnchor, player1ScoreAnchor.position, p1PointsAdded);
                Coin.SpawnCoins(coinPrefab, player2ScoreAnchor, player2ScoreAnchor.position, p2PointsAdded);

                UpdateBalanceScaleServerRpc(player1.point.Value, player2.point.Value);
            }
            else if (player1.choice == PlayerLogic.playerChoice.Cooperate && player2.choice == PlayerLogic.playerChoice.Cheat)
            {
                float p1PointsBefore = player1.point.Value;
                float p2PointsBefore = player2.point.Value;

                player1.point.Value += player1.coopPoint.Value;
                player2.point.Value += player2.cheatPoint.Value;

                player1Debug = $"+{player1.coopPoint.Value}";
                player2Debug = $"+{player2.cheatPoint.Value}";

                // 计算增加了多少分
                int p1PointsAdded = Mathf.FloorToInt(player1.point.Value - p1PointsBefore);
                int p2PointsAdded = Mathf.FloorToInt(player2.point.Value - p2PointsBefore);
                
                // 生成硬币
                Coin.SpawnCoins(coinPrefab, player1ScoreAnchor, player1ScoreAnchor.position, p1PointsAdded);
                Coin.SpawnCoins(coinPrefab, player2ScoreAnchor, player2ScoreAnchor.position, p2PointsAdded);

                UpdateBalanceScaleServerRpc(player1.point.Value, player2.point.Value);
            }
            else if (player1.choice == PlayerLogic.playerChoice.Cheat && player2.choice == PlayerLogic.playerChoice.Cooperate)
            {
                float p1PointsBefore = player1.point.Value;
                float p2PointsBefore = player2.point.Value;

                player1.point.Value += player1.cheatPoint.Value;
                player2.point.Value += player2.coopPoint.Value;

                player1Debug = $"+{player1.cheatPoint.Value}";
                player2Debug = $"+{player2.coopPoint.Value}";

                // 计算增加了多少分
                int p1PointsAdded = Mathf.FloorToInt(player1.point.Value - p1PointsBefore);
                int p2PointsAdded = Mathf.FloorToInt(player2.point.Value - p2PointsBefore);
                
                // 生成硬币
                Coin.SpawnCoins(coinPrefab, player1ScoreAnchor, player1ScoreAnchor.position, p1PointsAdded);
                Coin.SpawnCoins(coinPrefab, player2ScoreAnchor, player2ScoreAnchor.position, p2PointsAdded);

                UpdateBalanceScaleServerRpc(player1.point.Value, player2.point.Value);
            }
            else if (player1.choice == PlayerLogic.playerChoice.Cheat && player2.choice == PlayerLogic.playerChoice.Cheat)
            {
                float p1PointsBefore = player1.point.Value;
                float p2PointsBefore = player2.point.Value;

                player1.point.Value += 0f;
                player2.point.Value += 0f;

                player1Debug = "+0";
                player2Debug = "+0";

                // 计算增加了多少分
                int p1PointsAdded = Mathf.FloorToInt(player1.point.Value - p1PointsBefore);
                int p2PointsAdded = Mathf.FloorToInt(player2.point.Value - p2PointsBefore);
                
                // 生成硬币
                Coin.SpawnCoins(coinPrefab, player1ScoreAnchor, player1ScoreAnchor.position, p1PointsAdded);
                Coin.SpawnCoins(coinPrefab, player2ScoreAnchor, player2ScoreAnchor.position, p2PointsAdded);

                UpdateBalanceScaleServerRpc(player1.point.Value, player2.point.Value);
            }

            // 调用 UIManager 来更新调试信息
            UIManager uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.UpdateDebugInfo(player1Debug, player2Debug);
            }

            // 关键部分：在服务器端更新网络变量，让所有客户端同步显示
            if (NetworkManager.Singleton.IsServer)
            {
                GameManager.Instance.playerComponents[0].debugInfo.Value = player1Debug;
                GameManager.Instance.playerComponents[1].debugInfo.Value = player2Debug;
            }
        }

        ResetPlayersChoice();
        GameManager.Instance.ResetAllBlocksServerRpc();
        GameManager.Instance.currentGameState = GameManager.GameState.TutorReady;
        GameManager.Instance.chessComponents[0].backToOriginal = true;
        GameManager.Instance.chessComponents[1].backToOriginal = true;
        chessIsMoved = false;  // 重置棋子移动状态

        // 检查是否回合数达到上限
        if (!gameEnded && currentRound >= totalRounds)
        {
            Debug.Log($"{totalRounds} rounds completed.");
            gameEnded = true;
            
            // 显示第一阶段结算面板
            if (levelManager != null)
            {
                levelManager.ShowFirstPhaseSettlement();
            }
            else
            {
                // 原有的结束逻辑
                UIManager uiManager = FindObjectOfType<UIManager>();
                if (uiManager != null)
                {
                    string winner = "";
                    if (player1.point.Value > player2.point.Value)
                    {
                        winner = "Player 1 wins!";
                    }
                    else if (player2.point.Value > player1.point.Value)
                    {
                        winner = "Player 2 wins!";
                    }
                    else
                    {
                        winner = "It's a tie!";
                    }
                    uiManager.ShowGameOver($"{totalRounds} rounds completed\n{winner}");
                }
            }
        }

        // 如果游戏未结束，则继续进行下一回合
        if (!gameEnded)
        {
            currentRound++;
            Debug.Log($"Round {currentRound}");
        }
    }

    void CalculatePointWithoutCardAndGun()
    {
        if (gameEnded)
        {
            return;
        }

        // 计算完成后，构造调试信息字符串
        string player1Debug = "+0";
        string player2Debug = "+0";

        if (NetworkManager.LocalClientId == 0)
        {
            ApplyEffect();

            if (player1.choice == PlayerLogic.playerChoice.Cooperate && player2.choice == PlayerLogic.playerChoice.Cooperate)
            {
                float p1PointsBefore = player1.point.Value;
                float p2PointsBefore = player2.point.Value;

                player1.point.Value += player1.coopPoint.Value;
                player2.point.Value += player2.coopPoint.Value;

                player1Debug = $"+{player1.coopPoint.Value}";
                player2Debug = $"+{player2.coopPoint.Value}";

                // 计算增加了多少分
                int p1PointsAdded = Mathf.FloorToInt(player1.point.Value - p1PointsBefore);
                int p2PointsAdded = Mathf.FloorToInt(player2.point.Value - p2PointsBefore);
                
                // 生成硬币
                Coin.SpawnCoins(coinPrefab, player1ScoreAnchor, player1ScoreAnchor.position, p1PointsAdded);
                Coin.SpawnCoins(coinPrefab, player2ScoreAnchor, player2ScoreAnchor.position, p2PointsAdded);

                UpdateBalanceScaleServerRpc(player1.point.Value, player2.point.Value);
            }
            else if (player1.choice == PlayerLogic.playerChoice.Cooperate && player2.choice == PlayerLogic.playerChoice.Cheat)
            {
                float p1PointsBefore = player1.point.Value;
                float p2PointsBefore = player2.point.Value;

                player1.point.Value += player1.coopPoint.Value;
                player2.point.Value += player2.cheatPoint.Value;

                player1Debug = $"+{player1.coopPoint.Value}";
                player2Debug = $"+{player2.cheatPoint.Value}";

                // 计算增加了多少分
                int p1PointsAdded = Mathf.FloorToInt(player1.point.Value - p1PointsBefore);
                int p2PointsAdded = Mathf.FloorToInt(player2.point.Value - p2PointsBefore);
                
                // 生成硬币
                Coin.SpawnCoins(coinPrefab, player1ScoreAnchor, player1ScoreAnchor.position, p1PointsAdded);
                Coin.SpawnCoins(coinPrefab, player2ScoreAnchor, player2ScoreAnchor.position, p2PointsAdded);

                UpdateBalanceScaleServerRpc(player1.point.Value, player2.point.Value);
            }
            else if (player1.choice == PlayerLogic.playerChoice.Cheat && player2.choice == PlayerLogic.playerChoice.Cooperate)
            {
                float p1PointsBefore = player1.point.Value;
                float p2PointsBefore = player2.point.Value;

                player1.point.Value += player1.cheatPoint.Value;
                player2.point.Value += player2.coopPoint.Value;

                player1Debug = $"+{player1.cheatPoint.Value}";
                player2Debug = $"+{player2.coopPoint.Value}";

                // 计算增加了多少分
                int p1PointsAdded = Mathf.FloorToInt(player1.point.Value - p1PointsBefore);
                int p2PointsAdded = Mathf.FloorToInt(player2.point.Value - p2PointsBefore);
                
                // 生成硬币
                Coin.SpawnCoins(coinPrefab, player1ScoreAnchor, player1ScoreAnchor.position, p1PointsAdded);
                Coin.SpawnCoins(coinPrefab, player2ScoreAnchor, player2ScoreAnchor.position, p2PointsAdded);

                UpdateBalanceScaleServerRpc(player1.point.Value, player2.point.Value);
            }
            else if (player1.choice == PlayerLogic.playerChoice.Cheat && player2.choice == PlayerLogic.playerChoice.Cheat)
            {
                float p1PointsBefore = player1.point.Value;
                float p2PointsBefore = player2.point.Value;

                player1.point.Value += 0f;
                player2.point.Value += 0f;

                player1Debug = "+0";
                player2Debug = "+0";

                // 计算增加了多少分
                int p1PointsAdded = Mathf.FloorToInt(player1.point.Value - p1PointsBefore);
                int p2PointsAdded = Mathf.FloorToInt(player2.point.Value - p2PointsBefore);
                
                // 生成硬币
                Coin.SpawnCoins(coinPrefab, player1ScoreAnchor, player1ScoreAnchor.position, p1PointsAdded);
                Coin.SpawnCoins(coinPrefab, player2ScoreAnchor, player2ScoreAnchor.position, p2PointsAdded);

                UpdateBalanceScaleServerRpc(player1.point.Value, player2.point.Value);
            }

            // 调用 UIManager 来更新调试信息
            UIManager uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.UpdateDebugInfo(player1Debug, player2Debug);
            }

            // 关键部分：在服务器端更新网络变量，让所有客户端同步显示
            if (NetworkManager.Singleton.IsServer)
            {
                GameManager.Instance.playerComponents[0].debugInfo.Value = player1Debug;
                GameManager.Instance.playerComponents[1].debugInfo.Value = player2Debug;
            }

        }
        ResetPlayersChoice();
        GameManager.Instance.ResetAllBlocksServerRpc();
        GameManager.Instance.currentGameState = GameManager.GameState.TutorReady;
        GameManager.Instance.chessComponents[0].backToOriginal = true;
        GameManager.Instance.chessComponents[1].backToOriginal = true;
        chessIsMoved = false;  // 重置棋子移动状态
    }


    void CalculatePointWithGunAndCard()
    {
        if (gameEnded)
        {
            return;
        }

        // 计算完成后，构造调试信息字符串
        string player1Debug = "+0";
        string player2Debug = "+0";

        if (NetworkManager.LocalClientId == 0)
        {
            ApplyEffect();

            if (player1.choice == PlayerLogic.playerChoice.Cooperate && player2.choice == PlayerLogic.playerChoice.Cooperate)
            {
                float p1PointsBefore = player1.point.Value;
                float p2PointsBefore = player2.point.Value;
                
                player1.point.Value += player1.coopPoint.Value;
                player2.point.Value += player2.coopPoint.Value;

                player1Debug = $"+{player1.coopPoint.Value}";
                player2Debug = $"+{player2.coopPoint.Value}";

                // 计算增加了多少分
                int p1PointsAdded = Mathf.FloorToInt(player1.point.Value - p1PointsBefore);
                int p2PointsAdded = Mathf.FloorToInt(player2.point.Value - p2PointsBefore);
                
                // 生成硬币
                Coin.SpawnCoins(coinPrefab, player1ScoreAnchor, player1ScoreAnchor.position, p1PointsAdded);
                Coin.SpawnCoins(coinPrefab, player2ScoreAnchor, player2ScoreAnchor.position, p2PointsAdded);

                UpdateBalanceScaleServerRpc(player1.point.Value, player2.point.Value);
            }
            else if (player1.choice == PlayerLogic.playerChoice.Cooperate && player2.choice == PlayerLogic.playerChoice.Cheat)
            {
                float p1PointsBefore = player1.point.Value;
                float p2PointsBefore = player2.point.Value;
                
                player1.point.Value += player1.coopPoint.Value;
                player2.point.Value += player2.cheatPoint.Value;

                player1Debug = $"+{player1.coopPoint.Value}";
                player2Debug = $"+{player2.cheatPoint.Value}";

                // 计算增加了多少分
                int p1PointsAdded = Mathf.FloorToInt(player1.point.Value - p1PointsBefore);
                int p2PointsAdded = Mathf.FloorToInt(player2.point.Value - p2PointsBefore);
                
                // 生成硬币
                Coin.SpawnCoins(coinPrefab, player1ScoreAnchor, player1ScoreAnchor.position, p1PointsAdded);
                Coin.SpawnCoins(coinPrefab, player2ScoreAnchor, player2ScoreAnchor.position, p2PointsAdded);

                Gun1.GetComponent<GunController>().FireGun();

                UpdateBalanceScaleServerRpc(player1.point.Value, player2.point.Value);
            }
            else if (player1.choice == PlayerLogic.playerChoice.Cheat && player2.choice == PlayerLogic.playerChoice.Cooperate)
            {
                float p1PointsBefore = player1.point.Value;
                float p2PointsBefore = player2.point.Value;
                
                player1.point.Value += player1.cheatPoint.Value;
                player2.point.Value += player2.coopPoint.Value;

                player1Debug = $"+{player1.cheatPoint.Value}";
                player2Debug = $"+{player2.coopPoint.Value}";

                // 计算增加了多少分
                int p1PointsAdded = Mathf.FloorToInt(player1.point.Value - p1PointsBefore);
                int p2PointsAdded = Mathf.FloorToInt(player2.point.Value - p2PointsBefore);
                
                // 生成硬币
                Coin.SpawnCoins(coinPrefab, player1ScoreAnchor, player1ScoreAnchor.position, p1PointsAdded);
                Coin.SpawnCoins(coinPrefab, player2ScoreAnchor, player2ScoreAnchor.position, p2PointsAdded);

                Gun2.GetComponent<GunController>().FireGun();

                UpdateBalanceScaleServerRpc(player1.point.Value, player2.point.Value);
            }
            else if (player1.choice == PlayerLogic.playerChoice.Cheat && player2.choice == PlayerLogic.playerChoice.Cheat)
            {
                float p1PointsBefore = player1.point.Value;
                float p2PointsBefore = player2.point.Value;
                
                player1.point.Value += 0f;
                player2.point.Value += 0f;

                player1Debug = "+0";
                player2Debug = "+0";

                // 计算增加了多少分
                int p1PointsAdded = Mathf.FloorToInt(player1.point.Value - p1PointsBefore);
                int p2PointsAdded = Mathf.FloorToInt(player2.point.Value - p2PointsBefore);
                
                // 生成硬币
                Coin.SpawnCoins(coinPrefab, player1ScoreAnchor, player1ScoreAnchor.position, p1PointsAdded);
                Coin.SpawnCoins(coinPrefab, player2ScoreAnchor, player2ScoreAnchor.position, p2PointsAdded);

                Gun1.GetComponent<GunController>().FireGun();
                Gun2.GetComponent<GunController>().FireGun();

                UpdateBalanceScaleServerRpc(player1.point.Value, player2.point.Value);
            }

            // 调用 UIManager 来更新调试信息（你可以通过 GameObject.FindObjectOfType<UIManager>() 获取到 UIManager 对象）
            UIManager uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.UpdateDebugInfo(player1Debug, player2Debug);
            }

            // 关键部分：在服务器端更新网络变量，让所有客户端同步显示
            if (NetworkManager.Singleton.IsServer)
            {
                GameManager.Instance.playerComponents[0].debugInfo.Value = player1Debug;
                GameManager.Instance.playerComponents[1].debugInfo.Value = player2Debug;
            }


            // 检查是否有一方死亡
            if (Gun1.GetComponent<GunController>().gameEnded.Value)
            {
                // 玩家2死亡
                Debug.Log("Player 2 is dead! Game over.");
                gameEnded = true;
                if (uiManager != null)
                {
                    uiManager.NetworkShowGameOver("Player 2 is dead!");
                }
            }
            else if (Gun2.GetComponent<GunController>().gameEnded.Value)
            {
                // 玩家1死亡
                Debug.Log("Player 1 is dead! Game over.");
                gameEnded = true;
                if (uiManager != null)
                {
                    uiManager.NetworkShowGameOver("Player 1 is dead!");
                }
            }

            // 检查是否回合数达到上限
            if (!gameEnded && currentRound >= totalRounds)
            {
                Debug.Log($"{totalRounds} rounds completed. Game over.");
                gameEnded = true;
                if (uiManager != null)
                {
                    // 判断获胜者
                    string winner = "";
                    if (player1.point.Value > player2.point.Value)
                    {
                        winner = "Player 1 wins!";
                    }
                    else if (player2.point.Value > player1.point.Value)
                    {
                        winner = "Player 2 wins!";
                    }
                    else
                    {
                        winner = "It's a tie!";
                    }
                    uiManager.NetworkShowGameOver($"{totalRounds} rounds completed\n{winner}");
                }
            }

            // 如果游戏未结束，则继续进行下一回合
            if (!gameEnded)
            {
                currentRound++;
                Debug.Log($"Round {currentRound}");
            }

        }

        ResetPlayersChoice();
        GameManager.Instance.ResetAllBlocksServerRpc();
        GameManager.Instance.currentGameState = GameManager.GameState.Ready;
        GameManager.Instance.chessComponents[0].backToOriginal = true;
        GameManager.Instance.chessComponents[1].backToOriginal = true;
        chessIsMoved = false;

    }

    public void ResetRound()
    {
        gameEnded = false;
        currentRound = 1;  // 重置回合计数器
        Gun1.GetComponent<GunController>().ResetGun();
        Gun2.GetComponent<GunController>().ResetGun();

        // 重置天平到中立位置
        if (balanceScale != null)
        {
            balanceScale.UpdateScore(0, 0);
        }
    }



    //###############################################卡牌优先级

    void ApplyEffect()
    {
        // 1. 找到玩家1、玩家2各自打出的那张牌
        CardLogic p1Card = null;
        CardLogic p2Card = null;

        foreach (CardLogic cardLogic in deckLogic.cardLogics)
        {
            if (cardLogic.isOut)
            {
                if (cardLogic.belong == CardLogic.Belong.Player1)
                {
                    p1Card = cardLogic;
                }
                else if (cardLogic.belong == CardLogic.Belong.Player2)
                {
                    p2Card = cardLogic;
                }
            }
        }

        // 2. 如果两张牌都找到了，就排下序，然后按优先级依次执行
        if (p1Card != null && p2Card != null)
        {
            ApplyCardEffects(p1Card, p2Card);

            // 标记已经使用完
            p1Card.isOut = false;
            p2Card.isOut = false;
        }
    }

    private void ApplyCardEffects(CardLogic player1Card, CardLogic player2Card)
    {
        // 放进 List 准备排序
        List<CardLogic> cards = new List<CardLogic>() { player1Card, player2Card };

        // 按优先级升序排序（数字小的先执行 => 优先级越高）
        cards.Sort((cardA, cardB) =>
            CardLogic.GetEffectPriority(cardA.effect).CompareTo(
            CardLogic.GetEffectPriority(cardB.effect)));

        // 依次执行排序后的卡牌效果
        foreach (var card in cards)
        {
            card.OnEffect();
        }
    }

    //#########################################

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
}
