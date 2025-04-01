using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class CardGameManager : NetworkBehaviour
{
    public int cardsToDeal = 2;
    public GameObject cardPrefab;
    public Transform deckTrans;
    public float moveDuration = 1f;    // 卡牌移动的时间
    public float cardDelay = 0.2f;     // 每张卡牌之间的延迟

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

    // 发牌协程
    IEnumerator DealCards(Deck deck)
    {
        // 遍历每个玩家
        foreach (PlayerLogic player in GameManager.Instance.playerComponents)
        {
            // 为每个玩家发 cardsToDeal 张卡牌
            for (int i = 0; i < cardsToDeal; i++)
            {
                // 计算目标位置（这里可以稍微做个偏移）
                Vector3 targetPos = player.handPos.position - new Vector3(0, 0.01f * i, 0);
                Quaternion targetRot = player.handPos.rotation;

                // 使用含父物体的 Instantiate 重载，直接将卡牌生成在玩家手牌下（仅服务器生成）
                GameObject cardObj = Instantiate(cardPrefab, deckTrans.position, deckTrans.rotation, player.handPos);
                NetworkObject netObj = cardObj.GetComponent<NetworkObject>();
                netObj.Spawn();  // 只有服务器执行这行

                // 获取卡牌的 CardLogic 组件，并赋值牌堆中的数据
                CardLogic cardComponent = cardObj.GetComponent<CardLogic>();
                CardLogic cardData = deck.DealCard();

                if (cardComponent != null && cardData != null)
                {
                    // 设置 effect，会自动通过 NetworkVariable 同步到客户端
                    cardComponent.effect = cardData.effect;
                }
                else
                {
                    Debug.LogError("卡牌数据或组件获取失败！");
                }

                // 将卡牌添加到玩家手牌中（数据层面的添加）
                player.AddCard(cardComponent);
                if (player == GameManager.Instance.playerComponents[0])
                {
                    cardComponent.belong = CardLogic.Belong.Player1;
                }
                else if (player == GameManager.Instance.playerComponents[1])
                {
                    cardComponent.belong = CardLogic.Belong.Player2;
                }

                // 启动协程动画，将卡牌从牌堆位置平滑移动到目标位置和旋转
                StartCoroutine(MoveCardToHand(cardObj, player.handPos, targetPos, targetRot, moveDuration));

                // 每发完一张卡牌等待一段时间，再发下一张
                yield return new WaitForSeconds(cardDelay);
            }
        }
        yield return new WaitForSeconds(1f);
        handCardLogic1.Initialize();
        handCardLogic2.Initialize();
    }

    /// <summary>
    /// 将卡牌从当前的位置移动到目标手牌位置和旋转，动画完成后将卡牌设为目标的子物体。
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
        // 确保最终位置和旋转
        card.transform.position = targetPosition;
        card.transform.rotation = targetRotation;
        // 设置卡牌为玩家手牌的子物体
        card.transform.SetParent(targetParent);
    }
}
