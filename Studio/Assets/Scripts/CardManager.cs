using UnityEngine;
using System.Collections;

public class CardGameManager : MonoBehaviour
{
    // 基础属性设置
    public int cardsToDeal = 2;         // 每个玩家要发的牌数
    public GameObject cardPrefab;        // 卡牌预制体
    public Transform deckTrans;          // 牌堆的位置
    public float moveDuration = 1f;      // 卡牌移动的持续时间
    public float cardDelay = 0.2f;       // 每张牌发牌之间的延迟

    private Deck deck;                   // 牌堆对象

    void Start()
    {
        // 初始化牌堆并洗牌
        deck = new Deck(cardPrefab);
        deck.Shuffle();

        // 开始发牌协程
        StartCoroutine(DealCards());
    }

    // 发牌协程
    IEnumerator DealCards()
    {
        // 遍历每个玩家
        foreach (PlayerLogic player in GameManager.Instance.playerComponents)
        {
            // 为每个玩家发 cardsToDeal 张牌
            for (int i = 0; i < cardsToDeal; i++)
            {
                // 设置目标位置（每张牌有微小的垂直偏移）
                Vector3 targetPos = player.handPos.position - new Vector3(0, 0.01f * i, 0);
                Quaternion targetRot = player.handPos.rotation;

                // 在牌堆位置实例化卡牌
                GameObject cardObj = Instantiate(cardPrefab, deckTrans.position, deckTrans.rotation);

                // 获取卡牌的 CardLogic 组件
                CardLogic cardComponent = cardObj.GetComponent<CardLogic>();

                // 从牌堆中获取一张牌的数据
                CardLogic cardData = deck.DealCard();

                // 将牌堆中的数据赋值给实例化的卡牌对象
                if (cardComponent != null && cardData != null)
                {
                    cardComponent.effect = cardData.effect;
                }
                else
                {
                    Debug.LogError("卡牌数据或组件获取失败！");
                }

                // 将卡牌添加到玩家手牌中
                player.AddCard(cardComponent);

                // 启动协程将卡牌从牌堆平滑移动到目标位置和旋转
                StartCoroutine(MoveCardToHand(cardObj, player.handPos, targetPos, targetRot, moveDuration));

                // 每发一张牌等待一段时间，再发下一张
                yield return new WaitForSeconds(cardDelay);
            }
        }
    }

    /// <summary>
    /// 将卡牌从当前的位置移动到目标玩家位置和旋转，完成后将卡牌设为目标玩家的子物体
    /// </summary>
    IEnumerator MoveCardToHand(GameObject card, Transform targetParent, Vector3 targetPosition, Quaternion targetRotation, float duration)
    {
        Vector3 startPos = card.transform.position;
        Quaternion startRot = card.transform.rotation;
        float elapsed = 0f;

        // 平滑移动动画
        while (elapsed < duration)
        {
            card.transform.position = Vector3.Lerp(startPos, targetPosition, elapsed / duration);
            card.transform.rotation = Quaternion.Slerp(startRot, targetRotation, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // 确保最终位置和旋转精确
        card.transform.position = targetPosition;
        card.transform.rotation = targetRotation;
        
        // 设置卡牌为目标玩家的子物体
        card.transform.SetParent(targetParent);
    }
}
