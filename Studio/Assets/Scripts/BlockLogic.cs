using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
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
        if(NetworkManager.Singleton.LocalClientId == 0)
        {
            if (belonging == Belonging.Player1 && type == Type.Cooperate)
            {
                GameManager.Instance.SetPlayer1ChoiceServerRpc(PlayerLogic.playerChoice.Cooperate);
                GameManager.Instance.ChangeChess1ClickPointServerRpc(transform.position, transform.rotation);
            }
            else if (belonging == Belonging.Player1 && type == Type.Cheat)
            {
                GameManager.Instance.SetPlayer1ChoiceServerRpc(PlayerLogic.playerChoice.Cheat);
                GameManager.Instance.ChangeChess1ClickPointServerRpc(transform.position, transform.rotation);
            }
        }



        if (NetworkManager.Singleton.LocalClientId == 1)
        {
            if (belonging == Belonging.Player2 && type == Type.Cooperate)
            {
                GameManager.Instance.SetPlayer2ChoiceServerRpc(PlayerLogic.playerChoice.Cooperate);
                GameManager.Instance.ChangeChess2ClickPointServerRpc(transform.position,transform.rotation);
            }
            else if (belonging == Belonging.Player2 && type == Type.Cheat)
            {
                GameManager.Instance.SetPlayer2ChoiceServerRpc(PlayerLogic.playerChoice.Cheat);
                GameManager.Instance.ChangeChess2ClickPointServerRpc(transform.position, transform.rotation);
            }
        }
            

        
    }
}
