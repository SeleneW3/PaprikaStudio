using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    public float baseBet = 1f;
    public float betMultiplier = 2f;

    void Start()
    {
        
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
        if(GameManager.Instance.Player1.choice == PlayerLogic.playerChoice.Cooperate && GameManager.Instance.Player2.choice == PlayerLogic.playerChoice.Cooperate)
        {

        }
        else if(GameManager.Instance.Player1.choice == PlayerLogic.playerChoice.Cooperate && GameManager.Instance.Player2.choice == PlayerLogic.playerChoice.Cheat)
        {

        }
        else if(GameManager.Instance.Player1.choice == PlayerLogic.playerChoice.Cheat && GameManager.Instance.Player2.choice == PlayerLogic.playerChoice.Cooperate)
        {

        }
        else if(GameManager.Instance.Player1.choice == PlayerLogic.playerChoice.Cheat && GameManager.Instance.Player2.choice == PlayerLogic.playerChoice.Cheat)
        {

        }
    }
}
