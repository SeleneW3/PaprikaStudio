using System;
using System.Collections.Generic;
using UnityEngine;

public class Deck
{
    public List<CardLogic> cards = new List<CardLogic>();
    private GameObject cardPrefab; // 用于实例化卡牌的预制体

    // 构造函数，用于初始化卡牌堆，预先生成一定数量的卡牌
    public Deck(GameObject cardPrefab)
    {
        this.cardPrefab = cardPrefab;
        System.Random rand = new System.Random();
        for (int i = 0; i < 10; i++)
        {
            // 随机选择一种卡牌效果
            CardLogic.Effect effect = (CardLogic.Effect)rand.Next(0, 5);

            // 通过预制体实例化卡牌
            GameObject cardObj = GameObject.Instantiate(cardPrefab);
            CardLogic cardLogic = cardObj.GetComponent<CardLogic>();
            if (cardLogic != null)
            {
                cardLogic.effect = effect;
                cards.Add(cardLogic);
            }
            else
            {
                Debug.LogError("预制体中未找到 CardLogic 组件");
            }
        }
    }

    // 洗牌算法，使用 Fisher-Yates 算法
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

    // 抽一张卡牌，从卡牌堆中移除并返回
    public CardLogic DealCard()
    {
        if (cards.Count == 0)
            return null;
        CardLogic card = cards[0];
        cards.RemoveAt(0);
        return card;
    }
}