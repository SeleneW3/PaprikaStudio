using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    public Transform selectedCardPos;
    public float selectMoveDuration = 0.25f;
    public bool hasSelectedCard = false;

    // 添加网络变量来跟踪选中的卡牌
    private NetworkVariable<ulong> selectedCardId = new NetworkVariable<ulong>();

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
            AddCard(card);
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

    public void SelectCard(Transform card)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            SelectCardServerRpc(card.GetComponent<NetworkObject>().NetworkObjectId);
        }
        else
        {
            ProcessSelectCard(card);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void SelectCardServerRpc(ulong cardNetworkId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(cardNetworkId, out NetworkObject cardObj))
        {
            ProcessSelectCard(cardObj.transform);
        }
    }

    void ProcessSelectCard(Transform card)
    {
        CardLogic cardLogic = card.GetComponent<CardLogic>();
        if (!cardLogic.isSelected)
        {
            // 如果已经有选中的卡牌，先取消选中
            if (hasSelectedCard)
            {
                foreach (var c in cards)
                {
                    c.GetComponent<CardLogic>().isSelected = false;
                }
            }

            cardLogic.isSelected = true;
            hasSelectedCard = true;
            selectedCardId.Value = card.GetComponent<NetworkObject>().NetworkObjectId;
            UpdateCardSelectionClientRpc(card.GetComponent<NetworkObject>().NetworkObjectId, true);
        }
    }

    [ClientRpc]
    void UpdateCardSelectionClientRpc(ulong cardNetworkId, bool isSelected)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(cardNetworkId, out NetworkObject cardObj))
        {
            CardLogic cardLogic = cardObj.GetComponent<CardLogic>();
            cardLogic.isSelected = isSelected;
            hasSelectedCard = isSelected;
        }
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
        RemoveCard(card);

        CardLogic cardLogic = card.GetComponent<CardLogic>();
        cardLogic.isSelected = false;
        cardLogic.isOut = true;
        hasSelectedCard = false;

        // 移动卡牌到牌堆
        StartCoroutine(MoveCardToDeck(card));

        DeckLogic deckLogic = deck.GetComponent<DeckLogic>();
        deckLogic.cardLogics.Add(cardLogic);

        UpdateCardStateClientRpc(card.GetComponent<NetworkObject>().NetworkObjectId, true, false);
        AddCardToDeckClientRpc(card.GetComponent<NetworkObject>().NetworkObjectId);
    }

    public void AddCard(Transform card)
    {
        cards.Add(card);
        originalPositions.Add(card.localPosition);
        originalRotations.Add(card.localRotation);
    }

    public void RemoveCard(Transform card)
    {
        int index = cards.IndexOf(card);
        if (index >= 0)
        {
            cards.RemoveAt(index);
            originalPositions.RemoveAt(index);
            originalRotations.RemoveAt(index);
        }
    }


    [ClientRpc]
    void UpdateCardStateClientRpc(ulong cardNetworkId, bool isOut, bool isSelected)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(cardNetworkId, out NetworkObject cardObj))
        {
            CardLogic cardLogic = cardObj.GetComponent<CardLogic>();
            cardLogic.isOut = isOut;
            cardLogic.isSelected = isSelected;
            hasSelectedCard = isSelected;
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
        if (cards == null || cards.Count == 0) return;

        // 0‑1 过渡量：控制扇形开合
        transition += (opened ? 1 : -1) * Time.deltaTime / duration;
        transition = Mathf.Clamp01(transition);

        /* ---------- 统计未选中牌，用来算扇形 ---------- */
        int animatedCount = cards.Count(c => !c.GetComponent<CardLogic>().isSelected);
        int visualIndex = 0;

        for (int i = 0; i < cards.Count; i++)
        {
            var logic = cards[i].GetComponent<CardLogic>();

            Vector3 targetPos;
            Quaternion targetRot;

            if (logic.isSelected)              // ====== 被选中：飞到指定位置 ======
            {
                // 如果 selectedCardPos 跟牌同一个父物体，用 localPosition/Rotation 即可
                targetPos = selectedCardPos.localPosition;
                targetRot = selectedCardPos.localRotation;
            }
            else                               // ====== 未选中：照常排扇形 ======
            {
                float angle = 0f;
                float offsetX = 0f;
                if (animatedCount > 1)
                {
                    angle = fanAngle / 2f - (fanAngle / (animatedCount - 1)) * visualIndex;
                    offsetX = (visualIndex - (animatedCount - 1) / 2f) * spread;
                }

                targetRot = Quaternion.Euler(0, 0, angle);
                targetPos = originalPositions[i] + new Vector3(offsetX, 0, 0);

                visualIndex++;                 // 只对未选中牌递增
            }

            /* ---------- 插值移动 / 旋转 ---------- */
            /* ---------- 插值移动 / 旋转 ---------- */
            if (logic.isSelected)                         // 选中牌
            {
                float tSel = Time.deltaTime / selectMoveDuration;
                cards[i].localPosition = Vector3.Lerp(cards[i].localPosition, targetPos, tSel);
                cards[i].localRotation = Quaternion.Lerp(cards[i].localRotation, targetRot, tSel);
            }
            else                                           // 其余牌：用 0‑1 的 transition
            {
                cards[i].localPosition = Vector3.Lerp(originalPositions[i], targetPos, transition);
                cards[i].localRotation = Quaternion.Lerp(originalRotations[i], targetRot, transition);
            }

        }
    }



}
