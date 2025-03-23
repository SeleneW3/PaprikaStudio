using System;
using System.Collections.Generic;
using UnityEngine;

public class Deck
{
    public List<CardLogic> cards = new List<CardLogic>();
    private GameObject cardPrefab; // ����ʵ�������Ƶ�Ԥ�Ƽ�

    // ���캯������Ԥ�Ƽ�����������һ���ƣ����� 10 ���ƣ�
    public Deck(GameObject cardPrefab)
    {
        this.cardPrefab = cardPrefab;
        System.Random rand = new System.Random();
        for (int i = 0; i < 10; i++)
        {
            // ������ɿ���Ч��
            CardLogic.Effect effect = (CardLogic.Effect)rand.Next(0, 2);

            // ͨ��Ԥ�Ƽ�ʵ��������
            GameObject cardObj = GameObject.Instantiate(cardPrefab);
            CardLogic cardLogic = cardObj.GetComponent<CardLogic>();
            if (cardLogic != null)
            {
                cardLogic.effect = effect;
                cards.Add(cardLogic);
            }
            else
            {
                Debug.LogError("Ԥ�Ƽ���û���ҵ� CardLogic �����");
            }
        }
    }

    // ϴ�Ʒ�����ʹ�� Fisher-Yates �㷨��
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

    // ��һ���ƣ������ƶ����Ƴ�
    public CardLogic DealCard()
    {
        if (cards.Count == 0)
            return null;
        CardLogic card = cards[0];
        cards.RemoveAt(0);
        return card;
    }
}
