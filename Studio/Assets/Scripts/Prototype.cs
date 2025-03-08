using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Prototype : MonoBehaviour
{
    [Header("Game Settings")]
    private int totalRounds;
    private int currentRound = 0;
    private int player1Score = 0;
    private int player2Score = 0;
    private bool gameEnded = false;

    [Header("UI Elements")]
    public TextMeshProUGUI roundText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI currentPlayerText;

    private bool? player1Choice = null;
    private bool? player2Choice = null;

    // Start is called before the first frame update
    void Start()
    {
        InitializeGame();
    }

    void InitializeGame()
    {
        totalRounds = Random.Range(4, 8);
        currentRound = 1;
        player1Score = 0;
        player2Score = 0;
        gameEnded = false;
        player1Choice = null;
        player2Choice = null;
        
        UpdateUI();
        StartNewRound();
    }

    void Update()
    {
        if (gameEnded)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                InitializeGame();
            }
            return;
        }

        // Player 1 controls: A (Cooperate) and D (Cheat)
        if (!player1Choice.HasValue)
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                MakeChoice(1, true);
            }
            else if (Input.GetKeyDown(KeyCode.D))
            {
                MakeChoice(1, false);
            }
        }

        // Player 2 controls: LeftArrow (Cooperate) and RightArrow (Cheat)
        if (!player2Choice.HasValue)
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                MakeChoice(2, true);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                MakeChoice(2, false);
            }
        }
    }

    void MakeChoice(int player, bool cooperate)
    {
        if (player == 1)
        {
            player1Choice = cooperate;
        }
        else
        {
            player2Choice = cooperate;
        }

        UpdateWaitingStatus();

        if (player1Choice.HasValue && player2Choice.HasValue)
        {
            StartCoroutine(ShowRoundResult());
        }
    }

    void UpdateWaitingStatus()
    {
        if (!player1Choice.HasValue && !player2Choice.HasValue)
        {
            currentPlayerText.text = "Waiting for both players...";
        }
        else if (!player1Choice.HasValue)
        {
            currentPlayerText.text = "Waiting for Player 1...";
        }
        else if (!player2Choice.HasValue)
        {
            currentPlayerText.text = "Waiting for Player 2...";
        }
        else
        {
            currentPlayerText.text = "Processing results...";
        }
    }

    IEnumerator ShowRoundResult()
    {
        yield return new WaitForSeconds(1f);
        
        // Both cooperate
        if (player1Choice.Value && player2Choice.Value)
        {
            player1Score += 2;
            player2Score += 2;
            resultText.text = $"Round {currentRound} Results:\nBoth Players Cooperated: +2 points each!";
        }
        // Player 1 cooperates, Player 2 cheats
        else if (player1Choice.Value && !player2Choice.Value)
        {
            player1Score -= 1;
            player2Score += 3;
            resultText.text = $"Round {currentRound} Results:\nOne player cooperated, one player cheated\nPlayer 1: -1 point | Player 2: +3 points";
        }
        // Player 1 cheats, Player 2 cooperates
        else if (!player1Choice.Value && player2Choice.Value)
        {
            player1Score += 3;
            player2Score -= 1;
            resultText.text = $"Round {currentRound} Results:\nOne player cooperated, one player cheated\nPlayer 1: +3 points | Player 2: -1 point";
        }
        // Both cheat
        else
        {
            resultText.text = $"Round {currentRound} Results:\nBoth Players Cheated: No points!";
        }

        UpdateUI();
        yield return new WaitForSeconds(3f);

        currentRound++;
        if (currentRound > totalRounds)
        {
            EndGame();
        }
        else
        {
            StartNewRound();
        }
    }

    void StartNewRound()
    {
        player1Choice = null;
        player2Choice = null;
        resultText.text = "";
        UpdateUI();
        UpdateWaitingStatus();
    }

    void EndGame()
    {
        gameEnded = true;
        string winner = player1Score > player2Score ? "Player 1 Wins!" :
                       player2Score > player1Score ? "Player 2 Wins!" :
                       "It's a Tie!";
        resultText.text = $"Game Over! {winner}\nFinal Scores:\nPlayer 1: {player1Score}\nPlayer 2: {player2Score}\n\nPress SPACE to start new game";
        currentPlayerText.text = "";
    }

    void UpdateUI()
    {
        roundText.text = $"Current Round: {currentRound}";
        scoreText.text = $"Score - Player 1: {player1Score} | Player 2: {player2Score}";
    }
}
