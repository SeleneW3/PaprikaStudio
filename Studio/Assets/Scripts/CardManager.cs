using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class CardGameManager : NetworkBehaviour
{
    public int cardsToDeal = 2;
    public GameObject cardPrefab;
    public Transform deckTrans;
    public float moveDuration = 1f;    // �����ƶ���ʱ��
    public float cardDelay = 0.2f;     // ÿ�ſ���֮����ӳ�

    private Deck deck;

    public HandCardLogic handCardLogic1;
    public HandCardLogic handCardLogic2;

    void Start()
    {
        deck = new Deck(cardPrefab);
        deck.Shuffle();
    }

    private void OnEnable()
    {
        GameManager.OnPlayersReady += StartDealCards;
    }

    private void OnDisable()
    {
        GameManager.OnPlayersReady -= StartDealCards;
    }

    public void StartDealCards()
    {
        if (NetworkManager.LocalClientId == 0)
        {
            StartCoroutine(WaitAndStartDeal());
        }
    }

    IEnumerator WaitAndStartDeal()
    {
        while (deck == null)
        {
            yield return null;
        }
        StartCoroutine(DealCards(deck));
    }

    // ����Э��
    IEnumerator DealCards(Deck deck)
    {
        // ����ÿ�����
        foreach (PlayerLogic player in GameManager.Instance.playerComponents)
        {
            // Ϊÿ����ҷ� cardsToDeal �ſ���
            for (int i = 0; i < cardsToDeal; i++)
            {
                // ����Ŀ��λ�ã����������΢����ƫ�ƣ�
                Vector3 targetPos = player.handPos.position - new Vector3(0, 0.01f * i, 0);
                Quaternion targetRot = player.handPos.rotation;

                // ʹ�ú�������� Instantiate ���أ�ֱ�ӽ�������������������£������������ɣ�
                GameObject cardObj = Instantiate(cardPrefab, deckTrans.position, deckTrans.rotation, player.handPos);
                NetworkObject netObj = cardObj.GetComponent<NetworkObject>();
                netObj.Spawn();  // ֻ�з�����ִ������

                // ��ȡ���Ƶ� CardLogic ���������ֵ�ƶ��е�����
                CardLogic cardComponent = cardObj.GetComponent<CardLogic>();
                CardLogic cardData = deck.DealCard();

                if (cardComponent != null && cardData != null)
                {
                    // ���� effect�����Զ�ͨ�� NetworkVariable ͬ�����ͻ���
                    cardComponent.effect = cardData.effect;
                }
                else
                {
                    Debug.LogError("�������ݻ������ȡʧ�ܣ�");
                }

                // ��������ӵ���������У����ݲ������ӣ�
                player.AddCard(cardComponent);
                if (player == GameManager.Instance.playerComponents[0])
                {
                    cardComponent.belong = CardLogic.Belong.Player1;
                }
                else if (player == GameManager.Instance.playerComponents[1])
                {
                    cardComponent.belong = CardLogic.Belong.Player2;
                }

                // ����Э�̶����������ƴ��ƶ�λ��ƽ���ƶ���Ŀ��λ�ú���ת
                StartCoroutine(MoveCardToHand(cardObj, player.handPos, targetPos, targetRot, moveDuration));

                // ÿ����һ�ſ��Ƶȴ�һ��ʱ�䣬�ٷ���һ��
                yield return new WaitForSeconds(cardDelay);
            }
        }
        yield return new WaitForSeconds(1f);
        handCardLogic1.Initialize();
        handCardLogic2.Initialize();
    }

    /// <summary>
    /// �����ƴӵ�ǰ��λ���ƶ���Ŀ������λ�ú���ת��������ɺ󽫿�����ΪĿ��������塣
    /// </summary>
    IEnumerator MoveCardToHand(GameObject card, Transform targetParent, Vector3 targetPosition, Quaternion targetRotation, float duration)
    {
        Vector3 startPos = card.transform.position;
        Quaternion startRot = card.transform.rotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            card.transform.position = Vector3.Lerp(startPos, targetPosition, elapsed / duration);
            card.transform.rotation = Quaternion.Slerp(startRot, targetRotation, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        // ȷ������λ�ú���ת
        card.transform.position = targetPosition;
        card.transform.rotation = targetRotation;
        // ���ÿ���Ϊ������Ƶ�������
        card.transform.SetParent(targetParent);
    }
}
