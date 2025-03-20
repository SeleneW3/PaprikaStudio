using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockLogic : MonoBehaviour
{
    public enum Belonging
    {
        Player1,
        Player2
    }

    public enum Type
    {
        Cooperate,
        Cheat
    }

    public Belonging belonging;
    public Type type;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnMouseDown()
    {
        if(belonging == Belonging.Player1 && type == Type.Cooperate)
        {
            GameManager.Instance.SetPlayer1Choice(PlayerLogic.playerChoice.Cooperate);
            GameManager.Instance.ChangeChess1ClickPoint(transform);
        }
        else if(belonging == Belonging.Player1 && type == Type.Cheat)
        {
            GameManager.Instance.SetPlayer1Choice(PlayerLogic.playerChoice.Cheat);
            GameManager.Instance.ChangeChess1ClickPoint(transform);
        }

        
    }
}
