using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    public float baseBet = 1f;
    public float betMultiplier = 2f;

    public PlayerLogic player1;
    public PlayerLogic player2;

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
            GameManager.Instance.currentGameState = GameManager.GameState.Ready;
        }


    }

    void CalculatePoint()
    {
        //ApplyEffectToAllHandCards();

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

    void ApplyEffectToAllHandCards( )
    {
        foreach(PlayerLogic player in GameManager.Instance.playerComponents)
        {
            foreach(CardLogic card in player.hand)
            {
                card.OnEffect();
            }
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
