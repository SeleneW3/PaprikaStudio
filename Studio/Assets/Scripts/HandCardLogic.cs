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
    /// 展开卡牌（打开手牌），若是服务器直接更新状态，否则通过ServerRpc通知主机
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
    /// 收回卡牌（关闭手牌），若是服务器直接更新状态，否则通过ServerRpc通知主机
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
    /// 点击卡牌时调用
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

        // 启动协程平滑移动卡牌到牌堆位置
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
        // 确保卡牌最终到达目标位置
        card.position = targetPos;
        // 移动完成后，将卡牌设置为牌堆的子物体
        card.SetParent(deck.transform, false);
    }

    void Update()
    {
        if (cards == null || cards.Count == 0)
            return;

        // 根据 desired state 调整 transition 值（0～1之间）
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
                // 计算目标旋转角度：从 fanAngle/2 到 -fanAngle/2
                angle = fanAngle / 2 - (fanAngle / (count - 1)) * i;
                // 计算左右偏移，让中间的卡牌保持不动，两侧分别向左右移动
                offsetX = (i - (count - 1) / 2f) * spread;
            }
            // 如果只有一张卡牌，默认角度和偏移为 0

            Quaternion targetRot = Quaternion.Euler(0, 0, angle);
            Vector3 targetPos = originalPositions[i] + new Vector3(offsetX, 0, 0);

            // 通过插值计算当前状态
            cards[i].localPosition = Vector3.Lerp(originalPositions[i], targetPos, transition);
            cards[i].localRotation = Quaternion.Lerp(originalRotations[i], targetRot, transition);
        }
    }

}
