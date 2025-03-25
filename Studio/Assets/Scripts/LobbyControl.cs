using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyControl : NetworkBehaviour
{
    [SerializeField]
    Transform canvas;

    Transform content;
    GameObject cell;
    Button startButton;
    Toggle readyToggle;

    List<CellLogic> cellList = new List<CellLogic>();


    public void AddPlayer(ulong playerID, bool isReady)
    {
        GameObject cloneCell = Instantiate(cell);
        cloneCell.transform.SetParent(content,false);
        CellLogic cellLogic = cloneCell.GetComponent<CellLogic>();
        cellList.Add(cellLogic);
        cellLogic.Initial(playerID,isReady);
        cloneCell.SetActive(true);
    }

    public override void OnNetworkSpawn()
    {
        content = canvas.Find("PlayerList/Viewport/Content");
        cell = content.Find("Cell").gameObject;
        startButton = canvas.Find("StartButton").GetComponent<Button>();
        readyToggle = canvas.Find("Ready?").GetComponent<Toggle>();

        startButton.onClick.AddListener(OnStartClick);
        readyToggle.onValueChanged.AddListener(OnReadyToggle);

        AddPlayer(NetworkManager.LocalClientId,false);

        base.OnNetworkSpawn();
    }

    private void OnReadyToggle(bool arg0)
    {
        throw new NotImplementedException();
    }

    private void OnStartClick()
    {
        throw new NotImplementedException();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
