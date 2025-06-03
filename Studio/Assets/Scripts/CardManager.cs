using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class CardManager : NetworkBehaviour
{
    [Header("Deal Settings")]
    [Tooltip("默认每位玩家发的卡牌数量，如果不使用 StartDealCards(int) 指定，则使用此值。")]
    public int defaultCardsToDeal = 2;

    [Header("References")]
    public GameObject cardPrefab;
    public Transform deckTrans;
    public float moveDuration = 1f;
    public float cardDelay = 0.2f;

    public HandCardLogic handCardLogic1;
    public HandCardLogic handCardLogic2;

    private Deck deck;
    #region Singleton
    private static CardManager _instance;
    public static CardManager Instance => _instance;
    #endregion

    #region Unity Life‑cycle
    private void Start()
    {
        deck = new Deck(cardPrefab);
        deck.Shuffle();
    }
    #endregion

    #region Public API

    /// <summary>
    /// 服务器端调用：按 <paramref name="cardsPerPlayer"/> 张数给所有玩家发牌。
    /// </summary>
    public void StartDealCards(int cardsPerPlayer)
    {
        if (!IsServer) return; // 只有服务器生成与同步

        StartCoroutine(WaitAndStartDeal(cardsPerPlayer));
    }

    /// <summary>
    /// 服务器端调用：使用 Inspector 中的 <see cref="defaultCardsToDeal"/> 发牌。
    /// </summary>
    public void StartDealCards()
    {
        StartDealCards(defaultCardsToDeal);
    }
    #endregion

    private void Awake()
    {

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    #region Core Logic
    private IEnumerator WaitAndStartDeal(int cardsPerPlayer)
    {
        while (deck == null) yield return null;
        yield return DealCardsCoroutine(cardsPerPlayer);
    }

    private IEnumerator DealCardsCoroutine(int cardsPerPlayer)
    {
        foreach (PlayerLogic player in GameManager.Instance.playerComponents)
        {
            for (int i = 0; i < cardsPerPlayer; i++)
            {
                Vector3 targetPos = player.handPos.position - new Vector3(0, 0.01f * i, 0);
                Quaternion targetRot = player.handPos.rotation;

                // 生成并 Spawn（仅服务器）
                GameObject cardObj = Instantiate(cardPrefab, deckTrans.position, deckTrans.rotation, player.handPos);
                cardObj.GetComponent<NetworkObject>().Spawn(destroyWithScene: true);

                // 复制牌面数据
                CardLogic cardLogic = cardObj.GetComponent<CardLogic>();
                CardLogic template = deck.DealCard();
                if (cardLogic && template) cardLogic.effect = template.effect;

                // 记录归属
                player.AddCard(cardLogic);
                if(player.playerID == 1)
                {
                    cardLogic.SetBelongServerRpc(CardLogic.Belong.Player1);
                }
                else if (player.playerID == 2)
                {
                    cardLogic.SetBelongServerRpc(CardLogic.Belong.Player2);
                }

                // 动画
                StartCoroutine(MoveCardToHand(cardObj, player.handPos, targetPos, targetRot, moveDuration));

                yield return new WaitForSeconds(cardDelay);
            }
        }

        // 发完所有牌后初始化手牌显示
        yield return new WaitForSeconds(1f);
        handCardLogic1.Initialize();
        handCardLogic2.Initialize();
    }

    private IEnumerator MoveCardToHand(GameObject card, Transform targetParent, Vector3 targetPos, Quaternion targetRot, float duration)
    {
        Vector3 startPos = card.transform.position;
        Quaternion startRot = card.transform.rotation;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            card.transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            card.transform.rotation = Quaternion.Slerp(startRot, targetRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        card.transform.position = targetPos;
        card.transform.rotation = targetRot;
        card.transform.SetParent(targetParent);
    }
    #endregion
}
