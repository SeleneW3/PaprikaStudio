using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Linq;

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

    public static event Action OnPlayersReady;



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

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayer1ChoiceServerRpc(PlayerLogic.playerChoice playerChoice)
    {
        foreach (var player in playerComponents)
        {
            if (player.playerID == 1)
            {
                player.choice = playerChoice;
            }
        }
        SetPlayer1ChoiceClientRpc(playerChoice);
    }

    [ClientRpc]
    public void SetPlayer1ChoiceClientRpc(PlayerLogic.playerChoice playerChoice)
    {
        foreach (var player in playerComponents)
        {
            if (player.playerID == 1)
            {
                player.choice = playerChoice;
            }
        }
    }

    [ClientRpc]
    public void SetPlayer2ChoiceClientRpc(PlayerLogic.playerChoice playerChoice)
    {
        foreach (var player in playerComponents)
        {
            if (player.playerID == 2)
            {
                player.choice = playerChoice;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayer2ChoiceServerRpc(PlayerLogic.playerChoice playerChoice)
    {
        foreach (var player in playerComponents)
        {
            if (player.playerID == 2)
            {
                player.choice = playerChoice;
            }
        }
        SetPlayer2ChoiceClientRpc(playerChoice);
    }


    public void ChangeChess1ClickPoint(Transform clickPoint)
    {
        foreach (var chess in chessComponents)
        {
            if (chess.belonging == ChessLogic.Belonging.Player1)
            {
                chess.clickPoint = clickPoint;
                chess.Move();
            }
        }
    }

    public void ChangeChess2ClickPoint(Transform clickPoint)
    {
        foreach (var chess in chessComponents)
        {
            if (chess.belonging == ChessLogic.Belonging.Player2)
            {
                chess.clickPoint = clickPoint;
                chess.Move();
            }
        }
    }

    public void AddPlayer(PlayerLogic player)
    {
        if (!playerComponents.Contains(player))
        {
            if(player.playerID == 1)
            {
                GameManager.Instance.playerComponents[0] = player;
                GameManager.Instance.playerObjs[0] = player.gameObject;
            }
            else if(player.playerID == 2)
            {
                GameManager.Instance.playerComponents[1] = player;
                GameManager.Instance.playerObjs[1] = player.gameObject;
            }
        }

        if (playerComponents[1] != null && playerComponents[0] != null)
        {
            OnPlayersReady?.Invoke();
        }
    }
}
