using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class HandCardLogic : NetworkBehaviour
{
    public enum Belong
    {
        Player1,
        Player2
    }

    public float fanAngle = 45f;
    public float duration = 0.5f;
    public float spread = 0.5f;

    public bool opened = false;
    public Belong belong;

    public List<Transform> cards;
    public GameObject deck;

    private List<Vector3> originalPositions;
    private List<Quaternion> originalRotations;


    private float transition = 0f;

    public CardLogic selectedCard;

    void Start()
    {

        if (transform.childCount > 0)
            Initialize();
    }

    public void Initialize()
    {
        Debug.Log("Initialize");
        int count = transform.childCount;
        cards = new List<Transform>(count);
        originalPositions = new List<Vector3>(count);
        originalRotations = new List<Quaternion>(count);

        for (int i = 0; i < count; i++)
        {
            Transform card = transform.GetChild(i);
            cards.Add(card);
            originalPositions.Add(card.localPosition);
            originalRotations.Add(card.localRotation);
        }
    }

    /// <summary>
    /// 打开手牌，如果手牌已经打开，那么不会有任何效果
    /// </summary>
    public void Open()
    {
        if (NetworkManager.LocalClientId == 0)
        {
            Debug.Log("HandCardLogic Open");
            opened = true;
            UpdateState();
        }
        else
        {
            RequestUpdateStateServerRpc(true);
        }
    }

    /// <summary>
    /// 关闭手牌，如果手牌已经关闭，那么不会有任何效果
    /// </summary>
    public void Close()
    {
        if (NetworkManager.LocalClientId == 0)
        {
            Debug.Log("HandCardLogic Close");
            opened = false;
            UpdateState();
        }
        else
        {
            RequestUpdateStateServerRpc(false);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestUpdateStateServerRpc(bool state)
    {
        opened = state;
        UpdateState();
    }

    private void UpdateState()
    {
        UpdateStateClientRpc(opened);
    }

    [ClientRpc]
    private void UpdateStateClientRpc(bool state)
    {
        opened = state;
        Debug.Log("HandCardLogic UpdateState: " + state);
    }

    /// <summary>
    /// 发送一张卡牌
    /// </summary>
    public void SendCard(Transform card)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            SendCardServerRpc(card.GetComponent<NetworkObject>().NetworkObjectId);
        }
        else
        {
            ProcessSendCard(card);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void SendCardServerRpc(ulong cardNetworkId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(cardNetworkId, out NetworkObject cardObj))
        {
            ProcessSendCard(cardObj.transform);
        }
    }

    void ProcessSendCard(Transform card)
    {
        int index = cards.IndexOf(card);
        if (index >= 0)
        {
            cards.RemoveAt(index);
            originalPositions.RemoveAt(index);
            originalRotations.RemoveAt(index);
        }

        CardLogic cardLogic = card.GetComponent<CardLogic>();
        cardLogic.isOut = true;

        // 移动卡牌到牌堆
        StartCoroutine(MoveCardToDeck(card));

        DeckLogic deckLogic = deck.GetComponent<DeckLogic>();
        deckLogic.cardLogics.Add(cardLogic);

        UpdateCardStateClientRpc(card.GetComponent<NetworkObject>().NetworkObjectId, true);
        AddCardToDeckClientRpc(card.GetComponent<NetworkObject>().NetworkObjectId);
    }


    [ClientRpc]
    void UpdateCardStateClientRpc(ulong cardNetworkId, bool state)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(cardNetworkId, out NetworkObject cardObj))
        {
            cardObj.GetComponent<CardLogic>().isOut = state;
        }
    }

    [ClientRpc]
    void AddCardToDeckClientRpc(ulong cardNetworkId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(cardNetworkId, out NetworkObject cardObj))
        {
            DeckLogic deckLogic = deck.GetComponent<DeckLogic>();
            CardLogic cardLogic = cardObj.GetComponent<CardLogic>();
            if (!deckLogic.cardLogics.Contains(cardLogic))
            {
                deckLogic.cardLogics.Add(cardLogic);
            }
        }
    }

    IEnumerator MoveCardToDeck(Transform card)
    {
        Vector3 startPos = card.position;
        Vector3 targetPos = deck.transform.position;
        float duration = 0.5f; 
        float elapsed = 0f;

        while (elapsed < duration)
        {
            card.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        // 确保移动到目标位置
        card.position = targetPos;
        // 移动完成后，将卡牌的父对象设置为牌堆
        card.SetParent(deck.transform, false);
    }

    /// <summary>
    /// 更新手牌的位置和旋转
    /// </summary>
    void Update()
    {
        if (cards == null || cards.Count == 0)
            return;

        // 根据 desired state 更新 transition 的值，使其在 0 和 1 之间
        if (opened)
        {
            transition += Time.deltaTime / duration;
        }
        else
        {
            transition -= Time.deltaTime / duration;
        }
        transition = Mathf.Clamp01(transition);

        int count = cards.Count;
        for (int i = 0; i < count; i++)
        {
            float angle = 0f;
            float offsetX = 0f;
            if (count > 1)
            {
                // 计算目标旋转角度，使得卡牌在打开状态时，卡牌的旋转角度在 -fanAngle/2 和 fanAngle/2 之间
                angle = fanAngle / 2 - (fanAngle / (count - 1)) * i;
                // 计算卡牌的水平偏移量，使得卡牌在打开状态时，卡牌的位置在 -spread/2 和 spread/2 之间
                offsetX = (i - (count - 1) / 2f) * spread;
            }

            Quaternion targetRot = Quaternion.Euler(0, 0, angle);
            Vector3 targetPos = originalPositions[i] + new Vector3(offsetX, 0, 0);

            // 通过插值更新卡牌的位置和旋转
            cards[i].localPosition = Vector3.Lerp(originalPositions[i], targetPos, transition);
            cards[i].localRotation = Quaternion.Lerp(originalRotations[i], targetRot, transition);
        }
    }

}
