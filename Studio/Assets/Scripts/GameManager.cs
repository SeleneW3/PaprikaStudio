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
        TutorReady,
        TutorPlayerTurn,
        TutorCalculateTurn,
        Ready,
        PlayerTurn,
        CalculateTurn
    }

    public string localIP = "127.0.0.1";
    public string joinIP = "127.0.0.1";

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

    // Start �ڵ�һ�θ���ǰ����
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

    [ServerRpc(RequireOwnership = false)]
    public void ChangeChess1StateServerRpc()
    {
        foreach (var chess in chessComponents)
        {
            if (chess.belonging == ChessLogic.Belonging.Player1)
            {
                if(chess.state == 0)
                {
                    chess.state = 1;
                    chess.timer = 0f;
                }
            }
        }
        ChangeChess1StateClientRpc();
    }

    [ClientRpc]
    public void ChangeChess1StateClientRpc()
    {
        foreach (var chess in chessComponents)
        {
            if (chess.belonging == ChessLogic.Belonging.Player1)
            {
                if (chess.state == 0)
                {
                    chess.state = 1;
                    chess.timer = 0f;
                }
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ChangeChess2StateServerRpc()
    {
        foreach (var chess in chessComponents)
        {
            if (chess.belonging == ChessLogic.Belonging.Player2)
            {
                if (chess.state == 0)
                {
                    chess.state = 1;
                    chess.timer = 0f;
                }
            }
        }
        
    }

    [ClientRpc]
    public void ChangeChess2StateClientRpc()
    {
        foreach (var chess in chessComponents)
        {
            if (chess.belonging == ChessLogic.Belonging.Player2)
            {
                if (chess.state == 0)
                {
                    chess.state = 1;
                    chess.timer = 0f;
                }
            }
        }
    }

    [ServerRpc (RequireOwnership = false)]
    public void ChangeChess1ClickPointServerRpc(Vector3 clickpointpos, Quaternion clickrot)
    {
        foreach (var chess in chessComponents)
        {
            if (chess.belonging == ChessLogic.Belonging.Player1)
            {
                chess.clickPointRot = clickrot;
                chess.clickPointPos = clickpointpos;
            }
        }
        ChangeChess1ClickPointClientRpc(clickpointpos, clickrot);
    }

    [ClientRpc]
    public void ChangeChess1ClickPointClientRpc(Vector3 clickpointpos, Quaternion clickrot)
    {
        foreach (var chess in chessComponents)
        {
            if (chess.belonging == ChessLogic.Belonging.Player1)
            {
                chess.clickPointRot = clickrot;
                chess.clickPointPos = clickpointpos;
            }
        }
    }

    [ServerRpc (RequireOwnership = false)]
    public void ChangeChess2ClickPointServerRpc(Vector3 clickpointpos, Quaternion clickrot)
    {
        foreach (var chess in chessComponents)
        {
            if (chess.belonging == ChessLogic.Belonging.Player2)
            {
                chess.clickPointRot = clickrot;
                chess.clickPointPos = clickpointpos;
            }
        }
        ChangeChess2ClickPointClientRpc(clickpointpos, clickrot);
    }

    [ClientRpc]
    public void ChangeChess2ClickPointClientRpc(Vector3 clickpointpos, Quaternion clickrot)
    {
        foreach (var chess in chessComponents)
        {
            if (chess.belonging == ChessLogic.Belonging.Player2)
            {
                chess.clickPointRot = clickrot;
                chess.clickPointPos = clickpointpos;
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

    [ServerRpc(RequireOwnership = false)]
    public void ResetAllBlocksServerRpc()
    {
        // 找到所有的 BlockLogic 组件并重置它们
        BlockLogic[] blocks = FindObjectsOfType<BlockLogic>();
        foreach (var block in blocks)
        {
            block.ResetState();
        }
        ResetAllBlocksClientRpc();
    }

    [ClientRpc]
    public void ResetAllBlocksClientRpc()
    {
        // 在客户端也执行相同的重置操作
        BlockLogic[] blocks = FindObjectsOfType<BlockLogic>();
        foreach (var block in blocks)
        {
            block.ResetState();
        }
    }
}