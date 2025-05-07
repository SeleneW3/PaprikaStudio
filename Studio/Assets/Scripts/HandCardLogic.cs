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

    public NetworkVariable<int> selectedCardIndex =
    new(-1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server); 

    public bool hasSelectedCard = false;


    void Start()
    {

        if (transform.childCount > 0)
            Initialize();
    }

    public void Initialize()
    {
        //Debug.Log("Initialize");
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
            //Debug.Log("HandCardLogic Open");
            opened = true;
            UpdateState();
            
            // 播放卡牌移动音效
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX("CardMove");
            }
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
            //Debug.Log("HandCardLogic Close");
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
        //Debug.Log("HandCardLogic UpdateState: " + state);
    }

    /// <summary>
    /// 发送一张卡牌
    /// </summary>
    public void SendCard(Transform card)
    {
        // 播放出牌音效（本地播放，确保每个客户端都能听到）
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("CardOut");
        }
        
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
        cardLogic.isOut = true;

        // 移动卡牌到牌堆
        StartCoroutine(MoveCardToDeck(card));

        DeckLogic deckLogic = deck.GetComponent<DeckLogic>();
        deckLogic.cardLogics.Add(cardLogic);

        UpdateCardStateClientRpc(card.GetComponent<NetworkObject>().NetworkObjectId, true);
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
        Quaternion startRot = card.rotation;
        Vector3 targetPos = deck.transform.position;
        Quaternion targetRot = deck.transform.rotation;
        float duration = 0.5f; 
        float elapsed = 0f;

        while (elapsed < duration)
        {
            card.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            card.rotation = Quaternion.Slerp(startRot, targetRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 移动完成后，将卡牌的父对象设置为牌堆
        card.SetParent(deck.transform, false);

        card.localPosition = Vector3.zero;
        card.localRotation = Quaternion.identity;
    }



    [ServerRpc(RequireOwnership = false)]
    public void RequestSelectCardIndexServerRpc(int index,
                                            ServerRpcParams rpcParams = default)
    {
        selectedCardIndex.Value = index;
    }


    void Update()
    {
        if (cards == null || cards.Count == 0) return;

        /* ---------- 若运行中增删或重排了手牌，补齐缓存长度 ---------- */
        if (originalPositions.Count != cards.Count)
        {
            originalPositions = cards.Select(t => t.localPosition).ToList();
            originalRotations = cards.Select(t => t.localRotation).ToList();
        }

        /* ---------- 0‑1 过渡量：控制扇形开合 ---------- */
        transition = Mathf.MoveTowards(
            transition,
            opened ? 1f : 0f,
            Time.deltaTime / duration);

        /* ---------- 未被选中的牌数量，用来算扇形角 & 位移 ---------- */
        int animatedCount = (selectedCardIndex.Value >= 0 && selectedCardIndex.Value < cards.Count)
                            ? cards.Count - 1
                            : cards.Count;

        int visualIndex = 0;

        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] == null) continue;               // 防御式

            bool isSelected = (i == selectedCardIndex.Value);
            Vector3 targetPos;
            Quaternion targetRot;

            /* ===== ① 选中牌：飞到指定空物体 ===== */
            if (isSelected)
            {
                CardLogic cardLogic = cards[i].GetComponent<CardLogic>();
                cardLogic.disableHover = true; // 禁用 hover 效果
                targetPos = selectedCardPos.localPosition;
                targetRot = selectedCardPos.localRotation;

                // 把选中状态同步到 GameManager（保持你原有的判断）
                var selLogic = cards[i].GetComponent<CardLogic>();
                if (belong == Belong.Player1 && GameManager.Instance.playerComponents[0].selectCard != selLogic)
                    GameManager.Instance.playerComponents[0].selectCard = selLogic;
                else if (belong == Belong.Player2 && GameManager.Instance.playerComponents[1].selectCard != selLogic)
                    GameManager.Instance.playerComponents[1].selectCard = selLogic;
            }
            /* ===== ② 其它牌：扇形排布（目标始终“完全展开”位置） ===== */
            else
            {
                CardLogic cardLogic = cards[i].GetComponent<CardLogic>();
                cardLogic.disableHover = false; // 禁用 hover 效果
                float angle = 0f, offsetX = 0f;
                if (animatedCount > 1)
                {
                    angle = fanAngle * 0.5f
                             - fanAngle * visualIndex / (animatedCount - 1);
                    offsetX = (visualIndex - (animatedCount - 1) * 0.5f) * spread;
                }

                targetRot = Quaternion.Euler(0, 0, angle);
                targetPos = originalPositions[i] + new Vector3(offsetX, 0f, 0f);
                visualIndex++;
            }

            /* ---------- 插值 ---------- */
            if (isSelected)
            {
                // 选中牌：单独用 selectMoveDuration 做缓动
                float tSel = Mathf.Clamp01(Time.deltaTime / selectMoveDuration);
                cards[i].localPosition = Vector3.Lerp(cards[i].localPosition, targetPos, tSel);
                cards[i].localRotation = Quaternion.Slerp(cards[i].localRotation, targetRot, tSel);
            }
            else
            {
                // 其它牌：在 [闭合基准, 完全展开] 之间插值
                cards[i].localPosition = Vector3.Lerp(originalPositions[i], targetPos, transition);
                cards[i].localRotation = Quaternion.Slerp(originalRotations[i], targetRot, transition);
            }
        }
    }




}
