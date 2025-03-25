using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLogic : MonoBehaviour
{
    public int playerID;
    public string playerName;
    public float point;
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
        // GameManager.Instance.playerComponents.Add(this);
        // GameManager.Instance.playerObjs.Add(gameObject);
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
}
