using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

public class PlayerLogic : NetworkBehaviour
{
    public int playerID;
    public string playerName;
    public NetworkVariable<float> point = new NetworkVariable<float>(
    0f, 
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
    );

    public NetworkVariable<float> coopPoint = new NetworkVariable<float>(
    0f, 
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
    );

    public NetworkVariable<float> cheatPoint = new NetworkVariable<float>(
    0f,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
    );


    //######################新增显示每轮选择和分数########################################
    public NetworkVariable<FixedString64Bytes> debugInfo = new NetworkVariable<FixedString64Bytes>(
    new FixedString64Bytes(""),
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
    );

    // 新增 GunController 字段
    public GunController gunController;


    public List<CardLogic> hand = new List<CardLogic>();
    public Transform handPos;

    public NetworkVariable<bool> usedCard = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public enum playerChoice
    {
        None,
        Cooperate,
        Cheat
    }

    public playerChoice choice = playerChoice.None;

    public CardLogic selectCard = null;

    void Start()
    {
        GameManager.Instance.AddPlayer(this);
        if(NetworkManager.LocalClientId == 0)
        {
            coopPoint.Value = 3f;
            cheatPoint.Value = 5f;
        }
    }

    void Update()
    {
        
    }

    public void AddCard(CardLogic card)
    {
        hand.Add(card);
    }

    public void RemoveCard(CardLogic card)
    {
        hand.Remove(card);
    }

    public void ResetToInitial()
    {
        if (NetworkManager.LocalClientId == 0)
        {
            coopPoint.Value = 3f;
            cheatPoint.Value = 5f;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetUsedCardServerRpc(bool value)
    {
        usedCard.Value = value;
    }

    [ClientRpc]
    public void SetUsedCardClientRpc(bool value)
    {
        usedCard.Value = value;
    }
}
