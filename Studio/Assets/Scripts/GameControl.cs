using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameControl : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.Instance.playerComponents[0].choice != PlayerLogic.playerChoice.None && GameManager.Instance.playerComponents[1].choice != PlayerLogic.playerChoice.None)
        {
            bool allChessOnGround = true;
            foreach (var chess in GameManager.Instance.chessComponents)
            {
                
                if (!chess.isOnGround)
                {
                    allChessOnGround = false;
                    break;
                }
            }

            if(allChessOnGround)
            {
                GameManager.Instance.currentGameState = GameManager.GameState.CalculateTurn;
            }
            
        }

    }
}
