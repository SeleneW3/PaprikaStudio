using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }


    public enum GameState
    {
        Ready,
        PlayerTurn,
        CalculateTurn
    }

    public string ip = "127.0.0.1";

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
        if(SceneManager.GetActiveScene().name == "Init")
        {
            SceneManager.LoadScene("Start");
        }
    }

    void Update()
    {

    }

    public void LoadScene(string sceneName)
    {

        NetworkManager.SceneManager.LoadScene(sceneName , LoadSceneMode.Single);
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
