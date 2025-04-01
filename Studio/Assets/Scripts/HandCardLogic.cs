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
    /// չ�����ƣ������ƣ������Ƿ�����ֱ�Ӹ���״̬������ͨ��ServerRpc֪ͨ����
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
    /// �ջؿ��ƣ��ر����ƣ������Ƿ�����ֱ�Ӹ���״̬������ͨ��ServerRpc֪ͨ����
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
    /// �������ʱ����
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

        // ����Э��ƽ���ƶ����Ƶ��ƶ�λ��
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
        // ȷ���������յ���Ŀ��λ��
        card.position = targetPos;
        // �ƶ���ɺ󣬽���������Ϊ�ƶѵ�������
        card.SetParent(deck.transform, false);
    }

    void Update()
    {
        if (cards == null || cards.Count == 0)
            return;

        // ���� desired state ���� transition ֵ��0��1֮�䣩
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
                // ����Ŀ����ת�Ƕȣ��� fanAngle/2 �� -fanAngle/2
                angle = fanAngle / 2 - (fanAngle / (count - 1)) * i;
                // ��������ƫ�ƣ����м�Ŀ��Ʊ��ֲ���������ֱ��������ƶ�
                offsetX = (i - (count - 1) / 2f) * spread;
            }
            // ���ֻ��һ�ſ��ƣ�Ĭ�ϽǶȺ�ƫ��Ϊ 0

            Quaternion targetRot = Quaternion.Euler(0, 0, angle);
            Vector3 targetPos = originalPositions[i] + new Vector3(offsetX, 0, 0);

            // ͨ����ֵ���㵱ǰ״̬
            cards[i].localPosition = Vector3.Lerp(originalPositions[i], targetPos, transition);
            cards[i].localRotation = Quaternion.Lerp(originalRotations[i], targetRot, transition);
        }
    }

}
