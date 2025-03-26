using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardLogic : MonoBehaviour
{
    public enum Effect
    {
        None,
        ReversePoint,
        ReverseChoice,
        DoubleCheatPoint,
        DoubleCoopPoint,
    }
    public Effect effect;

    public enum Belong
    {
        Deck,
        Player1,
        Player2,
    }
    public Belong belong;

    public void OnEffect()
    {
        if (effect == Effect.ReversePoint)
        {
            ReversePoint();
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
}
