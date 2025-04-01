using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerLogic : NetworkBehaviour
{
    public int playerID;
    public string playerName;
    public float point;
    public float coopPoint = 3f;
    public float cheatPoint = 5f;
    public List<CardLogic> hand = new List<CardLogic>();
    public Transform handPos;

    public NetworkVariable<bool> selectCard = new NetworkVariable<bool>(
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

    void Start()
    {
        GameManager.Instance.AddPlayer(this);
        coopPoint = 3f;
        cheatPoint = 5f;
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
        coopPoint = 3f;
        cheatPoint = 5f;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetSelectCardServerRpc(bool value)
    {
        selectCard.Value = value;
    }

    [ClientRpc]
    public void SetSelectCardClientRpc(bool value)
    {
        selectCard.Value = value;
    }
}
