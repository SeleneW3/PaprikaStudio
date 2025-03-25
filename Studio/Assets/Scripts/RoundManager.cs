using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    public float baseBet = 1f;
    public float betMultiplier = 2f;
    public PlayerLogic player1;
    public PlayerLogic player2;

    void Start()
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
        }
    }

    void CalculatePoint()
    {
        ApplyEffectToAllHandCards();

        if(player1.choice == PlayerLogic.playerChoice.Cooperate && player2.choice == PlayerLogic.playerChoice.Cooperate)
        {

        }
        else if(player1.choice == PlayerLogic.playerChoice.Cooperate && player2.choice == PlayerLogic.playerChoice.Cheat)
        {
            player2.point += baseBet * betMultiplier;
        }
        else if(player1.choice == PlayerLogic.playerChoice.Cheat && player2.choice == PlayerLogic.playerChoice.Cooperate)
        {
            player1.point += baseBet * betMultiplier;
        }
        else if(player1.choice == PlayerLogic.playerChoice.Cheat && player2.choice == PlayerLogic.playerChoice.Cheat)
        {
        }
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
}
