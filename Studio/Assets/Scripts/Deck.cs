using System;
using System.Collections.Generic;
using UnityEngine;

public class Deck
{
    public List<CardLogic> cards = new List<CardLogic>();
    private GameObject cardPrefab; // 用于实例化卡牌的预制体

    // 构造函数，用于初始化卡牌堆，确保空白卡牌有两张，其他效果卡牌只有一张
    public Deck(GameObject cardPrefab)
    {
        this.cardPrefab = cardPrefab;

        // 创建一个固定的卡牌效果列表，其中包括 2 张空白卡牌
        List<CardLogic.Effect> effects = new List<CardLogic.Effect>
        {
            CardLogic.Effect.None,  // 空白卡牌
            CardLogic.Effect.None,  // 空白卡牌
            CardLogic.Effect.ReversePoint,
            CardLogic.Effect.ReverseChoice,
            CardLogic.Effect.ReverseCoopToCheat,
            CardLogic.Effect.ReverseCheatToCoop,
            CardLogic.Effect.ReverseOpponentDecision,
            CardLogic.Effect.DoubleCheatPoint,
            CardLogic.Effect.DoubleCoopPoint,
            CardLogic.Effect.AdjustPayoff
        };

        // 使用 Fisher-Yates 洗牌算法确保卡牌随机分布
        ShuffleEffects(effects);

        // 实例化卡牌并添加到卡牌堆
        foreach (var effect in effects)
        {
            GenerateCard(effect);
        }
    }

    private List<CardLogic.Effect> BuildEffectList() => new()
    {
    CardLogic.Effect.None, CardLogic.Effect.None,
    CardLogic.Effect.ReversePoint,  CardLogic.Effect.ReverseChoice,
    CardLogic.Effect.ReverseCoopToCheat, CardLogic.Effect.ReverseCheatToCoop,
    CardLogic.Effect.ReverseOpponentDecision,
    CardLogic.Effect.DoubleCheatPoint,  CardLogic.Effect.DoubleCoopPoint,
    CardLogic.Effect.AdjustPayoff
    };

    // 洗牌算法，使用 Fisher-Yates 算法来随机打乱效果列表
    private void ShuffleEffects(List<CardLogic.Effect> effects)
    {
        System.Random rand = new System.Random();
        for (int i = effects.Count - 1; i > 0; i--)
        {
            int r = rand.Next(i + 1);
            CardLogic.Effect temp = effects[i];
            effects[i] = effects[r];
            effects[r] = temp;
        }
    }

    // 公开的 Shuffle 方法，用来洗牌卡牌
    public void Shuffle()
    {
        System.Random rand = new System.Random();
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int r = rand.Next(i + 1);
            CardLogic temp = cards[i];
            cards[i] = cards[r];
            cards[r] = temp;
        }
    }

    // 抽一张卡牌，从卡牌堆中移除并返回
    public CardLogic DealCard()
    {
        if (cards.Count == 0)
        {
            Refill();         
            Shuffle();
        }

        CardLogic card = cards[0];
        cards.RemoveAt(0);
        return card;
    }

    /// <summary>把弃牌堆或一副新牌补回 Deck</summary>
    private void Refill()
    {
        List<CardLogic.Effect> effects = BuildEffectList(); // 仍然 2 张空白 + 8 张功能卡
        ShuffleEffects(effects);

        foreach (var effect in effects)
        {
            GenerateCard(effect);
        }
                           
    }

    public void GenerateCard(CardLogic.Effect effect)
    {
        GameObject cardObj = GameObject.Instantiate(cardPrefab);

        // 获取并调用 Spawn()，确保网络对象已生成
        var networkObj = cardObj.GetComponent<Unity.Netcode.NetworkObject>();
        if (networkObj != null && !networkObj.IsSpawned)
        {
            networkObj.Spawn();
        }

        CardLogic cardLogic = cardObj.GetComponent<CardLogic>();
        if (cardLogic != null)
        {
            // 此时 cardLogic.effect 修改时 NetworkVariable 已有所属的 NetworkBehaviour
            cardLogic.effect = effect;
            cards.Add(cardLogic);
        }
        else
        {
            Debug.LogError("预制体中未找到 CardLogic 组件");
        }
    }
}