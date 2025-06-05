using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DeckLogic : NetworkBehaviour
{
    public List<CardLogic> cardLogics = new List<CardLogic>();

    [Header("���߼�")]
    public CardLogic player1SendCard;
    public CardLogic player2SendCard;

    [Header("չʾλ��")]
    public Transform player1CardShowPos;
    public Transform player2CardShowPos;

    [Header("�Զ���������")]
    [Tooltip("չʾ�󣬶೤ʱ�䣨�룩�Զ��ջ��ƣ�<=0 ���Զ�����")]
    public float autoCollectDelay = 5f;

    // ���ڼ�¼ԭʼλ�ú���ת
    private Vector3 p1OriginalPos;
    private Quaternion p1OriginalRot;
    private Vector3 p2OriginalPos;
    private Quaternion p2OriginalRot;
    public bool hasShown = false;

    public NetworkVariable<bool> player1SendCardBool = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> player2SendCardBool = new NetworkVariable<bool>(false);

    // ����������Э�̣��Ա�ȡ��
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
    /// ���������ƶ������Ե�չʾλ�ò����棬ֻ��¼һ��ԭʼ transform��
    /// ���� autoCollectDelay ����Զ����� CollectSentCards��
    /// </summary>
    public void ShowSentCards()
    {
        if (!hasShown)
        {
            hasShown = true;
            // ��¼ԭʼ״̬
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
                Debug.Log("Switching to Player 2 Show Camera");
                cameraLogic.SwitchToPlayer2ShowCamera();
            }
            
        }

        // �ƶ�������
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

        // ����������Զ����ƣ�����Э��
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
    /// �Զ��ӳٺ��������
    /// </summary>
    private IEnumerator AutoCollectAfterDelay()
    {
        yield return new WaitForSeconds(autoCollectDelay);
        Debug.Log("自动收牌触发");
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
    /// ���������ջص����������λ�ú���ת����ֹͣ�Զ�����Э��
    /// </summary>
    public void CollectSentCards()
    {

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
            Debug.Log("Switching to Player 2 Camera");
            cameraLogic.SwitchToPlayer2Camera();
        }

        hasShown = false;

        // ֹͣ�Զ�����Э�̣����������У�
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
