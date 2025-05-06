using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public struct OnlinePlayerInfo : INetworkSerializable
{
    public ulong id;
    public bool isReady;
    public int displayIndex;  // 显示的玩家编号（0或1）

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref id);
        serializer.SerializeValue(ref isReady);
        serializer.SerializeValue(ref displayIndex);
    }
}

public class LobbyControl : NetworkBehaviour
{
    [Header("Required References")]
    [SerializeField] private Transform canvas;
    [SerializeField] private Transform content;
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private Button startButton;
    [SerializeField] private Button leaveButton;
    [SerializeField] private Toggle readyToggle;

    Dictionary<ulong, CellLogic> cellDictionary = new Dictionary<ulong, CellLogic>();
    Dictionary<ulong, OnlinePlayerInfo> allPlayerInfo = new Dictionary<ulong, OnlinePlayerInfo>();

    private int GetAvailablePlayerIndex()
    {
        if (!IsServer) return -1;

        var usedIndices = allPlayerInfo.Values.Select(p => p.displayIndex).ToList();
        
        if (!usedIndices.Contains(0)) return 0;
        if (!usedIndices.Contains(1)) return 1;
        
        Debug.LogWarning("Unexpected state: Both indices are already in use!");
        return 1;
    }

    public void AddPlayer(OnlinePlayerInfo playerInfo)
    {
        if (IsServer)
        {
            if (!allPlayerInfo.ContainsKey(playerInfo.id))
            {
                playerInfo.displayIndex = GetAvailablePlayerIndex();
            }
        }
        
        allPlayerInfo.Add(playerInfo.id, playerInfo);
        CreateUICell(playerInfo);

        if (IsServer)
        {
            // 服务器立即通知所有客户端更新
            UpdateAllPlayerInfo();
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
        }

        // 检查必要组件
        if (canvas == null)
        {
            Debug.LogError("Canvas reference is missing!");
            return;
        }

        if (content == null || cellPrefab == null || startButton == null || 
            leaveButton == null || readyToggle == null)
        {
            Debug.LogError("Some UI components are not assigned in the inspector!");
            return;
        }

        allPlayerInfo = new Dictionary<ulong, OnlinePlayerInfo>();
        allPlayerInfo = allPlayerInfo.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
        cellDictionary = new Dictionary<ulong, CellLogic>();

        startButton.onClick.AddListener(OnStartClick);
        leaveButton.onClick.AddListener(OnLeaveClick);
        readyToggle.onValueChanged.AddListener(OnReadyToggle);

        OnlinePlayerInfo playerInfo = new OnlinePlayerInfo();
        playerInfo.id = NetworkManager.LocalClientId;
        playerInfo.isReady = false;
        
        if (IsServer)
        {
            playerInfo.displayIndex = 0;
            AddPlayer(playerInfo);
        }
        else
        {
            // 客户端等待服务器分配索引
            NotifyServerOfJoinServerRpc(NetworkManager.LocalClientId);
        }

        base.OnNetworkSpawn();
    }

    [ServerRpc(RequireOwnership = false)]
    private void NotifyServerOfJoinServerRpc(ulong clientId)
    {
        OnlinePlayerInfo playerInfo = new OnlinePlayerInfo
        {
            id = clientId,
            isReady = false,
            displayIndex = GetAvailablePlayerIndex()
        };

        if (!allPlayerInfo.ContainsKey(clientId))
        {
            AddPlayer(playerInfo);
            UpdateAllPlayerInfo();
        }
    }

    private void OnClientConnected(ulong obj)
    {
        if (IsServer)
        {
            // 服务器端等待客户端的 NotifyServerOfJoinServerRpc 调用
            Debug.Log($"客户端已连接，等待其初始化请求，NetworkId: {obj}");
        }
    }

    [ClientRpc]
    void UpdatePlayerInfoClientRpc(OnlinePlayerInfo playerInfo)
    {
        if (!IsServer)  // 客户端处理
        {
            if (allPlayerInfo.ContainsKey(playerInfo.id))
            {
                // 更新现有玩家信息
                allPlayerInfo[playerInfo.id] = playerInfo;
                if (cellDictionary.ContainsKey(playerInfo.id))
                {
                    cellDictionary[playerInfo.id].Initial(playerInfo, playerInfo.displayIndex);
                }
            }
            else
            {
                // 添加新玩家
                AddPlayer(playerInfo);
            }
            // 刷新UI以确保正确的显示顺序
            RefreshUI();
        }
    }

    void UpdateAllPlayerInfo()
    {
        if (IsServer)
        {
            bool allReady = true;
            foreach (var playerInfo in allPlayerInfo.Values)
            {
                UpdatePlayerInfoClientRpc(playerInfo);
                if (!playerInfo.isReady)
                {
                    allReady = false;
                }
            }
            // 只有在服务器端且所有玩家都准备好时才显示开始按钮
            if (startButton != null)
            {
                startButton.gameObject.SetActive(allReady && allPlayerInfo.Count == 2);
            }
        }
    }

    void CreateUICell(OnlinePlayerInfo playerInfo)
    {
        GameObject cloneCell = Instantiate(cellPrefab);
        cloneCell.transform.SetParent(content, false);
        CellLogic cellLogic = cloneCell.GetComponent<CellLogic>();
        cellDictionary.Add(playerInfo.id, cellLogic);
        cellLogic.Initial(playerInfo, playerInfo.displayIndex);
        cloneCell.SetActive(true);
    }

    void ClearAllUICell()
    {
        foreach (var cell in cellDictionary)
        {
            Destroy(cell.Value.gameObject);
        }
        cellDictionary.Clear();
    }

    void RefreshUI()
    {
        ClearAllUICell();
        var sortedPlayers = allPlayerInfo.Values
            .OrderBy(info => info.displayIndex)
            .ToList();
        foreach (var playerInfo in sortedPlayers)
        {
            CreateUICell(playerInfo);
        }
    }

    private void OnReadyToggle(bool arg0)
    {
        if (cellDictionary.ContainsKey(NetworkManager.LocalClientId))
        {
            cellDictionary[NetworkManager.LocalClientId].SetReady(arg0);
            UpdatePlayerInfo(NetworkManager.LocalClientId, arg0);

            if (IsServer)
            {
                UpdateAllPlayerInfo();
            }
            else
            {
                UpdateAllPlayerInfosServerRpc(allPlayerInfo[NetworkManager.LocalClientId]);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void UpdateAllPlayerInfosServerRpc(OnlinePlayerInfo playerInfo)
    {
        allPlayerInfo[playerInfo.id] = playerInfo;
        if (cellDictionary.ContainsKey(playerInfo.id))
        {
            cellDictionary[playerInfo.id].SetReady(playerInfo.isReady);
        }
        UpdateAllPlayerInfo();
    }

    void UpdatePlayerInfo(ulong id, bool isReady)
    {
        if (allPlayerInfo.ContainsKey(id))
        {
            OnlinePlayerInfo info = allPlayerInfo[id];
            info.isReady = isReady;
            allPlayerInfo[id] = info;
        }
    }

    private void OnStartClick()
    {
        if (IsServer)
        {
            // 确保只有两个玩家
            if (allPlayerInfo.Count != 2)
            {
                Debug.LogError("需要正好两个玩家才能开始游戏！");
                return;
            }

            // 获取玩家列表（不包括主机）
            var clients = allPlayerInfo.Values
                .Where(p => p.id != NetworkManager.Singleton.LocalClientId)
                .OrderBy(p => p.id)
                .ToList();

            if (clients.Count > 0)
            {
                // 第一个连接的客户端是 Player1
                ulong player1Id = clients[0].id;
                ulong player2Id = NetworkManager.Singleton.LocalClientId; // 主机是 Player2

                // 设置玩家角色
                SetupPlayersServerRpc(player1Id, player2Id);
                
                // 加载游戏场景
                GameManager.Instance.LoadScene("Game");
            }
        }
    }

    [ServerRpc]
    private void SetupPlayersServerRpc(ulong player1Id, ulong player2Id)
    {
        Debug.Log($"服务器设置玩家角色 - Player1: {player1Id}, Player2: {player2Id}");
        SetupPlayersClientRpc(player1Id, player2Id);
    }

    [ClientRpc]
    private void SetupPlayersClientRpc(ulong player1Id, ulong player2Id)
    {
        // 设置玩家角色信息到 GameManager
        GameManager.Instance.SetPlayerRoles(player1Id, player2Id);
        
        string role = NetworkManager.Singleton.LocalClientId == player1Id ? "Player1" : 
                     NetworkManager.Singleton.LocalClientId == player2Id ? "Player2" : "Unknown";
        
        Debug.Log($"客户端收到角色设置 - 本地玩家ID: {NetworkManager.Singleton.LocalClientId}, 角色: {role}");
    }

    private void OnLeaveClick()
    {
        if (IsServer)  // 如果是房主
        {
            // 通知所有客户端房主解散了房间
            ReturnToStartClientRpc(true);
            // 关闭网络连接
            NetworkManager.Singleton.Shutdown();
            // 返回开始场景
            SceneManager.LoadScene("Start");
        }
        else  // 如果是普通玩家
        {
            // 通知服务器该玩家离开
            PlayerLeaveServerRpc(NetworkManager.LocalClientId);
            // 清理本地数据
            ClearAllUICell();
            allPlayerInfo.Clear();
            cellDictionary.Clear();
            // 断开网络连接
            NetworkManager.Singleton.Shutdown();
            // 返回开始场景
            SceneManager.LoadScene("Start");
        }
    }

    [ClientRpc]
    private void ReturnToStartClientRpc(bool isHostLeaving)
    {
        if (!IsServer)  // 避免房主重复执行
        {
            if (isHostLeaving)
            {
                Debug.Log("房主解散了房间");
            }
            // 清理本地数据
            ClearAllUICell();
            allPlayerInfo.Clear();
            cellDictionary.Clear();
            
            // 断开网络连接
            NetworkManager.Singleton.Shutdown();
            // 返回开始场景
            SceneManager.LoadScene("Start");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayerLeaveServerRpc(ulong clientId)
    {
        // 从玩家列表中移除
        if (allPlayerInfo.ContainsKey(clientId))
        {
            allPlayerInfo.Remove(clientId);
            // 通知其他客户端更新玩家列表
            PlayerLeftNotificationClientRpc(clientId);
        }
    }

    [ClientRpc]
    private void PlayerLeftNotificationClientRpc(ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId != clientId) // 不处理离开的玩家自己
        {
            // 从UI中移除离开的玩家
            if (cellDictionary.ContainsKey(clientId))
            {
                Destroy(cellDictionary[clientId].gameObject);
                cellDictionary.Remove(clientId);
            }
            
            // 从玩家信息中移除
            if (allPlayerInfo.ContainsKey(clientId))
            {
                allPlayerInfo.Remove(clientId);
            }

            Debug.Log($"玩家 {clientId} 离开了房间");
            
            // 刷新UI以确保显示正确
            RefreshUI();
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (IsServer)
        {
            // 服务器端处理客户端断开连接
            if (allPlayerInfo.ContainsKey(clientId))
            {
                allPlayerInfo.Remove(clientId);
                PlayerLeftNotificationClientRpc(clientId);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        base.OnNetworkDespawn();
    }
}
