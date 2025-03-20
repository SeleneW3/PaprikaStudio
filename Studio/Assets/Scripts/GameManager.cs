using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameObject player1;
    public GameObject player2;

    public enum GameState
    {
        Ready,
        PlayerTurn,
        CalculateTurn
    }

    public GameState currentGameState;

    public PlayerLogic Player1;
    public PlayerLogic Player2;

    public ChessLogic chess1;
    public ChessLogic chess2;

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
        Player1.choice = playerChoice;
    }

    public void ChangeChess1ClickPoint(Transform clickPoint)
    {
        chess1.SetClickPoint(clickPoint);
        chess1.Move();
    }
}
