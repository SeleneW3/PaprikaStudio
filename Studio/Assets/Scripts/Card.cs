using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardLogic : MonoBehaviour
{
    public enum Effect
    {
        None,
        Reverse,
    }
    public Effect effect;

    public void OnEffect()
    {
        if (effect == Effect.Reverse)
        {
               ReversePoint();
        }
        else
        {
            NoneEffect();
        }
    }

    public void ReversePoint()
    {
        int point = GameManager.Instance.playerComponents[1].point;
        GameManager.Instance.playerComponents[1].point = GameManager.Instance.playerComponents[0].point;
        GameManager.Instance.playerComponents[0].point = point;
    }

    public void NoneEffect()
    {
        return;
    }
}
