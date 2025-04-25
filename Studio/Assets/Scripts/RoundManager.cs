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
            
            // 初始化回合显示
            uiManager.UpdateRoundText(currentRound, totalRounds);
        }
        else if (uiManager == null)
        {
            Debug.LogError("UIManager未找到!");
        }
        
        // 测试硬币生成
        Debug.Log("Testing coin spawn...");
        if (coinPrefab != null)
        {
            if (player1ScoreAnchor != null)
            {
                Debug.Log("测试玩家1金币生成 - 1个金币");
                Coin.SpawnCoins(coinPrefab, player1ScoreAnchor, 1);
            }
            else
            {
                Debug.LogError("玩家1锚点未设置!");
            }
            
            if (player2ScoreAnchor != null)
            {
                Debug.Log("测试玩家2金币生成 - 3个金币");
                Coin.SpawnCoins(coinPrefab, player2ScoreAnchor, 1);
            }
            else
            {
                Debug.LogError("玩家2锚点未设置!");
            }
            
            Debug.Log("金币生成测试完成");
        }
        else
        {
            Debug.LogError("金币预制体未设置!");
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
                DialogManager.Instance.PlayElementRange(0, 12);
                DialogManager.Instance.PlayElementRange(0, 3);
                tutorState++;
            }
            else if(tutorState == 1)
            {
                tutorState++;
            }
            else if(tutorState == 99)
            {
                tutorState++;
                DialogManager.Instance.PlayElementRange(4, 5);
                GameManager.Instance.currentGameState = GameManager.GameState.Ready;
                return;
            }
            GameManager.Instance.currentGameState = GameManager.GameState.TutorPlayerTurn;
            
        }
        else if(GameManager.Instance.currentGameState == GameManager.GameState.TutorPlayerTurn)
        {
            if (player1.choice != PlayerLogic.playerChoice.None && player2.choice != PlayerLogic.playerChoice.None)
            {
                MovePiecesToPositions();
                GameManager.Instance.currentGameState = GameManager.GameState.TutorCalculateTurn;
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
                cardManager.StartDealCards();
                GameManager.Instance.currentGameState = GameManager.GameState.PlayerTurn;
            }
            else
            {
                Debug.LogError("CardManager not found!");
            }
        }
        else if(GameManager.Instance.currentGameState == GameManager.GameState.PlayerTurn)
        {
            if (player1.choice != PlayerLogic.playerChoice.None && player2.choice != PlayerLogic.playerChoice.None)
            {
                // 如果双方都选择了棋子放置，开始移动棋子
                MovePiecesToPositions();
            }
        }
        else if(GameManager.Instance.currentGameState == GameManager.GameState.CalculateTurn)
        {
           
            
            CalculatePointWithGunAndCard();
            //ChessMoveBack();
            //ResetChess();
            ResetPlayersChoice();
            ResetPlayers();
            GameManager.Instance.currentGameState = GameManager.GameState.Ready;

            // 使用UIManager更新回合UI
            UIManager uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.UpdateRoundText(currentRound, totalRounds);
            }
            else if (roundText != null) // 备用方案：直接更新
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
            chess.Move();  // 触发棋子的移动
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

    void CalculatePointWithCardButWithoutGun()
    {

        if (gameEnded)
        {
            return;  // 如果游戏已经结束，则不再执行其他操作
        }

        // 计算完成后，构造调试信息字符串
        string player1Debug = $"Player 1: {player1.choice} +0";
        string player2Debug = $"Player 2: {player2.choice} +0";


        if (NetworkManager.LocalClientId == 0)
        {
            ApplyEffect();

            if (player1.choice == PlayerLogic.playerChoice.Cooperate && player2.choice == PlayerLogic.playerChoice.Cooperate)
            {
                player1.point.Value += player1.coopPoint.Value;
                player2.point.Value += player2.coopPoint.Value;

                player1Debug = $"Player 1: Cooperate +{player1.coopPoint.Value}";
                player2Debug = $"Player 2: Cooperate +{player2.coopPoint.Value}";

                balanceScale.UpdateScore(player1.point.Value, player2.point.Value);
            }
            else if (player1.choice == PlayerLogic.playerChoice.Cooperate && player2.choice == PlayerLogic.playerChoice.Cheat)
            {
                player1.point.Value += player1.coopPoint.Value;
                player2.point.Value += player2.cheatPoint.Value;

                player1Debug = $"Player 1: Cooperate +{player1.coopPoint.Value}";
                player2Debug = $"Player 2: Cheat +{player2.cheatPoint.Value}";


                balanceScale.UpdateScore(player1.point.Value, player2.point.Value);
            }
            else if (player1.choice == PlayerLogic.playerChoice.Cheat && player2.choice == PlayerLogic.playerChoice.Cooperate)
            {
                player1.point.Value += player1.cheatPoint.Value;
                player2.point.Value += player2.coopPoint.Value;

                player1Debug = $"Player 1: Cheat +{player1.cheatPoint.Value}";
                player2Debug = $"Player 2: Cooperate +{player2.coopPoint.Value}";


                balanceScale.UpdateScore(player1.point.Value, player2.point.Value);
            }
            else if (player1.choice == PlayerLogic.playerChoice.Cheat && player2.choice == PlayerLogic.playerChoice.Cheat)
            {
                player1.point.Value += 0f;
                player2.point.Value += 0f;

                player1Debug = $"Player 1: Cheat +0";
                player2Debug = $"Player 2: Cheat +0";

                balanceScale.UpdateScore(player1.point.Value, player2.point.Value);
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

        }
        ResetPlayersChoice();
        GameManager.Instance.currentGameState = GameManager.GameState.Ready;
        GameManager.Instance.chessComponents[0].backToOriginal = true;
        GameManager.Instance.chessComponents[1].backToOriginal = true;

    }

    void CalculatePointWithoutCardAndGun()
    {

        if (gameEnded)
        {
            return;  // 如果游戏已经结束，则不再执行其他操作
        }

        // 计算完成后，构造调试信息字符串
        string player1Debug = $"Player 1: {player1.choice} +0";
        string player2Debug = $"Player 2: {player2.choice} +0";


        if (NetworkManager.LocalClientId == 0)
        {

            if (player1.choice == PlayerLogic.playerChoice.Cooperate && player2.choice == PlayerLogic.playerChoice.Cooperate)
            {
                player1.point.Value += player1.coopPoint.Value;
                player2.point.Value += player2.coopPoint.Value;

                player1Debug = $"Player 1: Cooperate +{player1.coopPoint.Value}";
                player2Debug = $"Player 2: Cooperate +{player2.coopPoint.Value}";

                balanceScale.UpdateScore(player1.point.Value, player2.point.Value);
            }
            else if (player1.choice == PlayerLogic.playerChoice.Cooperate && player2.choice == PlayerLogic.playerChoice.Cheat)
            {
                player1.point.Value += player1.coopPoint.Value;
                player2.point.Value += player2.cheatPoint.Value;

                player1Debug = $"Player 1: Cooperate +{player1.coopPoint.Value}";
                player2Debug = $"Player 2: Cheat +{player2.cheatPoint.Value}";


                balanceScale.UpdateScore(player1.point.Value, player2.point.Value);
            }
            else if (player1.choice == PlayerLogic.playerChoice.Cheat && player2.choice == PlayerLogic.playerChoice.Cooperate)
            {
                player1.point.Value += player1.cheatPoint.Value;
                player2.point.Value += player2.coopPoint.Value;

                player1Debug = $"Player 1: Cheat +{player1.cheatPoint.Value}";
                player2Debug = $"Player 2: Cooperate +{player2.coopPoint.Value}";


                balanceScale.UpdateScore(player1.point.Value, player2.point.Value);
            }
            else if (player1.choice == PlayerLogic.playerChoice.Cheat && player2.choice == PlayerLogic.playerChoice.Cheat)
            {
                player1.point.Value += 0f;
                player2.point.Value += 0f;

                player1Debug = $"Player 1: Cheat +0";
                player2Debug = $"Player 2: Cheat +0";

                balanceScale.UpdateScore(player1.point.Value, player2.point.Value);
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

        }
        ResetPlayersChoice();
        GameManager.Instance.currentGameState = GameManager.GameState.TutorReady;
        GameManager.Instance.chessComponents[0].backToOriginal = true;
        GameManager.Instance.chessComponents[1].backToOriginal = true;

    }


    void CalculatePointWithGunAndCard()
    {
        if (gameEnded)
        {
            return;  // 如果游戏已经结束，则不再执行其他操作
        }

        // 计算完成后，构造调试信息字符串
        string player1Debug = $"Player 1: {player1.choice} +0"; 
        string player2Debug = $"Player 2: {player2.choice} +0";

        if (NetworkManager.LocalClientId == 0)
        {
            ApplyEffect();

            if (player1.choice == PlayerLogic.playerChoice.Cooperate && player2.choice == PlayerLogic.playerChoice.Cooperate)
            {
                float p1PointsBefore = player1.point.Value;
                float p2PointsBefore = player2.point.Value;
                
                player1.point.Value += player1.coopPoint.Value;
                player2.point.Value += player2.coopPoint.Value;

                // 计算增加了多少分
                int p1PointsAdded = Mathf.FloorToInt(player1.point.Value - p1PointsBefore);
                int p2PointsAdded = Mathf.FloorToInt(player2.point.Value - p2PointsBefore);
                
                // 生成硬币
                Coin.SpawnCoins(coinPrefab, player1ScoreAnchor, player1ScoreAnchor.position, p1PointsAdded);
                Coin.SpawnCoins(coinPrefab, player2ScoreAnchor, player2ScoreAnchor.position, p2PointsAdded);

                player1Debug = $"Player 1: Cooperate +{player1.coopPoint.Value}";
                player2Debug = $"Player 2: Cooperate +{player2.coopPoint.Value}";

                balanceScale.UpdateScore(player1.point.Value, player2.point.Value);
            }
            else if (player1.choice == PlayerLogic.playerChoice.Cooperate && player2.choice == PlayerLogic.playerChoice.Cheat)
            {
                float p1PointsBefore = player1.point.Value;
                float p2PointsBefore = player2.point.Value;
                
                player1.point.Value += player1.coopPoint.Value;
                player2.point.Value += player2.cheatPoint.Value;

                // 计算增加了多少分
                int p1PointsAdded = Mathf.FloorToInt(player1.point.Value - p1PointsBefore);
                int p2PointsAdded = Mathf.FloorToInt(player2.point.Value - p2PointsBefore);
                
                // 生成硬币
                Coin.SpawnCoins(coinPrefab, player1ScoreAnchor, player1ScoreAnchor.position, p1PointsAdded);
                Coin.SpawnCoins(coinPrefab, player2ScoreAnchor, player2ScoreAnchor.position, p2PointsAdded);

                player1Debug = $"Player 1: Cooperate +{player1.coopPoint.Value}";
                player2Debug = $"Player 2: Cheat +{player2.cheatPoint.Value}";

                Gun1.GetComponent<GunController>().FireGun();  // 触发玩家1的枪动画

                balanceScale.UpdateScore(player1.point.Value, player2.point.Value);
            }
            else if (player1.choice == PlayerLogic.playerChoice.Cheat && player2.choice == PlayerLogic.playerChoice.Cooperate)
            {
                float p1PointsBefore = player1.point.Value;
                float p2PointsBefore = player2.point.Value;
                
                player1.point.Value += player1.cheatPoint.Value;
                player2.point.Value += player2.coopPoint.Value;

                // 计算增加了多少分
                int p1PointsAdded = Mathf.FloorToInt(player1.point.Value - p1PointsBefore);
                int p2PointsAdded = Mathf.FloorToInt(player2.point.Value - p2PointsBefore);
                
                // 生成硬币
                Coin.SpawnCoins(coinPrefab, player1ScoreAnchor, player1ScoreAnchor.position, p1PointsAdded);
                Coin.SpawnCoins(coinPrefab, player2ScoreAnchor, player2ScoreAnchor.position, p2PointsAdded);

                player1Debug = $"Player 1: Cheat +{player1.cheatPoint.Value}";
                player2Debug = $"Player 2: Cooperate +{player2.coopPoint.Value}";

                Gun2.GetComponent<GunController>().FireGun();  // 触发玩家2的枪动画

                balanceScale.UpdateScore(player1.point.Value, player2.point.Value);
            }
            else if (player1.choice == PlayerLogic.playerChoice.Cheat && player2.choice == PlayerLogic.playerChoice.Cheat)
            {
                float p1PointsBefore = player1.point.Value;
                float p2PointsBefore = player2.point.Value;
                
                player1.point.Value += 0f;
                player2.point.Value += 0f;

                // 计算增加了多少分
                int p1PointsAdded = Mathf.FloorToInt(player1.point.Value - p1PointsBefore);
                int p2PointsAdded = Mathf.FloorToInt(player2.point.Value - p2PointsBefore);
                
                // 生成硬币 (这里应该是0个硬币，因为分数没有增加)
                Coin.SpawnCoins(coinPrefab, player1ScoreAnchor, player1ScoreAnchor.position, p1PointsAdded);
                Coin.SpawnCoins(coinPrefab, player2ScoreAnchor, player2ScoreAnchor.position, p2PointsAdded);

                player1Debug = $"Player 1: Cheat +0";
                player2Debug = $"Player 2: Cheat +0";

                Gun1.GetComponent<GunController>().FireGun();  // 触发玩家1的枪动画
                Gun2.GetComponent<GunController>().FireGun();  // 触发玩家2的枪动画

                balanceScale.UpdateScore(player1.point.Value, player2.point.Value);
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
                    uiManager.ShowGameOver("Player 2 is dead!");
                }
            }
            else if (Gun2.GetComponent<GunController>().gameEnded.Value)
            {
                // 玩家1死亡
                Debug.Log("Player 1 is dead! Game over.");
                gameEnded = true;
                if (uiManager != null)
                {
                    uiManager.ShowGameOver("Player 1 is dead!");
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
                    uiManager.ShowGameOver($"{totalRounds} rounds completed\n{winner}");
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
        GameManager.Instance.currentGameState = GameManager.GameState.Ready;
        GameManager.Instance.chessComponents[0].backToOriginal = true;
        GameManager.Instance.chessComponents[1].backToOriginal = true;

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
