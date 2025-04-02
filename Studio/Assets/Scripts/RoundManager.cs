using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    public float baseBet = 1f;
    public float betMultiplier = 2f;

    public PlayerLogic player1;
    public PlayerLogic player2;

    public DeckLogic deckLogic;

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
            
        }
        else if(GameManager.Instance.currentGameState == GameManager.GameState.CalculateTurn)
        {
            
            CalculatePoint();
            ChessMoveBack();
            ResetChess();
            ResetPlayersChoice();
            ResetPlayers();
            GameManager.Instance.currentGameState = GameManager.GameState.Ready;
        }


    }

    void CalculatePoint()
    {
        ApplyEffect();

        if(player1.choice == PlayerLogic.playerChoice.Cooperate && player2.choice == PlayerLogic.playerChoice.Cooperate)
        {
            player1.point += player1.coopPoint;
            player2.point += player2.coopPoint;
        }
        else if(player1.choice == PlayerLogic.playerChoice.Cooperate && player2.choice == PlayerLogic.playerChoice.Cheat)
        {
            player1.point += player1.coopPoint;
            player2.point += player2.cheatPoint;
        }
        else if(player1.choice == PlayerLogic.playerChoice.Cheat && player2.choice == PlayerLogic.playerChoice.Cooperate)
        {
            player1.point += player1.cheatPoint;   
            player2.point += player2.coopPoint;
        }
        else if(player1.choice == PlayerLogic.playerChoice.Cheat && player2.choice == PlayerLogic.playerChoice.Cheat)
        {
            player1.point += 0f;
            player2.point += 0f;
        }

       ResetPlayersChoice();
        GameManager.Instance.currentGameState = GameManager.GameState.Ready;
        GameManager.Instance.chessComponents[0].backToOriginal = true;
        GameManager.Instance.chessComponents[1].backToOriginal = true;

    }



    //###############################################

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
        player1.SetSelectCardServerRpc(false);
        player2.SetSelectCardServerRpc(false);
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
