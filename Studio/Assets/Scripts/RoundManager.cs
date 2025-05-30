using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;

public class RoundManager : NetworkBehaviour
{
    public DeckLogic deckLogic;
    public DialogManager dialogManager;
    public float baseBet = 1f;
    public float betMultiplier = 2f;
    public LevelManager levelManager;  // 添加LevelManager引用

    [Header("Choice Statistics")]
    // 当前回合的选择统计
    public NetworkVariable<int> player1CurrentRoundCoopCount = new NetworkVariable<int>(0);
    public NetworkVariable<int> player1CurrentRoundCheatCount = new NetworkVariable<int>(0);
    public NetworkVariable<int> player2CurrentRoundCoopCount = new NetworkVariable<int>(0);
    public NetworkVariable<int> player2CurrentRoundCheatCount = new NetworkVariable<int>(0);

    // 总的选择统计
    public NetworkVariable<int> player1TotalCoopCount = new NetworkVariable<int>(0);
    public NetworkVariable<int> player1TotalCheatCount = new NetworkVariable<int>(0);
    public NetworkVariable<int> player2TotalCoopCount = new NetworkVariable<int>(0);
    public NetworkVariable<int> player2TotalCheatCount = new NetworkVariable<int>(0);

    [Header("Game Settings")]
    public int totalRounds = 5; // 游戏总回合数

    public PlayerLogic player1;
    public PlayerLogic player2;

    public GameObject Gun1;  // 玩家1的枪对象
    public GameObject Gun2;  // 玩家2的枪对象

    public GameObject ShowCamera1;
    public GameObject ShowCamera2;
    public GameObject Player1Camera;
    public GameObject Player2Camera;

    [Header("Balance Scale")]
    [SerializeField] private BalanceScale balanceScale;  // 天平引用

    [Header("Coin Effects")]
    public GameObject coinPrefab; // 硬币预制体
    public Transform player1ScoreAnchor; // 玩家1分数锚点
    public Transform player2ScoreAnchor; // 玩家2分数锚点
    public Transform player1CoinRespawnPos;
    public Transform player2CoinRespawnPos;

    public NetworkVariable<int> currentRound = new NetworkVariable<int>(0);
    private bool gameEnded = false;

    public int tutorState = 0;
    public int dialogIndex = 0;

    public int breakPoint = 0;

    [Header("Bool")]
    public bool chessIsMoved = false;
    public bool playerGotCard = false;
    public bool showCard = false;

    [Header("Gun Control")]
    public NetworkVariable<bool> player1CanFire = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> player2CanFire = new NetworkVariable<bool>(false);

    void Start()
    {
        // 初始化回合
        currentRound.Value = 1;

        if (dialogManager == null)
        {
            Debug.LogError("Dialog Manager is not assigned in the inspector!");
        }

        // 给UIManager设置枪支引用
        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager == null)
        {
            Debug.LogError("UIManager未找到!");
        }

        if(LevelManager.Instance.currentMode == LevelManager.Mode.Tutor)
        {
            totalRounds = 4;
        }
        else
        {
            totalRounds = 5;
        }
    }

    private void OnEnable()
    {
        GameManager.OnPlayersReady += AssignPlayers;
    }

    private void OnDisable()
    {
        GameManager.OnPlayersReady -= AssignPlayers;
    }

    void Update()
    {
        if(GameManager.Instance.currentGameState == GameManager.GameState.TutorReady)
        {
            if(tutorState == 0)
            {
                DialogManager.Instance.PlayRange(0, 3);
                tutorState++;
            }
            else if(tutorState == 1)
            {
                tutorState++;
            }
            else if(tutorState == 2)
            {
                tutorState++;
                DialogManager.Instance.PlayRange(4, 5);

                CardManager cardManager = FindObjectOfType<CardManager>();
                if (cardManager != null)
                {
                    cardManager.StartDealCards(2);
                    playerGotCard = true;
                }
                else
                {
                    Debug.LogError("CardManager not found!");
                }
                DialogManager.Instance.PlayRange(6, 9);
            }
            else if(tutorState == 3)
            {
                tutorState++;
            }
            else if(tutorState == 4)
            {
                tutorState++;
                DialogManager.Instance.PlayRange(10, 12);

                UIManager uiManager = FindObjectOfType<UIManager>();
                if (uiManager != null)
                {
                    uiManager.RequestRoundTextUpdate(currentRound.Value, totalRounds);
                }
                
            }
            GameManager.Instance.currentGameState = GameManager.GameState.TutorPlayerTurn;
        }
        else if (GameManager.Instance.currentGameState == GameManager.GameState.TutorShowState)
        {
            if(showCard == false)
            {
                showCard = true;
                deckLogic.ShowSentCards();
            }
            else
            {
                GameManager.Instance.currentGameState = GameManager.GameState.TutorPlayerTurn;
            }
        }
        else if(GameManager.Instance.currentGameState == GameManager.GameState.TutorPlayerTurn)
        {
            if (player1.choice != PlayerLogic.playerChoice.None && player2.choice != PlayerLogic.playerChoice.None)
            {
                if (playerGotCard)
                {
                    if (player1.usedCard.Value == true && player2.usedCard.Value == true)
                    {
                        MovePiecesToPositions();
                        GameManager.Instance.currentGameState = GameManager.GameState.TutorShowState;
                    }
                }
                else
                {
                    MovePiecesToPositions();
                    GameManager.Instance.currentGameState = GameManager.GameState.TutorCalculateTurn;
                }
            }

            bool player1Selected = player1.choice != PlayerLogic.playerChoice.None;
            bool player2Selected = player2.choice != PlayerLogic.playerChoice.None;
            
            UIManager uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.UpdateChoiceStatus(player1Selected, player2Selected);
            }
        }
        else if(GameManager.Instance.currentGameState == GameManager.GameState.TutorCalculateTurn)
        {
            CalculatePointWithoutGun();
        }
        else if(GameManager.Instance.currentGameState == GameManager.GameState.Ready)
        {
            CardManager cardManager = FindObjectOfType<CardManager>();
            if(LevelManager.Instance.currentMode == LevelManager.Mode.OnlyCard || 
                              LevelManager.Instance.currentMode == LevelManager.Mode.CardAndGun)
            {
                if (cardManager != null)
                {
                    cardManager.StartDealCards(5);
                    playerGotCard = true;
                }
                else
                {
                    Debug.LogError("CardManager not found!");
                }
            }

            GameManager.Instance.currentGameState = GameManager.GameState.PlayerTurn;
            
        }
        else if(GameManager.Instance.currentGameState == GameManager.GameState.PlayerTurn)
        {
            if(player1.choice != PlayerLogic.playerChoice.None && player2.choice != PlayerLogic.playerChoice.None)
            {
                if (playerGotCard)
                {
                    if (player1.usedCard.Value == true && player2.usedCard.Value == true)
                    {
                        MovePiecesToPositions();
                        GameManager.Instance.currentGameState = GameManager.GameState.PlayerShowState;
                    }
                }
                else
                {
                    MovePiecesToPositions();
                    GameManager.Instance.currentGameState = GameManager.GameState.CalculateTurn;
                }
            }

            bool player1Selected = player1.choice != PlayerLogic.playerChoice.None;
            bool player2Selected = player2.choice != PlayerLogic.playerChoice.None;
            
            UIManager uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.UpdateChoiceStatus(player1Selected, player2Selected);
            }
        }
        else if (GameManager.Instance.currentGameState == GameManager.GameState.PlayerShowState)
        {
            if (showCard == false)
            {
                showCard = true;
                deckLogic.ShowSentCards();
            }
            else
            {
                GameManager.Instance.currentGameState = GameManager.GameState.PlayerTurn;
            }
        }
        else if(GameManager.Instance.currentGameState == GameManager.GameState.CalculateTurn)
        {
            if (levelManager != null && 
                (levelManager.currentMode == LevelManager.Mode.OnlyGun|| 
                levelManager.currentMode == LevelManager.Mode.CardAndGun))
            {
                CalculatePointWithGun();
            }
            else
            {
                CalculatePointWithoutGun();
            }
            
            ChessMoveBack();
            ResetChess();
            ResetPlayersChoice();
            ResetPlayers();
            showCard = false;
            GameManager.Instance.currentGameState = GameManager.GameState.PlayerTurn;
        }
    }

    void MovePiecesToPositions()
    {
        foreach (var chess in GameManager.Instance.chessComponents)
        {
            chess.Move();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdateBalanceScaleServerRpc(float player1Score, float player2Score)
    {
        if (balanceScale != null)
        {
            UpdateBalanceScaleClientRpc(player1Score, player2Score);
        }
    }

    [ClientRpc]
    private void UpdateBalanceScaleClientRpc(float player1Score, float player2Score)
    {
        if (balanceScale != null)
        {
            balanceScale.UpdateScore(player1Score, player2Score);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdateRoundServerRpc()
    {
        if (!gameEnded)
        {
            currentRound.Value++;
            UpdateRoundClientRpc();
        }
    }

    [ClientRpc]
    private void UpdateRoundClientRpc()
    {
        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            uiManager.RequestRoundTextUpdate(currentRound.Value, totalRounds);
        }
    }

    void CalculatePointWithoutGun()
    {
        if (gameEnded)
        {
            return;
        }

        if (NetworkManager.LocalClientId == 0)
        {
            ApplyEffect();
            CalculatePoint(player1.choice, player2.choice);
        }
        ResetPlayersChoice();
        GameManager.Instance.ResetAllBlocksServerRpc();
        GameManager.Instance.chessComponents[0].backToOriginal = true;
        GameManager.Instance.chessComponents[1].backToOriginal = true;
        
        if(showCard == true)
        {
            if(NetworkManager.LocalClientId == 0)
            {
                GameManager.Instance.deck.ResetPlayerCardServerRpc();
            }
            showCard = false;
        }
        
        chessIsMoved = false;
        GameManager.Instance.currentGameState = GameManager.GameState.TutorReady;
    }

    void CalculatePointWithGun()
    {
        if (gameEnded)
        {
            return;
        }
        UIManager uiManager = FindObjectOfType<UIManager>();

        if (NetworkManager.LocalClientId == 0)
        {
            ApplyEffect();
            CalculatePoint(player1.choice, player2.choice);

            if (Gun1.GetComponent<GunController>().gameEnded.Value || 
                Gun2.GetComponent<GunController>().gameEnded.Value)
            {
                Debug.Log("Game ended due to gunshot!");
                gameEnded = true;
                if (uiManager != null)
                {
                    uiManager.ShowSettlementPanel();
                }
            }
        }

        ResetPlayersChoice();
        GameManager.Instance.ResetAllBlocksServerRpc();
        GameManager.Instance.chessComponents[0].backToOriginal = true;
        GameManager.Instance.chessComponents[1].backToOriginal = true;
        if (showCard == true)
        {
            if (NetworkManager.LocalClientId == 0)
            {
                GameManager.Instance.deck.ResetPlayerCardServerRpc();
            }
            showCard = false;
        }

        chessIsMoved = false;
        GameManager.Instance.currentGameState = GameManager.GameState.Ready;
    }

    void CalculatePoint(PlayerLogic.playerChoice player1Choice, PlayerLogic.playerChoice player2Choice)
    {
        UIManager uiManager = FindObjectOfType<UIManager>();

        float player1CurrentRoundPoint;
        float player2CurrentRoundPoint;

        // 存储开枪判断结果而不是直接开枪
        if (player2Choice == PlayerLogic.playerChoice.Cheat && Gun1 != null)
        {
            // 玩家1被欺骗，标记玩家1可以开枪
            player1CanFire.Value = true;
        }
        if (player1Choice == PlayerLogic.playerChoice.Cheat && Gun2 != null)
        {
            // 玩家2被欺骗，标记玩家2可以开枪
            player2CanFire.Value = true;
        }

        // 计算得分
        if(player1Choice == PlayerLogic.playerChoice.Cooperate && player2Choice == PlayerLogic.playerChoice.Cooperate)
        {
            player1CurrentRoundPoint = player1.coopPoint.Value;
            player2CurrentRoundPoint = player2.coopPoint.Value;
        }
        else if (player1Choice == PlayerLogic.playerChoice.Cooperate && player2Choice == PlayerLogic.playerChoice.Cheat)
        {
            player1CurrentRoundPoint = player1.coopPoint.Value;
            player2CurrentRoundPoint = player2.cheatPoint.Value;
        }
        else if (player1Choice == PlayerLogic.playerChoice.Cheat && player2Choice == PlayerLogic.playerChoice.Cooperate)
        {
            player1CurrentRoundPoint = player1.cheatPoint.Value;
            player2CurrentRoundPoint = player2.coopPoint.Value;
        }
        else
        {
            player1CurrentRoundPoint = 0f;
            player2CurrentRoundPoint = 0f;
        }

        player1.point.Value += player1CurrentRoundPoint;
        player2.point.Value += player2CurrentRoundPoint;

        string player1Debug = "+" + player1CurrentRoundPoint.ToString();
        string player2Debug = "+" + player2CurrentRoundPoint.ToString();

        if (uiManager != null)
        {
            // 更新debug信息并启动UI状态机
            uiManager.UpdateDebugInfo(player1Debug, player2Debug);
            
            // 如果状态机没有在运行，启动它
            if (!uiManager.IsStateMachineRunning())
            {
                uiManager.StartRoundSettlementUI();
            }
        }

        if (NetworkManager.Singleton.IsServer)
        {
            GameManager.Instance.playerComponents[0].debugInfo.Value = player1Debug;
            GameManager.Instance.playerComponents[1].debugInfo.Value = player2Debug;
        }

        if (!gameEnded && currentRound.Value >= totalRounds)
        {
            EndGame(uiManager);
        }
        else if (!gameEnded && NetworkManager.Singleton.IsServer)
        {
            UpdateRoundServerRpc();
        }
    }

    private void ResetCurrentRoundStatistics()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        
        player1CurrentRoundCoopCount.Value = 0;
        player1CurrentRoundCheatCount.Value = 0;
        player2CurrentRoundCoopCount.Value = 0;
        player2CurrentRoundCheatCount.Value = 0;
    }

    public void ResetAllStatistics()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        ResetCurrentRoundStatistics();
        player1TotalCoopCount.Value = 0;
        player1TotalCheatCount.Value = 0;
        player2TotalCoopCount.Value = 0;
        player2TotalCheatCount.Value = 0;
    }

    private void EndGame(UIManager uiManager)
    {
        gameEnded = true;
        uiManager.ShowSettlementPanel();
    }

    [ServerRpc(RequireOwnership = false)]
    public void FireGunServerRpc(ulong clientId)
    {
        if (clientId == 0 && player1CanFire.Value)
        {
            Gun1.GetComponent<GunController>().FireGun();
            player1CanFire.Value = false;
        }
        else if (clientId == 1 && player2CanFire.Value)
        {
            Gun2.GetComponent<GunController>().FireGun();
            player2CanFire.Value = false;
        }
    }

    void AssignPlayers()
    {
        foreach (var player in GameManager.Instance.playerComponents)
        {
            if (player.playerID == 1)
            {
                player1 = player;
            }
            else if (player.playerID == 2)
            {
                player2 = player;
            }
        }
    }

    void ResetPlayers()
    {
        player1.ResetToInitial();
        player2.ResetToInitial();
    }

    void ResetPlayersChoice()
    {
        player1.choice = PlayerLogic.playerChoice.None;
        player2.choice = PlayerLogic.playerChoice.None;
        player1.SetUsedCardServerRpc(false);
        player2.SetUsedCardServerRpc(false);
    }

    void ChessMoveBack()
    {
        foreach (var chess in GameManager.Instance.chessComponents)
        {

             chess.backToOriginal = true;

        }
    }

    void ResetChess()
    {
        foreach (var chess in GameManager.Instance.chessComponents)
        {
            chess.isOnGround = false;
        }
    }

    void ApplyEffect()
    {
        CardLogic p1Card = null;
        CardLogic p2Card = null;

        foreach (CardLogic cardLogic in deckLogic.cardLogics)
        {
            if (cardLogic.isOut)
            {
                if (cardLogic.belong.Value == CardLogic.Belong.Player1)
                {
                    p1Card = cardLogic;
                }
                else if (cardLogic.belong.Value == CardLogic.Belong.Player2)
                {
                    p2Card = cardLogic;
                }
            }
        }

        if (p1Card != null && p2Card != null)
        {
            ApplyCardEffects(p1Card, p2Card);

            p1Card.isOut = false;
            p2Card.isOut = false;
        }
    }

    private void ApplyCardEffects(CardLogic player1Card, CardLogic player2Card)
    {
        List<CardLogic> cards = new List<CardLogic>() { player1Card, player2Card };

        cards.Sort((cardA, cardB) =>
            CardLogic.GetEffectPriority(cardA.effect).CompareTo(
            CardLogic.GetEffectPriority(cardB.effect)));

        foreach (var card in cards)
        {
            card.OnEffect();
        }
    }

    public void ResetRound()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        gameEnded = false;
        currentRound.Value = 1;
        
        ResetAllStatistics();
        
        Gun1.GetComponent<GunController>().ResetGun();
        Gun2.GetComponent<GunController>().ResetGun();

        player1.point.Value = 0;
        player2.point.Value = 0;

        foreach (Transform anchor in new Transform[] { player1ScoreAnchor, player2ScoreAnchor })
        {
            if (anchor != null)
            {
                for (int i = anchor.childCount - 1; i >= 0; i--)
                {
                    Transform container = anchor.GetChild(i);
                    NetworkObject netObj = container.GetComponent<NetworkObject>();
                    if (netObj != null && netObj.IsSpawned)
                    {
                        netObj.Despawn();
                    }
                }
            }
        }

        if (balanceScale != null)
        {
            UpdateBalanceScaleServerRpc(0, 0);
        }
    }
}
