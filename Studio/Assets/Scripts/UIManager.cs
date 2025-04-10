using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI player1ScoreText;
    public TextMeshProUGUI player2ScoreText;
    public TextMeshProUGUI gameOverText; // 添加游戏结束文本

    // 新增的调试信息文本，用于显示玩家当回合的选择和加分情况
    public TextMeshProUGUI player1DebugText;
    public TextMeshProUGUI player2DebugText;

    void Start()
    {
        // 初始化时隐藏游戏结束文本
        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // 更新分数显示
        if (GameManager.Instance != null)
        {
            player1ScoreText.text = $"Player 1: {GameManager.Instance.playerComponents[0].point.Value}";
            player2ScoreText.text = $"Player 2: {GameManager.Instance.playerComponents[1].point.Value}";
            
            // 使用网络变量的值来更新调试信息显示
            player1DebugText.text = GameManager.Instance.playerComponents[0].debugInfo.Value.ToString();
            player2DebugText.text = GameManager.Instance.playerComponents[1].debugInfo.Value.ToString();
        }
    }

    // 显示游戏结束
    public void ShowGameOver(string reason = "")
    {
        if (gameOverText != null)
        {
            gameOverText.text = "GAME OVER" + (string.IsNullOrEmpty(reason) ? "" : "\n" + reason);
            gameOverText.gameObject.SetActive(true);
        }
    }

    // 隐藏游戏结束
    public void HideGameOver()
    {
        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(false);
        }
    }

    // 提供更新调试信息的接口
    public void UpdateDebugInfo(string player1Debug, string player2Debug)
    {
        player1DebugText.text = player1Debug;
        player2DebugText.text = player2Debug;
    }

    // 用于清空调试信息
    public void ClearDebugInfo()
    {
        player1DebugText.text = "";
        player2DebugText.text = "";
    }
} 