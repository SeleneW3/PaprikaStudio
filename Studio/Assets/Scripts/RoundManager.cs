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

    public PlayerLogic player1;
    public PlayerLogic player2;

    public GameObject Gun1;  // 玩家1的枪对象
    public GameObject Gun2;  // 玩家2的枪对象


    private int currentRound = 0;  // 当前回合计数器
    private bool gameEnded = false;


    void Start()
    {
        // 初始化回合
        currentRound = 1;

        if (dialogManager != null)
        {
            Debug.Log("Dialog Manager found, starting dialog");
            dialogManager.StartDialog();
        }
        else
        {
            Debug.LogError("Dialog Manager is not assigned in the inspector!");
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
        if(GameManager.Instance.currentGameState == GameManager.GameState.Ready)
        {
            
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
           
            
            CalculatePoint();
            ChessMoveBack();
            ResetChess();
            ResetPlayersChoice();
            ResetPlayers();
            GameManager.Instance.currentGameState = GameManager.GameState.Ready;

             if (roundText != null)
            {
                roundText.text = "Round " + currentRound;
            }

            // 设置游戏状态为准备阶段，进入下一回合前的等待
            GameManager.Instance.currentGameState = GameManager.GameState.Ready;
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


    void CalculatePoint()
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
            }
            else if (player1.choice == PlayerLogic.playerChoice.Cooperate && player2.choice == PlayerLogic.playerChoice.Cheat)
            {
                player1.point.Value += player1.coopPoint.Value;
                player2.point.Value += player2.cheatPoint.Value;

                player1Debug = $"Player 1: Cooperate +{player1.coopPoint.Value}";
                player2Debug = $"Player 2: Cheat +{player2.cheatPoint.Value}";

                Gun1.GetComponent<GunController>().FireGun();  // 触发玩家1的枪动画
            }
            else if (player1.choice == PlayerLogic.playerChoice.Cheat && player2.choice == PlayerLogic.playerChoice.Cooperate)
            {
                player1.point.Value += player1.cheatPoint.Value;
                player2.point.Value += player2.coopPoint.Value;

                player1Debug = $"Player 1: Cheat +{player1.cheatPoint.Value}";
                player2Debug = $"Player 2: Cooperate +{player2.coopPoint.Value}";

                Gun2.GetComponent<GunController>().FireGun();  // 触发玩家2的枪动画
            }
            else if (player1.choice == PlayerLogic.playerChoice.Cheat && player2.choice == PlayerLogic.playerChoice.Cheat)
            {
                player1.point.Value += 0f;
                player2.point.Value += 0f;

                player1Debug = $"Player 1: Cheat +0";
                player2Debug = $"Player 2: Cheat +0";

                Gun1.GetComponent<GunController>().FireGun();  // 触发玩家1的枪动画
                Gun2.GetComponent<GunController >().FireGun();  // 触发玩家2的枪动画
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
            }
            else if (Gun2.GetComponent<GunController>().gameEnded.Value)
            {
                // 玩家1死亡
                Debug.Log("Player 1 is dead! Game over.");
                gameEnded = true;
            }

            // 检查是否回合数达到 5
            if (!gameEnded && currentRound >= 5)
            {
                Debug.Log("5 rounds completed. Game over.");
                gameEnded = true;
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
