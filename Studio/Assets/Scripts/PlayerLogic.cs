using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLogic : MonoBehaviour
{
    public int playerID;
    public string playerName;
    public float point;
    public float coopPoint = 3f;
    public float cheatPoint = 5f;
    public List<CardLogic> hand = new List<CardLogic>();
    public Transform handPos;
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
}
