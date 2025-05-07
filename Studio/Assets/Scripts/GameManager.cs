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

    // 添加玩家ID存储
    private ulong player1NetworkId;
    private ulong player2NetworkId;
    
    // 添加网络变量来同步玩家ID
    public NetworkVariable<ulong> networkPlayer1Id = new NetworkVariable<ulong>();
    public NetworkVariable<ulong> networkPlayer2Id = new NetworkVariable<ulong>();

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

    public void SetPlayerRoles(ulong player1Id, ulong player2Id)
    {
        player1NetworkId = player1Id;
        player2NetworkId = player2Id;
        
        if (IsServer)
        {
            networkPlayer1Id.Value = player1Id;
            networkPlayer2Id.Value = player2Id;
        }

        // 清理现有的玩家组件列表
        playerComponents.Clear();
        playerObjs.Clear();
        
        Debug.Log($"设置玩家角色 - 本地玩家ID: {NetworkManager.Singleton.LocalClientId}");
        Debug.Log($"Player1(Host) ID: {player1NetworkId}, Player2(Client) ID: {player2NetworkId}");
    }

    public bool IsPlayer1()
    {
        return NetworkManager.Singleton.LocalClientId == networkPlayer1Id.Value;
    }

    public bool IsPlayer2()
    {
        return NetworkManager.Singleton.LocalClientId == networkPlayer2Id.Value;
    }

    public void AddPlayer(PlayerLogic player)
    {
        if (!playerComponents.Contains(player))
        {
            // 根据网络ID设置玩家ID
            if (player.OwnerClientId == networkPlayer1Id.Value)
            {
                player.playerID = 1;
                // 确保Player1总是在列表的第一个位置
                if (playerComponents.Count > 0 && playerComponents[0].playerID != 1)
                {
                    playerComponents.Insert(0, player);
                    playerObjs.Insert(0, player.gameObject);
                }
                else
                {
                    playerComponents.Add(player);
                    playerObjs.Add(player.gameObject);
                }
                Debug.Log($"添加Player1，NetworkId: {player.OwnerClientId}");
            }
            else if (player.OwnerClientId == networkPlayer2Id.Value)
            {
                player.playerID = 2;
                playerComponents.Add(player);
                playerObjs.Add(player.gameObject);
                Debug.Log($"添加Player2，NetworkId: {player.OwnerClientId}");
            }
            else
            {
                Debug.LogError($"未知的玩家NetworkId: {player.OwnerClientId}，当前Player1Id: {networkPlayer1Id.Value}, Player2Id: {networkPlayer2Id.Value}");
                return;
            }
        }
        
        if (playerComponents.Count == 2)
        {
            // 确保玩家顺序正确
            if (playerComponents[0].playerID != 1)
            {
                // 交换 PlayerLogic 组件
                PlayerLogic tempComponent = playerComponents[0];
                playerComponents[0] = playerComponents[1];
                playerComponents[1] = tempComponent;

                // 交换对应的 GameObject
                GameObject tempObject = playerObjs[0];
                playerObjs[0] = playerObjs[1];
                playerObjs[1] = tempObject;
            }
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
