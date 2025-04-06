using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CardLogic : NetworkBehaviour
{

    public Sprite noneEffectSprite;
    public Sprite reversePointSprite;
    public Sprite reverseChoiceSprite;
    public Sprite doubleCheatPointSprite;
    public Sprite doubleCoopPointSprite;
    public Sprite reverseCoopToCheatSprite;
    public Sprite reverseCheatToCoopSprite;
    public Sprite reverseOpponentDecisionSprite;
    public Sprite adjustPayoffSprite;

    public enum Effect
    {   
        ReverseCoopToCheat,       // 1本回合你的合作选择将逆转为欺骗
        ReverseCheatToCoop,       // 1本回合你的欺骗选择将逆转为合作

        ReverseOpponentDecision,  // 2逆转对方决策

        ReverseChoice,            //3

        DoubleCheatPoint,          //4
        DoubleCoopPoint,           //4

        AdjustPayoff,             // 5本回合你的合作收益+2，欺骗收益-2

        ReversePoint,             //6
        None,                     //6空白
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


// 在 Start 方法中初始化贴图，并监听 effectNetwork 的变化
    private void Start()
    {
        Debug.Log("CardLogic Start() called");
        UpdateSprite(); // 初始化时更新贴图

        effectNetwork.OnValueChanged += (oldEffect, newEffect) =>
        {
            Debug.Log($"Effect changed from {oldEffect} to {newEffect}");
            UpdateSprite();
        };
    }

    // 根据当前 effect 更新 SpriteRenderer 的贴图
    private void UpdateSprite()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("SpriteRenderer not found on " + gameObject.name);
            return;
        }

        switch (effect)
        {
            case Effect.ReversePoint:
                spriteRenderer.sprite = reversePointSprite;
                break;
            case Effect.DoubleCoopPoint:
                spriteRenderer.sprite = doubleCoopPointSprite;
                break;
            case Effect.DoubleCheatPoint:
                spriteRenderer.sprite = doubleCheatPointSprite;
                break;
            case Effect.ReverseChoice:
                spriteRenderer.sprite = reverseChoiceSprite;
                break;

            case Effect.ReverseCoopToCheat:
                spriteRenderer.sprite = reverseCoopToCheatSprite;
                break;
            case Effect.ReverseCheatToCoop:
                spriteRenderer.sprite = reverseCheatToCoopSprite;
                break;
            case Effect.ReverseOpponentDecision:
                spriteRenderer.sprite = reverseOpponentDecisionSprite;
                break;
            case Effect.AdjustPayoff:
                spriteRenderer.sprite = adjustPayoffSprite;
                break;

            default:
                spriteRenderer.sprite = noneEffectSprite;
                break;
        }

        Debug.Log("Sprite updated to: " + spriteRenderer.sprite.name);
    }
//  ！！！！！！！！！！！！！！！


    public void OnEffect()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

        switch (effect)
        {
            case Effect.ReversePoint:
                ReversePoint();
                spriteRenderer.sprite = reversePointSprite;
                break;
            case Effect.DoubleCoopPoint:
                DoubleCoopPoint();
                spriteRenderer.sprite = doubleCoopPointSprite;
                break;
            case Effect.DoubleCheatPoint:
                DoubleCheatPoint();
                spriteRenderer.sprite = doubleCheatPointSprite;
                break;
            case Effect.ReverseChoice:
                ReverseChoice();
                spriteRenderer.sprite = reverseChoiceSprite;
                break;

            case Effect.ReverseCoopToCheat:
                ReverseCoopToCheat();
                spriteRenderer.sprite = reverseCoopToCheatSprite;
                break;
            case Effect.ReverseCheatToCoop:
                ReverseCheatToCoop();
                spriteRenderer.sprite = reverseCheatToCoopSprite;
                break;
            case Effect.ReverseOpponentDecision:
                ReverseOpponentDecision();
                spriteRenderer.sprite = reverseOpponentDecisionSprite;
                break;
            case Effect.AdjustPayoff:
                AdjustPayoff();
                spriteRenderer.sprite = adjustPayoffSprite;
                break;

            default:
                NoneEffect();
                spriteRenderer.sprite = noneEffectSprite;
                break;
        }
    }



    //具体方法实现

    public void DoubleCheatPoint()
    {
        if (belong == Belong.Player1)
        {
            GameManager.Instance.playerComponents[0].cheatPoint.Value *= 2;
        }
        else if (belong == Belong.Player2)
        {
            GameManager.Instance.playerComponents[1].cheatPoint.Value *= 2;
        }
    }


    public void DoubleCoopPoint()
    {
        if (belong == Belong.Player1)
        {
            GameManager.Instance.playerComponents[0].coopPoint.Value *= 2;
        }
        else if (belong == Belong.Player2)
        {
            GameManager.Instance.playerComponents[1].coopPoint.Value *= 2;
        }
    }


    public void ReversePoint()
    {
        float temp = GameManager.Instance.playerComponents[1].point.Value;
        GameManager.Instance.playerComponents[1].point.Value = GameManager.Instance.playerComponents[0].point.Value;
        GameManager.Instance.playerComponents[0].point.Value = temp;
    }


    public void ReverseChoice()
    {
        PlayerLogic.playerChoice choice = GameManager.Instance.playerComponents[1].choice;
        GameManager.Instance.playerComponents[1].choice = GameManager.Instance.playerComponents[0].choice;
        GameManager.Instance.playerComponents[0].choice = choice;
    }


// 1. 本回合你的合作选择将逆转为欺骗
public void ReverseCoopToCheat()
{
    if (belong == Belong.Player1)
    {
        if (GameManager.Instance.playerComponents[0].choice == PlayerLogic.playerChoice.Cooperate)
        {
            GameManager.Instance.playerComponents[0].choice = PlayerLogic.playerChoice.Cheat;
            Debug.Log("Player1: Cooperation reversed to Cheat");
        }
    }
    else if (belong == Belong.Player2)
    {
        if (GameManager.Instance.playerComponents[1].choice == PlayerLogic.playerChoice.Cooperate)
        {
            GameManager.Instance.playerComponents[1].choice = PlayerLogic.playerChoice.Cheat;
            Debug.Log("Player2: Cooperation reversed to Cheat");
        }
    }
}

// 2. 本回合你的欺骗选择将逆转为合作
public void ReverseCheatToCoop()
{
    if (belong == Belong.Player1)
    {
        if (GameManager.Instance.playerComponents[0].choice == PlayerLogic.playerChoice.Cheat)
        {
            GameManager.Instance.playerComponents[0].choice = PlayerLogic.playerChoice.Cooperate;
            Debug.Log("Player1: Cheat reversed to Cooperation");
        }
    }
    else if (belong == Belong.Player2)
    {
        if (GameManager.Instance.playerComponents[1].choice == PlayerLogic.playerChoice.Cheat)
        {
            GameManager.Instance.playerComponents[1].choice = PlayerLogic.playerChoice.Cooperate;
            Debug.Log("Player2: Cheat reversed to Cooperation");
        }
    }
}

// 3. 逆转对方决策  
public void ReverseOpponentDecision()
{
    // 根据当前卡牌归属，找到对手
    if (belong == Belong.Player1)
    {
        // 对手为 Player2
        if (GameManager.Instance.playerComponents[1].choice == PlayerLogic.playerChoice.Cooperate)
        {
            GameManager.Instance.playerComponents[1].choice = PlayerLogic.playerChoice.Cheat;
            Debug.Log("Player2: Cooperation reversed to Cheat");
        }
        else if (GameManager.Instance.playerComponents[1].choice == PlayerLogic.playerChoice.Cheat)
        {
            GameManager.Instance.playerComponents[1].choice = PlayerLogic.playerChoice.Cooperate;
            Debug.Log("Player2: Cheat reversed to Cooperation");
        }
    }
    else if (belong == Belong.Player2)
    {
        // 对手为 Player1
        if (GameManager.Instance.playerComponents[0].choice == PlayerLogic.playerChoice.Cooperate)
        {
            GameManager.Instance.playerComponents[0].choice = PlayerLogic.playerChoice.Cheat;
            Debug.Log("Player1: Cooperation reversed to Cheat");
        }
        else if (GameManager.Instance.playerComponents[0].choice == PlayerLogic.playerChoice.Cheat)
        {
            GameManager.Instance.playerComponents[0].choice = PlayerLogic.playerChoice.Cooperate;
            Debug.Log("Player1: Cheat reversed to Cooperation");
        }
    }
}

    // 4. 本回合你的合作收益+2，欺骗收益-2
    public void AdjustPayoff()
    {
        if (belong == Belong.Player1)
        {
            if (GameManager.Instance.playerComponents[0].choice == PlayerLogic.playerChoice.Cooperate)
            {
                GameManager.Instance.playerComponents[0].coopPoint.Value += 2;
                Debug.Log("Player1: Cooperation payoff increased by 2");
            }
            else if (GameManager.Instance.playerComponents[0].choice == PlayerLogic.playerChoice.Cheat)
            {
                GameManager.Instance.playerComponents[0].cheatPoint.Value -= 2;
                Debug.Log("Player1: Cheat payoff decreased by 2");
            }
        }
        else if (belong == Belong.Player2)
        {
            if (GameManager.Instance.playerComponents[1].choice == PlayerLogic.playerChoice.Cooperate)
            {
                GameManager.Instance.playerComponents[1].coopPoint.Value += 2;
                Debug.Log("Player2: Cooperation payoff increased by 2");
            }
            else if (GameManager.Instance.playerComponents[1].choice == PlayerLogic.playerChoice.Cheat)
            {
                GameManager.Instance.playerComponents[1].cheatPoint.Value -= 2;
                Debug.Log("Player2: Cheat payoff decreased by 2");
            }
        }
    }


    public void NoneEffect()
    {
        return;
    }


//#################################################

public static int GetEffectPriority(Effect effect)
{
    switch (effect)
    {
        // 1级优先
        case Effect.ReverseCoopToCheat:
        case Effect.ReverseCheatToCoop:
            return 1;

        // 2级优先
        case Effect.ReverseOpponentDecision:
            return 2;

        // 3级优先
        case Effect.ReverseChoice:
            return 3;

        // 4级优先
        case Effect.DoubleCheatPoint:
        case Effect.DoubleCoopPoint:
            return 4;

        // 5级优先
        case Effect.AdjustPayoff:
            return 5;

        // 6级优先（或无效果）
        case Effect.ReversePoint:
        case Effect.None:
            return 6;
    }

    // 兜底返回一个较大的值
    return 999;
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
