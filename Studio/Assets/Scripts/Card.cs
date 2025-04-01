using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CardLogic : NetworkBehaviour
{
    public enum Effect
    {
        None,
        ReversePoint,
        ReverseChoice,
        DoubleCheatPoint,
        DoubleCoopPoint,
    }
    public NetworkVariable<Effect> effectNetwork = new NetworkVariable<Effect>(Effect.None);

    public Effect effect
    {
        get { return effectNetwork.Value; }
        set { effectNetwork.Value = value; }
    }

    public enum Belong
    {
        Player1,
        Player2,
    }
    public Belong belong;

    public bool isOut = false;

    public void OnEffect()
    {
        if (effect == Effect.ReversePoint)
        {
            ReversePoint();
        }
        else if (effect == Effect.DoubleCoopPoint)
        {
            DoubleCoopPoint();
        }
        else if (effect == Effect.DoubleCheatPoint)
        {
            DoubleCheatPoint();
        }
        else if (effect == Effect.ReverseChoice)
        {
            ReverseChoice();
        }
        else
        {
            NoneEffect();
        }
    }

    public void DoubleCheatPoint()
    {
        if(belong == Belong.Player1)
        {
            GameManager.Instance.playerComponents[0].cheatPoint *= 2;
        }
        else if(belong == Belong.Player2)
        {
            GameManager.Instance.playerComponents[1].cheatPoint *= 2;
        }
    }

    public void DoubleCoopPoint()
    {
        if (belong == Belong.Player1)
        {
            GameManager.Instance.playerComponents[0].coopPoint *= 2;
        }
        else if (belong == Belong.Player2)
        {
            GameManager.Instance.playerComponents[1].coopPoint *= 2;
        }
    }

    public void ReversePoint()
    {
        float point = GameManager.Instance.playerComponents[1].point;
        GameManager.Instance.playerComponents[1].point = GameManager.Instance.playerComponents[0].point;
        GameManager.Instance.playerComponents[0].point = point;
    }

    public void ReverseChoice()
    {
        PlayerLogic.playerChoice choice = GameManager.Instance.playerComponents[1].choice;
        GameManager.Instance.playerComponents[1].choice = GameManager.Instance.playerComponents[0].choice;
        GameManager.Instance.playerComponents[0].choice = choice;
    }

    public void NoneEffect()
    {
        return;
    }

    private void OnMouseEnter()
    {
        if (!isOut)
        {
            if(NetworkManager.LocalClientId == 0 && GetComponentInParent<HandCardLogic>().belong == HandCardLogic.Belong.Player1)
            {
                Debug.Log("Mouse Entered1");
                GetComponentInParent<HandCardLogic>().Open();
            }
            else if(NetworkManager.LocalClientId == 1 && GetComponentInParent<HandCardLogic>().belong == HandCardLogic.Belong.Player2)
            {
                GetComponentInParent<HandCardLogic>().Open();
            }

        }
        Debug.Log("Mouse Entered");
    }

    private void OnMouseExit()
    {
        if (!isOut)
        {
            if(NetworkManager.LocalClientId == 0 && GetComponentInParent<HandCardLogic>().belong == HandCardLogic.Belong.Player1)
            {
                GetComponentInParent<HandCardLogic>().Close();
            }
            else if(NetworkManager.LocalClientId == 1 && GetComponentInParent<HandCardLogic>().belong == HandCardLogic.Belong.Player2)
            {
                GetComponentInParent<HandCardLogic>().Close();
            }
        }
        Debug.Log("Mouse Exited");
    }

    private void OnMouseDown()
    {
        if(NetworkManager.LocalClientId== 0 && GetComponentInParent<HandCardLogic>().belong == HandCardLogic.Belong.Player1)
        {
            if (GameManager.Instance.playerComponents[0].selectCard.Value == false)
            {
                SendACard();
                GameManager.Instance.playerComponents[0].SetSelectCardServerRpc(true);
            }
        }
        else if(NetworkManager.LocalClientId == 1 && GetComponentInParent<HandCardLogic>().belong == HandCardLogic.Belong.Player2)
        {
            if (GameManager.Instance.playerComponents[1].selectCard.Value == false)
            {
                SendACard();
                GameManager.Instance.playerComponents[1].SetSelectCardServerRpc(true);
            }
        }
    }

    private void SendACard()
    {
        GetComponentInParent<HandCardLogic>().SendCard(transform);
    }
    
}
