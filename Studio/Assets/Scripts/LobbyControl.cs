using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public struct OnlinePlayerInfo : INetworkSerializable
{
    public ulong id;
    public bool isReady;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref id);
        serializer.SerializeValue(ref isReady);
    }
}

public class LobbyControl : NetworkBehaviour
{
    [SerializeField]
    Transform canvas;
    
    [Header("Sound Effects")]
    [SerializeField] private string uiClickSound = "ButtonClick"; // UI点击音效

    Transform content;
    GameObject cell;
    Button startButton;
    Button exitButton;
    Toggle readyToggle;

    Dictionary<ulong, CellLogic> cellDictionary = new Dictionary<ulong, CellLogic>();
    Dictionary<ulong, OnlinePlayerInfo> allPlayerInfo = new Dictionary<ulong, OnlinePlayerInfo>();


    public void AddPlayer(OnlinePlayerInfo playerInfo)
    {
        allPlayerInfo.Add(playerInfo.id, playerInfo);

        CreateUICell(playerInfo);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
        }

        allPlayerInfo = new Dictionary<ulong, OnlinePlayerInfo>();
        allPlayerInfo = allPlayerInfo.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);

        content = canvas.Find("PlayerList/Viewport/Content");
        cell = content.Find("Cell").gameObject;
        startButton = canvas.Find("StartButton").GetComponent<Button>();
        exitButton = canvas.Find("ExitButton").GetComponent<Button>();
        readyToggle = canvas.Find("Ready?").GetComponent<Toggle>();

        startButton.onClick.AddListener(OnStartClick);
        exitButton.onClick.AddListener(OnExitClick);
        readyToggle.onValueChanged.AddListener(OnReadyToggle);

        cellDictionary = new Dictionary<ulong, CellLogic>();

        OnlinePlayerInfo playerInfo = new OnlinePlayerInfo();
        playerInfo.id = NetworkManager.LocalClientId;
        playerInfo.isReady = false;


        AddPlayer(playerInfo);

        base.OnNetworkSpawn();
    }

    private void OnClientConnected(ulong obj)
    {
        OnlinePlayerInfo playerInfo = new OnlinePlayerInfo();
        playerInfo.id = obj;
        playerInfo.isReady = false;
        AddPlayer(playerInfo);

        UpdateAllPlayerInfo();
    }

    void UpdateAllPlayerInfo()
    {
        bool allReady = true;
        foreach (var item in allPlayerInfo)
        {
            UpdatePlayerInfoClientRpc(item.Value);
            if (!item.Value.isReady)
            {
                allReady = false;
            }
        }
        startButton.gameObject.SetActive(allReady);
    }

    [ClientRpc]
    void UpdatePlayerInfoClientRpc(OnlinePlayerInfo playerInfo)
    {
        if (!IsServer)
        {
            if (allPlayerInfo.ContainsKey(playerInfo.id))
            {
                allPlayerInfo[playerInfo.id] = playerInfo;
            }
            else
            {
                
                AddPlayer(playerInfo);
                RefreshUI();
            }
            UpdatePlayerCells();
        }
    }

    private void UpdatePlayerCells()
    {
        foreach(var item in allPlayerInfo)
        {
            cellDictionary[item.Key].SetReady(item.Value.isReady);
        }
    }

    void CreateUICell(OnlinePlayerInfo playerInfo)
    {
        GameObject cloneCell = Instantiate(cell);
        cloneCell.transform.SetParent(content, false);
        CellLogic cellLogic = cloneCell.GetComponent<CellLogic>();
        cellDictionary.Add(playerInfo.id, cellLogic);
        cellLogic.Initial(playerInfo);
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
        var sortedPlayers = allPlayerInfo.Values.OrderBy(info => info.id).ToList();
        foreach (var playerInfo in sortedPlayers)
        {
            CreateUICell(playerInfo);
        }
    }

    private void OnReadyToggle(bool arg0)
    {
        // 播放UI点击音效
        PlayUIClickSound();
        
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

    [ServerRpc(RequireOwnership = false)]
    void UpdateAllPlayerInfosServerRpc(OnlinePlayerInfo playerInfo)
    {
        allPlayerInfo[playerInfo.id] = playerInfo;
        cellDictionary[playerInfo.id].SetReady(playerInfo.isReady);
        UpdateAllPlayerInfo();
    }

    void UpdatePlayerInfo(ulong id, bool isReady)
    {
        OnlinePlayerInfo info = allPlayerInfo[id];
        info.isReady = isReady;
        allPlayerInfo[id] = info;
    }

    private void OnStartClick()
    {
        // 播放UI点击音效
        PlayUIClickSound();
        
        GameManager.Instance.LoadScene("Game");
    }

    private void OnExitClick()
    {
        // 播放UI点击音效
        PlayUIClickSound();
        
        ExitGame();
    }
    
    // 播放UI点击音效
    private void PlayUIClickSound()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(uiClickSound);
        }
    }

    private void ExitGame()
    {
        if (NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.IsHost)
            {
                NetworkManager.Singleton.Shutdown();
            }
            else if (NetworkManager.Singleton.IsClient)
            {
                NetworkManager.Singleton.Shutdown();
            }
        }

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitGame();
        }
    }
}
