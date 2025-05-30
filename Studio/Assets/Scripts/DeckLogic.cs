using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DeckLogic : NetworkBehaviour
{
    public List<CardLogic> cardLogics = new List<CardLogic>();

    [Header("牌逻辑")]
    public CardLogic player1SendCard;
    public CardLogic player2SendCard;

    [Header("展示位置")]
    public Transform player1CardShowPos;
    public Transform player2CardShowPos;

    [Header("自动收牌设置")]
    [Tooltip("展示后，多长时间（秒）自动收回牌；<=0 则不自动收牌")]
    public float autoCollectDelay = 5f;

    // 用于记录原始位置和旋转
    private Vector3 p1OriginalPos;
    private Quaternion p1OriginalRot;
    private Vector3 p2OriginalPos;
    private Quaternion p2OriginalRot;
    private bool hasShown = false;

    public NetworkVariable<bool> player1SendCardBool = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> player2SendCardBool = new NetworkVariable<bool>(false);

    // 跟踪启动的协程，以便取消
    private Coroutine autoCollectCoroutine;

    public CameraLogic cameraLogic;

    private void Start()
    {
        GameManager.Instance.deck = this;
    }

    private void Update()
    {
        foreach (CardLogic card in cardLogics)
        {
            if (card.belong.Value == CardLogic.Belong.Player1)
            {
                player1SendCard = card;
            }
            else if (card.belong.Value == CardLogic.Belong.Player2)
            {
                player2SendCard = card;
            }
        }
    }

    /// <summary>
    /// 将两张牌移动到各自的展示位置并翻面，只记录一次原始 transform，
    /// 并在 autoCollectDelay 秒后自动调用 CollectSentCards。
    /// </summary>
    public void ShowSentCards()
    {
        if (!hasShown)
        {
            // 记录原始状态
            if (player1SendCard != null)
            {
                p1OriginalPos = player1SendCard.transform.position;
                p1OriginalRot = player1SendCard.transform.rotation;
            }
            if (player2SendCard != null)
            {
                p2OriginalPos = player2SendCard.transform.position;
                p2OriginalRot = player2SendCard.transform.rotation;
            }
            if(NetworkManager.LocalClientId == 0)
            {
                cameraLogic.SwitchToPlayer1ShowCamera();
            }
            else if(NetworkManager.LocalClientId == 1)
            {
                cameraLogic.SwitchToPlayer2ShowCamera();
            }
            hasShown = true;
        }

        // 移动并翻面
        if (player1SendCard != null && player1CardShowPos != null)
        {
            var t = player1SendCard.transform;
            t.position = player1CardShowPos.position;
            t.rotation = player1CardShowPos.rotation;
            t.Rotate(0f, 180f, 0f, Space.Self);
        }
        if (player2SendCard != null && player2CardShowPos != null)
        {
            var t = player2SendCard.transform;
            t.position = player2CardShowPos.position;
            t.rotation = player2CardShowPos.rotation;
            t.Rotate(0f, 180f, 0f, Space.Self);
        }

        // 如果开启了自动收牌，重启协程
        if (autoCollectDelay > 0f)
        {
            if (autoCollectCoroutine != null)
            {
                StopCoroutine(autoCollectCoroutine);
            }
            autoCollectCoroutine = StartCoroutine(AutoCollectAfterDelay());
        }
    }

    /// <summary>
    /// 自动延迟后调用收牌
    /// </summary>
    private IEnumerator AutoCollectAfterDelay()
    {
        yield return new WaitForSeconds(autoCollectDelay);
        CollectSentCards();
        if(GameManager.Instance.currentGameState == GameManager.GameState.TutorShowState)
        {
            GameManager.Instance.currentGameState = GameManager.GameState.TutorCalculateTurn;
        }
        else
        {
            GameManager.Instance.currentGameState = GameManager.GameState.CalculateTurn;
        }
    }

    /// <summary>
    /// 将两张牌收回到它们最初的位置和旋转，并停止自动收牌协程
    /// </summary>
    public void CollectSentCards()
    {
        if (!hasShown) return;

        if (player1SendCard != null)
        {
            var t = player1SendCard.transform;
            t.position = p1OriginalPos;
            t.rotation = p1OriginalRot;
        }
        if (player2SendCard != null)
        {
            var t = player2SendCard.transform;
            t.position = p2OriginalPos;
            t.rotation = p2OriginalRot;
        }

        if(NetworkManager.LocalClientId == 0)
        {
            cameraLogic.SwitchToPlayer1Camera();
        }
        else if(NetworkManager.LocalClientId == 1)
        {
            cameraLogic.SwitchToPlayer2Camera();
        }

        hasShown = false;

        // 停止自动收牌协程（如仍在运行）
        if (autoCollectCoroutine != null)
        {
            StopCoroutine(autoCollectCoroutine);
            autoCollectCoroutine = null;
        }
    }

    [ServerRpc (RequireOwnership = false)]
    public void SetPlayerCardBoolServerRpc(int index, bool Bool)
    {
        if (index == 1)
        {
            player1SendCardBool.Value = Bool;
        }
        else if (index == 2)
        {
            player2SendCardBool.Value = Bool;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ResetPlayerCardServerRpc()
    {
        player1SendCard.belong.Value = CardLogic.Belong.Deck;
        player2SendCard.belong.Value = CardLogic.Belong.Deck;
        player1SendCardBool.Value = false;
        player2SendCardBool.Value = false;
        player1SendCard = null;
        player2SendCard = null;
    }
}
