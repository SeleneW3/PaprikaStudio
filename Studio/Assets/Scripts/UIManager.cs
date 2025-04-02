using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI player1ScoreText;
    public TextMeshProUGUI player2ScoreText;

    void Update()
    {
        // 更新分数显示
        if (GameManager.Instance != null)
        {
            player1ScoreText.text = $"Player 1: {GameManager.Instance.playerComponents[0].point}";
            player2ScoreText.text = $"Player 2: {GameManager.Instance.playerComponents[1].point}";
        }
    }
} 