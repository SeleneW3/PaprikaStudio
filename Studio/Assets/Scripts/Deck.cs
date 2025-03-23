using System;
using System.Collections.Generic;
using UnityEngine;

public class Deck
{
    public List<CardLogic> cards = new List<CardLogic>();
    private GameObject cardPrefab; // 用于实例化卡牌的预制件

    // 构造函数接收预制件参数，生成一副牌（例如 10 张牌）
    public Deck(GameObject cardPrefab)
    {
        this.cardPrefab = cardPrefab;
        System.Random rand = new System.Random();
        for (int i = 0; i < 10; i++)
        {
            // 随机生成卡牌效果
            CardLogic.Effect effect = (CardLogic.Effect)rand.Next(0, 2);

            // 通过预制件实例化卡牌
            GameObject cardObj = GameObject.Instantiate(cardPrefab);
            CardLogic cardLogic = cardObj.GetComponent<CardLogic>();
            if (cardLogic != null)
            {
                cardLogic.effect = effect;
                cards.Add(cardLogic);
            }
            else
            {
                Debug.LogError("预制件上没有找到 CardLogic 组件！");
            }
        }
    }

    // 洗牌方法（使用 Fisher-Yates 算法）
    public void Shuffle()
    {
        System.Random rand = new System.Random();
        for (int i = 0; i < cards.Count; i++)
        {
            int r = i + rand.Next(cards.Count - i);
            CardLogic temp = cards[r];
            cards[r] = cards[i];
            cards[i] = temp;
        }
    }

    // 发一张牌，并从牌堆中移除
    public CardLogic DealCard()
    {
        if (cards.Count == 0)
            return null;
        CardLogic card = cards[0];
        cards.RemoveAt(0);
        return card;
    }
}
