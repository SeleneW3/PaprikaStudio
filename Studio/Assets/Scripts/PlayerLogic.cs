using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLogic : MonoBehaviour
{
    public int playerID;
    public string playerName;
    public int point;
    public List<GameObject> hand = new List<GameObject>();
    public enum playerChoice
    {
        None,
        Cooperate,
        Cheat
    }

    public playerChoice choice = playerChoice.None;

    void Start()
    {
        if(playerID == 1)
        {
            GameManager.Instance.player1 = gameObject;
            GameManager.Instance.Player1 = this;
        }
    }

    void Update()
    {
        
    }

    public void AddCard(GameObject card)
    {
        hand.Add(card);
    }

    public void RemoveCard(GameObject card)
    {
        hand.Remove(card);
    }
}
