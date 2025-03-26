using UnityEngine;
using System.Collections;

public class CardGameManager : MonoBehaviour
{
    public int cardsToDeal = 2;
    public GameObject cardPrefab;
    public Transform deckTrans;
    public float moveDuration = 1f;    // �����ƶ���ʱ��
    public float cardDelay = 0.2f;     // ÿ�ſ���֮����ӳ�

    private Deck deck;

    void Start()
    {
        deck = new Deck(cardPrefab);
        deck.Shuffle();

        // ����Э�̷���
        //StartCoroutine(DealCards(deck));
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
        StartCoroutine(WaitAndStartDeal());
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

                // ���ƶ�λ��ʵ��������
                GameObject cardObj = Instantiate(cardPrefab, deckTrans.position, deckTrans.rotation);

                // ��ȡ���Ƶ� CardLogic ���
                CardLogic cardComponent = cardObj.GetComponent<CardLogic>();

                // ���ƶ��л�ȡһ���Ƶ�����
                CardLogic cardData = deck.DealCard();

                // ���ƶ��е����ݸ�ֵ��ʵ�����Ŀ������
                if (cardComponent != null && cardData != null)
                {
                    cardComponent.effect = cardData.effect;
                }
                else
                {
                    Debug.LogError("�������ݻ������ȡʧ�ܣ�");
                }

                // ��������ӵ���������У����ݲ������ӣ�
                player.AddCard(cardComponent);

                // ����Э�̶����������ƴ��ƶ�λ��ƽ���ƶ���Ŀ��λ�ú���ת
                StartCoroutine(MoveCardToHand(cardObj, player.handPos, targetPos, targetRot, moveDuration));

                // ÿ����һ�ſ��Ƶȴ�һ��ʱ�䣬�ٷ���һ��
                yield return new WaitForSeconds(cardDelay);
            }
        }
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
