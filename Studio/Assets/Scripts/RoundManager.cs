using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;

public class RoundManager : NetworkBehaviour
{
    public DeckLogic deckLogic;
    public DialogManager dialogManager;
    public TMP_Text roundText;
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
        Debug.Log("RoundManagerStart");

        if (dialogManager != null)
        {
            Debug.Log("Dialog Manager found, but not starting dialog automatically");
            // 移除自动调用StartDialog的代码
            // dialogManager.StartDialog();
        }
        else
        {
            Debug.LogError("Dialog Manager is not assigned in the inspector!");
        }

        // 给UIManager设置枪支引用
        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null && Gun1 != null && Gun2 != null)
        {
            Debug.Log("正在设置UIManager的枪支引用");
            
            // 初始化回合显示，但默认隐藏
            /*if (uiManager.roundText != null)
            {
                uiManager.roundText.gameObject.SetActive(false);
            }*/
        }
        else if (uiManager == null)
        {
            Debug.LogError("UIManager未找到!");
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
                    /*if (uiManager.roundText != null)
                    {
                        uiManager.roundText.gameObject.SetActive(true);
                    }*/
                    uiManager.UpdateRoundText(currentRound.Value, totalRounds);
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
            if (tutorState == 5)
            {
                tutorState++;
                GameManager.Instance.currentGameState = GameManager.GameState.Ready;
                return;
            }

            if (player1.choice != PlayerLogic.playerChoice.None && player2.choice != PlayerLogic.playerChoice.None
                )
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
            if (cardManager != null)
            {
                cardManager.StartDealCards(5);
                GameManager.Instance.currentGameState = GameManager.GameState.PlayerTurn;
            }
            else
            {
                Debug.LogError("CardManager not found!");
            }
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

                        GameManager.Instance.currentGameState = GameManager.GameState.CalculateTurn;
                    }
                    Debug.Log(tutorState);
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

            UIManager uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.UpdateRoundText(currentRound.Value, totalRounds);
            }
            else if (roundText != null)
            {
                roundText.text = $"ROUND {currentRound.Value}/{totalRounds}";
            }
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
        Debug.LogWarning("Into Calculate");
        if(showCard == true)
        {
            Debug.LogWarning("Resetting player cards");
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
            


            if (Gun1.GetComponent<GunController>().gameEnded.Value)
            {
                Debug.Log("Player 2 is dead! Game over.");
                gameEnded = true;
                if (uiManager != null)
                {
                    uiManager.NetworkShowGameOver("Player 2 is dead!");
                }
            }
            else if (Gun2.GetComponent<GunController>().gameEnded.Value)
            {
                Debug.Log("Player 1 is dead! Game over.");
                gameEnded = true;
                if (uiManager != null)
                {
                    uiManager.NetworkShowGameOver("Player 1 is dead!");
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
        player1CanFire.Value = false;
        player2CanFire.Value = false;
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

    void CalculatePoint(PlayerLogic.playerChoice player1Choice, PlayerLogic.playerChoice player2Choice)
    {
        UIManager uiManager = FindObjectOfType<UIManager>();

        float player1CurrentRoundPoint;
        float player2CurrentRoundPoint;

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

        float p1PointsBefore = player1.point.Value;
        float p2PointsBefore = player2.point.Value;

        player1.point.Value += player1CurrentRoundPoint;
        player2.point.Value += player2CurrentRoundPoint;

        string player1Debug = "+" + player1CurrentRoundPoint.ToString();
        string player2Debug = "+" + player2CurrentRoundPoint.ToString();

        int p1PointsAdded = Mathf.FloorToInt(player1.point.Value - p1PointsBefore);
        int p2PointsAdded = Mathf.FloorToInt(player2.point.Value - p2PointsBefore);

        Coin coin = FindObjectOfType<Coin>();

        coin.RequestSpawnCoins(player1ScoreAnchor.position, p1PointsAdded);
        coin.RequestSpawnCoins(player2ScoreAnchor.position, p2PointsAdded);

        UpdateBalanceScaleServerRpc(player1.point.Value, player2.point.Value);

        if (uiManager != null)
        {
            uiManager.UpdateDebugInfo(player1Debug, player2Debug);
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
        else if (!gameEnded)
        {
            currentRound.Value++;
            if (uiManager != null)
            {
                uiManager.UpdateRoundText(currentRound.Value, totalRounds);
            }
            Debug.Log($"Round {currentRound.Value}");
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
        Debug.Log($"{totalRounds} rounds completed.");
        gameEnded = true;

        if (uiManager != null)
        {
            string winner = "";
            if (player1.point.Value > player2.point.Value)
            {
                winner = "Player 1 wins!";
            }
            else if (player2.point.Value > player1.point.Value)
            {
                winner = "Player 2 wins!";
            }
            else
            {
                winner = "It's a tie!";
            }
            uiManager.ShowGameOver($"{totalRounds} rounds completed\n{winner}");
        }
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
}
