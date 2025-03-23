using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }


    public enum GameState
    {
        Ready,
        PlayerTurn,
        CalculateTurn
    }

    public GameState currentGameState;

    public List<GameObject> playerObjs = new List<GameObject>();

    public List<PlayerLogic> playerComponents = new List<PlayerLogic>();

    public List<ChessLogic> chessComponents = new List<ChessLogic>();



    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Start 在第一次更新前调用
    void Start()
    {
    }

    void Update()
    {

    }

    public void SetPlayer1Choice(PlayerLogic.playerChoice playerChoice)
    {
        foreach (var player in playerComponents)
        {
            if (player.playerID == 1)
            {
                player.choice = playerChoice;
            }
        }
    }

    public void ChangeChess1ClickPoint(Transform clickPoint)
    {
        foreach (var chess in chessComponents)
        {
            if (chess.belonging == ChessLogic.Belonging.Player1)
            {
                chess.clickPoint = clickPoint;
            }
        }
    }
}
